using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.EventLabels;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.EventLabels.List;

public record Query(string? Search = null) : IRequest<List<EventLabelDto>>;

public class Handler(AppDbContext context) : IRequestHandler<Query, List<EventLabelDto>>
{
    private readonly AppDbContext _context = context;

    public async Task<List<EventLabelDto>> Handle(Query request, CancellationToken cancellationToken)
    {
        var query = _context.EventLabels.AsQueryable();

        // Filter by search term (case-insensitive partial match)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(searchLower));
        }

        var labels = await query
            .OrderBy(e => e.Name)
            .Select(e => new EventLabelDto(
                e.Id,
                e.Name,
                e.Description,
                e.CreatedAt,
                null
            ))
            .ToListAsync(cancellationToken);

        return labels;
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapListEventLabelsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/event-labels", async (string? search = null, IMediator mediator = null!) =>
        {
            var query = new Query(search);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("ListEventLabels")
        .WithTags("EventLabels")
        .Produces<List<EventLabelDto>>(200);

        return app;
    }
}
