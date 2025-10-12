namespace MentalHealthBar.Contracts.Responses.HealthMetrics;

public record HealthMetricSummaryDto(
    Guid Id,
    string Type,
    decimal Value,
    DateOnly RecordedDate,
    DateTimeOffset CreatedAt
);
