using NodaTime;

namespace MentalHealthBar.Contracts.Responses.EventLabels;

public record EventLabelDto(
    Guid Id,
    string Name,
    string? Description,
    Instant CreatedAt,
    Instant? UpdatedAt
);
