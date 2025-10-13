using FluentValidation;
using MentalHealthBar.Api.Features.Assessments.Complete;
using MentalHealthBar.Api.Features.Assessments.Delete;
using MentalHealthBar.Api.Features.Assessments.GetById;
using MentalHealthBar.Api.Features.Assessments.GetHistory;
using MentalHealthBar.Api.Features.Assessments.GetTemplate;
using MentalHealthBar.Api.Features.Assessments.GetTemplates;
using MentalHealthBar.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Serilog;
using CreateMoodEntry = MentalHealthBar.Api.Features.MoodEntries.Create.Endpoint;
using GetMoodHistory = MentalHealthBar.Api.Features.MoodEntries.GetHistory.Endpoint;
using GetMoodById = MentalHealthBar.Api.Features.MoodEntries.GetById.Endpoint;
using UpdateMoodEntry = MentalHealthBar.Api.Features.MoodEntries.Update.Endpoint;
using DeleteMoodEntry = MentalHealthBar.Api.Features.MoodEntries.Delete.Endpoint;
using GetMoodStats = MentalHealthBar.Api.Features.MoodEntries.GetStats.Endpoint;
using RecordHealthMetric = MentalHealthBar.Api.Features.HealthMetrics.Record.Endpoint;
using GetHealthMetricHistory = MentalHealthBar.Api.Features.HealthMetrics.GetHistory.Endpoint;
using GetHealthMetricById = MentalHealthBar.Api.Features.HealthMetrics.GetById.Endpoint;
using UpdateHealthMetric = MentalHealthBar.Api.Features.HealthMetrics.Update.Endpoint;
using DeleteHealthMetric = MentalHealthBar.Api.Features.HealthMetrics.Delete.Endpoint;
using CreateEventLabel = MentalHealthBar.Api.Features.EventLabels.Create.Endpoint;
using ListEventLabels = MentalHealthBar.Api.Features.EventLabels.List.Endpoint;
using GetEventLabelById = MentalHealthBar.Api.Features.EventLabels.GetById.Endpoint;
using UpdateEventLabel = MentalHealthBar.Api.Features.EventLabels.Update.Endpoint;
using DeleteEventLabel = MentalHealthBar.Api.Features.EventLabels.Delete.Endpoint;
using ExportToCsv = MentalHealthBar.Api.Features.Export.ToCsv.Endpoint;
using ExportToJson = MentalHealthBar.Api.Features.Export.ToJson.Endpoint;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
    {
        // Enable NodaTime support for proper timezone handling
        npgsqlOptions.UseNodaTime();
    });
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Add scoped services
builder.Services.AddScoped<AssessmentTemplateSeeder>();

// Add OpenAPI - .NET 10 built-in support
builder.Services.AddOpenApi();

// Configure JSON serialization for NodaTime
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
});

// Add CORS - Environment-aware configuration
// PRODUCTION WARNING: Configure allowed origins in appsettings.Production.json
// Never use AllowAnyOrigin() or wildcard origins in production
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? Array.Empty<string>();

if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    Log.Information("CORS enabled for origins: {Origins}", string.Join(", ", corsOrigins));
}
else
{
    Log.Warning("CORS is disabled - no allowed origins configured");
}

// Validate CORS configuration in Production
if (builder.Environment.IsProduction() && corsOrigins.Length > 0)
{
    // Reject wildcard origins
    if (corsOrigins.Any(origin => origin == "*" || origin.Contains("*")))
    {
        throw new InvalidOperationException(
            "Production environment cannot use wildcard CORS origins. " +
            "Configure specific allowed origins in appsettings.Production.json");
    }

    // Reject localhost origins
    if (corsOrigins.Any(origin => origin.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
    {
        Log.Warning("Production environment has localhost in CORS origins - this may be unintentional");
    }

    Log.Information("CORS production validation passed for {Count} origins", corsOrigins.Length);
}

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "db", "ready" })
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgres",
        tags: new[] { "db", "ready" });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

// Only enable CORS if configured
if (corsOrigins.Length > 0)
{
    app.UseCors();
}

// API endpoints - Assessments
app.MapGetTemplatesEndpoint();
app.MapGetTemplateEndpoint();
app.MapCompleteAssessmentEndpoint();
app.MapGetHistoryEndpoint();
app.MapGetByIdEndpoint();
app.MapDeleteEndpoint();

// API endpoints - Mood Entries
CreateMoodEntry.MapCreateMoodEntryEndpoint(app);
GetMoodHistory.MapGetMoodHistoryEndpoint(app);
GetMoodById.MapGetMoodByIdEndpoint(app);
UpdateMoodEntry.MapUpdateMoodEntryEndpoint(app);
DeleteMoodEntry.MapDeleteMoodEntryEndpoint(app);
GetMoodStats.MapGetMoodStatsEndpoint(app);

// API endpoints - Health Metrics
RecordHealthMetric.MapRecordHealthMetricEndpoint(app);
GetHealthMetricHistory.MapGetHealthMetricHistoryEndpoint(app);
GetHealthMetricById.MapGetHealthMetricByIdEndpoint(app);
UpdateHealthMetric.MapUpdateHealthMetricEndpoint(app);
DeleteHealthMetric.MapDeleteHealthMetricEndpoint(app);

// API endpoints - Event Labels
CreateEventLabel.MapCreateEventLabelEndpoint(app);
ListEventLabels.MapListEventLabelsEndpoint(app);
GetEventLabelById.MapGetEventLabelByIdEndpoint(app);
UpdateEventLabel.MapUpdateEventLabelEndpoint(app);
DeleteEventLabel.MapDeleteEventLabelEndpoint(app);

// API endpoints - Export
ExportToCsv.MapExportToCsvEndpoint(app);
ExportToJson.MapExportToJsonEndpoint(app);

// Health check endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                exception = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
}).WithName("HealthCheck").WithTags("Health");

// Liveness probe - quick check without dependencies
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
}).WithName("LivenessCheck").WithTags("Health");

// Readiness probe - includes database checks
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).WithName("ReadinessCheck").WithTags("Health");

// Seed assessment templates on startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<AssessmentTemplateSeeder>();
    await seeder.SeedAsync();
}

try
{
    Log.Information("Starting Mental Health Bar API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible to tests
public partial class Program { }
