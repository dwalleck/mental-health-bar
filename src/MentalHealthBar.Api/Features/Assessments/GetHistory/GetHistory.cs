using MediatR;
using MentalHealthBar.Api.Domain.Assessments;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.Assessments;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.Assessments.GetHistory;

public record Query(
    string? Type = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    int Page = 1,
    int PageSize = 100
) : IRequest<AssessmentPagedResultDto>;

public class Handler(AppDbContext context) : IRequestHandler<Query, AssessmentPagedResultDto>
{
    private readonly AppDbContext _context = context;

    public async Task<AssessmentPagedResultDto> Handle(Query request, CancellationToken cancellationToken)
    {
        var query = _context.Assessments.AsQueryable();

        // Filter by type
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            if (Enum.TryParse<AssessmentType>(request.Type, ignoreCase: true, out var assessmentType))
            {
                query = query.Where(a => a.Type == assessmentType);
            }
        }

        // Filter by date range
        if (request.StartDate.HasValue)
        {
            query = query.Where(a => a.CompletedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(a => a.CompletedAt <= request.EndDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page = Math.Max(1, request.Page);

        var items = await query
            .OrderByDescending(a => a.CompletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AssessmentSummaryDto(
                a.Id,
                a.Type.ToString(),
                a.TotalScore,
                a.Severity.ToString(),
                a.CompletedAt,
                a.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return new AssessmentPagedResultDto(items, totalCount, page, pageSize);
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapGetHistoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assessments", async (
            string? type = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            int page = 1,
            int pageSize = 100,
            IMediator mediator = null!) =>
        {
            var query = new Query(type, startDate, endDate, page, pageSize);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetAssessmentHistory")
        .WithTags("Assessments")
        .Produces<AssessmentPagedResultDto>(200);

        return app;
    }
}
