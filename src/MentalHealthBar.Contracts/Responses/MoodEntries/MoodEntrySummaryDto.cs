using MentalHealthBar.Contracts.Responses.EventLabels;
using NodaTime;

namespace MentalHealthBar.Contracts.Responses.MoodEntries;

public record MoodEntrySummaryDto(
    Guid Id,
    int MoodScore,
    Instant RecordedAt,
    List<EventLabelDto> EventLabels,
    string? Notes,
    Instant CreatedAt
);
