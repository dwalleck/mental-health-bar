namespace MentalHealthBar.Contracts.Requests.Export;

public record ExportRequest(
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool IncludeAssessments,
    bool IncludeMoodEntries,
    bool IncludeHealthMetrics
);
