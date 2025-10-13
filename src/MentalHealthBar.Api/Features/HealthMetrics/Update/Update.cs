using FluentValidation;
using MediatR;
using MentalHealthBar.Api.Domain.HealthMetrics;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace MentalHealthBar.Api.Features.HealthMetrics.Update;

public record Command(
    Guid Id,
    decimal Value
) : IRequest<HealthMetricDto?>;

public class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Value must be greater than or equal to 0");
    }
}

public class Handler(AppDbContext context, IValidator<Command> validator) : IRequestHandler<Command, HealthMetricDto?>
{
    private readonly AppDbContext _context = context;
    private readonly IValidator<Command> _validator = validator;

    public async Task<HealthMetricDto?> Handle(Command request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var metric = await _context.HealthMetrics
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (metric == null)
        {
            return null;
        }

        // Validate value range based on metric type
        var maxValue = metric.Type switch
        {
            MetricType.SleepHours => 24,
            MetricType.WaterIntakeOz => 200,
            _ => throw new InvalidOperationException("Unknown metric type")
        };

        if (request.Value > maxValue)
        {
            var unit = metric.Type == MetricType.SleepHours ? "hours" : "oz";
            throw new ValidationException($"{metric.Type} must be between 0 and {maxValue} {unit}. Received: {request.Value}");
        }

        metric.Update(request.Value);
        await _context.SaveChangesAsync(cancellationToken);

        return new HealthMetricDto(
            metric.Id,
            metric.Type.ToString(),
            metric.Value,
            metric.RecordedDate,
            metric.CreatedAt,
            metric.UpdatedAt ?? SystemClock.Instance.GetCurrentInstant()
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapUpdateHealthMetricEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPut("/api/health-metrics/{id:guid}", async (Guid id, Command command, IMediator mediator) =>
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
                    errors = ex is FluentValidation.ValidationException fluentEx
                        ? fluentEx.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                        : new[] { new { field = "", message = ex.Message } }
                });
            }
        })
        .WithName("UpdateHealthMetric")
        .WithTags("HealthMetrics")
        .Produces<HealthMetricDto>(200)
        .Produces(400)
        .Produces(404);

        return app;
    }
}
