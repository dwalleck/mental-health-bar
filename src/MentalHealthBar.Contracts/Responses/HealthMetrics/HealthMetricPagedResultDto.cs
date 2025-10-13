namespace MentalHealthBar.Contracts.Responses.HealthMetrics;

public record HealthMetricPagedResultDto(
    List<HealthMetricSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
