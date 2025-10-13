using FluentValidation;
using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.EventLabels;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace MentalHealthBar.Api.Features.EventLabels.Update;

public record Command(
    Guid Id,
    string Name,
    string? Description
) : IRequest<EventLabelDto?>;

public class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .Length(1, 50)
            .WithMessage("Name must be between 1 and 50 characters")
            .Matches(@"^[a-zA-Z0-9\s\-]+$")
            .WithMessage("Name must contain only alphanumeric characters, spaces, and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(200)
            .WithMessage("Description must not exceed 200 characters");
    }
}

public class Handler(AppDbContext context, IValidator<Command> validator) : IRequestHandler<Command, EventLabelDto?>
{
    private readonly AppDbContext _context = context;
    private readonly IValidator<Command> _validator = validator;

    public async Task<EventLabelDto?> Handle(Command request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var label = await _context.EventLabels
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (label == null)
        {
            return null;
        }

        // Check for name conflict (case-insensitive, excluding current label)
        var existing = await _context.EventLabels
            .FirstOrDefaultAsync(e => e.Id != request.Id && e.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"An event label with name '{request.Name}' already exists");
        }

        label.Update(request.Name, request.Description);
        await _context.SaveChangesAsync(cancellationToken);

        return new EventLabelDto(
            label.Id,
            label.Name,
            label.Description,
            label.CreatedAt,
            label.UpdatedAt ?? SystemClock.Instance.GetCurrentInstant()
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapUpdateEventLabelEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/event-labels/{id:guid}", async (Guid id, Command command, IMediator mediator) =>
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
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .WithName("UpdateEventLabel")
        .WithTags("EventLabels")
        .Produces<EventLabelDto>(200)
        .Produces(400)
        .Produces(404)
        .Produces(409);

        return app;
    }
}
