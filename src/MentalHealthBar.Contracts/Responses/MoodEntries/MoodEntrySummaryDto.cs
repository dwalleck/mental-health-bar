using MentalHealthBar.Contracts.Responses.EventLabels;

namespace MentalHealthBar.Contracts.Responses.MoodEntries;

public record MoodEntrySummaryDto(
    Guid Id,
    int MoodScore,
    DateTimeOffset RecordedAt,
    List<EventLabelDto> EventLabels,
    string? Notes,
    DateTimeOffset CreatedAt
);
