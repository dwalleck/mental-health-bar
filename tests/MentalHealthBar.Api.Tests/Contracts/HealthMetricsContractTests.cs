using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NodaTime;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Contracts;

/// <summary>
/// Contract tests for Health Metrics API endpoints
/// Based on contracts/health-metrics.yaml
/// Following TDD: Tests MUST FAIL until endpoints are implemented
/// </summary>
public class HealthMetricsContractTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public HealthMetricsContractTests()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task RecordHealthMetric_ValidRequest_ReturnsCreated()
    {
        // Arrange - Use a unique date based on current time to avoid conflicts
        var uniqueDaysBack = Random.Shared.Next(100, 10000);
        var request = new RecordHealthMetricRequestDto
        {
            Type = "SleepHours",
            Value = 7.5m,
            RecordedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-uniqueDaysBack))
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/health-metrics", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<HealthMetricCreateResponseDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(result.Type).IsEqualTo(request.Type);
        await Assert.That(result.Value).IsEqualTo(request.Value);
    }

    [Test]
    public async Task RecordHealthMetric_InvalidValue_ReturnsBadRequest()
    {
        // Arrange - Invalid sleep hours (exceeds 24)
        var request = new RecordHealthMetricRequestDto
        {
            Type = "SleepHours",
            Value = 30m,  // Invalid
            RecordedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/health-metrics", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task RecordHealthMetric_DuplicateDate_ReturnsConflict()
    {
        // Arrange - Create first metric
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-Random.Shared.Next(100, 10000)));
        var firstRequest = new RecordHealthMetricRequestDto
        {
            Type = "SleepHours",
            Value = 7.5m,
            RecordedDate = date
        };
        await _client.PostAsJsonAsync("/api/health-metrics", firstRequest);

        // Try to create duplicate for same date and type
        var duplicateRequest = new RecordHealthMetricRequestDto
        {
            Type = "SleepHours",
            Value = 8.0m,
            RecordedDate = date
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/health-metrics", duplicateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task GetHealthMetricHistory_WithoutFilters_ReturnsPagedList()
    {
        // Act
        var response = await _client.GetAsync("/api/health-metrics");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<HealthMetricHistoryResponseDto>();
        await Assert.That(history).IsNotNull();
        await Assert.That(history!.Items).IsNotNull();
        await Assert.That(history.TotalCount).IsGreaterThanOrEqualTo(0);
        await Assert.That(history.Page).IsGreaterThan(0);
        await Assert.That(history.PageSize).IsGreaterThan(0);
    }

    [Test]
    public async Task GetHealthMetricHistory_WithTypeFilter_ReturnsFilteredList()
    {
        // Arrange
        var type = "SleepHours";

        // Act
        var response = await _client.GetAsync($"/api/health-metrics?type={type}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<HealthMetricHistoryResponseDto>();
        await Assert.That(history).IsNotNull();
        // All returned items should match the filter
        foreach (var item in history!.Items)
        {
            await Assert.That(item.Type).IsEqualTo(type);
        }
    }

    [Test]
    public async Task GetHealthMetricHistory_WithDateRange_ReturnsFilteredList()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var response = await _client.GetAsync(
            $"/api/health-metrics?startDate={startDate:O}&endDate={endDate:O}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<HealthMetricHistoryResponseDto>();
        await Assert.That(history).IsNotNull();
        // All entries should be within date range
        foreach (var item in history!.Items)
        {
            await Assert.That(item.RecordedDate).IsGreaterThanOrEqualTo(startDate);
            await Assert.That(item.RecordedDate).IsLessThanOrEqualTo(endDate);
        }
    }

    [Test]
    public async Task GetHealthMetricHistory_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        int pageSize = 10;
        int page = 1;

        // Act
        var response = await _client.GetAsync(
            $"/api/health-metrics?pageSize={pageSize}&page={page}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<HealthMetricHistoryResponseDto>();
        await Assert.That(history).IsNotNull();
        await Assert.That(history!.PageSize).IsEqualTo(pageSize);
        await Assert.That(history.Page).IsEqualTo(page);
        await Assert.That(history.Items.Count).IsLessThanOrEqualTo(pageSize);
    }

    [Test]
    public async Task GetHealthMetric_ValidId_ReturnsHealthMetric()
    {
        // Arrange - Create metric first
        var createRequest = new RecordHealthMetricRequestDto
        {
            Type = "WaterIntakeOz",
            Value = 64.0m,
            RecordedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-Random.Shared.Next(100, 10000)))
        };
        var createResponse = await _client.PostAsJsonAsync("/api/health-metrics", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<HealthMetricCreateResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/health-metrics/{created!.Id}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var metric = await response.Content.ReadFromJsonAsync<HealthMetricDetailResponseDto>();
        await Assert.That(metric).IsNotNull();
        await Assert.That(metric!.Id).IsEqualTo(created.Id);
        await Assert.That(metric.Type).IsEqualTo(created.Type);
        await Assert.That(metric.Value).IsEqualTo(created.Value);
    }

    [Test]
    public async Task GetHealthMetric_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/health-metrics/{invalidId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateHealthMetric_ValidRequest_ReturnsOk()
    {
        // Arrange - Create metric first
        var createRequest = new RecordHealthMetricRequestDto
        {
            Type = "SleepHours",
            Value = 6.5m,
            RecordedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-Random.Shared.Next(100, 10000)))
        };
        var createResponse = await _client.PostAsJsonAsync("/api/health-metrics", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<HealthMetricCreateResponseDto>();

        var updateRequest = new UpdateHealthMetricRequestDto
        {
            Value = 7.0m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/health-metrics/{created!.Id}", updateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<HealthMetricDetailResponseDto>();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Value).IsEqualTo(updateRequest.Value);
        await Assert.That(updated.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task UpdateHealthMetric_InvalidValue_ReturnsBadRequest()
    {
        // Arrange - Create metric first
        var createRequest = new RecordHealthMetricRequestDto
        {
            Type = "SleepHours",
            Value = 7.0m,
            RecordedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-Random.Shared.Next(100, 10000)))
        };
        var createResponse = await _client.PostAsJsonAsync("/api/health-metrics", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<HealthMetricCreateResponseDto>();

        var updateRequest = new UpdateHealthMetricRequestDto
        {
            Value = -5.0m  // Invalid negative value
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/health-metrics/{created!.Id}", updateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateHealthMetric_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        var updateRequest = new UpdateHealthMetricRequestDto
        {
            Value = 8.0m
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/health-metrics/{invalidId}", updateRequest);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteHealthMetric_ValidId_ReturnsNoContent()
    {
        // Arrange - Create metric first
        var createRequest = new RecordHealthMetricRequestDto
        {
            Type = "WaterIntakeOz",
            Value = 48.0m,
            RecordedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-Random.Shared.Next(100, 10000)))
        };
        var createResponse = await _client.PostAsJsonAsync("/api/health-metrics", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<HealthMetricCreateResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/health-metrics/{created!.Id}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteHealthMetric_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/health-metrics/{invalidId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

// DTOs matching the OpenAPI contract
public record RecordHealthMetricRequestDto
{
    public string Type { get; init; } = "";
    public decimal Value { get; init; }
    public DateOnly RecordedDate { get; init; }
}

public record UpdateHealthMetricRequestDto
{
    public decimal Value { get; init; }
}

// Response DTO for Record endpoint (no UpdatedAt)
public record HealthMetricCreateResponseDto(
    Guid Id,
    string Type,
    decimal Value,
    DateOnly RecordedDate,
    Instant CreatedAt
);

// Response DTO for GetById and Update endpoints (has UpdatedAt)
public record HealthMetricDetailResponseDto(
    Guid Id,
    string Type,
    decimal Value,
    DateOnly RecordedDate,
    Instant CreatedAt,
    Instant? UpdatedAt
);

// Response DTO for history items (no UpdatedAt)
public record HealthMetricSummaryDto(
    Guid Id,
    string Type,
    decimal Value,
    DateOnly RecordedDate,
    Instant CreatedAt
);

public record HealthMetricHistoryResponseDto(
    List<HealthMetricSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
