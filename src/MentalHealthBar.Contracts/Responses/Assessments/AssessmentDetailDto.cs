using NodaTime;

namespace MentalHealthBar.Contracts.Responses.Assessments;

public record AssessmentDetailDto(
    Guid Id,
    string Type,
    Dictionary<string, int> Responses,
    int TotalScore,
    string Severity,
    Instant CompletedAt,
    Instant CreatedAt,
    Instant? UpdatedAt
);
