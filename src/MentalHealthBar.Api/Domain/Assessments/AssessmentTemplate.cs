namespace MentalHealthBar.Api.Domain.Assessments;

public class AssessmentTemplate
{
    public Guid Id { get; init; }
    public AssessmentType Type { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<Question> Questions { get; init; } = new();
    public ScoringRules ScoringRules { get; init; } = null!;

    private AssessmentTemplate() { } // EF Core constructor

    public AssessmentTemplate(AssessmentType type, string name, string description, List<Question> questions, ScoringRules scoringRules)
    {
        Id = Guid.NewGuid();
        Type = type;
        Name = name;
        Description = description;
        Questions = questions;
        ScoringRules = scoringRules;
    }
}

public record Question(
    string Id,
    string Text,
    List<AnswerOption> Options
);

public record AnswerOption(int Value, string Label);

public record ScoringRules(
    int MinScore,
    int MaxScore,
    Dictionary<string, string> SeverityRanges
);
