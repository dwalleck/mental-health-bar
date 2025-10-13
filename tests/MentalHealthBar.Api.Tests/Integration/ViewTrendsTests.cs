using System.Net;
using System.Net.Http.Json;
using MentalHealthBar.Contracts.Responses.Assessments;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Microsoft.AspNetCore.Mvc.Testing;
using NodaTime;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Integration;

/// <summary>
/// Integration test for Scenario 7: View trends across mood, health, and assessments
/// User Story: As a user, I want to view trend data for tables and graphs
/// to understand patterns over time
/// </summary>
public class ViewTrendsTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ViewTrendsTests()
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
    public async Task Scenario7_ViewMoodTrends_Last30Days_ReturnsDataForGraphing()
    {
        // Scenario: User views mood trends for past 30 days
        // Expected: Data suitable for line chart (date vs mood score)

        // Arrange: Create mood entries over 30 days
        var endDate = SystemClock.Instance.GetCurrentInstant();
        var startDate = endDate.Minus(Duration.FromDays(30));

        await CreateMoodEntry(3, startDate.Plus(Duration.FromDays(2)));
        await CreateMoodEntry(2, startDate.Plus(Duration.FromDays(10)));
        await CreateMoodEntry(4, startDate.Plus(Duration.FromDays(20)));
        await CreateMoodEntry(3, startDate.Plus(Duration.FromDays(28)));

        // Act: Request mood history for 30 days
        var response = await _client.GetAsync($"/api/mood-entries?startDate={Uri.EscapeDataString(startDate.ToDateTimeOffset().ToString("o"))}&endDate={Uri.EscapeDataString(endDate.ToDateTimeOffset().ToString("o"))}");

        // Assert: Data returned
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<MoodPagedResultDto>();
        var entries = pagedResult!.Items;

        // Assert: All entries within date range
        await Assert.That(entries).IsNotNull();
        await Assert.That(entries.Count).IsGreaterThanOrEqualTo(4);

        foreach (var entry in entries)
        {
            await Assert.That(entry.RecordedAt).IsGreaterThan(startDate);
            await Assert.That(entry.RecordedAt).IsLessThan(endDate);
        }
    }

    [Test]
    public async Task Scenario7_ViewMoodStatistics_ReturnsAggregateData()
    {
        // Scenario: User wants to see mood statistics (average, median, distribution)
        // Expected: Statistical summary for selected date range

        // Arrange: Create entries with known distribution
        var now = SystemClock.Instance.GetCurrentInstant();
        var baseDate = now.Minus(Duration.FromDays(7));
        await CreateMoodEntry(1, baseDate);
        await CreateMoodEntry(2, baseDate.Plus(Duration.FromDays(1)));
        await CreateMoodEntry(3, baseDate.Plus(Duration.FromDays(2)));
        await CreateMoodEntry(4, baseDate.Plus(Duration.FromDays(3)));
        await CreateMoodEntry(5, baseDate.Plus(Duration.FromDays(4)));
        // Average: 3.0, Median: 3

        // Act: Request mood statistics
        var startDate = Uri.EscapeDataString(baseDate.ToDateTimeOffset().ToString("o"));
        var endDate = Uri.EscapeDataString(now.ToDateTimeOffset().ToString("o"));
        var response = await _client.GetAsync($"/api/mood-entries/stats?startDate={startDate}&endDate={endDate}");

        // Assert: Statistics calculated
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<MoodStatsDto>();

        // Assert: Stats include aggregate metrics
        await Assert.That(stats).IsNotNull();
        await Assert.That(stats!.Count).IsGreaterThanOrEqualTo(5);
        await Assert.That(stats.Average).IsGreaterThan(0);
        await Assert.That(stats.Average).IsLessThanOrEqualTo(5);
    }

    [Test]
    public async Task Scenario7_ViewAssessmentTrends_ShowsProgressOverTime()
    {
        // Scenario: User completes PHQ-9 monthly to track depression trends
        // Expected: Historical scores show improvement or decline

        // Arrange: Complete assessments over 3 months
        var now = SystemClock.Instance.GetCurrentInstant();
        await CreateAssessment("PHQ9", 15, now.Minus(Duration.FromDays(90))); // Moderate-severe
        await CreateAssessment("PHQ9", 11, now.Minus(Duration.FromDays(60))); // Moderate
        await CreateAssessment("PHQ9", 7, now.Minus(Duration.FromDays(30)));  // Mild
        await CreateAssessment("PHQ9", 3, now);               // Minimal

        // Act: Request assessment history for PHQ9
        var response = await _client.GetAsync("/api/assessments?type=PHQ9");

        // Assert: Shows progression
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<AssessmentPagedResultDto>();
        var history = pagedResult!.Items;

        // Assert: Ordered by date (most recent first)
        await Assert.That(history).IsNotNull();
        var phq9History = history.Where(a => a.Type == "PHQ9")
            .OrderByDescending(a => a.CompletedAt)
            .ToList();

        await Assert.That(phq9History.Count).IsGreaterThanOrEqualTo(4);

        // Verify trend shows improvement (scores decreasing over time)
        await Assert.That(phq9History[0].TotalScore).IsLessThan(phq9History[phq9History.Count - 1].TotalScore);
    }

    [Test]
    public async Task Scenario7_ViewHealthMetricTrends_DisplaysSleepPatterns()
    {
        // Scenario: User views sleep trends to identify patterns
        // Expected: Sleep hours over time data for charting

        // Arrange: Record sleep for past week
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int i = 7; i > 0; i--)
        {
            var sleep = 6.0m + (i % 3); // Varies between 6-8 hours
            await RecordHealthMetric("SleepHours", sleep, today.AddDays(-i));
        }

        // Act: Request sleep metrics
        var response = await _client.GetAsync("/api/health-metrics?type=SleepHours");

        // Assert: Data for graphing
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<HealthMetricPagedResultDto>();
        var metrics = pagedResult!.Items;

        await Assert.That(metrics).IsNotNull();
        await Assert.That(metrics.Count).IsGreaterThanOrEqualTo(7);

        // All values should be sleep hours
        foreach (var metric in metrics)
        {
            await Assert.That(metric.Type).IsEqualTo("SleepHours");
            await Assert.That(metric.Value).IsGreaterThanOrEqualTo(0);
            await Assert.That(metric.Value).IsLessThanOrEqualTo(24);
        }
    }

    // Helper methods
    private async Task<Guid> CreateMoodEntry(int score, Instant recordedAt, List<Guid>? eventLabelIds = null)
    {
        var request = new { MoodScore = score, RecordedAt = recordedAt, EventLabelIds = eventLabelIds ?? new List<Guid>(), Notes = (string?)null };
        var response = await _client.PostAsJsonAsync("/api/mood-entries", request);
        var result = await response.Content.ReadFromJsonAsync<MoodEntryDto>();
        return result!.Id;
    }

    private async Task<Guid> CreateAssessment(string type, int targetScore, Instant completedAt)
    {
        var questionCount = type == "PHQ9" ? 9 : 7;
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
