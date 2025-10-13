using System.Text;
using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace MentalHealthBar.Api.Features.Export.ToCsv;

public record Command : IRequest<ExportResult>
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool IncludeAssessments { get; init; } = true;
    public bool IncludeMoodEntries { get; init; } = true;
    public bool IncludeHealthMetrics { get; init; } = true;
    public bool IncludeEventLabels { get; init; } = true;
}

public record ExportResult(byte[] Data, string FileName, string ContentType);

public class Handler : IRequestHandler<Command, ExportResult>
{
    private readonly AppDbContext _context;

    public Handler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExportResult> Handle(Command request, CancellationToken cancellationToken)
    {
        // Validate date range
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value < request.StartDate.Value)
        {
            throw new ArgumentException("EndDate must be greater than or equal to StartDate");
        }

        var csv = new StringBuilder();

        // Export Assessments
        if (request.IncludeAssessments)
        {
            csv.AppendLine("=== ASSESSMENTS ===");
            csv.AppendLine("Id,Type,TotalScore,Severity,CompletedAt,CreatedAt");

            var startInstant = request.StartDate.HasValue
                ? Instant.FromDateTimeUtc(request.StartDate.Value.ToUniversalTime())
                : (Instant?)null;
            var endInstant = request.EndDate.HasValue
                ? Instant.FromDateTimeUtc(request.EndDate.Value.ToUniversalTime())
                : (Instant?)null;

            var assessments = await _context.Assessments
                .Where(a => (!startInstant.HasValue || a.CompletedAt >= startInstant.Value) &&
                           (!endInstant.HasValue || a.CompletedAt <= endInstant.Value))
                .OrderBy(a => a.CompletedAt)
                .ToListAsync(cancellationToken);

            foreach (var assessment in assessments)
            {
                csv.AppendLine($"{assessment.Id},{assessment.Type},{assessment.TotalScore},{assessment.Severity},{assessment.CompletedAt:O},{assessment.CreatedAt:O}");
            }
            csv.AppendLine();
        }

        // Export Mood Entries
        if (request.IncludeMoodEntries)
        {
            csv.AppendLine("=== MOOD ENTRIES ===");
            csv.AppendLine("Id,MoodScore,RecordedAt,EventLabels,Notes,CreatedAt");

            var startInstantMood = request.StartDate.HasValue
                ? Instant.FromDateTimeUtc(request.StartDate.Value.ToUniversalTime())
                : (Instant?)null;
            var endInstantMood = request.EndDate.HasValue
                ? Instant.FromDateTimeUtc(request.EndDate.Value.ToUniversalTime())
                : (Instant?)null;

            var moodEntries = await _context.MoodEntries
                .Where(m => (!startInstantMood.HasValue || m.RecordedAt >= startInstantMood.Value) &&
                           (!endInstantMood.HasValue || m.RecordedAt <= endInstantMood.Value))
                .OrderBy(m => m.RecordedAt)
                .Include(m => m.MoodEntryEventLabels)
                    .ThenInclude(mel => mel.EventLabel)
                .ToListAsync(cancellationToken);

            foreach (var entry in moodEntries)
            {
                var eventLabelNames = entry.MoodEntryEventLabels
                    .Select(mel => mel.EventLabel.Name)
                    .ToList();
                var labels = string.Join(";", eventLabelNames);
                var notes = entry.Notes?.Replace("\"", "\"\"").Replace("\n", " ") ?? "";
                csv.AppendLine($"{entry.Id},{entry.MoodScore},{entry.RecordedAt:O},\"{labels}\",\"{notes}\",{entry.CreatedAt:O}");
            }
            csv.AppendLine();
        }

        // Export Health Metrics
        if (request.IncludeHealthMetrics)
        {
            csv.AppendLine("=== HEALTH METRICS ===");
            csv.AppendLine("Id,Type,Value,RecordedDate,CreatedAt");

            var startDateOnly = request.StartDate.HasValue
                ? DateOnly.FromDateTime(request.StartDate.Value)
                : (DateOnly?)null;
            var endDateOnly = request.EndDate.HasValue
                ? DateOnly.FromDateTime(request.EndDate.Value)
                : (DateOnly?)null;

            var healthMetrics = await _context.HealthMetrics
                .Where(h => (!startDateOnly.HasValue || h.RecordedDate >= startDateOnly.Value) &&
                           (!endDateOnly.HasValue || h.RecordedDate <= endDateOnly.Value))
                .OrderBy(h => h.RecordedDate)
                .ToListAsync(cancellationToken);

            foreach (var metric in healthMetrics)
            {
                csv.AppendLine($"{metric.Id},{metric.Type},{metric.Value},{metric.RecordedDate:O},{metric.CreatedAt:O}");
            }
        }

        var data = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"mental-health-data-{DateTime.UtcNow:yyyy-MM-dd}.csv";

        return new ExportResult(data, fileName, "text/csv");
    }
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapExportToCsvEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/export/csv", async (Command command, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(command);
                return Results.File(result.Data, result.ContentType, result.FileName);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                // Return the actual error for debugging
                return Results.Problem($"Export failed: {ex.Message}", statusCode: 500);
            }
        })
        .WithName("ExportToCsv")
        .WithTags("Export")
        .Produces(200, contentType: "text/csv")
        .Produces(400);

        return app;
    }
}
