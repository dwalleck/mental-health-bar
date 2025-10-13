using NodaTime;

namespace MentalHealthBar.Contracts.Responses.Export;

public record ExportDataResponse(
    List<AssessmentExport> Assessments,
    List<MoodEntryExport> MoodEntries,
    List<HealthMetricExport> HealthMetrics,
    List<string> EventLabels,
    DateRangeExport DateRange,
    Instant ExportedAt
);

public record DateRangeExport(Instant Start, Instant End);

public record AssessmentExport(
    Guid Id,
    string Type,
    Dictionary<string, int> Responses,
    int TotalScore,
    string Severity,
    Instant CompletedAt,
    Instant CreatedAt
);

public record MoodEntryExport(
    Guid Id,
    int MoodScore,
    string MoodLabel,
    Instant RecordedAt,
    List<string> EventLabelNames,
    string? Notes,
    Instant CreatedAt
);

public record HealthMetricExport(
    Guid Id,
    string Type,
    decimal Value,
    DateOnly RecordedDate,
    Instant CreatedAt
);
