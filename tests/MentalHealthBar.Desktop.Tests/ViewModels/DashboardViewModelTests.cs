using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using MentalHealthBar.Desktop.Services;
using MentalHealthBar.Desktop.ViewModels;

namespace MentalHealthBar.Desktop.Tests.ViewModels;

public class DashboardViewModelTests
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly DashboardViewModel _viewModel;

    public DashboardViewModelTests()
    {
        _apiClientMock = new Mock<IApiClient>();
        _viewModel = new DashboardViewModel(_apiClientMock.Object);
    }

    [Test]
    public async Task LoadDashboardData_WithRecentMood_PopulatesRecentMoodProperty()
    {
        // Arrange
        var expectedMood = new MoodEntryResponse
        {
            Id = Guid.NewGuid(),
            MoodScore = 4,
            RecordedAt = DateTimeOffset.Now.AddHours(-1),
            Tags = new List<string> { "work", "exercise" }
        };

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, default))
            .ReturnsAsync(new List<MoodEntryResponse> { expectedMood });

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 1, default))
            .ReturnsAsync(new List<AssessmentResponse>());

        _apiClientMock.Setup(x => x.GetMoodStatsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), default))
            .ReturnsAsync(new MoodStatsResponse { Count = 5, Average = 3.5m });

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(new List<HealthMetricResponse>());

        // Act
        await _viewModel.RefreshCommand.Execute();

        // Assert
        Assert.That(_viewModel.RecentMood, Is.Not.Null);
        Assert.That(_viewModel.RecentMood.MoodScore, Is.EqualTo(4));
        Assert.That(_viewModel.QuickStats, Has.Count.GreaterThan(0));
        Assert.That(_viewModel.QuickStats.Any(s => s.Contains("Last Mood: 4/5")), Is.True);
    }

    [Test]
    public async Task LoadDashboardData_WithNoRecentMood_ShowsNoEntriesMessage()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, default))
            .ReturnsAsync(new List<MoodEntryResponse>());

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 1, default))
            .ReturnsAsync(new List<AssessmentResponse>());

        _apiClientMock.Setup(x => x.GetMoodStatsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), default))
            .ReturnsAsync(new MoodStatsResponse { Count = 0, Average = 0 });

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(new List<HealthMetricResponse>());

        // Act
        await _viewModel.RefreshCommand.Execute();

        // Assert
        Assert.That(_viewModel.RecentMood, Is.Null);
        Assert.That(_viewModel.QuickStats.Any(s => s.Contains("No recent mood entries")), Is.True);
    }

    [Test]
    public async Task LoadDashboardData_WithLastAssessment_PopulatesAssessmentInfo()
    {
        // Arrange
        var expectedAssessment = new AssessmentResponse
        {
            Id = Guid.NewGuid(),
            Type = "PHQ9",
            TotalScore = 12,
            Severity = "Moderate",
            CompletedAt = DateTimeOffset.Now.AddDays(-2)
        };

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, default))
            .ReturnsAsync(new List<MoodEntryResponse>());

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 1, default))
            .ReturnsAsync(new List<AssessmentResponse> { expectedAssessment });

        _apiClientMock.Setup(x => x.GetMoodStatsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), default))
            .ReturnsAsync(new MoodStatsResponse { Count = 0, Average = 0 });

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(new List<HealthMetricResponse>());

        // Act
        await _viewModel.RefreshCommand.Execute();

        // Assert
        Assert.That(_viewModel.LastAssessment, Is.Not.Null);
        Assert.That(_viewModel.LastAssessment.Type, Is.EqualTo("PHQ9"));
        Assert.That(_viewModel.LastAssessment.TotalScore, Is.EqualTo(12));
        Assert.That(_viewModel.QuickStats.Any(s => s.Contains("Last Assessment: PHQ9")), Is.True);
        Assert.That(_viewModel.QuickStats.Any(s => s.Contains("Score: 12 (Moderate)")), Is.True);
    }

    [Test]
    public async Task LoadDashboardData_WithHealthMetrics_ShowsAverages()
    {
        // Arrange
        var healthMetrics = new List<HealthMetricResponse>
        {
            new() { Type = "SleepHours", Value = 7.5m, RecordedDate = "2025-01-01" },
            new() { Type = "SleepHours", Value = 8.0m, RecordedDate = "2025-01-02" },
            new() { Type = "WaterIntakeOz", Value = 64m, RecordedDate = "2025-01-01" },
            new() { Type = "WaterIntakeOz", Value = 72m, RecordedDate = "2025-01-02" }
        };

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, default))
            .ReturnsAsync(new List<MoodEntryResponse>());

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 1, default))
            .ReturnsAsync(new List<AssessmentResponse>());

        _apiClientMock.Setup(x => x.GetMoodStatsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), default))
            .ReturnsAsync(new MoodStatsResponse { Count = 0, Average = 0 });

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(healthMetrics);

        // Act
        await _viewModel.RefreshCommand.Execute();

        // Assert
        Assert.That(_viewModel.QuickStats.Any(s => s.Contains("Avg Sleep (week): 7.8 hours")), Is.True);
        Assert.That(_viewModel.QuickStats.Any(s => s.Contains("Avg Water (week): 68 oz")), Is.True);
    }

    [Test]
    public async Task LoadDashboardData_OnApiError_ShowsErrorMessage()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, default))
            .ThrowsAsync(new Exception("API connection failed"));

        // Act
        await _viewModel.RefreshCommand.Execute();

        // Assert
        Assert.That(_viewModel.QuickStats, Has.Count.EqualTo(1));
        Assert.That(_viewModel.QuickStats[0], Does.StartWith("Error loading data:"));
        Assert.That(_viewModel.IsLoading, Is.False);
    }

    [Test]
    public void Commands_AreInitializedCorrectly()
    {
        // Assert
        Assert.That(_viewModel.LogMoodCommand, Is.Not.Null);
        Assert.That(_viewModel.TakeAssessmentCommand, Is.Not.Null);
        Assert.That(_viewModel.ViewTrendsCommand, Is.Not.Null);
        Assert.That(_viewModel.RefreshCommand, Is.Not.Null);
    }

    [Test]
    public async Task IsLoading_ChangesCorrectlyDuringDataLoad()
    {
        // Arrange
        var tcs = new TaskCompletionSource<List<MoodEntryResponse>>();
        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, default))
            .Returns(tcs.Task);

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 1, default))
            .ReturnsAsync(new List<AssessmentResponse>());

        _apiClientMock.Setup(x => x.GetMoodStatsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), default))
            .ReturnsAsync(new MoodStatsResponse { Count = 0, Average = 0 });

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(new List<HealthMetricResponse>());

        // Act
        var loadTask = _viewModel.RefreshCommand.Execute();

        // Assert - Loading should be true during operation
        Assert.That(_viewModel.IsLoading, Is.True);

        // Complete the async operation
        tcs.SetResult(new List<MoodEntryResponse>());
        await loadTask;

        // Assert - Loading should be false after completion
        Assert.That(_viewModel.IsLoading, Is.False);
    }
}