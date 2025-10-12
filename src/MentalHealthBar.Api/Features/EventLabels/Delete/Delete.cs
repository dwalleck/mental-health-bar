using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.EventLabels.Delete;

public record Command(Guid Id) : IRequest<bool>;

public class Handler(AppDbContext context) : IRequestHandler<Command, bool>
{
    private readonly AppDbContext _context = context;

    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
        var label = await _context.EventLabels
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (label == null || label.IsDeleted)
        {
            return false;
        }

        label.SoftDelete();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapDeleteEventLabelEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/event-labels/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var deleted = await mediator.Send(new Command(id));
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteEventLabel")
        .WithTags("EventLabels")
        .Produces(204)
        .Produces(404);

        return app;
    }
}
