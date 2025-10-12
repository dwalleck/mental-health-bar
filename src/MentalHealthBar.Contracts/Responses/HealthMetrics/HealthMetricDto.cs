namespace MentalHealthBar.Contracts.Responses.HealthMetrics;

public record HealthMetricDto(
    Guid Id,
    string Type,
    decimal Value,
    DateOnly RecordedDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
