using MentalHealthBar.Api.Domain.EventLabels;

namespace MentalHealthBar.Api.Domain.MoodEntries;

/// <summary>
/// Junction entity for many-to-many relationship between MoodEntry and EventLabel
/// </summary>
public class MoodEntryEventLabel
{
    public Guid MoodEntryId { get; set; }
    public MoodEntry MoodEntry { get; set; } = null!;

    public Guid EventLabelId { get; set; }
    public EventLabel EventLabel { get; set; } = null!;

    // Optional: Add metadata fields if needed in the future
    // public DateTimeOffset AddedAt { get; set; }
    // public int DisplayOrder { get; set; }
}