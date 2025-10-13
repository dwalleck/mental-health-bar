using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.Assessments.Delete;

public record Command(Guid Id) : IRequest<bool>;

public class Handler(AppDbContext context) : IRequestHandler<Command, bool>
{
    private readonly AppDbContext _context = context;

    public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
    {
        var assessment = await _context.Assessments
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (assessment == null)
        {
            return false;
        }

        _context.Assessments.Remove(assessment);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapDeleteEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/assessments/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var deleted = await mediator.Send(new Command(id));
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteAssessment")
        .WithTags("Assessments")
        .Produces(204)
        .Produces(404);

        return app;
    }
}
