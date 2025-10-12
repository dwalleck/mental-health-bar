namespace MentalHealthBar.Contracts.Requests.Assessments;

public record CompleteAssessmentRequest(
    string Type,
    DateTimeOffset CompletedAt,
    Dictionary<string, int> Responses
);
