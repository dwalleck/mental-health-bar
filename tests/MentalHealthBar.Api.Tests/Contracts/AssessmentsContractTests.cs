using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Contracts;

/// <summary>
/// Contract tests for Assessments API endpoints
/// These tests verify the API contract based on contracts/assessments.yaml
/// Following TDD: These tests MUST FAIL until endpoints are implemented
/// </summary>
public class AssessmentsContractTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AssessmentsContractTests()
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
    public async Task GetAssessmentTemplates_ReturnsOkWithTemplateList()
    {
        // Act
        var response = await _client.GetAsync("/api/assessments/templates");

        // Assert - Following TDD, this should be 404/501 until implemented
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var templates = await response.Content.ReadFromJsonAsync<List<AssessmentTemplateDto>>();
        await Assert.That(templates).IsNotNull();
        await Assert.That(templates!.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetAssessmentTemplate_ValidType_ReturnsTemplate()
    {
        // Arrange
        var type = "PHQ9";

        // Act
        var response = await _client.GetAsync($"/api/assessments/templates/{type}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var template = await response.Content.ReadFromJsonAsync<AssessmentTemplateDto>();
        await Assert.That(template).IsNotNull();
        await Assert.That(template!.Type).IsEqualTo(type);
        await Assert.That(template.Questions.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetAssessmentTemplate_InvalidType_ReturnsNotFound()
    {
        // Arrange
        var invalidType = "INVALID_TYPE";

        // Act
        var response = await _client.GetAsync($"/api/assessments/templates/{invalidType}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CompleteAssessment_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CompleteAssessmentRequestDto
        {
            Type = "PHQ9",
            Responses = new Dictionary<string, int>
            {
                ["Q1"] = 2,
                ["Q2"] = 1,
                ["Q3"] = 3,
                ["Q4"] = 0,
                ["Q5"] = 2,
                ["Q6"] = 1,
                ["Q7"] = 2,
                ["Q8"] = 1,
                ["Q9"] = 0
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/assessments", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<AssessmentResponseDto>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(result.Type).IsEqualTo(request.Type);
        await Assert.That(result.TotalScore).IsGreaterThanOrEqualTo(0);
        await Assert.That(result.Severity).IsNotNull();
    }

    [Test]
    public async Task CompleteAssessment_InvalidResponses_ReturnsBadRequest()
    {
        // Arrange - Missing required responses
        var request = new CompleteAssessmentRequestDto
        {
            Type = "PHQ9",
            Responses = new Dictionary<string, int>
            {
                ["Q1"] = 2
                // Missing Q2-Q9
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/assessments", request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GetAssessmentHistory_WithoutFilters_ReturnsPagedList()
    {
        // Act
        var response = await _client.GetAsync("/api/assessments");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<AssessmentHistoryResponseDto>();
        await Assert.That(history).IsNotNull();
        await Assert.That(history!.Items).IsNotNull();
        await Assert.That(history.TotalCount).IsGreaterThanOrEqualTo(0);
        await Assert.That(history.Page).IsGreaterThan(0);
        await Assert.That(history.PageSize).IsGreaterThan(0);
    }

    [Test]
    public async Task GetAssessmentHistory_WithTypeFilter_ReturnsFilteredList()
    {
        // Arrange
        var type = "PHQ9";

        // Act
        var response = await _client.GetAsync($"/api/assessments?type={type}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<AssessmentHistoryResponseDto>();
        await Assert.That(history).IsNotNull();
        // All returned items should match the filter
        foreach (var item in history!.Items)
        {
            await Assert.That(item.Type).IsEqualTo(type);
        }
    }

    [Test]
    public async Task GetAssessment_ValidId_ReturnsAssessment()
    {
        // Arrange - First create an assessment
        var createRequest = new CompleteAssessmentRequestDto
        {
            Type = "PHQ9",
            Responses = new Dictionary<string, int>
            {
                ["Q1"] = 2, ["Q2"] = 1, ["Q3"] = 3, ["Q4"] = 0,
                ["Q5"] = 2, ["Q6"] = 1, ["Q7"] = 2, ["Q8"] = 1, ["Q9"] = 0
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/assessments", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<AssessmentResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/assessments/{created!.Id}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var assessment = await response.Content.ReadFromJsonAsync<AssessmentResponseDto>();
        await Assert.That(assessment).IsNotNull();
        await Assert.That(assessment!.Id).IsEqualTo(created.Id);
    }

    [Test]
    public async Task GetAssessment_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/assessments/{invalidId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteAssessment_ValidId_ReturnsNoContent()
    {
        // Arrange - First create an assessment
        var createRequest = new CompleteAssessmentRequestDto
        {
            Type = "PHQ9",
            Responses = new Dictionary<string, int>
            {
                ["Q1"] = 2, ["Q2"] = 1, ["Q3"] = 3, ["Q4"] = 0,
                ["Q5"] = 2, ["Q6"] = 1, ["Q7"] = 2, ["Q8"] = 1, ["Q9"] = 0
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/assessments", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<AssessmentResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/assessments/{created!.Id}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeleteAssessment_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/assessments/{invalidId}");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

// DTOs matching the OpenAPI contract
public record AssessmentTemplateDto(
    string Type,
    string Name,
    string Description,
    List<QuestionDto> Questions,
    ScoringRulesDto ScoringRules
);

public record QuestionDto(
    string Id,
    string Text,
    List<AnswerOptionDto> Options
);

public record AnswerOptionDto(
    int Value,
    string Label
);

public record ScoringRulesDto(
    int MinScore,
    int MaxScore,
    Dictionary<string, string> SeverityRanges
);

public record CompleteAssessmentRequestDto
{
    public string Type { get; init; } = "";
    public Dictionary<string, int> Responses { get; init; } = new();
    public DateTime? CompletedAt { get; init; }
}

public record AssessmentResponseDto(
    Guid Id,
    string Type,
    DateTime CompletedAt,
    Dictionary<string, int> Responses,
    int TotalScore,
    string Severity,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record AssessmentHistoryResponseDto(
    List<AssessmentSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record AssessmentSummaryDto(
    Guid Id,
    string Type,
    DateTime CompletedAt,
    int TotalScore,
    string Severity
);
