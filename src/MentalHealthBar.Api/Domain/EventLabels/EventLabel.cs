using System.Text.RegularExpressions;
using MassTransit;
using MentalHealthBar.Api.Domain.MoodEntries;
using NodaTime;

namespace MentalHealthBar.Api.Domain.EventLabels;

public partial class EventLabel
{
    private const int MaxNameLength = 50;
    private const int MaxDescriptionLength = 200;

    [GeneratedRegex(@"^[a-zA-Z0-9\s\-]+$")]
    private static partial Regex NameValidationRegex();

    public Guid Id { get; init; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Instant CreatedAt { get; init; }
    public Instant? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public Instant? DeletedAt { get; set; }

    // Navigation property for many-to-many relationship
    public ICollection<MoodEntryEventLabel> MoodEntryEventLabels { get; set; } = new List<MoodEntryEventLabel>();

    private EventLabel() { } // EF Core constructor

    public EventLabel(string name, string? description = null)
    {
        Id = NewId.NextSequentialGuid();
        CreatedAt = SystemClock.Instance.GetCurrentInstant();

        SetName(name);
        SetDescription(description);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be empty", nameof(name));
        }

        if (name.Length > MaxNameLength)
        {
            throw new ArgumentException($"Name must not exceed {MaxNameLength} characters", nameof(name));
        }

        if (!NameValidationRegex().IsMatch(name))
        {
            throw new ArgumentException(
                "Name must contain only alphanumeric characters, spaces, and hyphens",
                nameof(name));
        }

        Name = name;
    }

    public void SetDescription(string? description)
    {
        if (description?.Length > MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Description must not exceed {MaxDescriptionLength} characters",
                nameof(description));
        }
        Description = description;
    }

    public void Update(string name, string? description = null)
    {
        SetName(name);
        SetDescription(description);
        UpdatedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = SystemClock.Instance.GetCurrentInstant();
    }
}
