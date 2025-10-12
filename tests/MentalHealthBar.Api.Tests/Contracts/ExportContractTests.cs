using System.Net;
using System.Net.Http.Json;
using MentalHealthBar.Contracts.Responses.EventLabels;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Contracts;

/// <summary>
/// Contract tests for Export API endpoints
/// Based on contracts/export.yaml
/// Following TDD: Tests MUST FAIL until endpoints are implemented
/// </summary>
public class ExportContractTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExportContractTests()
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
    public async Task ExportToCsv_DefaultRequest_ReturnsCsvFile()
    {
        // Arrange
        var request = new ExportRequestDto();

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/csv", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/csv");

        // Verify Content-Disposition header
        var contentDisposition = response.Content.Headers.ContentDisposition;
        await Assert.That(contentDisposition).IsNotNull();
        await Assert.That(contentDisposition!.DispositionType).IsEqualTo("attachment");
        await Assert.That(contentDisposition.FileName).Contains("mental-health-data");
        await Assert.That(contentDisposition.FileName).Contains(".csv");
    }

    [Test]
    public async Task ExportToCsv_WithDateRange_ReturnsCsvFile()
    {
        // Arrange
        var request = new ExportRequestDto
        {
            StartDate = DateTimeOffset.UtcNow.AddDays(-30),
            EndDate = DateTimeOffset.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/csv", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/csv");

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).IsNotNull();
    }

    [Test]
    public async Task ExportToCsv_WithSelectiveIncludes_ReturnsCsvFile()
    {
        // Arrange
        var request = new ExportRequestDto
        {
            IncludeAssessments = true,
            IncludeMoodEntries = false,
            IncludeHealthMetrics = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/csv", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/csv");
    }

    [Test]
    public async Task ExportToCsv_InvalidDateRange_ReturnsBadRequest()
    {
        // Arrange - EndDate before StartDate
        var request = new ExportRequestDto
        {
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(-30)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/csv", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ExportToJson_DefaultRequest_ReturnsJsonFile()
    {
        // Arrange
        var request = new ExportRequestDto();

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request);

        // Assert - Temporarily check error content
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Export failed with status {response.StatusCode}: {errorContent}");
        }
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");

        // Verify Content-Disposition header
        var contentDisposition = response.Content.Headers.ContentDisposition;
        await Assert.That(contentDisposition).IsNotNull();
        await Assert.That(contentDisposition!.DispositionType).IsEqualTo("attachment");
        await Assert.That(contentDisposition.FileName).Contains("mental-health-data");
        await Assert.That(contentDisposition.FileName).Contains(".json");

        var exportData = await response.Content.ReadFromJsonAsync<ExportDataResponseDto>();
        await Assert.That(exportData).IsNotNull();
        await Assert.That(exportData!.ExportedAt).IsGreaterThan(DateTimeOffset.MinValue);
        await Assert.That(exportData.DateRange).IsNotNull();
        await Assert.That(exportData.Assessments).IsNotNull();
        await Assert.That(exportData.MoodEntries).IsNotNull();
        await Assert.That(exportData.HealthMetrics).IsNotNull();
        await Assert.That(exportData.EventLabels).IsNotNull();
    }

    [Test]
    public async Task ExportToJson_WithDateRange_ReturnsFilteredData()
    {
        // Arrange
        var startDate = DateTimeOffset.UtcNow.AddDays(-7);
        var endDate = DateTimeOffset.UtcNow;
        var request = new ExportRequestDto
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var exportData = await response.Content.ReadFromJsonAsync<ExportDataResponseDto>();
        await Assert.That(exportData).IsNotNull();
        await Assert.That(exportData!.DateRange.Start).IsEqualTo(startDate);
        await Assert.That(exportData.DateRange.End).IsEqualTo(endDate);
    }

    [Test]
    public async Task ExportToJson_WithSelectiveIncludes_ReturnsFilteredData()
    {
        // Arrange - Only include mood entries
        var request = new ExportRequestDto
        {
            IncludeAssessments = false,
            IncludeMoodEntries = true,
            IncludeHealthMetrics = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var exportData = await response.Content.ReadFromJsonAsync<ExportDataResponseDto>();
        await Assert.That(exportData).IsNotNull();
        // Note: Even excluded items should be present as empty arrays
        await Assert.That(exportData!.Assessments).IsNotNull();
        await Assert.That(exportData.MoodEntries).IsNotNull();
        await Assert.That(exportData.HealthMetrics).IsNotNull();
    }

    [Test]
    public async Task ExportToJson_InvalidDateRange_ReturnsBadRequest()
    {
        // Arrange - EndDate before StartDate
        var request = new ExportRequestDto
        {
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(-30)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ExportToJson_VerifyDataStructure_ContainsAllFields()
    {
        // Arrange
        var request = new ExportRequestDto();

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var exportData = await response.Content.ReadFromJsonAsync<ExportDataResponseDto>();
        await Assert.That(exportData).IsNotNull();

        // Verify required fields are present
        await Assert.That(exportData!.ExportedAt).IsGreaterThan(DateTimeOffset.MinValue);
        await Assert.That(exportData.DateRange).IsNotNull();
        await Assert.That(exportData.DateRange.Start).IsGreaterThan(DateTimeOffset.MinValue);
        await Assert.That(exportData.DateRange.End).IsGreaterThan(DateTimeOffset.MinValue);
        await Assert.That(exportData.Assessments).IsNotNull();
        await Assert.That(exportData.MoodEntries).IsNotNull();
        await Assert.That(exportData.HealthMetrics).IsNotNull();
        await Assert.That(exportData.EventLabels).IsNotNull();
    }

    [Test]
    public async Task ExportToJson_VerifyAssessmentStructure()
    {
        // Arrange - This test assumes there's at least one assessment
        var request = new ExportRequestDto { IncludeAssessments = true };

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var exportData = await response.Content.ReadFromJsonAsync<ExportDataResponseDto>();

        // If there are assessments, verify their structure
        if (exportData!.Assessments.Count > 0)
        {
            var assessment = exportData.Assessments[0];
            await Assert.That(assessment.Id).IsNotEqualTo(Guid.Empty);
            await Assert.That(assessment.Type).IsNotNull();
            await Assert.That(assessment.CompletedAt).IsGreaterThan(DateTimeOffset.MinValue);
            await Assert.That(assessment.TotalScore).IsGreaterThanOrEqualTo(0);
            await Assert.That(assessment.Severity).IsNotNull();
            await Assert.That(assessment.Responses).IsNotNull();
        }
    }

    [Test]
    public async Task ExportToJson_VerifyMoodEntryStructure()
    {
        // Arrange
        var request = new ExportRequestDto { IncludeMoodEntries = true };

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var exportData = await response.Content.ReadFromJsonAsync<ExportDataResponseDto>();

        // If there are mood entries, verify their structure
        if (exportData!.MoodEntries.Count > 0)
        {
            var entry = exportData.MoodEntries[0];
            await Assert.That(entry.Id).IsNotEqualTo(Guid.Empty);
            await Assert.That(entry.MoodScore).IsGreaterThan(0);
            await Assert.That(entry.MoodLabel).IsNotNull();
            await Assert.That(entry.RecordedAt).IsGreaterThan(DateTimeOffset.MinValue);
            await Assert.That(entry.EventLabelNames).IsNotNull();
        }
    }

    [Test]
    public async Task ExportToJson_VerifyHealthMetricStructure()
    {
        // Arrange
        var request = new ExportRequestDto { IncludeHealthMetrics = true };

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var exportData = await response.Content.ReadFromJsonAsync<ExportDataResponseDto>();

        // If there are health metrics, verify their structure
        if (exportData!.HealthMetrics.Count > 0)
        {
            var metric = exportData.HealthMetrics[0];
            await Assert.That(metric.Id).IsNotEqualTo(Guid.Empty);
            await Assert.That(metric.Type).IsNotNull();
            await Assert.That(metric.Value).IsGreaterThanOrEqualTo(0);
            await Assert.That(metric.RecordedDate).IsGreaterThan(DateOnly.MinValue);
        }
    }
}

// DTOs matching the OpenAPI contract
public record ExportRequestDto
{
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public bool IncludeAssessments { get; init; } = true;
    public bool IncludeMoodEntries { get; init; } = true;
    public bool IncludeHealthMetrics { get; init; } = true;
    public bool IncludeEventLabels { get; init; } = true;
}

public record ExportDataResponseDto(
    List<ExportedAssessmentDto> Assessments,
    List<ExportedMoodEntryDto> MoodEntries,
    List<ExportedHealthMetricDto> HealthMetrics,
    List<string> EventLabels,
    DateRangeDto DateRange,
    DateTimeOffset ExportedAt
);

public record DateRangeDto(
    DateTimeOffset Start,
    DateTimeOffset End
);

public record ExportedAssessmentDto(
    Guid Id,
    string Type,
    Dictionary<string, int> Responses,
    int TotalScore,
    string Severity,
    DateTimeOffset CompletedAt,
    DateTimeOffset CreatedAt
);

public record ExportedMoodEntryDto(
    Guid Id,
    int MoodScore,
    string MoodLabel,
    DateTimeOffset RecordedAt,
    List<string> EventLabelNames,
    string? Notes,
    DateTimeOffset CreatedAt
);

public record ExportedHealthMetricDto(
    Guid Id,
    string Type,
    decimal Value,
    DateOnly RecordedDate,
    DateTimeOffset CreatedAt
);
