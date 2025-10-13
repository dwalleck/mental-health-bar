using NodaTime;

namespace MentalHealthBar.Contracts.Requests.MoodEntries;

public record CreateMoodEntryRequest(
    int MoodScore,
    Instant RecordedAt,
    List<Guid> EventLabelIds,
    string? Notes
);
