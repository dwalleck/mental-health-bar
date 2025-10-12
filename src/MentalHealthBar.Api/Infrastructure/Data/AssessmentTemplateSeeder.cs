using System.Text.Json;
using MentalHealthBar.Api.Domain.Assessments;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Infrastructure.Data;

public class AssessmentTemplateSeeder(AppDbContext context, ILogger<AssessmentTemplateSeeder> logger)
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<AssessmentTemplateSeeder> _logger = logger;

    public async Task SeedAsync()
    {
        // Check if templates already exist
        if (await _context.AssessmentTemplates.AnyAsync())
        {
            _logger.LogInformation("Assessment templates already seeded, skipping");
            return;
        }

        var seedFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "Seeds", "assessment-templates.json");

        if (!File.Exists(seedFilePath))
        {
            _logger.LogWarning("Seed file not found at {Path}", seedFilePath);
            return;
        }

        var json = await File.ReadAllTextAsync(seedFilePath);
        var templateData = JsonSerializer.Deserialize<List<AssessmentTemplateDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (templateData == null || templateData.Count == 0)
        {
            _logger.LogWarning("No templates found in seed file");
            return;
        }

        foreach (var dto in templateData)
        {
            var type = Enum.Parse<AssessmentType>(dto.Type);
            var template = new AssessmentTemplate(
                type,
                dto.Name,
                dto.Description,
                dto.Questions.Select(q => new Question(
                    q.Id,
                    q.Text,
                    q.Options.Select(o => new AnswerOption(o.Value, o.Label)).ToList()
                )).ToList(),
                new ScoringRules(
                    dto.ScoringRules.MinScore,
                    dto.ScoringRules.MaxScore,
                    dto.ScoringRules.SeverityRanges
                )
            );

            _context.AssessmentTemplates.Add(template);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} assessment templates", templateData.Count);
    }

    // DTOs for deserialization
    private class AssessmentTemplateDto
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<QuestionDto> Questions { get; set; } = new();
        public ScoringRulesDto ScoringRules { get; set; } = null!;
    }

    private class QuestionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<AnswerOptionDto> Options { get; set; } = new();
    }

    private class AnswerOptionDto
    {
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    private class ScoringRulesDto
    {
        public int MinScore { get; set; }
        public int MaxScore { get; set; }
        public Dictionary<string, string> SeverityRanges { get; set; } = new();
    }
}
