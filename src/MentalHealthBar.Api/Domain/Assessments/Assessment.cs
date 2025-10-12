using MassTransit;

namespace MentalHealthBar.Api.Domain.Assessments;

public class Assessment
{
    public Guid Id { get; init; }
    public AssessmentType Type { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
    public Dictionary<string, int> Responses { get; init; } = new();
    public int TotalScore { get; private set; }
    public SeverityLevel Severity { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }

    private Assessment() { } // EF Core constructor

    public Assessment(AssessmentType type, Dictionary<string, int> responses, DateTimeOffset completedAt)
    {
        Id = NewId.NextSequentialGuid();
        Type = type;
        Responses = responses ?? throw new ArgumentNullException(nameof(responses));
        CompletedAt = completedAt;
        CreatedAt = DateTimeOffset.UtcNow;

        ValidateResponses();
        CalculateScore();
    }

    private void ValidateResponses()
    {
        var expectedQuestionCount = Type switch
        {
            AssessmentType.PHQ9 => 9,
            AssessmentType.BDI => 21,
            AssessmentType.GAD7 => 7,
            AssessmentType.BAI => 21,
            _ => throw new ArgumentOutOfRangeException(nameof(Type))
        };

        if (Responses.Count != expectedQuestionCount)
        {
            throw new InvalidOperationException(
                $"{Type} requires exactly {expectedQuestionCount} responses. Received: {Responses.Count}");
        }

        if (Responses.Values.Any(v => v < 0 || v > 3))
        {
            throw new ArgumentOutOfRangeException(nameof(Responses),
                "Each response must be between 0 and 3");
        }

        if (CompletedAt > DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(CompletedAt),
                "CompletedAt cannot be in the future");
        }
    }

    public void CalculateScore()
    {
        TotalScore = Responses.Values.Sum();
        Severity = CalculateSeverity();
    }

    private SeverityLevel CalculateSeverity()
    {
        return Type switch
        {
            AssessmentType.PHQ9 => TotalScore switch
            {
                >= 0 and <= 4 => SeverityLevel.Minimal,
                >= 5 and <= 9 => SeverityLevel.Mild,
                >= 10 and <= 14 => SeverityLevel.Moderate,
                >= 15 and <= 19 => SeverityLevel.ModeratelySevere,
                >= 20 and <= 27 => SeverityLevel.Severe,
                _ => throw new InvalidOperationException($"Invalid PHQ-9 score: {TotalScore}")
            },
            AssessmentType.GAD7 => TotalScore switch
            {
                >= 0 and <= 4 => SeverityLevel.Minimal,
                >= 5 and <= 9 => SeverityLevel.Mild,
                >= 10 and <= 14 => SeverityLevel.Moderate,
                >= 15 and <= 21 => SeverityLevel.Severe,
                _ => throw new InvalidOperationException($"Invalid GAD-7 score: {TotalScore}")
            },
            AssessmentType.BDI => TotalScore switch
            {
                >= 0 and <= 13 => SeverityLevel.Minimal,
                >= 14 and <= 19 => SeverityLevel.Mild,
                >= 20 and <= 28 => SeverityLevel.Moderate,
                >= 29 and <= 63 => SeverityLevel.Severe,
                _ => throw new InvalidOperationException($"Invalid BDI score: {TotalScore}")
            },
            AssessmentType.BAI => TotalScore switch
            {
                >= 0 and <= 7 => SeverityLevel.Minimal,
                >= 8 and <= 15 => SeverityLevel.Mild,
                >= 16 and <= 25 => SeverityLevel.Moderate,
                >= 26 and <= 63 => SeverityLevel.Severe,
                _ => throw new InvalidOperationException($"Invalid BAI score: {TotalScore}")
            },
            _ => throw new ArgumentOutOfRangeException(nameof(Type))
        };
    }
}

public enum AssessmentType
{
    PHQ9 = 1,
    BDI = 2,
    GAD7 = 3,
    BAI = 4
}

public enum SeverityLevel
{
    Minimal = 0,
    Mild = 1,
    Moderate = 2,
    ModeratelySevere = 3,
    Severe = 4
}
