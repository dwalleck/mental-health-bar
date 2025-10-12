namespace MentalHealthBar.Contracts.Responses.MoodEntries;

public record MoodStatsDto(
    int Count,
    double Average,
    int Median,
    Dictionary<int, int> ScoreDistribution
);
