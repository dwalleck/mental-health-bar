namespace MentalHealthBar.Contracts.Requests.MoodEntries;

public record UpdateMoodEntryRequest(
    int MoodScore,
    List<Guid> EventLabelIds,
    string? Notes
);
