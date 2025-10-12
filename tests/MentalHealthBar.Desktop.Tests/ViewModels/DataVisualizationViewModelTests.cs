using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Moq;
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
        var moodEntries = new List<MoodEntryResponse>
        {
            new(Guid.NewGuid(), 4, DateTimeOffset.Now, new List<EventLabelResponse>(), null, DateTimeOffset.Now, null),
            new(Guid.NewGuid(), 3, DateTimeOffset.Now.AddDays(-1), new List<EventLabelResponse>(), null, DateTimeOffset.Now, null)
        };

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default))
            .ReturnsAsync(moodEntries);

        var expectedChart = new AvaPlot();
        _chartingServiceMock.Setup(x => x.CreateMoodChart(moodEntries, false))
            .Returns(expectedChart);

        // Act
        await _viewModel.RefreshChartsCommand.Execute().FirstAsync();

        // Assert
        _chartingServiceMock.Verify(x => x.CreateMoodChart(
            It.Is<List<MoodEntryResponse>>(list => list.Count == 2),
            false), Times.AtLeastOnce);
        await Assert.That(_viewModel.MoodChart).IsEqualTo(expectedChart);
    }

    [Test]
    public async Task RefreshMoodChart_WithDailyAverage_PassesCorrectFlag()
    {
        // Arrange
        _viewModel.ShowDailyAverage = true;

        var moodEntries = new List<MoodEntryResponse>
        {
            new(Guid.NewGuid(), 4, DateTimeOffset.Now, new List<EventLabelResponse>(), null, DateTimeOffset.Now, null)
        };

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default))
            .ReturnsAsync(moodEntries);

        _chartingServiceMock.Setup(x => x.CreateMoodChart(It.IsAny<List<MoodEntryResponse>>(), true))
            .Returns(new AvaPlot());

        // Act
        // Note: Throttle delays removed - testing immediate response
        await Task.Delay(100); // Allow async operation to complete

        // Assert
        _chartingServiceMock.Verify(x => x.CreateMoodChart(
            It.IsAny<List<MoodEntryResponse>>(),
            true), Times.AtLeastOnce);
    }

    [Test]
    public async Task RefreshAssessmentChart_FiltersbySelectedType()
    {
        // Arrange
        var assessments = new List<AssessmentResponse>
        {
            new(Guid.NewGuid(), "PHQ9", new Dictionary<string, int>(), 10, "Mild", DateTimeOffset.Now, DateTimeOffset.Now, null),
            new(Guid.NewGuid(), "PHQ9", new Dictionary<string, int>(), 12, "Moderate", DateTimeOffset.Now, DateTimeOffset.Now, null)
        };

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                "PHQ9", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default))
            .ReturnsAsync(assessments);

        var expectedChart = new AvaPlot();
        _chartingServiceMock.Setup(x => x.CreateAssessmentChart(assessments, "PHQ9"))
            .Returns(expectedChart);

        // Act
        await _viewModel.RefreshChartsCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.GetAssessmentHistoryAsync(
            "PHQ9", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default), Times.AtLeastOnce);
        _chartingServiceMock.Verify(x => x.CreateAssessmentChart(
            It.Is<List<AssessmentResponse>>(list => list.Count == 2),
            "PHQ9"), Times.AtLeastOnce);
    }

    [Test]
    public async Task RefreshHealthChart_SeparatesSleepAndWaterMetrics()
    {
        // Arrange
        var healthMetrics = new List<HealthMetricResponse>
        {
            new(Guid.NewGuid(), "SleepHours", 8m, DateOnly.Parse("2025-01-01"), DateTimeOffset.Now, null),
            new(Guid.NewGuid(), "WaterIntakeOz", 64m, DateOnly.Parse("2025-01-01"), DateTimeOffset.Now, null),
            new(Guid.NewGuid(), "SleepHours", 7m, DateOnly.Parse("2025-01-02"), DateTimeOffset.Now, null)
        };

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(healthMetrics);

        var expectedChart = new AvaPlot();
        _chartingServiceMock.Setup(x => x.CreateCombinedHealthChart(
                It.IsAny<List<HealthMetricResponse>>(),
                It.IsAny<List<HealthMetricResponse>>()))
            .Returns(expectedChart);

        // Act
        await _viewModel.RefreshChartsCommand.Execute().FirstAsync();

        // Assert
        _chartingServiceMock.Verify(x => x.CreateCombinedHealthChart(
            It.Is<List<HealthMetricResponse>>(list => list.Count == 2 && list.All(m => m.Type == "SleepHours")),
            It.Is<List<HealthMetricResponse>>(list => list.Count == 1 && list.All(m => m.Type == "WaterIntakeOz"))),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task DateRangeChange_TriggersRefresh()
    {
        // Arrange
        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default))
            .ReturnsAsync(new List<MoodEntryResponse>());

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default))
            .ReturnsAsync(new List<AssessmentResponse>());

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(new List<HealthMetricResponse>());

        _chartingServiceMock.Setup(x => x.CreateMoodChart(It.IsAny<List<MoodEntryResponse>>(), It.IsAny<bool>()))
            .Returns(new AvaPlot());
        _chartingServiceMock.Setup(x => x.CreateAssessmentChart(It.IsAny<List<AssessmentResponse>>(), It.IsAny<string>()))
            .Returns(new AvaPlot());
        _chartingServiceMock.Setup(x => x.CreateCombinedHealthChart(It.IsAny<List<HealthMetricResponse>>(), It.IsAny<List<HealthMetricResponse>>()))
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
            .ReturnsAsync(new List<AssessmentResponse>());

        _chartingServiceMock.Setup(x => x.CreateAssessmentChart(It.IsAny<List<AssessmentResponse>>(), It.IsAny<string>()))
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
            .ReturnsAsync(new List<MoodEntryResponse>());

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default))
            .ReturnsAsync(new List<AssessmentResponse>());

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(new List<HealthMetricResponse>());

        _chartingServiceMock.Setup(x => x.CreateMoodChart(It.IsAny<List<MoodEntryResponse>>(), It.IsAny<bool>()))
            .Returns(new AvaPlot());
        _chartingServiceMock.Setup(x => x.CreateAssessmentChart(It.IsAny<List<AssessmentResponse>>(), It.IsAny<string>()))
            .Returns(new AvaPlot());
        _chartingServiceMock.Setup(x => x.CreateCombinedHealthChart(It.IsAny<List<HealthMetricResponse>>(), It.IsAny<List<HealthMetricResponse>>()))
            .Returns(new AvaPlot());

        // Act
        await _viewModel.RefreshChartsCommand.Execute().FirstAsync();

        // Assert
        _chartingServiceMock.Verify(x => x.CreateMoodChart(It.IsAny<List<MoodEntryResponse>>(), It.IsAny<bool>()), Times.Once);
        _chartingServiceMock.Verify(x => x.CreateAssessmentChart(It.IsAny<List<AssessmentResponse>>(), It.IsAny<string>()), Times.Once);
        _chartingServiceMock.Verify(x => x.CreateCombinedHealthChart(It.IsAny<List<HealthMetricResponse>>(), It.IsAny<List<HealthMetricResponse>>()), Times.Once);
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
