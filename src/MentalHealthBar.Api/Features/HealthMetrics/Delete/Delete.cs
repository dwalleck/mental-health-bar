using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.HealthMetrics.Delete;

public record Command(Guid Id) : IRequest<bool>;

public class Handler(AppDbContext context) : IRequestHandler<Command, bool>
{
    private readonly AppDbContext _context = context;

    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
        var metric = await _context.HealthMetrics
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (metric == null || metric.IsDeleted)
        {
            return false;
        }

        metric.SoftDelete();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapDeleteHealthMetricEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/health-metrics/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var deleted = await mediator.Send(new Command(id));
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteHealthMetric")
        .WithTags("HealthMetrics")
        .Produces(204)
        .Produces(404);

        return app;
    }
}
