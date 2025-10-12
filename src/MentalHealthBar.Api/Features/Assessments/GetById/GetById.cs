using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.Assessments;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.Assessments.GetById;

public record Query(Guid Id) : IRequest<AssessmentDetailDto?>;

public class Handler(AppDbContext context) : IRequestHandler<Query, AssessmentDetailDto?>
{
    private readonly AppDbContext _context = context;

    public async Task<AssessmentDetailDto?> Handle(Query request, CancellationToken cancellationToken)
    {
        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (assessment == null)
        {
            return null;
        }

        return new AssessmentDetailDto(
            assessment.Id,
            assessment.Type.ToString(),
            assessment.Responses,
            assessment.TotalScore,
            assessment.Severity.ToString(),
            assessment.CompletedAt,
            assessment.CreatedAt,
            assessment.UpdatedAt
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapGetByIdEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assessments/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var assessment = await mediator.Send(new Query(id));
            return assessment != null ? Results.Ok(assessment) : Results.NotFound();
        })
        .WithName("GetAssessmentById")
        .WithTags("Assessments")
        .Produces<AssessmentDetailDto>(200)
        .Produces(404);

        return app;
    }
}
