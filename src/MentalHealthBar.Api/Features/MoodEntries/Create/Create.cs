using FluentValidation;
using MediatR;
using MentalHealthBar.Api.Domain.MoodEntries;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MentalHealthBar.Api.Features.MoodEntries.Create;

public record Command(
    int MoodScore,
    DateTimeOffset? RecordedAt,
    List<Guid>? EventLabelIds,
    string? Notes
) : IRequest<MoodEntryDto>;

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

        RuleFor(x => x.RecordedAt)
            .Must(recordedAt =>
            {
                if (!recordedAt.HasValue) return true;
                var maxFutureDate = DateTimeOffset.UtcNow.AddDays(30);
                return recordedAt.Value <= maxFutureDate;
            })
            .WithMessage("RecordedAt cannot be more than 30 days in the future");
    }
}

public class Handler(AppDbContext context, IValidator<Command> validator, ILogger<Handler> logger) : IRequestHandler<Command, MoodEntryDto>
{
    private readonly AppDbContext _context = context;
    private readonly IValidator<Command> _validator = validator;
    private readonly ILogger<Handler> _logger = logger;

    public async Task<MoodEntryDto> Handle(Command request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var recordedAt = request.RecordedAt ?? DateTimeOffset.UtcNow;
        var eventLabelIds = request.EventLabelIds ?? new List<Guid>();

        _logger.LogInformation("Creating MoodEntry with {Count} EventLabelIds: {Ids}",
            eventLabelIds.Count, string.Join(", ", eventLabelIds));

        var entry = new MoodEntry(
            request.MoodScore,
            recordedAt,
            eventLabelIds,
            request.Notes
        );

        _context.MoodEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("MoodEntry saved with ID: {Id}", entry.Id);

        // Load the entry with EventLabels via the junction table
        var savedEntry = await _context.MoodEntries
            .AsNoTracking()
            .Include(m => m.MoodEntryEventLabels)
                .ThenInclude(mel => mel.EventLabel)
            .FirstAsync(m => m.Id == entry.Id, cancellationToken);

        _logger.LogInformation("Loaded MoodEntry with {Count} EventLabels",
            savedEntry.MoodEntryEventLabels.Count);

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
            null
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapCreateMoodEntryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/mood-entries", async (Command command, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return Results.Created($"/api/mood-entries/{result.Id}", result);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new
                {
                    errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                });
            }
        })
        .WithName("CreateMoodEntry")
        .WithTags("MoodEntries")
        .Produces<MoodEntryDto>(201)
        .Produces(400);

        return app;
    }
}
