namespace MentalHealthBar.Contracts.Requests.EventLabels;

public record UpdateEventLabelRequest(
    string Name,
    string? Description
);
