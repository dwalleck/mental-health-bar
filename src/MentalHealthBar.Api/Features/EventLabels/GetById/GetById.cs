using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.EventLabels;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.EventLabels.GetById;

public record Query(Guid Id) : IRequest<EventLabelDto?>;

public class Handler(AppDbContext context) : IRequestHandler<Query, EventLabelDto?>
{
    private readonly AppDbContext _context = context;

    public async Task<EventLabelDto?> Handle(Query request, CancellationToken cancellationToken)
    {
        var label = await _context.EventLabels
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (label == null)
        {
            return null;
        }

        return new EventLabelDto(
            label.Id,
            label.Name,
            label.Description,
            label.CreatedAt,
            label.UpdatedAt
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapGetEventLabelByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/event-labels/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var label = await mediator.Send(new Query(id));
            return label != null ? Results.Ok(label) : Results.NotFound();
        })
        .WithName("GetEventLabelById")
        .WithTags("EventLabels")
        .Produces<EventLabelDto>(200)
        .Produces(404);

        return app;
    }
}
