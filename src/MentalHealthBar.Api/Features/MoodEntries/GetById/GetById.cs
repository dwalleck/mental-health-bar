using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.MoodEntries.GetById;

public record Query(Guid Id) : IRequest<MoodEntryDto?>;

public class Handler(AppDbContext context) : IRequestHandler<Query, MoodEntryDto?>
{
    private readonly AppDbContext _context = context;

    public async Task<MoodEntryDto?> Handle(Query request, CancellationToken cancellationToken)
    {
        // Include the junction table and EventLabels
        var entry = await _context.MoodEntries
            .Include(m => m.MoodEntryEventLabels)
                .ThenInclude(mel => mel.EventLabel)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (entry == null)
        {
            return null;
        }

        // Map EventLabels from the junction table relationships
        var eventLabels = entry.MoodEntryEventLabels
            .Select(mel => new EventLabelDto(
                mel.EventLabel.Id,
                mel.EventLabel.Name,
                mel.EventLabel.Description,
                mel.EventLabel.CreatedAt,
                mel.EventLabel.UpdatedAt))
            .ToList();

        return new MoodEntryDto(
            entry.Id,
            entry.MoodScore,
            entry.RecordedAt,
            eventLabels,
            entry.Notes,
            entry.CreatedAt,
            entry.UpdatedAt
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapGetMoodByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/mood-entries/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var entry = await mediator.Send(new Query(id));
            return entry != null ? Results.Ok(entry) : Results.NotFound();
        })
        .WithName("GetMoodEntryById")
        .WithTags("MoodEntries")
        .Produces<MoodEntryDto>(200)
        .Produces(404);

        return app;
    }
}
