using FluentValidation;
using MassTransit;
using MediatR;
using MentalHealthBar.Api.Domain.Assessments;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.Assessments;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.Assessments.Complete;

public record Command(
    string Type,
    Dictionary<string, int> Responses,
    DateTimeOffset? CompletedAt
) : IRequest<AssessmentResultDto>;

public class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(type => Enum.TryParse<AssessmentType>(type, ignoreCase: true, out _))
            .WithMessage("Invalid assessment type. Valid types: PHQ9, BDI, GAD7, BAI");

        RuleFor(x => x.Responses)
            .NotEmpty()
            .WithMessage("Responses are required");

        RuleFor(x => x.CompletedAt)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow)
            .When(x => x.CompletedAt.HasValue)
            .WithMessage("CompletedAt cannot be in the future");

        RuleFor(x => x.Responses)
            .Must((command, responses) =>
            {
                if (!Enum.TryParse<AssessmentType>(command.Type, ignoreCase: true, out var type))
                    return true; // Let the Type validation handle this

                var expectedCount = type switch
                {
                    AssessmentType.PHQ9 => 9,
                    AssessmentType.BDI => 21,
                    AssessmentType.GAD7 => 7,
                    AssessmentType.BAI => 21,
                    _ => 0
                };

                return responses.Count == expectedCount;
            })
            .WithMessage(command =>
            {
                if (!Enum.TryParse<AssessmentType>(command.Type, ignoreCase: true, out var type))
                    return "Invalid assessment type";

                var expectedCount = type switch
                {
                    AssessmentType.PHQ9 => 9,
                    AssessmentType.BDI => 21,
                    AssessmentType.GAD7 => 7,
                    AssessmentType.BAI => 21,
                    _ => 0
                };

                return $"{command.Type} requires exactly {expectedCount} responses. Received: {command.Responses.Count}";
            });

        RuleFor(x => x.Responses)
            .Must(responses => responses.Values.All(v => v >= 0 && v <= 3))
            .WithMessage("All response values must be between 0 and 3");
    }
}

public class Handler(AppDbContext context, IValidator<Command> validator) : IRequestHandler<Command, AssessmentResultDto>
{
    private readonly AppDbContext _context = context;
    private readonly IValidator<Command> _validator = validator;

    public async Task<AssessmentResultDto> Handle(Command request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var assessmentType = Enum.Parse<AssessmentType>(request.Type, ignoreCase: true);
        var completedAt = request.CompletedAt ?? DateTimeOffset.UtcNow;

        var assessment = new Assessment(assessmentType, request.Responses, completedAt);

        _context.Assessments.Add(assessment);
        await _context.SaveChangesAsync(cancellationToken);

        return new AssessmentResultDto(
            assessment.Id,
            assessment.Type.ToString(),
            assessment.TotalScore,
            assessment.Severity.ToString(),
            assessment.CompletedAt
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapCompleteAssessmentEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/assessments", async (Command command, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return Results.Created($"/api/assessments/{result.Id}", result);
            }
            catch (ValidationException ex)
            {
                return Results.BadRequest(new
                {
                    errors = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                });
            }
        })
        .WithName("CompleteAssessment")
        .WithTags("Assessments")
        .Produces<AssessmentResultDto>(201)
        .Produces(400);

        return app;
    }
}
