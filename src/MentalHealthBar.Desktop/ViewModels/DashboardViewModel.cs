using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using MentalHealthBar.Desktop.Services;

namespace MentalHealthBar.Desktop.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private const int RecentMoodLookbackDays = 7;
    private const int RecentAssessmentLookbackDays = 30;

    private readonly IApiClient _apiClient;
    private MoodEntrySummaryResponse? _recentMood;
    private AssessmentSummaryResponse? _lastAssessment;
    private ObservableCollection<string> _quickStats;
    private bool _isLoading;

    public DashboardViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
        _quickStats = new ObservableCollection<string>();

        LogMoodCommand = ReactiveCommand.CreateFromTask(NavigateToMoodEntry);
        TakeAssessmentCommand = ReactiveCommand.CreateFromTask(NavigateToAssessments);
        ViewTrendsCommand = ReactiveCommand.CreateFromTask(NavigateToTrends);
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadDashboardData);

        // Initialize data - Views should call InitializeAsync() or subscribe to RefreshCommand on load
        // Removed fire-and-forget initialization to prevent unhandled exceptions
    }

    /// <summary>
    /// Initializes the dashboard by loading all data.
    /// Should be called by the View when it's activated/loaded.
    /// </summary>
    public Task InitializeAsync() => LoadDashboardData();

    public MoodEntrySummaryResponse? RecentMood
    {
        get => _recentMood;
        set => this.RaiseAndSetIfChanged(ref _recentMood, value);
    }

    public AssessmentSummaryResponse? LastAssessment
    {
        get => _lastAssessment;
        set => this.RaiseAndSetIfChanged(ref _lastAssessment, value);
    }

    public ObservableCollection<string> QuickStats
    {
        get => _quickStats;
        set => this.RaiseAndSetIfChanged(ref _quickStats, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    public ReactiveCommand<Unit, Unit> LogMoodCommand { get; }
    public ReactiveCommand<Unit, Unit> TakeAssessmentCommand { get; }
    public ReactiveCommand<Unit, Unit> ViewTrendsCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    private async Task LoadDashboardData()
    {
        try
        {
            IsLoading = true;
            QuickStats.Clear();

            // Load recent mood entry
            var moodHistory = await _apiClient.GetMoodHistoryAsync(
                startDate: DateTime.Now.AddDays(-RecentMoodLookbackDays),
                endDate: DateTime.Now,
                pageSize: 1,
                cancellationToken: CancellationToken);

            RecentMood = moodHistory.Items.FirstOrDefault();

            if (RecentMood != null)
            {
                QuickStats.Add($"Last Mood: {RecentMood.MoodScore}/5 ({GetMoodLabel(RecentMood.MoodScore)})");
                QuickStats.Add($"Logged: {RecentMood.RecordedAt:g}");
            }
            else
            {
                QuickStats.Add("No recent mood entries");
            }

            // Load last assessment
            var assessmentHistory = await _apiClient.GetAssessmentHistoryAsync(
                startDate: DateTime.Now.AddDays(-RecentAssessmentLookbackDays),
                endDate: DateTime.Now,
                pageSize: 1,
                cancellationToken: CancellationToken);

            LastAssessment = assessmentHistory.Items.FirstOrDefault();

            if (LastAssessment != null)
            {
                QuickStats.Add($"Last Assessment: {LastAssessment.Type}");
                QuickStats.Add($"Score: {LastAssessment.TotalScore} ({LastAssessment.Severity})");
                QuickStats.Add($"Completed: {LastAssessment.CompletedAt:d}");
            }
            else
            {
                QuickStats.Add("No recent assessments");
            }

            // Load mood statistics for the week
            var moodStats = await _apiClient.GetMoodStatsAsync(
                startDate: DateTime.Now.AddDays(-RecentMoodLookbackDays),
                endDate: DateTime.Now,
                cancellationToken: CancellationToken);

            if (moodStats != null && moodStats.Count > 0)
            {
                QuickStats.Add($"Week Average Mood: {moodStats.Average:F1}/5");
                QuickStats.Add($"Total Entries: {moodStats.Count}");
            }

            // Load health metrics summary
            var healthMetrics = await _apiClient.GetHealthMetricsHistoryAsync(
                startDate: DateTime.Now.AddDays(-RecentMoodLookbackDays),
                endDate: DateTime.Now,
                cancellationToken: CancellationToken);

            if (healthMetrics.Items.Any())
            {
                var sleepMetrics = healthMetrics.Items.Where(m => m.Type == "SleepHours").ToList();
                var waterMetrics = healthMetrics.Items.Where(m => m.Type == "WaterIntakeOz").ToList();

                if (sleepMetrics.Any())
                {
                    var avgSleep = sleepMetrics.Average(m => m.Value);
                    QuickStats.Add($"Avg Sleep (week): {avgSleep:F1} hours");
                }

                if (waterMetrics.Any())
                {
                    var avgWater = waterMetrics.Average(m => m.Value);
                    QuickStats.Add($"Avg Water (week): {avgWater:F0} oz");
                }
            }
        }
        catch (Exception ex)
        {
            QuickStats.Clear();
            QuickStats.Add($"Error loading data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string GetMoodLabel(int score)
    {
        return score switch
        {
            1 => "Worst",
            2 => "Below Average",
            3 => "Average",
            4 => "Above Average",
            5 => "Best",
            _ => "Unknown"
        };
    }

    private async Task NavigateToMoodEntry()
    {
        // TODO: Navigate to mood entry view
        await Task.CompletedTask;
    }

    private async Task NavigateToAssessments()
    {
        // TODO: Navigate to assessments view
        await Task.CompletedTask;
    }

    private async Task NavigateToTrends()
    {
        // TODO: Navigate to data visualization view
        await Task.CompletedTask;
    }
}