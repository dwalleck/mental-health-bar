using MentalHealthBar.Contracts.Responses.MoodEntries;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using NodaTime;

namespace MentalHealthBar.Desktop.Models;

public static class DtoExtensions
{
    // Extension methods for MoodEntryDto
    public static List<string> GetTags(this MoodEntryDto moodEntry)
    {
        return moodEntry.EventLabels?.Select(e => e.Name).ToList() ?? new List<string>();
    }

    public static MoodEntryDto WithTags(this MoodEntryDto moodEntry, List<string> tags)
    {
        var eventLabels = tags.Select(t => new EventLabelResponse(
            Id: Guid.NewGuid(),
            Name: t,
            Description: null,
            CreatedAt: SystemClock.Instance.GetCurrentInstant(),
            UpdatedAt: null
        )).ToList();

        return moodEntry with { EventLabels = eventLabels };
    }

    // Extension methods for HealthMetricDto
    public static string GetRecordedDateString(this HealthMetricDto metric)
    {
        return metric.RecordedDate.ToString("yyyy-MM-dd");
    }

    public static HealthMetricDto WithRecordedDate(this HealthMetricDto metric, string date)
    {
        return metric with { RecordedDate = DateOnly.Parse(date) };
    }
}