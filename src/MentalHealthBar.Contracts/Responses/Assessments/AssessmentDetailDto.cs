namespace MentalHealthBar.Contracts.Responses.Assessments;

public record AssessmentDetailDto(
    Guid Id,
    string Type,
    Dictionary<string, int> Responses,
    int TotalScore,
    string Severity,
    DateTimeOffset CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
