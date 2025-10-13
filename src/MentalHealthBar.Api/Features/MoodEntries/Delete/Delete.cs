using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.MoodEntries.Delete;

public record Command(Guid Id) : IRequest<bool>;

public class Handler(AppDbContext context) : IRequestHandler<Command, bool>
{
    private readonly AppDbContext _context = context;

    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
        var entry = await _context.MoodEntries
            .IgnoreQueryFilters() // Need to check even if already soft-deleted
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (entry == null || entry.IsDeleted)
        {
            return false;
        }

        entry.SoftDelete();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapDeleteMoodEntryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/mood-entries/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var deleted = await mediator.Send(new Command(id));
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteMoodEntry")
        .WithTags("MoodEntries")
        .Produces(204)
        .Produces(404);

        return app;
    }
}
