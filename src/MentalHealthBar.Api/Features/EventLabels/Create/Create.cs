using FluentValidation;
using MediatR;
using MentalHealthBar.Api.Domain.EventLabels;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.EventLabels;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.EventLabels.Create;

public record Command(
    string Name,
    string? Description
) : IRequest<EventLabelDto>;

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

public class Handler(AppDbContext context, IValidator<Command> validator) : IRequestHandler<Command, EventLabelDto>
{
    private readonly AppDbContext _context = context;
    private readonly IValidator<Command> _validator = validator;

    public async Task<EventLabelDto> Handle(Command request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Check for duplicate (case-insensitive)
        var existing = await _context.EventLabels
            .FirstOrDefaultAsync(e => e.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"An event label with name '{request.Name}' already exists");
        }

        var label = new EventLabel(request.Name, request.Description);

        _context.EventLabels.Add(label);
        await _context.SaveChangesAsync(cancellationToken);

        return new EventLabelDto(
            label.Id,
            label.Name,
            label.Description,
            label.CreatedAt,
            null
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapCreateEventLabelEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/event-labels", async (Command command, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return Results.Created($"/api/event-labels/{result.Id}", result);
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
        .WithName("CreateEventLabel")
        .WithTags("EventLabels")
        .Produces<EventLabelDto>(201)
        .Produces(400)
        .Produces(409);

        return app;
    }
}
