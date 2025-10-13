using NodaTime;

namespace MentalHealthBar.Contracts.Responses.Assessments;

public record AssessmentSummaryDto(
    Guid Id,
    string Type,
    int TotalScore,
    string Severity,
    Instant CompletedAt,
    Instant CreatedAt
);
