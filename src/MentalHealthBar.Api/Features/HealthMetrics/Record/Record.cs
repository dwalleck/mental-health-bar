using FluentValidation;
using MediatR;
using MentalHealthBar.Api.Domain.HealthMetrics;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.HealthMetrics;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.HealthMetrics.Record;

public record Command(
    string Type,
    decimal Value,
    DateOnly RecordedDate
) : IRequest<HealthMetricDto>;

public class Validator : AbstractValidator<Command>
{
    public Validator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(type => Enum.TryParse<MetricType>(type, ignoreCase: true, out _))
            .WithMessage("Invalid metric type. Valid types: SleepHours, WaterIntakeOz");

        RuleFor(x => x.Value)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Value must be greater than or equal to 0");

        RuleFor(x => x)
            .Must(command =>
            {
                if (!Enum.TryParse<MetricType>(command.Type, ignoreCase: true, out var type))
                    return true; // Let Type validation handle this

                return type switch
                {
                    MetricType.SleepHours => command.Value <= 24,
                    MetricType.WaterIntakeOz => command.Value <= 200,
                    _ => true
                };
            })
            .WithMessage(command =>
            {
                if (!Enum.TryParse<MetricType>(command.Type, ignoreCase: true, out var type))
                    return "Invalid metric type";

                return type switch
                {
                    MetricType.SleepHours => $"Sleep hours must be between 0 and 24. Received: {command.Value}",
                    MetricType.WaterIntakeOz => $"Water intake must be between 0 and 200 oz. Received: {command.Value}",
                    _ => "Invalid value for metric type"
                };
            });

        RuleFor(x => x.RecordedDate)
            .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            .WithMessage("RecordedDate cannot be more than 7 days in the future");
    }
}

public class Handler(AppDbContext context, IValidator<Command> validator) : IRequestHandler<Command, HealthMetricDto>
{
    private readonly AppDbContext _context = context;
    private readonly IValidator<Command> _validator = validator;

    public async Task<HealthMetricDto> Handle(Command request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var metricType = Enum.Parse<MetricType>(request.Type, ignoreCase: true);

        // Check for duplicate
        var existing = await _context.HealthMetrics
            .FirstOrDefaultAsync(h => h.Type == metricType && h.RecordedDate == request.RecordedDate, cancellationToken);

        if (existing != null)
        {
            throw new InvalidOperationException($"A {request.Type} metric already exists for {request.RecordedDate}");
        }

        var metric = new HealthMetric(metricType, request.Value, request.RecordedDate);

        _context.HealthMetrics.Add(metric);
        await _context.SaveChangesAsync(cancellationToken);

        return new HealthMetricDto(
            metric.Id,
            metric.Type.ToString(),
            metric.Value,
            metric.RecordedDate,
            metric.CreatedAt,
            null
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapRecordHealthMetricEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/health-metrics", async (Command command, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return Results.Created($"/api/health-metrics/{result.Id}", result);
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
        .WithName("RecordHealthMetric")
        .WithTags("HealthMetrics")
        .Produces<HealthMetricDto>(201)
        .Produces(400)
        .Produces(409);

        return app;
    }
}
