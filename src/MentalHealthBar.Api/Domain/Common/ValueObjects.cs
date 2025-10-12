namespace MentalHealthBar.Api.Domain.Common;

public record MoodScore
{
    public int Value { get; }
    public string Label { get; }

    public MoodScore(int value)
    {
        if (value < 1 || value > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Mood score must be 1-5");
        }

        Value = value;
        Label = value switch
        {
            1 => "Worst",
            2 => "Below Average",
            3 => "Average",
            4 => "Above Average",
            5 => "Best",
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    public static implicit operator int(MoodScore score) => score.Value;
    public static explicit operator MoodScore(int value) => new(value);
}

public record DateRange(DateOnly Start, DateOnly End)
{
    public static DateRange Last7Days() =>
        new(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow));

    public static DateRange Last30Days() =>
        new(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), DateOnly.FromDateTime(DateTime.UtcNow));

    public static DateRange Last90Days() =>
        new(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90)), DateOnly.FromDateTime(DateTime.UtcNow));

    public static DateRange AllTime() =>
        new(DateOnly.MinValue, DateOnly.FromDateTime(DateTime.UtcNow));

    public int DayCount => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= Start && date <= End;
}
