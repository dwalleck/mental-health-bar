using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Moq;
using NodaTime;
using MentalHealthBar.Contracts.Responses.Assessments;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
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
        var expectedMood = new MoodEntrySummaryDto(
            Guid.NewGuid(),
            4,
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromHours(1)),
            new List<EventLabelDto>(),
            null,
            SystemClock.Instance.GetCurrentInstant()
        );

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MoodPagedResultDto(
                new List<MoodEntrySummaryDto> { expectedMood },
                1, 1, 1));

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssessmentPagedResultDto(
                new List<AssessmentSummaryDto>(),
                0, 1, 1));

        _apiClientMock.Setup(x => x.GetMoodStatsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MoodStatsDto(5, 3.5, 4, new Dictionary<int, int>()));

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthMetricPagedResultDto(
                new List<HealthMetricSummaryDto>(),
                0, 1, 365));

        // Act
        await _viewModel.RefreshCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.RecentMood).IsNotNull();
        await Assert.That(_viewModel.RecentMood.MoodScore).IsEqualTo(4);
        await Assert.That(_viewModel.QuickStats.Count).IsGreaterThan(0);
        await Assert.That(_viewModel.QuickStats.Any(s => s.Contains("Last Mood: 4/5"))).IsTrue();
    }

    [Test]
    public async Task LoadDashboardData_WithNoRecentMood_ShowsNoEntriesMessage()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MoodPagedResultDto(
                new List<MoodEntrySummaryDto>(),
                0, 1, 1));

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssessmentPagedResultDto(
                new List<AssessmentSummaryDto>(),
                0, 1, 1));

        _apiClientMock.Setup(x => x.GetMoodStatsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MoodStatsDto(0, 0, 0, new Dictionary<int, int>()));

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthMetricPagedResultDto(
                new List<HealthMetricSummaryDto>(),
                0, 1, 365));

        // Act
        await _viewModel.RefreshCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.RecentMood).IsNull();
        await Assert.That(_viewModel.QuickStats.Any(s => s.Contains("No recent mood entries"))).IsTrue();
    }

    [Test]
    public async Task LoadDashboardData_WithLastAssessment_PopulatesAssessmentInfo()
    {
        // Arrange
        var expectedAssessment = new AssessmentSummaryDto(
            Guid.NewGuid(),
            "PHQ9",
            12,
            "Moderate",
            SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(2)),
            SystemClock.Instance.GetCurrentInstant()
        );

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MoodPagedResultDto(
                new List<MoodEntrySummaryDto>(),
                0, 1, 1));

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssessmentPagedResultDto(
                new List<AssessmentSummaryDto> { expectedAssessment },
                1, 1, 1));

        _apiClientMock.Setup(x => x.GetMoodStatsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MoodStatsDto(0, 0, 0, new Dictionary<int, int>()));

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthMetricPagedResultDto(
                new List<HealthMetricSummaryDto>(),
                0, 1, 365));

        // Act
        await _viewModel.RefreshCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.LastAssessment).IsNotNull();
        await Assert.That(_viewModel.LastAssessment.Type).IsEqualTo("PHQ9");
        await Assert.That(_viewModel.LastAssessment.TotalScore).IsEqualTo(12);
        await Assert.That(_viewModel.QuickStats.Any(s => s.Contains("Last Assessment: PHQ9"))).IsTrue();
        await Assert.That(_viewModel.QuickStats.Any(s => s.Contains("Score: 12 (Moderate)"))).IsTrue();
    }

    [Test]
    public async Task LoadDashboardData_WithHealthMetrics_ShowsAverages()
    {
        // Arrange
        var healthMetrics = new List<HealthMetricSummaryDto>
        {
            new(Guid.NewGuid(), "SleepHours", 7.5m, DateOnly.Parse("2025-01-01"), SystemClock.Instance.GetCurrentInstant()),
            new(Guid.NewGuid(), "SleepHours", 8.0m, DateOnly.Parse("2025-01-02"), SystemClock.Instance.GetCurrentInstant()),
            new(Guid.NewGuid(), "WaterIntakeOz", 64m, DateOnly.Parse("2025-01-01"), SystemClock.Instance.GetCurrentInstant()),
            new(Guid.NewGuid(), "WaterIntakeOz", 72m, DateOnly.Parse("2025-01-02"), SystemClock.Instance.GetCurrentInstant())
        };

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MoodPagedResultDto(
                new List<MoodEntrySummaryDto>(),
                0, 1, 1));

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssessmentPagedResultDto(
                new List<AssessmentSummaryDto>(),
                0, 1, 1));

        _apiClientMock.Setup(x => x.GetMoodStatsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MoodStatsDto(0, 0, 0, new Dictionary<int, int>()));

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthMetricPagedResultDto(
                healthMetrics,
                4, 1, 365));

        // Act
        await _viewModel.RefreshCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.QuickStats.Any(s => s.Contains("Avg Sleep (week): 7.8 hours"))).IsTrue();
        await Assert.That(_viewModel.QuickStats.Any(s => s.Contains("Avg Water (week): 68 oz"))).IsTrue();
    }

    [Test]
    public async Task LoadDashboardData_OnApiError_ShowsErrorMessage()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API connection failed"));

        // Act
        await _viewModel.RefreshCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.QuickStats.Count).IsEqualTo(1);
        await Assert.That(_viewModel.QuickStats[0]).StartsWith("Error loading data:");
        await Assert.That(_viewModel.IsLoading).IsFalse();
    }

    [Test]
    public async Task Commands_AreInitializedCorrectly()
    {
        // Assert
        await Assert.That(_viewModel.LogMoodCommand).IsNotNull();
        await Assert.That(_viewModel.TakeAssessmentCommand).IsNotNull();
        await Assert.That(_viewModel.ViewTrendsCommand).IsNotNull();
        await Assert.That(_viewModel.RefreshCommand).IsNotNull();
    }

    [Test]
    public async Task IsLoading_ChangesCorrectlyDuringDataLoad()
    {
        // Arrange
        var tcs = new TaskCompletionSource<MoodPagedResultDto>();
        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 1, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssessmentPagedResultDto(
                new List<AssessmentSummaryDto>(),
                0, 1, 1));

        _apiClientMock.Setup(x => x.GetMoodStatsAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MoodStatsDto(0, 0, 0, new Dictionary<int, int>()));

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HealthMetricPagedResultDto(
                new List<HealthMetricSummaryDto>(),
                0, 1, 365));

        // Act
        var loadTask = _viewModel.RefreshCommand.Execute();

        // Assert - Loading should be true during operation
        await Assert.That(_viewModel.IsLoading).IsTrue();

        // Complete the async operation
        tcs.SetResult(new MoodPagedResultDto(
            new List<MoodEntrySummaryDto>(),
            0, 1, 1));
        await loadTask;

        // Assert - Loading should be false after completion
        await Assert.That(_viewModel.IsLoading).IsFalse();
    }
}
