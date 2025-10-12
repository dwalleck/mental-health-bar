namespace MentalHealthBar.Contracts.Responses.EventLabels;

public record EventLabelDto(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
