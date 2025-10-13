using NodaTime;

namespace MentalHealthBar.Contracts.Requests.Assessments;

public record CompleteAssessmentRequest(
    string Type,
    Instant CompletedAt,
    Dictionary<string, int> Responses
);
