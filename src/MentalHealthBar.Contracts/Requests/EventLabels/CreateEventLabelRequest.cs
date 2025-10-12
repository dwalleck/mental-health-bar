namespace MentalHealthBar.Contracts.Requests.EventLabels;

public record CreateEventLabelRequest(
    string Name,
    string? Description
);
