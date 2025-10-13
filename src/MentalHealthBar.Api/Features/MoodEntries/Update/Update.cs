using FluentValidation;
using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace MentalHealthBar.Api.Features.MoodEntries.Update;

public record Command(
    Guid Id,
    int MoodScore,
    List<Guid>? EventLabelIds,
    string? Notes
) : IRequest<MoodEntryDto?>;

public class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        // Only validate request-level concerns here
        // Domain validation (score range, max tags, notes length) is handled by MoodEntry entity

        // No request-level validation needed for this endpoint
        // All business rules are enforced by the domain
    }
}

public class Handler(AppDbContext context, IValidator<Command> validator) : IRequestHandler<Command, MoodEntryDto?>
{
    private readonly AppDbContext _context = context;
    private readonly IValidator<Command> _validator = validator;

    public async Task<MoodEntryDto?> Handle(Command request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        try
        {
            // Include the junction table relationships to enable updating
            var entry = await _context.MoodEntries
                .Include(m => m.MoodEntryEventLabels)
                .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

            if (entry == null)
            {
                return null;
            }

            // Domain entity will validate business rules (score range, max tags, notes length)
            entry.Update(request.MoodScore, request.EventLabelIds ?? new List<Guid>(), request.Notes);

            await _context.SaveChangesAsync(cancellationToken);

            // Load the updated entry with EventLabels via the junction table
            var savedEntry = await _context.MoodEntries
                .AsNoTracking()
                .Include(m => m.MoodEntryEventLabels)
                    .ThenInclude(mel => mel.EventLabel)
                .FirstAsync(m => m.Id == entry.Id, cancellationToken);

            // Map EventLabels from the junction table relationships
            var eventLabels = savedEntry.MoodEntryEventLabels
                .Select(mel => new EventLabelDto(
                    mel.EventLabel.Id,
                    mel.EventLabel.Name,
                    mel.EventLabel.Description,
                    mel.EventLabel.CreatedAt,
                    mel.EventLabel.UpdatedAt))
                .ToList();

            return new MoodEntryDto(
                savedEntry.Id,
                savedEntry.MoodScore,
                savedEntry.RecordedAt,
                eventLabels,
                savedEntry.Notes,
                savedEntry.CreatedAt,
                savedEntry.UpdatedAt ?? SystemClock.Instance.GetCurrentInstant()
            );
        }
        catch (ArgumentOutOfRangeException ex)
        {
            // Domain validation failed - convert to FluentValidation exception for consistent error handling
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(ex.ParamName ?? "Value", ex.Message)
            });
        }
        catch (ArgumentException ex)
        {
            // Domain validation failed - convert to FluentValidation exception for consistent error handling
            throw new ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure(ex.ParamName ?? "Value", ex.Message)
            });
        }
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapUpdateMoodEntryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/mood-entries/{id:guid}", async (Guid id, Command command, IMediator mediator) =>
        {
            try
            {
                var commandWithId = command with { Id = id };
                var result = await mediator.Send(commandWithId);
                return result != null ? Results.Ok(result) : Results.NotFound();
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new
                {
                    errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                });
            }
        })
        .WithName("UpdateMoodEntry")
        .WithTags("MoodEntries")
        .Produces<MoodEntryDto>(200)
        .Produces(400)
        .Produces(404);

        return app;
    }
}
