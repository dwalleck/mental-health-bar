using MediatR;
using MentalHealthBar.Api.Domain.Assessments;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.Assessments;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.Assessments.GetTemplate;

public record Query(string Type) : IRequest<TemplateDetailDto?>;

public class Handler(AppDbContext context) : IRequestHandler<Query, TemplateDetailDto?>
{
    private readonly AppDbContext _context = context;

    public async Task<TemplateDetailDto?> Handle(Query request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AssessmentType>(request.Type, ignoreCase: true, out var assessmentType))
        {
            return null;
        }

        var template = await _context.AssessmentTemplates
            .FirstOrDefaultAsync(t => t.Type == assessmentType, cancellationToken);

        if (template == null)
        {
            return null;
        }

        return new TemplateDetailDto(
            template.Id,
            template.Type.ToString(),
            template.Name,
            template.Description,
            template.Questions.Select(q => new QuestionDto(
                q.Id,
                q.Text,
                q.Options.Select(o => new AnswerOptionDto(o.Value, o.Label)).ToList()
            )).ToList(),
            new ScoringRulesDto(
                template.ScoringRules.MinScore,
                template.ScoringRules.MaxScore,
                template.ScoringRules.SeverityRanges
            )
        );
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapGetTemplateEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assessments/templates/{type}", async (string type, IMediator mediator) =>
        {
            var template = await mediator.Send(new Query(type));
            return template != null ? Results.Ok(template) : Results.NotFound();
        })
        .WithName("GetAssessmentTemplate")
        .WithTags("Assessments")
        .Produces<TemplateDetailDto>(200)
        .Produces(404);

        return app;
    }
}
