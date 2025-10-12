namespace MentalHealthBar.Contracts.Responses.Assessments;

public record AssessmentPagedResultDto(
    List<AssessmentSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
