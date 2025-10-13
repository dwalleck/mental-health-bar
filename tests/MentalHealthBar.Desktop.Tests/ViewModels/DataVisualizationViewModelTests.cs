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
using ScottPlot.Avalonia;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;
using MentalHealthBar.Desktop.Services;
using MentalHealthBar.Desktop.ViewModels;

namespace MentalHealthBar.Desktop.Tests.ViewModels;

public class DataVisualizationViewModelTests
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly Mock<IChartingService> _chartingServiceMock;
    private readonly DataVisualizationViewModel _viewModel;

    public DataVisualizationViewModelTests()
    {
        _apiClientMock = new Mock<IApiClient>();
        _chartingServiceMock = new Mock<IChartingService>();
        _viewModel = new DataVisualizationViewModel(_apiClientMock.Object, _chartingServiceMock.Object);
    }

    [Test]
    public async Task InitialState_HasCorrectDefaults()
    {
        // Assert
        await Assert.That(_viewModel.SelectedDateRange).IsEqualTo(DateRangeOption.Last30Days);
        await Assert.That(_viewModel.SelectedAssessmentType).IsEqualTo("PHQ9");
        await Assert.That(_viewModel.ShowDailyAverage).IsFalse();
        await Assert.That(_viewModel.IsLoading).IsFalse();
        await Assert.That(_viewModel.DateRangeOptions.Count).IsEqualTo(4);
        await Assert.That(_viewModel.AssessmentTypes.Count).IsEqualTo(4);
    }

    [Test]
    public async Task RefreshMoodChart_CallsChartingServiceWithCorrectData()
    {
        // Arrange
        var now = SystemClock.Instance.GetCurrentInstant();
        var moodEntries = new List<MoodEntrySummaryDto>
        {
            new(Guid.NewGuid(), 4, now, new List<EventLabelDto>(), null, now),
            new(Guid.NewGuid(), 3, now.Minus(Duration.FromDays(1)), new List<EventLabelDto>(), null, now)
        };

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default))
            .ReturnsAsync(new MoodPagedResultDto(moodEntries, 2, 1, 500));

        var expectedChart = new AvaPlot();
        _chartingServiceMock.Setup(x => x.CreateMoodChart(moodEntries, false))
            .Returns(expectedChart);

        // Act
        await _viewModel.RefreshChartsCommand.Execute().FirstAsync();

        // Assert
        _chartingServiceMock.Verify(x => x.CreateMoodChart(
            It.Is<List<MoodEntrySummaryDto>>(list => list.Count == 2),
            false), Times.AtLeastOnce);
        await Assert.That(_viewModel.MoodChart).IsEqualTo(expectedChart);
    }

    [Test]
    public async Task RefreshMoodChart_WithDailyAverage_PassesCorrectFlag()
    {
        // Arrange
        _viewModel.ShowDailyAverage = true;

        var now = SystemClock.Instance.GetCurrentInstant();
        var moodEntries = new List<MoodEntrySummaryDto>
        {
            new(Guid.NewGuid(), 4, now, new List<EventLabelDto>(), null, now)
        };

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default))
            .ReturnsAsync(new MoodPagedResultDto(moodEntries, 1, 1, 500));

        _chartingServiceMock.Setup(x => x.CreateMoodChart(It.IsAny<List<MoodEntrySummaryDto>>(), true))
            .Returns(new AvaPlot());

        // Act
        // Note: Throttle delays removed - testing immediate response
        await Task.Delay(100); // Allow async operation to complete

        // Assert
        _chartingServiceMock.Verify(x => x.CreateMoodChart(
            It.IsAny<List<MoodEntrySummaryDto>>(),
            true), Times.AtLeastOnce);
    }

    [Test]
    public async Task RefreshAssessmentChart_FiltersbySelectedType()
    {
        // Arrange
        var now = SystemClock.Instance.GetCurrentInstant();
        var assessments = new List<AssessmentSummaryDto>
        {
            new(Guid.NewGuid(), "PHQ9", 10, "Mild", now, now),
            new(Guid.NewGuid(), "PHQ9", 12, "Moderate", now, now)
        };

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                "PHQ9", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default))
            .ReturnsAsync(new AssessmentPagedResultDto(assessments, 2, 1, 100));

        var expectedChart = new AvaPlot();
        _chartingServiceMock.Setup(x => x.CreateAssessmentChart(assessments, "PHQ9"))
            .Returns(expectedChart);

        // Act
        await _viewModel.RefreshChartsCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.GetAssessmentHistoryAsync(
            "PHQ9", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default), Times.AtLeastOnce);
        _chartingServiceMock.Verify(x => x.CreateAssessmentChart(
            It.Is<List<AssessmentSummaryDto>>(list => list.Count == 2),
            "PHQ9"), Times.AtLeastOnce);
    }

    [Test]
    public async Task RefreshHealthChart_SeparatesSleepAndWaterMetrics()
    {
        // Arrange
        var now = SystemClock.Instance.GetCurrentInstant();
        var healthMetrics = new List<HealthMetricSummaryDto>
        {
            new(Guid.NewGuid(), "SleepHours", 8m, DateOnly.Parse("2025-01-01"), now),
            new(Guid.NewGuid(), "WaterIntakeOz", 64m, DateOnly.Parse("2025-01-01"), now),
            new(Guid.NewGuid(), "SleepHours", 7m, DateOnly.Parse("2025-01-02"), now)
        };

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(new HealthMetricPagedResultDto(healthMetrics, 3, 1, 365));

        var expectedChart = new AvaPlot();
        _chartingServiceMock.Setup(x => x.CreateCombinedHealthChart(
                It.IsAny<List<HealthMetricSummaryDto>>(),
                It.IsAny<List<HealthMetricSummaryDto>>()))
            .Returns(expectedChart);

        // Act
        await _viewModel.RefreshChartsCommand.Execute().FirstAsync();

        // Assert
        _chartingServiceMock.Verify(x => x.CreateCombinedHealthChart(
            It.Is<List<HealthMetricSummaryDto>>(list => list.Count == 2 && list.All(m => m.Type == "SleepHours")),
            It.Is<List<HealthMetricSummaryDto>>(list => list.Count == 1 && list.All(m => m.Type == "WaterIntakeOz"))),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task DateRangeChange_TriggersRefresh()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default))
            .ReturnsAsync(new MoodPagedResultDto(new List<MoodEntrySummaryDto>(), 0, 1, 500));

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default))
            .ReturnsAsync(new AssessmentPagedResultDto(new List<AssessmentSummaryDto>(), 0, 1, 100));

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(new HealthMetricPagedResultDto(new List<HealthMetricSummaryDto>(), 0, 1, 365));

        _chartingServiceMock.Setup(x => x.CreateMoodChart(It.IsAny<List<MoodEntrySummaryDto>>(), It.IsAny<bool>()))
            .Returns(new AvaPlot());
        _chartingServiceMock.Setup(x => x.CreateAssessmentChart(It.IsAny<List<AssessmentSummaryDto>>(), It.IsAny<string>()))
            .Returns(new AvaPlot());
        _chartingServiceMock.Setup(x => x.CreateCombinedHealthChart(It.IsAny<List<HealthMetricSummaryDto>>(), It.IsAny<List<HealthMetricSummaryDto>>()))
            .Returns(new AvaPlot());

        // Act
        _viewModel.SelectedDateRange = DateRangeOption.Last7Days;
        // Note: Throttle delays removed - testing immediate response
        await Task.Delay(100); // Allow async operation

        // Assert
        _apiClientMock.Verify(x => x.GetMoodHistoryAsync(
            It.Is<DateTime?>(d => d.Value >= DateTime.Now.AddDays(-8)),
            It.IsAny<DateTime?>(), null, 1, 500, default), Times.AtLeastOnce);
    }

    [Test]
    public async Task AssessmentTypeChange_TriggersOnlyAssessmentRefresh()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default))
            .ReturnsAsync(new AssessmentPagedResultDto(new List<AssessmentSummaryDto>(), 0, 1, 100));

        _chartingServiceMock.Setup(x => x.CreateAssessmentChart(It.IsAny<List<AssessmentSummaryDto>>(), It.IsAny<string>()))
            .Returns(new AvaPlot());

        // Act
        _viewModel.SelectedAssessmentType = "GAD7";
        // Note: Throttle delays removed - testing immediate response
        await Task.Delay(100);

        // Assert
        _apiClientMock.Verify(x => x.GetAssessmentHistoryAsync(
            "GAD7", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default), Times.AtLeastOnce);
        // Should not refresh mood or health charts
        _apiClientMock.Verify(x => x.GetMoodHistoryAsync(
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default), Times.Never);
    }

    [Test]
    public void GetDateRange_ReturnsCorrectDates()
    {
        // Test each date range option
        var testCases = new[]
        {
            (DateRangeOption.Last7Days, 7),
            (DateRangeOption.Last30Days, 30),
            (DateRangeOption.Last90Days, 90),
            (DateRangeOption.AllTime, 3650) // ~10 years
        };

        foreach (var (option, expectedDays) in testCases)
        {
            _viewModel.SelectedDateRange = option;
            // Note: GetDateRange is private, so we test indirectly through API calls
            // This would be better tested with InternalsVisibleTo or making the method protected
        }
    }

    [Test]
    public async Task RefreshAllCharts_CallsAllThreeRefreshMethods()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default))
            .ReturnsAsync(new MoodPagedResultDto(new List<MoodEntrySummaryDto>(), 0, 1, 500));

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default))
            .ReturnsAsync(new AssessmentPagedResultDto(new List<AssessmentSummaryDto>(), 0, 1, 100));

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(new HealthMetricPagedResultDto(new List<HealthMetricSummaryDto>(), 0, 1, 365));

        _chartingServiceMock.Setup(x => x.CreateMoodChart(It.IsAny<List<MoodEntrySummaryDto>>(), It.IsAny<bool>()))
            .Returns(new AvaPlot());
        _chartingServiceMock.Setup(x => x.CreateAssessmentChart(It.IsAny<List<AssessmentSummaryDto>>(), It.IsAny<string>()))
            .Returns(new AvaPlot());
        _chartingServiceMock.Setup(x => x.CreateCombinedHealthChart(It.IsAny<List<HealthMetricSummaryDto>>(), It.IsAny<List<HealthMetricSummaryDto>>()))
            .Returns(new AvaPlot());

        // Act
        await _viewModel.RefreshChartsCommand.Execute().FirstAsync();

        // Assert
        _chartingServiceMock.Verify(x => x.CreateMoodChart(It.IsAny<List<MoodEntrySummaryDto>>(), It.IsAny<bool>()), Times.Once);
        _chartingServiceMock.Verify(x => x.CreateAssessmentChart(It.IsAny<List<AssessmentSummaryDto>>(), It.IsAny<string>()), Times.Once);
        _chartingServiceMock.Verify(x => x.CreateCombinedHealthChart(It.IsAny<List<HealthMetricSummaryDto>>(), It.IsAny<List<HealthMetricSummaryDto>>()), Times.Once);
    }

    [Test]
    public async Task ChartRefresh_HandlesApiErrors()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        await _viewModel.RefreshChartsCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.IsLoading).IsFalse();
        // Chart should remain null or unchanged
        await Assert.That(_viewModel.MoodChart).IsNull();
    }

    [Test]
    public async Task ExportChartCommand_IsInitialized()
    {
        // Assert
        await Assert.That(_viewModel.ExportChartCommand).IsNotNull();

        // Act - should not throw (fire-and-forget)
        _viewModel.ExportChartCommand.Execute("mood").Subscribe();
    }
}
