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
        RuleFor(x => x.MoodScore)
            .InclusiveBetween(1, 5)
            .WithMessage("Mood score must be between 1 (Worst) and 5 (Best)");

        RuleFor(x => x.EventLabelIds)
            .Must(labelIds => labelIds == null || labelIds.Count <= 10)
            .WithMessage("Maximum 10 event labels allowed per entry");

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .WithMessage("Notes must not exceed 500 characters");
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

        // Include the junction table relationships to enable updating
        var entry = await _context.MoodEntries
            .Include(m => m.MoodEntryEventLabels)
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (entry == null)
        {
            return null;
        }

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
