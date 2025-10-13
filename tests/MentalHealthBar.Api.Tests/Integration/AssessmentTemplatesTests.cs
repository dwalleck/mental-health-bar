using System.Net;
using System.Net.Http.Json;
using MentalHealthBar.Contracts.Responses.Assessments;
using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace MentalHealthBar.Api.Tests.Integration;

/// <summary>
/// Integration test for Scenario 1: View assessment templates
/// User Story: As a user, I want to view available assessment templates
/// so I can choose which assessment to complete
/// </summary>
public class AssessmentTemplatesTests : IDisposable
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AssessmentTemplatesTests()
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
    public async Task Scenario1_ViewAssessmentTemplates_ReturnsAll4Types()
    {
        // Scenario: User opens the application and navigates to assessments
        // Expected: All 4 assessment types are available (PHQ-9, BDI, GAD-7, BAI)

        // Act: User views assessment templates
        var response = await _client.GetAsync("/api/assessments/templates");

        // Assert: Request succeeds
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var templates = await response.Content.ReadFromJsonAsync<List<TemplateDto>>(_factory);

        // Assert: All 4 assessment types are returned
        await Assert.That(templates).IsNotNull();
        await Assert.That(templates!.Count).IsEqualTo(4);

        // Assert: Expected assessment types are present
        var types = templates.Select(t => t.Type).ToList();
        await Assert.That(types.Contains("PHQ9")).IsTrue();
        await Assert.That(types.Contains("BDI")).IsTrue();
        await Assert.That(types.Contains("GAD7")).IsTrue();
        await Assert.That(types.Contains("BAI")).IsTrue();

        // Assert: Each template has required information
        foreach (var template in templates)
        {
            await Assert.That(template.Id).IsNotEqualTo(Guid.Empty);
            await Assert.That(template.Name).IsNotNull();
            await Assert.That(template.Description).IsNotNull();
        }
    }

    [Test]
    public async Task Scenario1_ViewSpecificTemplate_PHQ9_ReturnsFullDetails()
    {
        // Scenario: User selects PHQ-9 assessment to view questions
        // Expected: Template includes all 9 questions with answer options

        // Arrange: Get templates to find PHQ-9 ID
        var templatesResponse = await _client.GetAsync("/api/assessments/templates");
        var templates = await templatesResponse.Content.ReadFromJsonAsync<List<TemplateDto>>(_factory);
        var phq9 = templates!.First(t => t.Type == "PHQ9");

        // Act: User requests PHQ-9 template details
        var response = await _client.GetAsync($"/api/assessments/templates/{phq9.Type}");

        // Assert: Request succeeds
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var template = await response.Content.ReadFromJsonAsync<TemplateDetailDto>(_factory);

        // Assert: PHQ-9 has 9 questions
        await Assert.That(template).IsNotNull();
        await Assert.That(template!.Questions).IsNotNull();
        await Assert.That(template.Questions.Count).IsEqualTo(9);

        // Assert: Each question has 4 answer options (0-3)
        foreach (var question in template.Questions)
        {
            await Assert.That(question.Options).IsNotNull();
            await Assert.That(question.Options.Count).IsEqualTo(4);

            // Verify options have values 0, 1, 2, 3
            var values = question.Options.Select(o => o.Value).OrderBy(v => v).ToList();
            await Assert.That(values[0]).IsEqualTo(0);
            await Assert.That(values[1]).IsEqualTo(1);
            await Assert.That(values[2]).IsEqualTo(2);
            await Assert.That(values[3]).IsEqualTo(3);
        }

        // Assert: Scoring rules are present
        await Assert.That(template.ScoringRules).IsNotNull();
        await Assert.That(template.ScoringRules.MinScore).IsEqualTo(0);
        await Assert.That(template.ScoringRules.MaxScore).IsEqualTo(27);
    }
}
