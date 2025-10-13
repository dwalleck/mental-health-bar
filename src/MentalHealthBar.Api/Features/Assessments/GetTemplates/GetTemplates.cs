using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.Assessments;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.Assessments.GetTemplates;

public record Query : IRequest<List<TemplateDto>>;

public class Handler(AppDbContext context) : IRequestHandler<Query, List<TemplateDto>>
{
    private readonly AppDbContext _context = context;

    public async Task<List<TemplateDto>> Handle(Query request, CancellationToken cancellationToken)
    {
        var templates = await _context.AssessmentTemplates
            .OrderBy(t => t.Type)
            .Select(t => new TemplateDto(
                t.Id,
                t.Type.ToString(),
                t.Name,
                t.Description
            ))
            .ToListAsync(cancellationToken);

        return templates;
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapGetTemplatesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/assessments/templates", async (IMediator mediator) =>
        {
            var templates = await mediator.Send(new Query());
            return Results.Ok(templates);
        })
        .WithName("GetAssessmentTemplates")
        .WithTags("Assessments")
        .Produces<List<TemplateDto>>(200);

        return app;
    }
}
