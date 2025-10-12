namespace MentalHealthBar.Contracts.Responses.Export;

public record ExportDataResponse(
    List<AssessmentExport> Assessments,
    List<MoodEntryExport> MoodEntries,
    List<HealthMetricExport> HealthMetrics,
    List<string> EventLabels,
    DateRangeExport DateRange,
    DateTimeOffset ExportedAt
);

public record DateRangeExport(DateTimeOffset Start, DateTimeOffset End);

public record AssessmentExport(
    Guid Id,
    string Type,
    Dictionary<string, int> Responses,
    int TotalScore,
    string Severity,
    DateTimeOffset CompletedAt,
    DateTimeOffset CreatedAt
);

public record MoodEntryExport(
    Guid Id,
    int MoodScore,
    string MoodLabel,
    DateTimeOffset RecordedAt,
    List<string> EventLabelNames,
    string? Notes,
    DateTimeOffset CreatedAt
);

public record HealthMetricExport(
    Guid Id,
    string Type,
    decimal Value,
    DateOnly RecordedDate,
    DateTimeOffset CreatedAt
);
