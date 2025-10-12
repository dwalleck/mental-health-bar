using MassTransit;

namespace MentalHealthBar.Api.Domain.HealthMetrics;

public class HealthMetric
{
    private const decimal MaxSleepHours = 24;
    private const decimal MaxWaterIntakeOz = 200;
    private const int MaxFutureDays = 7;

    public Guid Id { get; init; }
    public MetricType Type { get; init; }
    public decimal Value { get; private set; }
    public DateOnly RecordedDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    private HealthMetric() { } // EF Core constructor

    public HealthMetric(MetricType type, decimal value, DateOnly recordedDate)
    {
        Id = NewId.NextSequentialGuid();
        Type = type;
        RecordedDate = recordedDate;
        CreatedAt = DateTimeOffset.UtcNow;

        ValidateRecordedDate(recordedDate);
        SetValue(value);
    }

    public void SetValue(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value cannot be negative");
        }

        var maxValue = Type switch
        {
            MetricType.SleepHours => MaxSleepHours,
            MetricType.WaterIntakeOz => MaxWaterIntakeOz,
            _ => throw new ArgumentOutOfRangeException(nameof(Type))
        };

        if (value > maxValue)
        {
            var unit = Type == MetricType.SleepHours ? "hours" : "oz";
            throw new ArgumentOutOfRangeException(nameof(value),
                $"{Type} must be between 0 and {maxValue} {unit}. Received: {value}");
        }

        Value = value;
    }

    public void Update(decimal value)
    {
        SetValue(value);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateRecordedDate(DateOnly recordedDate)
    {
        var maxFutureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(MaxFutureDays));
        if (recordedDate > maxFutureDate)
        {
            throw new ArgumentOutOfRangeException(nameof(recordedDate),
                $"RecordedDate cannot be more than {MaxFutureDays} days in the future");
        }
    }
}

public enum MetricType
{
    SleepHours = 1,
    WaterIntakeOz = 2
}
