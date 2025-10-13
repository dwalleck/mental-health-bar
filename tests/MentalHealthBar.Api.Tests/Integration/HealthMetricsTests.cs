using System.Net;
using System.Net.Http.Json;
using Bogus;
using MentalHealthBar.Contracts.Requests.HealthMetrics;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Integration;

/// <summary>
/// Integration test for Scenario 6: Record health metrics
/// User Story: As a user, I want to log daily health metrics (sleep, water intake)
/// to identify correlations with mood
/// </summary>
public class HealthMetricsTests : IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Faker _faker;

    public HealthMetricsTests()
    {
        _factory = new TestWebApplicationFactory<Program>();
        _client = _factory.CreateClient();
        _faker = new Faker();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Scenario6_RecordSleepAndWater_SameDate_BothSaved()
    {
        // Scenario: User logs both sleep hours and water intake for today
        // Expected: Both metrics saved with same date (unique constraint per type+date)

        // Use a unique date in the past to avoid conflicts
        var uniqueDate = DateOnly.FromDateTime(_faker.Date.Between(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(-1)));

        // Arrange: Sleep metric
        var sleepRequest = new RecordHealthMetricRequest(
            Type: "SleepHours",
            Value: 7.5m,
            RecordedDate: uniqueDate
        );

        // Act: User records sleep hours
        var sleepResponse = await _client.PostAsJsonAsync("/api/health-metrics", sleepRequest, _factory);

        // Assert: Sleep metric saved
        await Assert.That(sleepResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var sleepResult = await sleepResponse.Content.ReadFromJsonAsync<HealthMetricDto>(_factory);
        await Assert.That(sleepResult!.Value).IsEqualTo(7.5m);

        // Arrange: Water intake metric
        var waterRequest = new RecordHealthMetricRequest(
            Type: "WaterIntakeOz",
            Value: 64.0m,
            RecordedDate: uniqueDate
        );

        // Act: User records water intake
        var waterResponse = await _client.PostAsJsonAsync("/api/health-metrics", waterRequest, _factory);

        // Assert: Water metric saved (no conflict, different type)
        await Assert.That(waterResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var waterResult = await waterResponse.Content.ReadFromJsonAsync<HealthMetricDto>(_factory);
        await Assert.That(waterResult!.Value).IsEqualTo(64.0m);
    }

    [Test]
    public async Task Scenario6_RecordDuplicateMetric_SameTypeAndDate_ReturnsConflict()
    {
        // Scenario: User accidentally records sleep hours twice for same date
        // Expected: System rejects duplicate with conflict error

        var uniqueDate = DateOnly.FromDateTime(_faker.Date.Between(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(-1)));

        // Arrange: Record sleep once
        var request1 = new RecordHealthMetricRequest(
            Type: "SleepHours",
            Value: 7.0m,
            RecordedDate: uniqueDate
        );
        await _client.PostAsJsonAsync("/api/health-metrics", request1, _factory);

        // Act: Attempt to record sleep again for same date
        var request2 = new RecordHealthMetricRequest(
            Type: "SleepHours",
            Value: 8.0m,  // Different value, but same type+date
            RecordedDate: uniqueDate
        );
        var response = await _client.PostAsJsonAsync("/api/health-metrics", request2, _factory);

        // Assert: Conflict error
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Scenario6_UpdateHealthMetric_ChangesValue()
    {
        // Scenario: User realizes they logged wrong sleep hours
        // Expected: Can update value (type and date immutable)

        var uniqueDate = DateOnly.FromDateTime(_faker.Date.Between(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(-1)));

        // Arrange: Record initial metric
        var createRequest = new RecordHealthMetricRequest(
            Type: "SleepHours",
            Value: 6.0m,
            RecordedDate: uniqueDate
        );

        var createResponse = await _client.PostAsJsonAsync("/api/health-metrics", createRequest, _factory);
        var created = await createResponse.Content.ReadFromJsonAsync<HealthMetricDto>(_factory);

        // Act: Update to correct value
        var updateRequest = new UpdateHealthMetricRequest(Value: 7.5m);
        var updateResponse = await _client.PutAsJsonAsync($"/api/health-metrics/{created!.Id}", updateRequest, _factory);

        // Assert: Update succeeds
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Verify updated value
        var getResponse = await _client.GetAsync($"/api/health-metrics/{created.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<HealthMetricDto>(_factory);

        await Assert.That(updated!.Value).IsEqualTo(7.5m);
        await Assert.That(updated.Type).IsEqualTo("SleepHours"); // Type unchanged
        await Assert.That(updated.RecordedDate).IsEqualTo(uniqueDate); // Date unchanged
    }

    [Test]
    public async Task Scenario6_RecordInvalidSleepHours_ReturnsBadRequest()
    {
        // Scenario: User enters impossible sleep hours (>24)
        // Expected: System rejects with validation error

        // Arrange: Invalid sleep value
        var request = new RecordHealthMetricRequest(
            Type: "SleepHours",
            Value: 25.0m,  // Invalid (max 24)
            RecordedDate: DateOnly.FromDateTime(_faker.Date.Between(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(-1)))
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/health-metrics", request, _factory);

        // Assert: Validation error
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Scenario6_RecordInvalidWaterIntake_ReturnsBadRequest()
    {
        // Scenario: User enters unrealistic water intake (>200 oz)
        // Expected: System rejects with validation error

        // Arrange: Invalid water value
        var request = new RecordHealthMetricRequest(
            Type: "WaterIntakeOz",
            Value: 250.0m,  // Invalid (max 200)
            RecordedDate: DateOnly.FromDateTime(_faker.Date.Between(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(-1)))
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/health-metrics", request, _factory);

        // Assert: Validation error
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Scenario6_ViewHealthMetricsHistory_FilteredByType()
    {
        // Scenario: User wants to see sleep history for past week
        // Expected: Returns only sleep metrics within date range

        // Arrange: Record metrics over several days with unique dates
        var baseDate = _faker.Date.Between(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(-10));

        await RecordMetric("SleepHours", 7.0m, DateOnly.FromDateTime(baseDate.AddDays(-7)));
        await RecordMetric("SleepHours", 6.5m, DateOnly.FromDateTime(baseDate.AddDays(-5)));
        await RecordMetric("SleepHours", 8.0m, DateOnly.FromDateTime(baseDate.AddDays(-2)));
        await RecordMetric("WaterIntakeOz", 64.0m, DateOnly.FromDateTime(baseDate.AddDays(-3))); // Should not appear

        // Act: Request sleep metrics only
        var response = await _client.GetAsync("/api/health-metrics?type=SleepHours");

        // Assert: Only sleep metrics returned
        var pagedResult = await response.Content.ReadFromJsonAsync<HealthMetricPagedResultDto>(_factory);
        var metrics = pagedResult!.Items;

        await Assert.That(metrics).IsNotNull();
        var sleepMetrics = metrics.Where(m => m.Type == "SleepHours").ToList();
        var otherMetrics = metrics.Where(m => m.Type != "SleepHours").ToList();

        await Assert.That(sleepMetrics.Count).IsGreaterThanOrEqualTo(3);
        await Assert.That(otherMetrics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Scenario6_DeleteHealthMetric_SoftDeletes()
    {
        // Scenario: User wants to remove incorrect health metric entry
        // Expected: Metric is soft deleted

        // Arrange: Create metric
        var request = new RecordHealthMetricRequest(
            Type: "SleepHours",
            Value: 5.0m,
            RecordedDate: DateOnly.FromDateTime(_faker.Date.Between(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(-1)))
        );

        var createResponse = await _client.PostAsJsonAsync("/api/health-metrics", request, _factory);
        var created = await createResponse.Content.ReadFromJsonAsync<HealthMetricDto>(_factory);

        // Act: Delete metric
        var deleteResponse = await _client.DeleteAsync($"/api/health-metrics/{created!.Id}");

        // Assert: Delete succeeds
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Assert: Metric no longer retrievable
        var getResponse = await _client.GetAsync($"/api/health-metrics/{created.Id}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Scenario6_RecordZeroValues_AllowsZero()
    {
        // Scenario: User had insomnia (0 hours sleep) or forgot to drink water (0 oz)
        // Expected: Zero values are valid and saved

        var uniqueDate = DateOnly.FromDateTime(_faker.Date.Between(DateTime.UtcNow.AddYears(-1), DateTime.UtcNow.AddDays(-1)));

        // Arrange: Zero sleep (insomnia)
        var request = new RecordHealthMetricRequest(
            Type: "SleepHours",
            Value: 0.0m,
            RecordedDate: uniqueDate
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/health-metrics", request, _factory);

        // Assert: Zero is valid
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<HealthMetricDto>(_factory);
        await Assert.That(result!.Value).IsEqualTo(0.0m);
    }

    // Helper method
    private async Task<Guid> RecordMetric(string type, decimal value, DateOnly date)
    {
        var request = new RecordHealthMetricRequest(
            Type: type,
            Value: value,
            RecordedDate: date
        );

        var response = await _client.PostAsJsonAsync("/api/health-metrics", request, _factory);
        var result = await response.Content.ReadFromJsonAsync<HealthMetricDto>(_factory);
        return result!.Id;
    }

    // DTOs
    private record RecordHealthMetricRequest(
        string Type,
        decimal Value,
        DateOnly RecordedDate
    );

    private record UpdateHealthMetricRequest(decimal Value);

    private record HealthMetricDto(
        Guid Id,
        string Type,
        decimal Value,
        DateOnly RecordedDate
    );
}
