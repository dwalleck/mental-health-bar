namespace MentalHealthBar.Contracts.Requests.HealthMetrics;

public record RecordHealthMetricRequest(
    string Type,
    decimal Value,
    DateOnly RecordedDate
);
