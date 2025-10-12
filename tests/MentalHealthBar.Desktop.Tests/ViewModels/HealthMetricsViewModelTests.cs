using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using MentalHealthBar.Desktop.Services;
using MentalHealthBar.Desktop.ViewModels;

namespace MentalHealthBar.Desktop.Tests.ViewModels;

public class HealthMetricsViewModelTests
{
    private readonly Mock<IApiClient> _apiClientMock;
    private readonly HealthMetricsViewModel _viewModel;

    public HealthMetricsViewModelTests()
    {
        _apiClientMock = new Mock<IApiClient>();
        _viewModel = new HealthMetricsViewModel(_apiClientMock.Object);
    }

    [Test]
    public void InitialState_HasCorrectDefaults()
    {
        // Assert
        Assert.That(_viewModel.SleepHours, Is.EqualTo(8.0m));
        Assert.That(_viewModel.WaterIntakeOz, Is.EqualTo(64.0m));
        Assert.That(_viewModel.RecordedDate, Is.EqualTo(DateOnly.FromDateTime(DateTime.Today)));
        Assert.That(_viewModel.IsLoading, Is.False);
        Assert.That(_viewModel.StatusMessage, Is.Null);
    }

    [Test]
    public async Task SaveSleepCommand_CreatesNewMetric_WhenNotExists()
    {
        // Arrange
        _viewModel.SleepHours = 7.5m;
        _viewModel.RecordedDate = DateOnly.FromDateTime(DateTime.Today);

        var createdMetric = new HealthMetricResponse
        {
            Id = Guid.NewGuid(),
            Type = "SleepHours",
            Value = 7.5m,
            RecordedDate = DateTime.Today.ToString("yyyy-MM-dd")
        };

        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(
                It.Is<RecordHealthMetricRequest>(r =>
                    r.Type == "SleepHours" &&
                    r.Value == 7.5m),
                default))
            .ReturnsAsync(createdMetric);

        // Act
        await _viewModel.SaveSleepCommand.Execute();
        await Task.Delay(100); // Allow for async status message update

        // Assert
        _apiClientMock.Verify(x => x.RecordHealthMetricAsync(It.IsAny<RecordHealthMetricRequest>(), default), Times.Once);
        Assert.That(_viewModel.RecentMetrics, Contains.Item(createdMetric));
    }

    [Test]
    public async Task SaveSleepCommand_UpdatesExistingMetric_WhenExists()
    {
        // Arrange
        var existingMetric = new HealthMetricResponse
        {
            Id = Guid.NewGuid(),
            Type = "SleepHours",
            Value = 7.0m,
            RecordedDate = DateTime.Today.ToString("yyyy-MM-dd")
        };

        _viewModel.RecentMetrics.Add(existingMetric);
        _viewModel.SleepHours = 8.5m;
        _viewModel.RecordedDate = DateOnly.FromDateTime(DateTime.Today);

        var updatedMetric = new HealthMetricResponse
        {
            Id = existingMetric.Id,
            Type = "SleepHours",
            Value = 8.5m,
            RecordedDate = DateTime.Today.ToString("yyyy-MM-dd"),
            UpdatedAt = DateTimeOffset.Now
        };

        _apiClientMock.Setup(x => x.UpdateHealthMetricAsync(
                existingMetric.Id,
                It.Is<UpdateHealthMetricRequest>(r => r.Value == 8.5m),
                default))
            .ReturnsAsync(updatedMetric);

        // Act
        await _viewModel.SaveSleepCommand.Execute();

        // Assert
        _apiClientMock.Verify(x => x.UpdateHealthMetricAsync(existingMetric.Id, It.IsAny<UpdateHealthMetricRequest>(), default), Times.Once);
        Assert.That(_viewModel.RecentMetrics[0].Value, Is.EqualTo(8.5m));
    }

    [Test]
    public async Task SaveWaterCommand_CreatesNewMetric()
    {
        // Arrange
        _viewModel.WaterIntakeOz = 72m;

        var createdMetric = new HealthMetricResponse
        {
            Id = Guid.NewGuid(),
            Type = "WaterIntakeOz",
            Value = 72m,
            RecordedDate = DateTime.Today.ToString("yyyy-MM-dd")
        };

        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(
                It.Is<RecordHealthMetricRequest>(r =>
                    r.Type == "WaterIntakeOz" &&
                    r.Value == 72m),
                default))
            .ReturnsAsync(createdMetric);

        // Act
        await _viewModel.SaveWaterCommand.Execute();

        // Assert
        _apiClientMock.Verify(x => x.RecordHealthMetricAsync(It.IsAny<RecordHealthMetricRequest>(), default), Times.Once);
        Assert.That(_viewModel.RecentMetrics, Contains.Item(createdMetric));
    }

    [Test]
    public async Task SaveBothCommand_SavesBothMetrics()
    {
        // Arrange
        _viewModel.SleepHours = 7.5m;
        _viewModel.WaterIntakeOz = 80m;

        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(
                It.Is<RecordHealthMetricRequest>(r => r.Type == "SleepHours"),
                default))
            .ReturnsAsync(new HealthMetricResponse { Id = Guid.NewGuid(), Type = "SleepHours", Value = 7.5m });

        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(
                It.Is<RecordHealthMetricRequest>(r => r.Type == "WaterIntakeOz"),
                default))
            .ReturnsAsync(new HealthMetricResponse { Id = Guid.NewGuid(), Type = "WaterIntakeOz", Value = 80m });

        // Act
        await _viewModel.SaveBothCommand.Execute();

        // Assert
        _apiClientMock.Verify(x => x.RecordHealthMetricAsync(It.IsAny<RecordHealthMetricRequest>(), default), Times.Exactly(2));
    }

    [Test]
    public void SaveCommands_CanExecute_BasedOnValidRanges()
    {
        // Valid ranges
        _viewModel.SleepHours = 8m;
        _viewModel.WaterIntakeOz = 64m;
        Assert.That(_viewModel.SaveSleepCommand.CanExecute().Subscribe(), Is.Not.Null);
        Assert.That(_viewModel.SaveWaterCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Invalid sleep (> 24)
        _viewModel.SleepHours = 25m;
        Assert.That(_viewModel.SaveSleepCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Invalid water (> 200)
        _viewModel.WaterIntakeOz = 201m;
        Assert.That(_viewModel.SaveWaterCommand.CanExecute().Subscribe(), Is.Not.Null);

        // Negative values
        _viewModel.SleepHours = -1m;
        _viewModel.WaterIntakeOz = -5m;
        Assert.That(_viewModel.SaveBothCommand.CanExecute().Subscribe(), Is.Not.Null);
    }

    [Test]
    public async Task LoadRecentMetrics_PopulatesCollection()
    {
        // Arrange
        var metrics = new List<HealthMetricResponse>
        {
            new() { Id = Guid.NewGuid(), Type = "SleepHours", Value = 8m, RecordedDate = "2025-01-01" },
            new() { Id = Guid.NewGuid(), Type = "WaterIntakeOz", Value = 64m, RecordedDate = "2025-01-01" },
            new() { Id = Guid.NewGuid(), Type = "SleepHours", Value = 7m, RecordedDate = "2025-01-02" }
        };

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(metrics);

        // Act
        await _viewModel.LoadRecentMetricsCommand.Execute();

        // Assert
        Assert.That(_viewModel.RecentMetrics, Has.Count.EqualTo(3));
        // Should be sorted by date descending, then by type
        Assert.That(_viewModel.RecentMetrics[0].RecordedDate, Is.EqualTo("2025-01-02"));
    }

    [Test]
    public async Task RecordedDate_Change_LoadsMetricsForThatDate()
    {
        // Arrange
        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5));
        var metricsForDate = new List<HealthMetricResponse>
        {
            new() { Type = "SleepHours", Value = 9m, RecordedDate = targetDate.ToString("yyyy-MM-dd") },
            new() { Type = "WaterIntakeOz", Value = 96m, RecordedDate = targetDate.ToString("yyyy-MM-dd") }
        };

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null,
                It.Is<DateTime?>(d => DateOnly.FromDateTime(d.Value) == targetDate),
                It.Is<DateTime?>(d => DateOnly.FromDateTime(d.Value) == targetDate),
                1, 365, default))
            .ReturnsAsync(metricsForDate);

        // Act
        _viewModel.RecordedDate = targetDate;
        await Task.Delay(100); // Allow async operation to complete

        // Assert
        Assert.That(_viewModel.SleepHours, Is.EqualTo(9m));
        Assert.That(_viewModel.WaterIntakeOz, Is.EqualTo(96m));
    }

    [Test]
    public async Task DeleteMetric_RemovesFromCollection()
    {
        // Arrange
        var metricToDelete = new HealthMetricResponse
        {
            Id = Guid.NewGuid(),
            Type = "SleepHours",
            Value = 8m,
            RecordedDate = "2025-01-01"
        };

        _viewModel.RecentMetrics.Add(metricToDelete);

        _apiClientMock.Setup(x => x.DeleteHealthMetricAsync(metricToDelete.Id, default))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.DeleteMetricCommand.Execute(metricToDelete.Id);

        // Assert
        _apiClientMock.Verify(x => x.DeleteHealthMetricAsync(metricToDelete.Id, default), Times.Once);
        Assert.That(_viewModel.RecentMetrics, Does.Not.Contain(metricToDelete));
    }

    [Test]
    public async Task StatusMessage_ShowsAndClearsAutomatically()
    {
        // Arrange
        var createdMetric = new HealthMetricResponse
        {
            Id = Guid.NewGuid(),
            Type = "SleepHours",
            Value = 8m
        };

        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(It.IsAny<RecordHealthMetricRequest>(), default))
            .ReturnsAsync(createdMetric);

        // Act
        var saveTask = _viewModel.SaveSleepCommand.Execute();

        // Assert - Should show status during save
        await Task.Delay(50);
        Assert.That(_viewModel.StatusMessage, Is.Not.Null);

        await saveTask;

        // Status should be set to success message
        Assert.That(_viewModel.StatusMessage, Does.Contain("successfully"));

        // Wait for auto-clear (3 seconds in implementation)
        await Task.Delay(3100);
        Assert.That(_viewModel.StatusMessage, Is.Null);
    }

    [Test]
    public async Task SaveMetric_OnError_ShowsErrorMessage()
    {
        // Arrange
        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(It.IsAny<RecordHealthMetricRequest>(), default))
            .ThrowsAsync(new Exception("API error"));

        // Act
        await _viewModel.SaveSleepCommand.Execute();

        // Assert
        Assert.That(_viewModel.StatusMessage, Does.Contain("Error"));
        Assert.That(_viewModel.IsLoading, Is.False);
    }

    [Test]
    public void SleepHours_RoundsToOneDecimal()
    {
        // Act
        _viewModel.SleepHours = 7.567m;

        // Assert
        Assert.That(_viewModel.SleepHours, Is.EqualTo(7.6m));
    }

    [Test]
    public void WaterIntakeOz_RoundsToOneDecimal()
    {
        // Act
        _viewModel.WaterIntakeOz = 64.234m;

        // Assert
        Assert.That(_viewModel.WaterIntakeOz, Is.EqualTo(64.2m));
    }
}