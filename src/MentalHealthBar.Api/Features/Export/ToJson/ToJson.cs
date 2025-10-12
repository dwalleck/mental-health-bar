using System.Text;
using System.Text.Json;
using MediatR;
using MentalHealthBar.Api.Infrastructure.Data;
using MentalHealthBar.Contracts.Responses.Export;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Features.Export.ToJson;

public record Command : IRequest<ExportResult>
{
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public bool IncludeAssessments { get; init; } = true;
    public bool IncludeMoodEntries { get; init; } = true;
    public bool IncludeHealthMetrics { get; init; } = true;
    public bool IncludeEventLabels { get; init; } = true;
}

public record ExportResult(byte[] Data, string FileName, string ContentType);

public class Handler(AppDbContext context) : IRequestHandler<Command, ExportResult>
{
    private readonly AppDbContext _context = context;

    public async Task<ExportResult> Handle(Command request, CancellationToken cancellationToken)
    {
        // Validate date range
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate.Value < request.StartDate.Value)
        {
            throw new ArgumentException("EndDate must be greater than or equal to StartDate");
        }

        var actualStartDate = request.StartDate ?? DateTimeOffset.UtcNow.AddYears(-1); // Default to 1 year ago
        var actualEndDate = request.EndDate ?? DateTimeOffset.UtcNow;

        // Export Assessments
        var assessments = request.IncludeAssessments
            ? await _context.Assessments
                .Where(a => (!request.StartDate.HasValue || a.CompletedAt >= request.StartDate.Value) &&
                           (!request.EndDate.HasValue || a.CompletedAt <= request.EndDate.Value))
                .OrderBy(a => a.CompletedAt)
                .Select(a => new AssessmentExport(
                    a.Id,
                    a.Type.ToString(),
                    a.Responses,
                    a.TotalScore,
                    a.Severity.ToString(),
                    a.CompletedAt,
                    a.CreatedAt
                ))
                .ToListAsync(cancellationToken)
            : new List<AssessmentExport>();

        // Export Mood Entries
        var moodEntriesList = request.IncludeMoodEntries
            ? await _context.MoodEntries
                .Where(m => (!request.StartDate.HasValue || m.RecordedAt >= request.StartDate.Value) &&
                           (!request.EndDate.HasValue || m.RecordedAt <= request.EndDate.Value))
                .OrderBy(m => m.RecordedAt)
                .Include(m => m.MoodEntryEventLabels)
                    .ThenInclude(mel => mel.EventLabel)
                .ToListAsync(cancellationToken)
            : new List<Domain.MoodEntries.MoodEntry>();

        var moodEntries = moodEntriesList.Select(m => new MoodEntryExport(
            m.Id,
            m.MoodScore,
            GetMoodLabel(m.MoodScore),
            m.RecordedAt,
            m.MoodEntryEventLabels.Select(mel => mel.EventLabel.Name).ToList(),
            m.Notes,
            m.CreatedAt
        )).ToList();

        // Export Health Metrics
        var healthMetrics = request.IncludeHealthMetrics
            ? await _context.HealthMetrics
                .Where(h => (!request.StartDate.HasValue || h.RecordedDate >= DateOnly.FromDateTime(request.StartDate.Value.DateTime)) &&
                           (!request.EndDate.HasValue || h.RecordedDate <= DateOnly.FromDateTime(request.EndDate.Value.DateTime)))
                .OrderBy(h => h.RecordedDate)
                .Select(h => new HealthMetricExport(
                    h.Id,
                    h.Type.ToString(),
                    h.Value,
                    h.RecordedDate,
                    h.CreatedAt
                ))
                .ToListAsync(cancellationToken)
            : new List<HealthMetricExport>();

        // Export Event Labels (distinct list of all event label names used)
        var eventLabels = request.IncludeEventLabels
            ? await _context.EventLabels
                .Select(el => el.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToListAsync(cancellationToken)
            : new List<string>();

        var exportData = new ExportDataResponse(
            assessments,
            moodEntries,
            healthMetrics,
            eventLabels,
            new DateRangeExport(actualStartDate, actualEndDate),
            DateTimeOffset.UtcNow
        );

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var data = Encoding.UTF8.GetBytes(json);
        var fileName = $"mental-health-data-{DateTime.UtcNow:yyyy-MM-dd}.json";

        return new ExportResult(data, fileName, "application/json");
    }

    private static string GetMoodLabel(int score) => score switch
    {
        1 => "Worst",
        2 => "Below Average",
        3 => "Average",
        4 => "Above Average",
        5 => "Best",
        _ => "Unknown"
    };
}

public static class Endpoint
{
    public static IEndpointRouteBuilder MapExportToJsonEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/export/json", async (Command command, IMediator mediator) =>
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
        .WithName("ExportToJson")
        .WithTags("Export")
        .Produces(200, contentType: "application/json")
        .Produces(400);

        return app;
    }
}
