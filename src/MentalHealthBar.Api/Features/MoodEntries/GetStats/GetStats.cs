using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace MentalHealthBar.Api.Features.MoodEntries.GetStats;

public record Query(
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<MoodStatsDto>;

public class Handler(AppDbContext context) : IRequestHandler<Query, MoodStatsDto>
{
    private readonly AppDbContext _context = context;

    public async Task<MoodStatsDto> Handle(Query request, CancellationToken cancellationToken)
    {
        var query = _context.MoodEntries.AsQueryable();

        // Filter by date range
        if (request.StartDate.HasValue)
        {
            var startInstant = Instant.FromDateTimeUtc(request.StartDate.Value.ToUniversalTime());
            query = query.Where(m => m.RecordedAt >= startInstant);
        }

        if (request.EndDate.HasValue)
        {
            var endInstant = Instant.FromDateTimeUtc(request.EndDate.Value.ToUniversalTime());
            query = query.Where(m => m.RecordedAt <= endInstant);
        }

        var entries = await query
            .Select(m => m.MoodScore)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return new MoodStatsDto(0, 0, 0, new Dictionary<int, int>());
        }

        var count = entries.Count;
        var average = entries.Average();

        // Calculate median
        var sortedScores = entries.OrderBy(s => s).ToList();
        var median = sortedScores.Count % 2 == 0
            ? (sortedScores[sortedScores.Count / 2 - 1] + sortedScores[sortedScores.Count / 2]) / 2
            : sortedScores[sortedScores.Count / 2];

        // Calculate distribution
        var distribution = entries
            .GroupBy(s => s)
            .ToDictionary(g => g.Key, g => g.Count());

        // Ensure all scores 1-5 are represented
        for (int i = 1; i <= 5; i++)
        {
            if (!distribution.ContainsKey(i))
            {
                distribution[i] = 0;
            }
        }

        return new MoodStatsDto(count, average, median, distribution);
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapGetMoodStatsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/mood-entries/stats", async (
            DateTime? startDate = null,
            DateTime? endDate = null,
            IMediator mediator = null!) =>
        {
            var query = new Query(startDate, endDate);
            var result = await mediator.Send(query);
            return Results.Ok(result);
        })
        .WithName("GetMoodStats")
        .WithTags("MoodEntries")
        .Produces<MoodStatsDto>(200);

        return app;
    }
}
