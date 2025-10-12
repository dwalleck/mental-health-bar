namespace MentalHealthBar.Contracts.Responses.Assessments;

public record TemplateDto(
    Guid Id,
    string Type,
    string Name,
    string Description
);
