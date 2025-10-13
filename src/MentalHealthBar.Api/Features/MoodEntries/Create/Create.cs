using FluentValidation;
using MediatR;
using MentalHealthBar.Api.Domain.MoodEntries;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.EventLabels;
using MentalHealthBar.Contracts.Responses.MoodEntries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace MentalHealthBar.Api.Features.MoodEntries.Create;

public record Command(
    int MoodScore,
    DateTime? RecordedAt,
    List<Guid>? EventLabelIds,
    string? Notes
) : IRequest<MoodEntryDto>;

public class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        // Only validate request-level concerns here
        // Domain validation (score range, max tags, notes length, date validation) is handled by MoodEntry entity

        // Ensure RecordedAt is provided in UTC if specified
        RuleFor(x => x.RecordedAt)
            .Must(recordedAt => !recordedAt.HasValue || recordedAt.Value.Kind == DateTimeKind.Utc)
            .When(x => x.RecordedAt.HasValue)
            .WithMessage("RecordedAt must be in UTC format");
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

        try
        {
            var recordedAt = request.RecordedAt.HasValue
                ? Instant.FromDateTimeUtc(request.RecordedAt.Value.ToUniversalTime())
                : SystemClock.Instance.GetCurrentInstant();
            var eventLabelIds = request.EventLabelIds ?? new List<Guid>();

            // Validate recordedAt at domain level
            MoodEntry.ValidateRecordedAt(recordedAt);

            _logger.LogInformation("Creating MoodEntry with {Count} EventLabelIds: {Ids}",
                eventLabelIds.Count, string.Join(", ", eventLabelIds));

            // Domain entity will validate business rules (score range, max tags, notes length)
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
