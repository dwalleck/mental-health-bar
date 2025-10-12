using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.HealthMetrics.GetById;

public record Query(Guid Id) : IRequest<HealthMetricDto?>;

public class Handler(AppDbContext context) : IRequestHandler<Query, HealthMetricDto?>
{
    private readonly AppDbContext _context = context;

    public async Task<HealthMetricDto?> Handle(Query request, CancellationToken cancellationToken)
    {
        var metric = await _context.HealthMetrics
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (metric == null)
        {
            return null;
        }

        return new HealthMetricDto(
            metric.Id,
            metric.Type.ToString(),
            metric.Value,
            metric.RecordedDate,
            metric.CreatedAt,
            metric.UpdatedAt
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapGetHealthMetricByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health-metrics/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var metric = await mediator.Send(new Query(id));
            return metric != null ? Results.Ok(metric) : Results.NotFound();
        })
        .WithName("GetHealthMetricById")
        .WithTags("HealthMetrics")
        .Produces<HealthMetricDto>(200)
        .Produces(404);

        return app;
    }
}
