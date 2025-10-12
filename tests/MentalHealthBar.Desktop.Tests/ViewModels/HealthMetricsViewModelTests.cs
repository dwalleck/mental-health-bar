using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Moq;
using MentalHealthBar.Contracts.Requests.HealthMetrics;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using MentalHealthBar.Desktop.Services;
using MentalHealthBar.Desktop.ViewModels;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

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
    public async Task InitialState_HasCorrectDefaults()
    {
        // Assert
        await Assert.That(_viewModel.SleepHours).IsEqualTo(8.0m);
        await Assert.That(_viewModel.WaterIntakeOz).IsEqualTo(64.0m);
        await Assert.That(_viewModel.RecordedDate).IsEqualTo(DateOnly.FromDateTime(DateTime.Today));
        await Assert.That(_viewModel.IsLoading).IsFalse();
        await Assert.That(_viewModel.StatusMessage).IsNull();
    }

    [Test]
    public async Task SaveSleepCommand_CreatesNewMetric_WhenNotExists()
    {
        // Arrange
        _viewModel.SleepHours = 7.5m;
        _viewModel.RecordedDate = DateOnly.FromDateTime(DateTime.Today);

        var createdMetric = new HealthMetricDto(
            Guid.NewGuid(),
            "SleepHours",
            7.5m,
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.Now,
            null
        );

        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(
                It.Is<RecordHealthMetricRequest>(r =>
                    r.Type == "SleepHours" &&
                    r.Value == 7.5m),
                default))
            .ReturnsAsync(createdMetric);

        // Act
        await _viewModel.SaveSleepCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.RecordHealthMetricAsync(It.IsAny<RecordHealthMetricRequest>(), default), Times.Once);
        await Assert.That(_viewModel.RecentMetrics).Contains(createdMetric);
    }

    [Test]
    public async Task SaveSleepCommand_UpdatesExistingMetric_WhenExists()
    {
        // Arrange
        var existingMetric = new HealthMetricDto(
            Guid.NewGuid(),
            "SleepHours",
            7.0m,
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.Now,
            null
        );

        _viewModel.RecentMetrics.Add(existingMetric);
        _viewModel.SleepHours = 8.5m;
        _viewModel.RecordedDate = DateOnly.FromDateTime(DateTime.Today);

        var updatedMetric = new HealthMetricDto(
            existingMetric.Id,
            "SleepHours",
            8.5m,
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.Now,
            DateTimeOffset.Now
        );

        _apiClientMock.Setup(x => x.UpdateHealthMetricAsync(
                existingMetric.Id,
                It.Is<UpdateHealthMetricRequest>(r => r.Value == 8.5m),
                default))
            .ReturnsAsync(updatedMetric);

        // Act
        await _viewModel.SaveSleepCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.UpdateHealthMetricAsync(existingMetric.Id, It.IsAny<UpdateHealthMetricRequest>(), default), Times.Once);
        await Assert.That(_viewModel.RecentMetrics[0].Value).IsEqualTo(8.5m);
    }

    [Test]
    public async Task SaveWaterCommand_CreatesNewMetric()
    {
        // Arrange
        _viewModel.WaterIntakeOz = 72m;

        var createdMetric = new HealthMetricDto(
            Guid.NewGuid(),
            "WaterIntakeOz",
            72m,
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.Now,
            null
        );

        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(
                It.Is<RecordHealthMetricRequest>(r =>
                    r.Type == "WaterIntakeOz" &&
                    r.Value == 72m),
                default))
            .ReturnsAsync(createdMetric);

        // Act
        await _viewModel.SaveWaterCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.RecordHealthMetricAsync(It.IsAny<RecordHealthMetricRequest>(), default), Times.Once);
        await Assert.That(_viewModel.RecentMetrics).Contains(createdMetric);
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
            .ReturnsAsync(new HealthMetricDto(Guid.NewGuid(), "SleepHours", 7.5m, DateOnly.FromDateTime(DateTime.Today), DateTimeOffset.Now, null));

        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(
                It.Is<RecordHealthMetricRequest>(r => r.Type == "WaterIntakeOz"),
                default))
            .ReturnsAsync(new HealthMetricDto(Guid.NewGuid(), "WaterIntakeOz", 80m, DateOnly.FromDateTime(DateTime.Today), DateTimeOffset.Now, null));

        // Act
        await _viewModel.SaveBothCommand.Execute().FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.RecordHealthMetricAsync(It.IsAny<RecordHealthMetricRequest>(), default), Times.Exactly(2));
    }

    [Test]
    public async Task SaveCommands_CanExecute_BasedOnValidRanges()
    {
        // Valid ranges
        _viewModel.SleepHours = 8m;
        _viewModel.WaterIntakeOz = 64m;
        var canSleep1 = await _viewModel.SaveSleepCommand.CanExecute.FirstAsync();
        var canWater1 = await _viewModel.SaveWaterCommand.CanExecute.FirstAsync();
        await Assert.That(canSleep1).IsTrue();
        await Assert.That(canWater1).IsTrue();

        // Invalid sleep (> 24)
        _viewModel.SleepHours = 25m;
        var canSleep2 = await _viewModel.SaveSleepCommand.CanExecute.FirstAsync();
        await Assert.That(canSleep2).IsFalse();

        // Invalid water (> 200)
        _viewModel.WaterIntakeOz = 201m;
        var canWater2 = await _viewModel.SaveWaterCommand.CanExecute.FirstAsync();
        await Assert.That(canWater2).IsFalse();

        // Negative values
        _viewModel.SleepHours = -1m;
        _viewModel.WaterIntakeOz = -5m;
        var canBoth = await _viewModel.SaveBothCommand.CanExecute.FirstAsync();
        await Assert.That(canBoth).IsFalse();
    }

    [Test]
    public async Task LoadRecentMetrics_PopulatesCollection()
    {
        // Arrange
        var metrics = new List<HealthMetricDto>
        {
            new(Guid.NewGuid(), "SleepHours", 8m, DateOnly.Parse("2025-01-01"), DateTimeOffset.Now, null),
            new(Guid.NewGuid(), "WaterIntakeOz", 64m, DateOnly.Parse("2025-01-01"), DateTimeOffset.Now, null),
            new(Guid.NewGuid(), "SleepHours", 7m, DateOnly.Parse("2025-01-02"), DateTimeOffset.Now, null)
        };

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), 1, 365, default))
            .ReturnsAsync(metrics);

        // Act
        await _viewModel.LoadRecentMetricsCommand.Execute().FirstAsync();

        // Assert
        await Assert.That(_viewModel.RecentMetrics.Count).IsEqualTo(3);
        // Should be sorted by date descending, then by type
        await Assert.That(_viewModel.RecentMetrics[0].RecordedDate).IsEqualTo(DateOnly.Parse("2025-01-02"));
    }

    [Test]
    public async Task RecordedDate_Change_LoadsMetricsForThatDate()
    {
        // Arrange
        var targetDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5));
        var metricsForDate = new List<HealthMetricDto>
        {
            new(Guid.NewGuid(), "SleepHours", 9m, targetDate, DateTimeOffset.Now, null),
            new(Guid.NewGuid(), "WaterIntakeOz", 96m, targetDate, DateTimeOffset.Now, null)
        };

        _apiClientMock.Setup(x => x.GetHealthMetricsHistoryAsync(
                null,
                It.Is<DateTime?>(d => DateOnly.FromDateTime(d.Value) == targetDate),
                It.Is<DateTime?>(d => DateOnly.FromDateTime(d.Value) == targetDate),
                1, 365, default))
            .ReturnsAsync(metricsForDate);

        // Act
        _viewModel.RecordedDate = targetDate;
        // Note: In production code, consider exposing an awaitable task for date changes to avoid delays in tests
        await Task.Delay(100); // Allow reactive property to propagate

        // Assert
        await Assert.That(_viewModel.SleepHours).IsEqualTo(9m);
        await Assert.That(_viewModel.WaterIntakeOz).IsEqualTo(96m);
    }

    [Test]
    public async Task DeleteMetric_RemovesFromCollection()
    {
        // Arrange
        var metricToDelete = new HealthMetricDto(
            Guid.NewGuid(),
            "SleepHours",
            8m,
            DateOnly.Parse("2025-01-01"),
            DateTimeOffset.Now,
            null
        );

        _viewModel.RecentMetrics.Add(metricToDelete);

        _apiClientMock.Setup(x => x.DeleteHealthMetricAsync(metricToDelete.Id, default))
            .Returns(Task.CompletedTask);

        // Act
        await _viewModel.DeleteMetricCommand.Execute(metricToDelete.Id).FirstAsync();

        // Assert
        _apiClientMock.Verify(x => x.DeleteHealthMetricAsync(metricToDelete.Id, default), Times.Once);
        await Assert.That(_viewModel.RecentMetrics).DoesNotContain(metricToDelete);
    }

    [Test]
    public async Task StatusMessage_ShowsAfterSave()
    {
        // Arrange
        var createdMetric = new HealthMetricDto(
            Guid.NewGuid(),
            "SleepHours",
            8m,
            DateOnly.FromDateTime(DateTime.Today),
            DateTimeOffset.Now,
            null
        );

        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(It.IsAny<RecordHealthMetricRequest>(), default))
            .ReturnsAsync(createdMetric);

        // Act
        await _viewModel.SaveSleepCommand.Execute().FirstAsync();

        // Assert - Status should be set to success message
        // Note: Testing auto-clear timing would require mocking the timer mechanism in production code
        await Assert.That(_viewModel.StatusMessage).Contains("successfully");
    }

    [Test]
    public async Task SaveMetric_OnError_ShowsErrorMessage()
    {
        // Arrange
        _apiClientMock.Setup(x => x.RecordHealthMetricAsync(It.IsAny<RecordHealthMetricRequest>(), default))
            .ThrowsAsync(new Exception("API error"));

        // Act
        try
        {
            await _viewModel.SaveSleepCommand.Execute().FirstAsync();
        }
        catch
        {
            // Expected - command may propagate error
        }

        // Assert
        await Assert.That(_viewModel.StatusMessage).Contains("Error");
        await Assert.That(_viewModel.IsLoading).IsFalse();
    }

    [Test]
    public async Task SleepHours_RoundsToOneDecimal()
    {
        // Act
        _viewModel.SleepHours = 7.567m;

        // Assert
        await Assert.That(_viewModel.SleepHours).IsEqualTo(7.6m);
    }

    [Test]
    public async Task WaterIntakeOz_RoundsToOneDecimal()
    {
        // Act
        _viewModel.WaterIntakeOz = 64.234m;

        // Assert
        await Assert.That(_viewModel.WaterIntakeOz).IsEqualTo(64.2m);
    }
}
