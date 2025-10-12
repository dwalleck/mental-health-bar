namespace MentalHealthBar.Contracts.Responses.MoodEntries;

public record MoodPagedResultDto(
    List<MoodEntrySummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
