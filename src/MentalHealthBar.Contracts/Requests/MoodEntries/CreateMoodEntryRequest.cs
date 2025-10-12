namespace MentalHealthBar.Contracts.Requests.MoodEntries;

public record CreateMoodEntryRequest(
    int MoodScore,
    DateTimeOffset RecordedAt,
    List<Guid> EventLabelIds,
    string? Notes
);
