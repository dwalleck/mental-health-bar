using MassTransit;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealthBar.Api.Domain.MoodEntries;

public class MoodEntry
{
    private const int MaxTags = 10;
    private const int MaxNotesLength = 500;
    private const int MaxFutureDays = 30;

    public Guid Id { get; init; }
    public int MoodScore { get; private set; }
    public DateTimeOffset RecordedAt { get; init; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation property for many-to-many relationship
    public ICollection<MoodEntryEventLabel> MoodEntryEventLabels { get; set; } = new List<MoodEntryEventLabel>();

    // Computed property for backward compatibility with existing API contracts
    [NotMapped]
    public IEnumerable<Guid> EventLabelIds =>
        MoodEntryEventLabels?.Select(mel => mel.EventLabelId) ?? Enumerable.Empty<Guid>();

    private MoodEntry()
    {
        // EF Core constructor
        MoodEntryEventLabels = new List<MoodEntryEventLabel>();
    }

    public MoodEntry(int moodScore, DateTimeOffset recordedAt, IEnumerable<Guid>? eventLabelIds = null, string? notes = null)
    {
        Id = NewId.NextSequentialGuid();
        RecordedAt = recordedAt;
        CreatedAt = DateTimeOffset.UtcNow;
        MoodEntryEventLabels = new List<MoodEntryEventLabel>();

        SetMoodScore(moodScore);
        SetEventLabels(eventLabelIds ?? Array.Empty<Guid>());
        SetNotes(notes);
    }

    public void SetMoodScore(int score)
    {
        if (score < 1 || score > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(score),
                "Mood score must be between 1 (Worst) and 5 (Best)");
        }
        MoodScore = score;
    }

    public void SetEventLabels(IEnumerable<Guid> eventLabelIds)
    {
        var labelArray = eventLabelIds.ToArray();
        if (labelArray.Length > MaxTags)
        {
            throw new ArgumentException($"Maximum {MaxTags} event labels allowed per entry", nameof(eventLabelIds));
        }

        // Clear existing relationships
        MoodEntryEventLabels.Clear();

        // Add new relationships
        foreach (var labelId in labelArray)
        {
            MoodEntryEventLabels.Add(new MoodEntryEventLabel
            {
                MoodEntryId = this.Id,
                EventLabelId = labelId
            });
        }
    }

    public void SetNotes(string? notes)
    {
        if (notes?.Length > MaxNotesLength)
        {
            throw new ArgumentException($"Notes must not exceed {MaxNotesLength} characters", nameof(notes));
        }
        Notes = notes;
    }

    public void Update(int moodScore, IEnumerable<Guid>? eventLabelIds = null, string? notes = null)
    {
        SetMoodScore(moodScore);
        if (eventLabelIds != null) SetEventLabels(eventLabelIds);
        SetNotes(notes);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }

    public static void ValidateRecordedAt(DateTimeOffset recordedAt)
    {
        var maxFutureDate = DateTimeOffset.UtcNow.AddDays(MaxFutureDays);
        if (recordedAt > maxFutureDate)
        {
            throw new ArgumentOutOfRangeException(nameof(recordedAt),
                $"RecordedAt cannot be more than {MaxFutureDays} days in the future");
        }
    }
}
