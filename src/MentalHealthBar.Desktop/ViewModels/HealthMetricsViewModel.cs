using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using MentalHealthBar.Desktop.Services;

namespace MentalHealthBar.Desktop.ViewModels;

public class HealthMetricsViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private decimal _sleepHours = 8.0m;
    private decimal _waterIntakeOz = 64.0m;
    private DateOnly _recordedDate = DateOnly.FromDateTime(DateTime.Today);
    private ObservableCollection<HealthMetricSummaryResponse> _recentMetrics;
    private bool _isLoading;
    private string? _statusMessage;

    public HealthMetricsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _recentMetrics = new ObservableCollection<HealthMetricSummaryResponse>();

        // Commands with validation
        var canSave = this.WhenAnyValue(
            x => x.SleepHours,
            x => x.WaterIntakeOz,
            x => x.IsLoading,
            (sleep, water, loading) =>
                sleep >= 0 && sleep <= 24 &&
                water >= 0 && water <= 200 &&
                !loading);

        SaveSleepCommand = ReactiveCommand.CreateFromTask(SaveSleepMetric, canSave);
        SaveWaterCommand = ReactiveCommand.CreateFromTask(SaveWaterMetric, canSave);
        SaveBothCommand = ReactiveCommand.CreateFromTask(SaveBothMetrics, canSave);
        LoadRecentMetricsCommand = ReactiveCommand.CreateFromTask(LoadRecentMetrics);
        DeleteMetricCommand = ReactiveCommand.CreateFromTask<Guid>(DeleteMetric);

        // Load recent metrics on creation
        _ = LoadRecentMetrics();
    }

    public decimal SleepHours
    {
        get => _sleepHours;
        set => this.RaiseAndSetIfChanged(ref _sleepHours, Math.Round(value, 1));
    }

    public decimal WaterIntakeOz
    {
        get => _waterIntakeOz;
        set => this.RaiseAndSetIfChanged(ref _waterIntakeOz, Math.Round(value, 1));
    }

    public DateOnly RecordedDate
    {
        get => _recordedDate;
        set
        {
            this.RaiseAndSetIfChanged(ref _recordedDate, value);
            _ = LoadMetricsForDate(value);
        }
    }

    public ObservableCollection<HealthMetricSummaryResponse> RecentMetrics
    {
        get => _recentMetrics;
        set => this.RaiseAndSetIfChanged(ref _recentMetrics, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public ReactiveCommand<Unit, Unit> SaveSleepCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveWaterCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveBothCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadRecentMetricsCommand { get; }
    public ReactiveCommand<Guid, Unit> DeleteMetricCommand { get; }

    private async Task SaveSleepMetric()
    {
        await SaveMetric("SleepHours", SleepHours);
    }

    private async Task SaveWaterMetric()
    {
        await SaveMetric("WaterIntakeOz", WaterIntakeOz);
    }

    private async Task SaveBothMetrics()
    {
        await SaveMetric("SleepHours", SleepHours);
        await SaveMetric("WaterIntakeOz", WaterIntakeOz);
    }

    private async Task SaveMetric(string type, decimal value)
    {
        try
        {
            IsLoading = true;
            StatusMessage = $"Saving {type}...";

            // Check if metric exists for this date
            var existing = RecentMetrics.FirstOrDefault(m =>
                m.Type == type &&
                m.RecordedDate == RecordedDate);

            if (existing != null)
            {
                // Update existing metric
                var updateRequest = new UpdateHealthMetricRequest(Value: value);
                var updated = await _apiClient.UpdateHealthMetricAsync(existing.Id, updateRequest);

                // Update in collection (convert to summary)
                var index = RecentMetrics.IndexOf(existing);
                RecentMetrics[index] = new HealthMetricSummaryResponse(
                    updated.Id,
                    updated.Type,
                    updated.Value,
                    updated.RecordedDate,
                    updated.CreatedAt
                );

                StatusMessage = $"{type} updated successfully";
            }
            else
            {
                // Create new metric
                var request = new RecordHealthMetricRequest(
                    Type: type,
                    Value: value,
                    RecordedDate: RecordedDate
                );

                var result = await _apiClient.RecordHealthMetricAsync(request);

                // Add to collection (convert to summary)
                RecentMetrics.Insert(0, new HealthMetricSummaryResponse(
                    result.Id,
                    result.Type,
                    result.Value,
                    result.RecordedDate,
                    result.CreatedAt
                ));

                StatusMessage = $"{type} recorded successfully";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving {type}: {ex.Message}";
            Console.WriteLine($"Error saving metric: {ex.Message}");
        }
        finally
        {
            IsLoading = false;

            // Clear status message after 3 seconds
            await Task.Delay(3000);
            StatusMessage = null;
        }
    }

    private async Task LoadRecentMetrics()
    {
        try
        {
            IsLoading = true;

            var metrics = await _apiClient.GetHealthMetricsHistoryAsync(
                startDate: DateTime.Now.AddDays(-30),
                endDate: DateTime.Now);

            RecentMetrics.Clear();
            foreach (var metric in metrics.Items.OrderByDescending(m => m.RecordedDate).ThenBy(m => m.Type))
            {
                RecentMetrics.Add(metric);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading metrics: {ex.Message}";
            Console.WriteLine($"Error loading metrics: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadMetricsForDate(DateOnly date)
    {
        try
        {
            var dateTime = date.ToDateTime(TimeOnly.MinValue);
            var metrics = await _apiClient.GetHealthMetricsHistoryAsync(
                startDate: dateTime,
                endDate: dateTime);

            // Update values if metrics exist for this date
            var sleepMetric = metrics.Items.FirstOrDefault(m => m.Type == "SleepHours");
            if (sleepMetric != null)
            {
                SleepHours = sleepMetric.Value;
            }

            var waterMetric = metrics.Items.FirstOrDefault(m => m.Type == "WaterIntakeOz");
            if (waterMetric != null)
            {
                WaterIntakeOz = waterMetric.Value;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading metrics for date: {ex.Message}");
        }
    }

    private async Task DeleteMetric(Guid id)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Deleting metric...";

            await _apiClient.DeleteHealthMetricAsync(id);

            // Remove from collection
            var toRemove = RecentMetrics.FirstOrDefault(m => m.Id == id);
            if (toRemove != null)
            {
                RecentMetrics.Remove(toRemove);
            }

            StatusMessage = "Metric deleted successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting metric: {ex.Message}";
            Console.WriteLine($"Error deleting metric: {ex.Message}");
        }
        finally
        {
            IsLoading = false;

            // Clear status message after 3 seconds
            await Task.Delay(3000);
            StatusMessage = null;
        }
    }
}