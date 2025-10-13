using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ScottPlot.Avalonia;
using MentalHealthBar.Desktop.Services;

namespace MentalHealthBar.Desktop.ViewModels;

public class DataVisualizationViewModel : ViewModelBase
{
    private readonly IApiClient _apiClient;
    private readonly IChartingService _chartingService;

    private AvaPlot? _moodChart;
    private AvaPlot? _assessmentChart;
    private AvaPlot? _healthChart;
    private DateRangeOption _selectedDateRange = DateRangeOption.Last30Days;
    private string _selectedAssessmentType = "PHQ9";
    private bool _showDailyAverage = false;
    private bool _isLoading;
    private ObservableCollection<DateRangeOption> _dateRangeOptions;
    private ObservableCollection<string> _assessmentTypes;

    public DataVisualizationViewModel(IApiClient apiClient, IChartingService chartingService)
    {
        _apiClient = apiClient;
        _chartingService = chartingService;

        _dateRangeOptions = new ObservableCollection<DateRangeOption>
        {
            DateRangeOption.Last7Days,
            DateRangeOption.Last30Days,
            DateRangeOption.Last90Days,
            DateRangeOption.AllTime
        };

        _assessmentTypes = new ObservableCollection<string>
        {
            "PHQ9",
            "BDI",
            "GAD7",
            "BAI"
        };

        // Commands
        RefreshChartsCommand = ReactiveCommand.CreateFromTask(RefreshAllCharts);
        ExportChartCommand = ReactiveCommand.Create<string>(ExportChart);

        // React to property changes
        this.WhenAnyValue(x => x.SelectedDateRange)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(async _ => await RefreshAllCharts());

        this.WhenAnyValue(x => x.SelectedAssessmentType)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(async _ => await RefreshAssessmentChart());

        this.WhenAnyValue(x => x.ShowDailyAverage)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(async _ => await RefreshMoodChart());

        // Load initial data
        _ = RefreshAllCharts();
    }

    public AvaPlot? MoodChart
    {
        get => _moodChart;
        set => this.RaiseAndSetIfChanged(ref _moodChart, value);
    }

    public AvaPlot? AssessmentChart
    {
        get => _assessmentChart;
        set => this.RaiseAndSetIfChanged(ref _assessmentChart, value);
    }

    public AvaPlot? HealthChart
    {
        get => _healthChart;
        set => this.RaiseAndSetIfChanged(ref _healthChart, value);
    }

    public DateRangeOption SelectedDateRange
    {
        get => _selectedDateRange;
        set => this.RaiseAndSetIfChanged(ref _selectedDateRange, value);
    }

    public string SelectedAssessmentType
    {
        get => _selectedAssessmentType;
        set => this.RaiseAndSetIfChanged(ref _selectedAssessmentType, value);
    }

    public bool ShowDailyAverage
    {
        get => _showDailyAverage;
        set => this.RaiseAndSetIfChanged(ref _showDailyAverage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ObservableCollection<DateRangeOption> DateRangeOptions
    {
        get => _dateRangeOptions;
        set => this.RaiseAndSetIfChanged(ref _dateRangeOptions, value);
    }

    public ObservableCollection<string> AssessmentTypes
    {
        get => _assessmentTypes;
        set => this.RaiseAndSetIfChanged(ref _assessmentTypes, value);
    }

    public ReactiveCommand<Unit, Unit> RefreshChartsCommand { get; }
    public ReactiveCommand<string, Unit> ExportChartCommand { get; }

    private async Task RefreshAllCharts()
    {
        await Task.WhenAll(
            RefreshMoodChart(),
            RefreshAssessmentChart(),
            RefreshHealthChart());
    }

    private async Task RefreshMoodChart()
    {
        try
        {
            IsLoading = true;
            var (startDate, endDate) = GetDateRange();

            var moodEntries = await _apiClient.GetMoodHistoryAsync(
                startDate: startDate,
                endDate: endDate,
                pageSize: 500);

            MoodChart = _chartingService.CreateMoodChart(moodEntries.Items, ShowDailyAverage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing mood chart: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAssessmentChart()
    {
        try
        {
            IsLoading = true;
            var (startDate, endDate) = GetDateRange();

            var assessments = await _apiClient.GetAssessmentHistoryAsync(
                type: SelectedAssessmentType,
                startDate: startDate,
                endDate: endDate);

            AssessmentChart = _chartingService.CreateAssessmentChart(assessments.Items, SelectedAssessmentType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing assessment chart: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshHealthChart()
    {
        try
        {
            IsLoading = true;
            var (startDate, endDate) = GetDateRange();

            var healthMetrics = await _apiClient.GetHealthMetricsHistoryAsync(
                startDate: startDate,
                endDate: endDate,
                pageSize: 365);

            var sleepMetrics = healthMetrics.Items.Where(m => m.Type == "SleepHours").ToList();
            var waterMetrics = healthMetrics.Items.Where(m => m.Type == "WaterIntakeOz").ToList();

            HealthChart = _chartingService.CreateCombinedHealthChart(sleepMetrics, waterMetrics);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing health chart: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private (DateTime startDate, DateTime endDate) GetDateRange()
    {
        var endDate = DateTime.Now;
        var startDate = SelectedDateRange switch
        {
            DateRangeOption.Last7Days => endDate.AddDays(-7),
            DateRangeOption.Last30Days => endDate.AddDays(-30),
            DateRangeOption.Last90Days => endDate.AddDays(-90),
            DateRangeOption.AllTime => endDate.AddYears(-10),
            _ => endDate.AddDays(-30)
        };

        return (startDate, endDate);
    }

    private void ExportChart(string chartType)
    {
        // TODO: Implement chart export to PNG
        Console.WriteLine($"Export {chartType} chart requested");
    }
}

public enum DateRangeOption
{
    Last7Days,
    Last30Days,
    Last90Days,
    AllTime
}