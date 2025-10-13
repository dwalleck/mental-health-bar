using System.Net;
using System.Net.Http.Json;
using System.Text;
using MentalHealthBar.Contracts.Responses.EventLabels;
using Microsoft.AspNetCore.Mvc.Testing;
using NodaTime;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Integration;

/// <summary>
/// Integration test for Scenario 9: Export data to CSV and JSON
/// User Story: As a user, I want to export all my data
/// to share with healthcare providers or for backup
/// </summary>
public class ExportTests : IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testId = Guid.NewGuid().ToString("N")[..8]; // Unique suffix for this test run

    public ExportTests()
    {
        _factory = new TestWebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Scenario9_ExportToCsv_IncludesAllDataTypes()
    {
        // Scenario: User clicks "Export to CSV" to get all data
        // Expected: CSV file with assessments, mood entries, and health metrics

        // Arrange: Create event label and sample data
        var now = SystemClock.Instance.GetCurrentInstant();
        var workId = await CreateEventLabel("work");
        await CreateAssessment("PHQ9", 7, now.Minus(Duration.FromDays(7)));
        await CreateMoodEntry(3, now.Minus(Duration.FromDays(5)), new List<Guid> { workId });
        await RecordHealthMetric("SleepHours", 7.0m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)));

        // Arrange: Export request
        var request = new ExportRequest(
            StartDate: now.Minus(Duration.FromDays(30)).ToDateTimeOffset(),
            EndDate: now.ToDateTimeOffset(),
            IncludeAssessments: true,
            IncludeMoodEntries: true,
            IncludeHealthMetrics: true
        );

        // Act: Request CSV export
        var response = await _client.PostAsJsonAsync("/api/export/csv", request, _factory);

        // Assert: Export succeeds
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Assert: Content-Type is CSV
        await Assert.That(response.Content.Headers.ContentType?.MediaType).IsEqualTo("text/csv");

        // Assert: Content-Disposition header suggests filename
        var contentDisposition = response.Content.Headers.ContentDisposition;
        await Assert.That(contentDisposition).IsNotNull();
        await Assert.That(contentDisposition!.DispositionType).IsEqualTo("attachment");
        await Assert.That(contentDisposition.FileName).Contains(".csv");

        // Assert: CSV content includes data
        var csvContent = await response.Content.ReadAsStringAsync();
        await Assert.That(csvContent).IsNotNull();
        await Assert.That(csvContent.Length).IsGreaterThan(0);

        // Verify CSV has headers and data
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        await Assert.That(lines.Length).IsGreaterThan(1); // At least header + 1 data row
    }

    [Test]
    public async Task Scenario9_ExportToJson_StructuredFormat()
    {
        // Scenario: User exports to JSON for programmatic processing
        // Expected: Structured JSON with separate sections for each data type

        // Arrange: Create event label and sample data
        var now = SystemClock.Instance.GetCurrentInstant();
        var exerciseId = await CreateEventLabel("exercise");
        await CreateAssessment("GAD7", 8, now.Minus(Duration.FromDays(5)));
        await CreateMoodEntry(4, now.Minus(Duration.FromDays(2)), new List<Guid> { exerciseId });
        await RecordHealthMetric("WaterIntakeOz", 64.0m, DateOnly.FromDateTime(DateTime.UtcNow));

        // Arrange: Export request
        var request = new ExportRequest(
            StartDate: now.Minus(Duration.FromDays(30)).ToDateTimeOffset(),
            EndDate: now.ToDateTimeOffset(),
            IncludeAssessments: true,
            IncludeMoodEntries: true,
            IncludeHealthMetrics: true
        );

        // Act: Request JSON export
        var response = await _client.PostAsJsonAsync("/api/export/json", request, _factory);

        // Assert: Export succeeds
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Assert: Content-Type is JSON
        await Assert.That(response.Content.Headers.ContentType?.MediaType).Contains("json");

        // Assert: JSON structure is correct
        var exportData = await response.Content.ReadFromJsonAsync<ExportDataDto>(_factory);

        await Assert.That(exportData).IsNotNull();
        await Assert.That(exportData!.Assessments).IsNotNull();
        await Assert.That(exportData.MoodEntries).IsNotNull();
        await Assert.That(exportData.HealthMetrics).IsNotNull();

        // Verify data is present
        await Assert.That(exportData.Assessments.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(exportData.MoodEntries.Count).IsGreaterThanOrEqualTo(1);
        await Assert.That(exportData.HealthMetrics.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task Scenario9_ExportWithDateRange_OnlyIncludesDataInRange()
    {
        // Scenario: User exports only data from past 7 days
        // Expected: Only recent data included

        // Arrange: Create data at different times
        var now = SystemClock.Instance.GetCurrentInstant();
        await CreateMoodEntry(3, now.Minus(Duration.FromDays(30)), new List<Guid>()); // Outside range
        await CreateMoodEntry(4, now.Minus(Duration.FromDays(5)), new List<Guid>());  // Inside range
        await CreateMoodEntry(3, now, new List<Guid>());              // Inside range

        // Arrange: Export request for last 7 days only
        var request = new ExportRequest(
            StartDate: now.Minus(Duration.FromDays(7)).ToDateTimeOffset(),
            EndDate: now.ToDateTimeOffset(),
            IncludeAssessments: true,
            IncludeMoodEntries: true,
            IncludeHealthMetrics: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request, _factory);

        // Assert
        var exportData = await response.Content.ReadFromJsonAsync<ExportDataDto>(_factory);

        await Assert.That(exportData).IsNotNull();

        // Verify all mood entries are within date range
        var sevenDaysAgo = now.Minus(Duration.FromDays(7));
        foreach (var entry in exportData!.MoodEntries)
        {
            await Assert.That(entry.RecordedAt > sevenDaysAgo).IsTrue();
            await Assert.That(entry.RecordedAt <= now).IsTrue();
        }
    }

    [Test]
    public async Task Scenario9_ExportSelectiveDataTypes_OnlyIncludesRequested()
    {
        // Scenario: User wants to export only mood entries (not assessments or health metrics)
        // Expected: Only mood entries in export

        // Arrange: Create event label and all data types
        var now = SystemClock.Instance.GetCurrentInstant();
        var testId = await CreateEventLabel("test");
        await CreateAssessment("PHQ9", 5, now.Minus(Duration.FromDays(5)));
        await CreateMoodEntry(3, now.Minus(Duration.FromDays(2)), new List<Guid> { testId });
        await RecordHealthMetric("SleepHours", 7.0m, DateOnly.FromDateTime(DateTime.UtcNow));

        // Arrange: Export only mood entries
        var request = new ExportRequest(
            StartDate: now.Minus(Duration.FromDays(30)).ToDateTimeOffset(),
            EndDate: now.ToDateTimeOffset(),
            IncludeAssessments: false,      // Exclude
            IncludeMoodEntries: true,       // Include
            IncludeHealthMetrics: false     // Exclude
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request, _factory);

        // Assert
        var exportData = await response.Content.ReadFromJsonAsync<ExportDataDto>(_factory);

        await Assert.That(exportData).IsNotNull();
        await Assert.That(exportData!.MoodEntries.Count).IsGreaterThan(0);
        await Assert.That(exportData.Assessments.Count).IsEqualTo(0);
        await Assert.That(exportData.HealthMetrics.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Scenario9_ExportEmptyData_ReturnsEmptyStructure()
    {
        // Scenario: New user exports data before entering any
        // Expected: Export succeeds with empty collections (not error)

        // Arrange: Export request (no data created)
        var now = SystemClock.Instance.GetCurrentInstant();
        var request = new ExportRequest(
            StartDate: now.Minus(Duration.FromDays(30)).ToDateTimeOffset(),
            EndDate: now.ToDateTimeOffset(),
            IncludeAssessments: true,
            IncludeMoodEntries: true,
            IncludeHealthMetrics: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/json", request, _factory);

        // Assert: Export succeeds (doesn't fail on empty data)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var exportData = await response.Content.ReadFromJsonAsync<ExportDataDto>(_factory);

        // Assert: Empty collections (not null)
        await Assert.That(exportData).IsNotNull();
        await Assert.That(exportData!.Assessments).IsNotNull();
        await Assert.That(exportData.MoodEntries).IsNotNull();
        await Assert.That(exportData.HealthMetrics).IsNotNull();
    }

    // Helper methods
    private async Task<Guid> CreateMoodEntry(int score, Instant recordedAt, List<Guid> eventLabelIds)
    {
        var request = new { MoodScore = score, RecordedAt = recordedAt, EventLabelIds = eventLabelIds, Notes = (string?)null };
        var response = await _client.PostAsJsonAsync("/api/mood-entries", request, _factory);

        // Check for success and provide detailed error information if it fails
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create mood entry. " +
                              $"Status: {response.StatusCode}, " +
                              $"EventLabelIds: {string.Join(", ", eventLabelIds)}, " +
                              $"Response: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<MoodEntryDto>(_factory);
        if (result == null || result.Id == Guid.Empty)
        {
            throw new Exception("Created mood entry but received invalid response");
        }

        return result.Id;
    }

    private async Task<Guid> CreateEventLabel(string name)
    {
        // Add test ID suffix to ensure uniqueness across test runs
        var uniqueName = $"{name}-{_testId}";
        var request = new { Name = uniqueName, Description = (string?)null };

        var response = await _client.PostAsJsonAsync("/api/event-labels", request, _factory);

        // Check for success and provide detailed error information if it fails
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create event label '{uniqueName}'. " +
                              $"Status: {response.StatusCode}, " +
                              $"Response: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<EventLabelDto>(_factory);
        if (result == null || result.Id == Guid.Empty)
        {
            throw new Exception($"Created event label '{uniqueName}' but received invalid response");
        }

        return result!.Id;
    }

    private async Task<Guid> CreateAssessment(string type, int targetScore, Instant completedAt)
    {
        var questionCount = type switch { "PHQ9" => 9, "GAD7" => 7, _ => 9 };
        var responses = new Dictionary<string, int>();
        var scorePerQuestion = targetScore / questionCount;
        var remainder = targetScore % questionCount;

        for (int i = 1; i <= questionCount; i++)
        {
            responses[$"Q{i}"] = Math.Min(scorePerQuestion + (i <= remainder ? 1 : 0), 3);
        }

        var request = new { Type = type, CompletedAt = completedAt, Responses = responses };
        var response = await _client.PostAsJsonAsync("/api/assessments", request, _factory);
        var result = await response.Content.ReadFromJsonAsync<AssessmentResultDto>(_factory);
        return result!.Id;
    }

    private async Task<Guid> RecordHealthMetric(string type, decimal value, DateOnly date)
    {
        var request = new { Type = type, Value = value, RecordedDate = date };
        var response = await _client.PostAsJsonAsync("/api/health-metrics", request, _factory);
        var result = await response.Content.ReadFromJsonAsync<HealthMetricDto>(_factory);
        return result!.Id;
    }

    // DTOs
    private record ExportRequest(
        DateTimeOffset StartDate,
        DateTimeOffset EndDate,
        bool IncludeAssessments,
        bool IncludeMoodEntries,
        bool IncludeHealthMetrics
    );

    private record ExportDataDto(
        List<AssessmentExportDto> Assessments,
        List<MoodEntryExportDto> MoodEntries,
        List<HealthMetricExportDto> HealthMetrics
    );

    private record AssessmentExportDto(Guid Id, string Type, Instant CompletedAt, int TotalScore, string Severity);
    private record MoodEntryExportDto(Guid Id, int MoodScore, Instant RecordedAt, List<EventLabelDto> EventLabels, string? Notes);
    private record HealthMetricExportDto(Guid Id, string Type, decimal Value, DateOnly RecordedDate);

    private record MoodEntryDto(Guid Id, int MoodScore, Instant RecordedAt);
    private record AssessmentResultDto(Guid Id, string Type, int TotalScore, string Severity);
    private record HealthMetricDto(Guid Id, string Type, decimal Value, DateOnly RecordedDate);
}
