using System.Net;
using System.Net.Http.Json;
using MentalHealthBar.Contracts.Responses.Assessments;
using Microsoft.AspNetCore.Mvc.Testing;
using NodaTime;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Integration;

/// <summary>
/// Integration test for Scenario 3: View assessment history
/// User Story: As a user, I want to view my assessment history
/// in both table and graph format to track progress over time
/// </summary>
public class AssessmentHistoryTests : IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AssessmentHistoryTests()
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
    public async Task Scenario3_ViewAssessmentHistory_ReturnsAllCompletedAssessments()
    {
        // Scenario: User has completed multiple assessments over time
        // Expected: History displays all assessments in chronological order

        // Arrange: Complete 3 PHQ-9 assessments at different times
        var now = SystemClock.Instance.GetCurrentInstant();
        var assessment1 = await CreateAssessment("PHQ9", 5, now.Minus(Duration.FromDays(14))); // Mild
        var assessment2 = await CreateAssessment("PHQ9", 11, now.Minus(Duration.FromDays(7))); // Moderate
        var assessment3 = await CreateAssessment("PHQ9", 3, now);              // Minimal

        // Act: User requests assessment history
        var response = await _client.GetAsync("/api/assessments");

        // Assert: Request succeeds
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<AssessmentPagedResultDto>(_factory);
        var history = pagedResult!.Items;

        // Assert: All 3 assessments are returned
        await Assert.That(history).IsNotNull();
        await Assert.That(history!.Count).IsGreaterThanOrEqualTo(3);

        // Assert: Most recent assessment is first (descending order)
        var recentAssessments = history.Take(3).ToList();
        await Assert.That(recentAssessments[0].CompletedAt).IsGreaterThan(recentAssessments[1].CompletedAt);
        await Assert.That(recentAssessments[1].CompletedAt).IsGreaterThan(recentAssessments[2].CompletedAt);

        // Assert: Assessments include score and severity
        foreach (var assessment in recentAssessments)
        {
            await Assert.That(assessment.TotalScore).IsGreaterThanOrEqualTo(0);
            await Assert.That(assessment.Severity).IsNotNull();
        }
    }

    [Test]
    public async Task Scenario3_FilterAssessmentsByType_ReturnsOnlyRequestedType()
    {
        // Scenario: User has completed multiple assessment types
        // Expected: Can filter history to show only specific type (e.g., PHQ-9)

        // Arrange: Complete different assessment types
        var now = SystemClock.Instance.GetCurrentInstant();
        await CreateAssessment("PHQ9", 5, now.Minus(Duration.FromDays(7)));
        await CreateAssessment("GAD7", 8, now.Minus(Duration.FromDays(5)));
        await CreateAssessment("PHQ9", 3, now);

        // Act: User filters by PHQ9 only
        var response = await _client.GetAsync("/api/assessments?type=PHQ9");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<AssessmentPagedResultDto>(_factory);
        var history = pagedResult!.Items;

        // Assert: Only PHQ-9 assessments are returned
        await Assert.That(history).IsNotNull();
        var phq9Count = history!.Count(a => a.Type == "PHQ9");
        var otherCount = history!.Count(a => a.Type != "PHQ9");

        await Assert.That(phq9Count).IsGreaterThanOrEqualTo(2);
        await Assert.That(otherCount).IsEqualTo(0);
    }

    [Test]
    public async Task Scenario3_FilterAssessmentsByDateRange_ReturnsOnlyWithinRange()
    {
        // Scenario: User wants to see assessments from specific time period
        // Expected: Only assessments within date range are returned

        // Arrange: Complete assessments across different dates
        var now = SystemClock.Instance.GetCurrentInstant();
        await CreateAssessment("PHQ9", 5, now.Minus(Duration.FromDays(30))); // Outside range
        await CreateAssessment("PHQ9", 7, now.Minus(Duration.FromDays(10))); // Inside range
        await CreateAssessment("PHQ9", 3, now.Minus(Duration.FromDays(5)));  // Inside range

        // Act: User requests last 14 days
        var startDate = Uri.EscapeDataString(now.Minus(Duration.FromDays(14)).ToDateTimeOffset().ToString("o"));
        var endDate = Uri.EscapeDataString(now.ToDateTimeOffset().ToString("o"));
        var response = await _client.GetAsync($"/api/assessments?startDate={startDate}&endDate={endDate}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<AssessmentPagedResultDto>(_factory);
        var history = pagedResult!.Items;

        // Assert: Only recent assessments within 14 days
        await Assert.That(history).IsNotNull();
        foreach (var assessment in history!)
        {
            await Assert.That(assessment.CompletedAt).IsGreaterThan(now.Minus(Duration.FromDays(14)));
        }
    }

    [Test]
    public async Task Scenario3_ViewAssessmentById_ReturnsDetailedResponses()
    {
        // Scenario: User clicks on a specific assessment to view details
        // Expected: Full assessment with all responses is displayed

        // Arrange: Complete an assessment
        var assessmentId = await CreateAssessment("PHQ9", 11, SystemClock.Instance.GetCurrentInstant());

        // Act: User requests specific assessment details
        var response = await _client.GetAsync($"/api/assessments/{assessmentId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var assessment = await response.Content.ReadFromJsonAsync<AssessmentDetailDto>(_factory);

        // Assert: Full details including individual responses
        await Assert.That(assessment).IsNotNull();
        await Assert.That(assessment!.Id).IsEqualTo(assessmentId);
        await Assert.That(assessment.Responses).IsNotNull();
        await Assert.That(assessment.Responses.Count).IsEqualTo(9); // PHQ-9 has 9 questions
        await Assert.That(assessment.TotalScore).IsEqualTo(11);
    }

    [Test]
    public async Task Scenario3_DeleteAssessment_RemovesFromHistory()
    {
        // Scenario: User wants to remove an incorrectly completed assessment
        // Expected: Assessment is deleted and no longer appears in history

        // Arrange: Complete an assessment
        var assessmentId = await CreateAssessment("PHQ9", 5, SystemClock.Instance.GetCurrentInstant());

        // Act: User deletes the assessment
        var deleteResponse = await _client.DeleteAsync($"/api/assessments/{assessmentId}");

        // Assert: Delete succeeds
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Assert: Assessment no longer retrievable
        var getResponse = await _client.GetAsync($"/api/assessments/{assessmentId}");
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // Helper method to create assessments
    private async Task<Guid> CreateAssessment(string type, int targetScore, Instant completedAt)
    {
        var questionCount = type switch
        {
            "PHQ9" => 9,
            "GAD7" => 7,
            "BDI" => 21,
            "BAI" => 21,
            _ => throw new ArgumentException($"Unknown assessment type: {type}")
        };

        // Distribute score across questions
        var responses = new Dictionary<string, int>();
        var scorePerQuestion = targetScore / questionCount;
        var remainder = targetScore % questionCount;

        for (int i = 1; i <= questionCount; i++)
        {
            var score = scorePerQuestion + (i <= remainder ? 1 : 0);
            responses[$"Q{i}"] = Math.Min(score, 3); // Cap at 3 per question
        }

        var request = new
        {
            Type = type,
            CompletedAt = completedAt,
            Responses = responses
        };

        var response = await _client.PostAsJsonAsync("/api/assessments", request, _factory);
        var result = await response.Content.ReadFromJsonAsync<AssessmentResultDto>(_factory);
        return result!.Id;
    }
}
