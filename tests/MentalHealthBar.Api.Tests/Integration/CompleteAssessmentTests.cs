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
/// Integration test for Scenario 2: Complete assessment and calculate score
/// User Story: As a user, I want to complete a PHQ-9 assessment
/// and see my calculated score and severity level
/// </summary>
public class CompleteAssessmentTests : IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CompleteAssessmentTests()
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
    public async Task Scenario2_CompletePhq9Assessment_CalculatesCorrectScore()
    {
        // Scenario: User completes PHQ-9 with specific responses
        // Expected: System calculates correct total score and severity level

        // Arrange: Prepare PHQ-9 responses (all 0s = minimal depression)
        var responses = new Dictionary<string, int>
        {
            { "Q1", 0 }, { "Q2", 0 }, { "Q3", 0 },
            { "Q4", 0 }, { "Q5", 0 }, { "Q6", 0 },
            { "Q7", 0 }, { "Q8", 0 }, { "Q9", 0 }
        };

        var request = new
        {
            Type = "PHQ9",
            CompletedAt = SystemClock.Instance.GetCurrentInstant(),
            Responses = responses
        };

        // Act: User submits completed assessment
        var response = await _client.PostAsJsonAsync("/api/assessments", request, _factory);

        // Assert: Request succeeds
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<AssessmentResultDto>(_factory);

        // Assert: Score is calculated correctly (all 0s = 0 total)
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.TotalScore).IsEqualTo(0);
        await Assert.That(result.Severity).IsEqualTo("Minimal");

        // Assert: Response includes assessment ID and timestamp
        await Assert.That(result.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(result.CompletedAt).IsLessThanOrEqualTo(SystemClock.Instance.GetCurrentInstant());
    }

    [Test]
    public async Task Scenario2_CompletePhq9Assessment_ModerateSeverity_CalculatesCorrectly()
    {
        // Scenario: User completes PHQ-9 indicating moderate depression
        // Expected: Score and severity reflect moderate depression

        // Arrange: Responses indicating moderate symptoms (score 10-14)
        var responses = new Dictionary<string, int>
        {
            { "Q1", 1 }, { "Q2", 1 }, { "Q3", 1 },  // 3
            { "Q4", 2 }, { "Q5", 1 }, { "Q6", 1 },  // +4 = 7
            { "Q7", 2 }, { "Q8", 1 }, { "Q9", 1 }   // +4 = 11 (Moderate)
        };

        var request = new
        {
            Type = "PHQ9",
            CompletedAt = SystemClock.Instance.GetCurrentInstant(),
            Responses = responses
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/assessments", request, _factory);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<AssessmentResultDto>(_factory);
        await Assert.That(result!.TotalScore).IsEqualTo(11);
        await Assert.That(result.Severity).IsEqualTo("Moderate");
    }

    [Test]
    public async Task Scenario2_CompleteGad7Assessment_CalculatesCorrectly()
    {
        // Scenario: User completes GAD-7 anxiety assessment
        // Expected: System correctly calculates anxiety score

        // Arrange: GAD-7 responses (7 questions, 0-3 scale)
        var responses = new Dictionary<string, int>
        {
            { "Q1", 2 }, { "Q2", 2 }, { "Q3", 1 },  // 5
            { "Q4", 1 }, { "Q5", 1 }, { "Q6", 1 },  // +3 = 8
            { "Q7", 1 }                              // +1 = 9 (Mild)
        };

        var request = new
        {
            Type = "GAD7",
            CompletedAt = SystemClock.Instance.GetCurrentInstant(),
            Responses = responses
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/assessments", request, _factory);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<AssessmentResultDto>(_factory);
        await Assert.That(result!.TotalScore).IsEqualTo(9);
        await Assert.That(result.Severity).IsEqualTo("Mild");
    }

    [Test]
    public async Task Scenario2_CompleteAssessment_InvalidResponses_ReturnsBadRequest()
    {
        // Scenario: User submits assessment with invalid data
        // Expected: System returns validation error

        // Arrange: Missing responses (only 5 of 9 questions)
        var responses = new Dictionary<string, int>
        {
            { "Q1", 0 }, { "Q2", 0 }, { "Q3", 0 },
            { "Q4", 0 }, { "Q5", 0 }
        };

        var request = new
        {
            Type = "PHQ9",
            CompletedAt = SystemClock.Instance.GetCurrentInstant(),
            Responses = responses
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/assessments", request, _factory);

        // Assert: Validation error
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Scenario2_CompleteAssessment_FutureDate_ReturnsBadRequest()
    {
        // Scenario: User tries to submit assessment with future date
        // Expected: System rejects with validation error

        // Arrange: Valid responses but future completion date
        var responses = new Dictionary<string, int>
        {
            { "Q1", 0 }, { "Q2", 0 }, { "Q3", 0 },
            { "Q4", 0 }, { "Q5", 0 }, { "Q6", 0 },
            { "Q7", 0 }, { "Q8", 0 }, { "Q9", 0 }
        };

        var request = new
        {
            Type = "PHQ9",
            CompletedAt = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(1)), // Future date
            Responses = responses
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/assessments", request, _factory);

        // Assert: Validation error
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }
}
