namespace MentalHealthBar.Contracts.Responses.Assessments;

public record AssessmentResultDto(
    Guid Id,
    string Type,
    int TotalScore,
    string Severity,
    DateTimeOffset CompletedAt
);
