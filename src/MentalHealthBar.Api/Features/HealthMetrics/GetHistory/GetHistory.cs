using MediatR;
using MentalHealthBar.Api.Domain.HealthMetrics;
using MentalHealthBar.Api.Infrastructure.Constants;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.HealthMetrics.GetHistory;

public record Query(
    string? Type = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int Page = 1,
    int PageSize = 100
) : IRequest<HealthMetricPagedResultDto>;

public class Handler(AppDbContext context) : IRequestHandler<Query, HealthMetricPagedResultDto>
{
    private readonly AppDbContext _context = context;

    public async Task<HealthMetricPagedResultDto> Handle(Query request, CancellationToken cancellationToken)
    {
        var query = _context.HealthMetrics.AsQueryable();

        // Filter by type
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            if (Enum.TryParse<MetricType>(request.Type, ignoreCase: true, out var metricType))
            {
                query = query.Where(h => h.Type == metricType);
            }
        }

        // Filter by date range
        if (request.StartDate.HasValue)
        {
            query = query.Where(h => h.RecordedDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(h => h.RecordedDate <= request.EndDate.Value);
        }

        // Pagination: Using separate CountAsync() and Skip().Take() queries
        // This is acceptable for current scope and typical dataset sizes
        // For very large datasets (millions of records), consider:
        // - Cursor-based pagination (using ID or timestamp as cursor)
        // - Fetch N+1 items to determine "has more" without full count
        var totalCount = await query.CountAsync(cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, PaginationConstants.HealthMetricsMinPageSize, PaginationConstants.HealthMetricsMaxPageSize);
        var page = Math.Max(1, request.Page);

        var items = await query
            .OrderByDescending(h => h.RecordedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HealthMetricSummaryDto(
                h.Id,
                h.Type.ToString(),
                h.Value,
                h.RecordedDate,
                h.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new HealthMetricPagedResultDto(items, totalCount, page, pageSize);
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapGetHealthMetricHistoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health-metrics", async (
            string? type = null,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            int page = 1,
            int pageSize = 100,
            IMediator mediator = null!) =>
        {
            var query = new Query(type, startDate, endDate, page, pageSize);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetHealthMetricHistory")
        .WithTags("HealthMetrics")
        .Produces<HealthMetricPagedResultDto>(200);

        return app;
    }
}
