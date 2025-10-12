namespace MentalHealthBar.Contracts.Responses.Assessments;

public record TemplateDetailDto(
    Guid Id,
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

public record AnswerOptionDto(int Value, string Label);

public record ScoringRulesDto(
    int MinScore,
    int MaxScore,
    Dictionary<string, string> SeverityRanges
);
