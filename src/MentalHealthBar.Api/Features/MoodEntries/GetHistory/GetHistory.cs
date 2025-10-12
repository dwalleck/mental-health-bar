using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.MoodEntries.GetHistory;

public record Query(
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null,
    Guid? EventLabelId = null,
    int Page = 1,
    int PageSize = 100
) : IRequest<MoodPagedResultDto>;

public class Handler(AppDbContext context) : IRequestHandler<Query, MoodPagedResultDto>
{
    private readonly AppDbContext _context = context;

    public async Task<MoodPagedResultDto> Handle(Query request, CancellationToken cancellationToken)
    {
        var query = _context.MoodEntries.AsQueryable();

        // Filter by date range
        if (request.StartDate.HasValue)
        {
            query = query.Where(m => m.RecordedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(m => m.RecordedAt <= request.EndDate.Value);
        }

        // Filter by event label using the junction table
        if (request.EventLabelId.HasValue)
        {
            query = query.Where(m => m.MoodEntryEventLabels.Any(mel => mel.EventLabelId == request.EventLabelId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageSize = Math.Clamp(request.PageSize, 1, 500);
        var page = Math.Max(1, request.Page);

        // Include the junction table and EventLabels
        var moodEntries = await query
            .OrderByDescending(m => m.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(m => m.MoodEntryEventLabels)
                .ThenInclude(mel => mel.EventLabel)
            .ToListAsync(cancellationToken);

        // Map to DTOs using the junction table relationships
        var items = moodEntries.Select(m => new MoodEntrySummaryDto(
            m.Id,
            m.MoodScore,
            m.RecordedAt,
            m.MoodEntryEventLabels.Select(mel => new EventLabelDto(
                mel.EventLabel.Id,
                mel.EventLabel.Name,
                mel.EventLabel.Description,
                mel.EventLabel.CreatedAt,
                mel.EventLabel.UpdatedAt)).ToList(),
            m.Notes,
            m.CreatedAt
        )).ToList();

        return new MoodPagedResultDto(items, totalCount, page, pageSize);
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapGetMoodHistoryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/mood-entries", async (
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            Guid? eventLabelId = null,
            int page = 1,
            int pageSize = 100,
            IMediator mediator = null!) =>
        {
            var query = new Query(startDate, endDate, eventLabelId, page, pageSize);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetMoodHistory")
        .WithTags("MoodEntries")
        .Produces<MoodPagedResultDto>(200);

        return app;
    }
}
