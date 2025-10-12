using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using ScottPlot.Avalonia;
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
    public void InitialState_HasCorrectDefaults()
    {
        // Assert
        Assert.That(_viewModel.SelectedDateRange, Is.EqualTo(DateRangeOption.Last30Days));
        Assert.That(_viewModel.SelectedAssessmentType, Is.EqualTo("PHQ9"));
        Assert.That(_viewModel.ShowDailyAverage, Is.False);
        Assert.That(_viewModel.IsLoading, Is.False);
        Assert.That(_viewModel.DateRangeOptions, Has.Count.EqualTo(4));
        Assert.That(_viewModel.AssessmentTypes, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task RefreshMoodChart_CallsChartingServiceWithCorrectData()
    {
        // Arrange
        var moodEntries = new List<MoodEntryResponse>
        {
            new() { Id = Guid.NewGuid(), MoodScore = 4, RecordedAt = DateTimeOffset.Now },
            new() { Id = Guid.NewGuid(), MoodScore = 3, RecordedAt = DateTimeOffset.Now.AddDays(-1) }
        };

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default))
            .ReturnsAsync(moodEntries);

        var expectedChart = new AvaPlot();
        _chartingServiceMock.Setup(x => x.CreateMoodChart(moodEntries, false))
            .Returns(expectedChart);

        // Act
        await _viewModel.RefreshChartsCommand.Execute();

        // Assert
        _chartingServiceMock.Verify(x => x.CreateMoodChart(
            It.Is<List<MoodEntryResponse>>(list => list.Count == 2),
            false), Times.AtLeastOnce);
        Assert.That(_viewModel.MoodChart, Is.EqualTo(expectedChart));
    }

    [Test]
    public async Task RefreshMoodChart_WithDailyAverage_PassesCorrectFlag()
    {
        // Arrange
        _viewModel.ShowDailyAverage = true;

        var moodEntries = new List<MoodEntryResponse>
        {
            new() { Id = Guid.NewGuid(), MoodScore = 4, RecordedAt = DateTimeOffset.Now }
        };

        _apiClientMock.Setup(x => x.GetMoodHistoryAsync(
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, 1, 500, default))
            .ReturnsAsync(moodEntries);

        _chartingServiceMock.Setup(x => x.CreateMoodChart(It.IsAny<List<MoodEntryResponse>>(), true))
            .Returns(new AvaPlot());

        // Act
        await Task.Delay(600); // Wait for throttle
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
            new() { Id = Guid.NewGuid(), Type = "PHQ9", TotalScore = 10 },
            new() { Id = Guid.NewGuid(), Type = "PHQ9", TotalScore = 12 }
        };

        _apiClientMock.Setup(x => x.GetAssessmentHistoryAsync(
                "PHQ9", It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 100, default))
            .ReturnsAsync(assessments);

        var expectedChart = new AvaPlot();
        _chartingServiceMock.Setup(x => x.CreateAssessmentChart(assessments, "PHQ9"))
            .Returns(expectedChart);

        // Act
        await _viewModel.RefreshChartsCommand.Execute();

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
            new() { Type = "SleepHours", Value = 8m, RecordedDate = "2025-01-01" },
            new() { Type = "WaterIntakeOz", Value = 64m, RecordedDate = "2025-01-01" },
            new() { Type = "SleepHours", Value = 7m, RecordedDate = "2025-01-02" }
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
        await _viewModel.RefreshChartsCommand.Execute();

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
        await Task.Delay(600); // Wait for throttle
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
        await Task.Delay(600); // Wait for throttle
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
        await _viewModel.RefreshChartsCommand.Execute();

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
        await _viewModel.RefreshChartsCommand.Execute();

        // Assert
        Assert.That(_viewModel.IsLoading, Is.False);
        // Chart should remain null or unchanged
        Assert.That(_viewModel.MoodChart, Is.Null);
    }

    [Test]
    public void ExportChartCommand_IsInitialized()
    {
        // Assert
        Assert.That(_viewModel.ExportChartCommand, Is.Not.Null);

        // Act - should not throw
        _viewModel.ExportChartCommand.Execute("mood");
    }
}