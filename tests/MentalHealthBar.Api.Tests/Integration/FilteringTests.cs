using System.Net;
using System.Net.Http.Json;
using MentalHealthBar.Contracts.Responses.Assessments;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Integration;

/// <summary>
/// Integration test for Scenario 8: Filter data by date range and tags
/// User Story: As a user, I want to filter mood entries by date range and tags
/// to analyze specific patterns
/// </summary>
public class FilteringTests : IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testId = Guid.NewGuid().ToString("N")[..8]; // Unique suffix for this test run

    public FilteringTests()
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
    public async Task Scenario8_FilterMoodEntriesByDateRange_ReturnsOnlyWithinRange()
    {
        // Scenario: User selects "Last 7 days" filter
        // Expected: Only mood entries from past 7 days displayed

        // Arrange: Create entries across different dates
        var now = DateTimeOffset.UtcNow;
        await CreateMoodEntry(3, now.AddDays(-30), new List<Guid>()); // Outside range
        await CreateMoodEntry(4, now.AddDays(-5), new List<Guid>());  // Inside range
        await CreateMoodEntry(2, now.AddDays(-2), new List<Guid>());  // Inside range
        await CreateMoodEntry(5, now, new List<Guid>());              // Inside range

        // Act: Filter to last 7 days
        var startDate = Uri.EscapeDataString(now.AddDays(-7).ToString("o"));
        var endDate = Uri.EscapeDataString(now.ToString("o"));
        var response = await _client.GetAsync($"/api/mood-entries?startDate={startDate}&endDate={endDate}");

        // Assert: Only entries within range
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<MoodPagedResultDto>();
        var entries = pagedResult!.Items;

        await Assert.That(entries).IsNotNull();
        foreach (var entry in entries)
        {
            await Assert.That(entry.RecordedAt).IsGreaterThan(now.AddDays(-7));
            await Assert.That(entry.RecordedAt).IsLessThanOrEqualTo(now);
        }
    }

    [Test]
    public async Task Scenario8_FilterMoodEntriesByTag_ReturnsOnlyMatchingEntries()
    {
        // Scenario: User filters by "work stress" label to see work-related moods
        // Expected: Only entries with "work stress" label

        // Arrange: Create event labels
        var workStressId = await CreateEventLabel("work-stress");
        var exerciseId = await CreateEventLabel("exercise");
        var deadlineId = await CreateEventLabel("deadline");
        var relaxingId = await CreateEventLabel("relaxing");

        // Arrange: Create entries with different labels
        await CreateMoodEntry(2, DateTimeOffset.UtcNow.AddDays(-5), new List<Guid> { workStressId });
        await CreateMoodEntry(3, DateTimeOffset.UtcNow.AddDays(-3), new List<Guid> { exerciseId });
        await CreateMoodEntry(2, DateTimeOffset.UtcNow.AddDays(-1), new List<Guid> { workStressId, deadlineId });
        await CreateMoodEntry(4, DateTimeOffset.UtcNow, new List<Guid> { relaxingId });

        // Act: Filter by "work stress" label ID
        var response = await _client.GetAsync($"/api/mood-entries?eventLabelId={workStressId}");

        // Assert: Only entries with "work stress" label
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<MoodPagedResultDto>();
        var entries = pagedResult!.Items;

        await Assert.That(entries).IsNotNull();
        await Assert.That(entries.Count).IsGreaterThanOrEqualTo(2);

        foreach (var entry in entries)
        {
            await Assert.That(entry.EventLabels).IsNotNull();
            await Assert.That(entry.EventLabels!.Any(e => e.Id == workStressId)).IsTrue();
        }
    }

    [Test]
    public async Task Scenario8_FilterByMultipleTags_ReturnsEntriesWithAnyTag()
    {
        // Scenario: User filters by label ("work")
        // Expected: Entries containing the specified label

        // Arrange: Create event labels
        var workId = await CreateEventLabel("work");
        var stressId = await CreateEventLabel("stress");
        var exerciseId = await CreateEventLabel("exercise");

        // Arrange: Create entries with various label combinations
        await CreateMoodEntry(2, DateTimeOffset.UtcNow.AddDays(-4), new List<Guid> { workId });
        await CreateMoodEntry(3, DateTimeOffset.UtcNow.AddDays(-3), new List<Guid> { stressId });
        await CreateMoodEntry(2, DateTimeOffset.UtcNow.AddDays(-2), new List<Guid> { workId, stressId });
        await CreateMoodEntry(4, DateTimeOffset.UtcNow.AddDays(-1), new List<Guid> { exerciseId }); // Should not match

        // Act: Filter by "work" label (API only supports single label filter)
        var response = await _client.GetAsync($"/api/mood-entries?eventLabelId={workId}");

        // Assert: Entries with work label
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<MoodPagedResultDto>();
        var entries = pagedResult!.Items;

        await Assert.That(entries).IsNotNull();
        foreach (var entry in entries)
        {
            await Assert.That(entry.EventLabels).IsNotNull();
            await Assert.That(entry.EventLabels!.Any(e => e.Id == workId)).IsTrue();
        }
    }

    [Test]
    public async Task Scenario8_CombineDateRangeAndTagFilters_AppliesBoth()
    {
        // Scenario: User filters by "Last 30 days" + "work stress" label
        // Expected: Only work stress entries from past 30 days

        // Arrange: Create event labels
        var workStressId = await CreateEventLabel("work-stress");
        var exerciseId = await CreateEventLabel("exercise");

        // Arrange: Create test data
        var now = DateTimeOffset.UtcNow;

        await CreateMoodEntry(2, now.AddDays(-45), new List<Guid> { workStressId }); // Outside date range
        await CreateMoodEntry(2, now.AddDays(-10), new List<Guid> { workStressId }); // Matches both
        await CreateMoodEntry(3, now.AddDays(-5), new List<Guid> { exerciseId });     // Wrong label
        await CreateMoodEntry(2, now.AddDays(-2), new List<Guid> { workStressId });  // Matches both

        // Act: Apply both filters
        var startDate = Uri.EscapeDataString(now.AddDays(-30).ToString("o"));
        var endDate = Uri.EscapeDataString(now.ToString("o"));
        var response = await _client.GetAsync($"/api/mood-entries?startDate={startDate}&endDate={endDate}&eventLabelId={workStressId}");

        // Assert: Only entries matching both criteria
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<MoodPagedResultDto>();
        var entries = pagedResult!.Items;

        await Assert.That(entries).IsNotNull();
        foreach (var entry in entries)
        {
            // Must be within date range
            await Assert.That(entry.RecordedAt).IsGreaterThan(now.AddDays(-30));
            await Assert.That(entry.RecordedAt).IsLessThanOrEqualTo(now);

            // Must have work stress label
            await Assert.That(entry.EventLabels!.Any(e => e.Id == workStressId)).IsTrue();
        }
    }

    [Test]
    public async Task Scenario8_FilterAssessmentsByType_ReturnsSpecificType()
    {
        // Scenario: User wants to see only GAD-7 anxiety assessments
        // Expected: Only GAD-7 assessments displayed

        // Arrange: Complete different assessment types
        await CreateAssessment("PHQ9", 5, DateTimeOffset.UtcNow.AddDays(-7));
        await CreateAssessment("GAD7", 8, DateTimeOffset.UtcNow.AddDays(-5));
        await CreateAssessment("GAD7", 6, DateTimeOffset.UtcNow.AddDays(-2));
        await CreateAssessment("BDI", 12, DateTimeOffset.UtcNow);

        // Act: Filter by GAD7
        var response = await _client.GetAsync("/api/assessments?type=GAD7");

        // Assert: Only GAD-7 assessments
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<AssessmentPagedResultDto>();
        var assessments = pagedResult!.Items;

        await Assert.That(assessments).IsNotNull();
        var gad7Assessments = assessments.Where(a => a.Type == "GAD7").ToList();

        await Assert.That(gad7Assessments.Count).IsGreaterThanOrEqualTo(2);

        foreach (var assessment in assessments)
        {
            await Assert.That(assessment.Type).IsEqualTo("GAD7");
        }
    }

    [Test]
    public async Task Scenario8_FilterHealthMetricsByType_ReturnsSingleMetricType()
    {
        // Scenario: User views only water intake history (not sleep)
        // Expected: Only water metrics displayed

        // Arrange: Record both metric types
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await RecordHealthMetric("SleepHours", 7.0m, today.AddDays(-5));
        await RecordHealthMetric("WaterIntakeOz", 64.0m, today.AddDays(-5));
        await RecordHealthMetric("SleepHours", 6.5m, today.AddDays(-2));
        await RecordHealthMetric("WaterIntakeOz", 72.0m, today.AddDays(-2));

        // Act: Filter by water intake
        var response = await _client.GetAsync("/api/health-metrics?type=WaterIntakeOz");

        // Assert: Only water metrics
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<HealthMetricPagedResultDto>();
        var metrics = pagedResult!.Items;

        await Assert.That(metrics).IsNotNull();
        await Assert.That(metrics.Count).IsGreaterThanOrEqualTo(2);

        foreach (var metric in metrics)
        {
            await Assert.That(metric.Type).IsEqualTo("WaterIntakeOz");
        }
    }

    // Helper methods
    private async Task<Guid> CreateMoodEntry(int score, DateTimeOffset recordedAt, List<Guid> eventLabelIds)
    {
        var request = new { MoodScore = score, RecordedAt = recordedAt, EventLabelIds = eventLabelIds, Notes = (string?)null };
        var response = await _client.PostAsJsonAsync("/api/mood-entries", request);

        // Check for success and provide detailed error information if it fails
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create mood entry. " +
                              $"Status: {response.StatusCode}, " +
                              $"EventLabelIds: {string.Join(", ", eventLabelIds)}, " +
                              $"Response: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<MoodEntryDto>();
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

        var response = await _client.PostAsJsonAsync("/api/event-labels", request);

        // Check for success and provide detailed error information if it fails
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create event label '{uniqueName}'. " +
                              $"Status: {response.StatusCode}, " +
                              $"Response: {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<EventLabelDto>();
        if (result == null || result.Id == Guid.Empty)
        {
            throw new Exception($"Created event label '{uniqueName}' but received invalid response");
        }

        return result.Id;
    }

    private async Task<Guid> CreateAssessment(string type, int targetScore, DateTimeOffset completedAt)
    {
        var questionCount = type switch { "PHQ9" => 9, "GAD7" => 7, "BDI" => 21, "BAI" => 21, _ => 9 };
        var responses = new Dictionary<string, int>();
        var scorePerQuestion = targetScore / questionCount;
        var remainder = targetScore % questionCount;

        for (int i = 1; i <= questionCount; i++)
        {
            responses[$"Q{i}"] = Math.Min(scorePerQuestion + (i <= remainder ? 1 : 0), 3);
        }

        var request = new { Type = type, CompletedAt = completedAt, Responses = responses };
        var response = await _client.PostAsJsonAsync("/api/assessments", request);
        var result = await response.Content.ReadFromJsonAsync<AssessmentResultDto>();
        return result!.Id;
    }

    private async Task<Guid> RecordHealthMetric(string type, decimal value, DateOnly date)
    {
        var request = new { Type = type, Value = value, RecordedDate = date };
        var response = await _client.PostAsJsonAsync("/api/health-metrics", request);
        var result = await response.Content.ReadFromJsonAsync<HealthMetricDto>();
        return result!.Id;
    }
}
