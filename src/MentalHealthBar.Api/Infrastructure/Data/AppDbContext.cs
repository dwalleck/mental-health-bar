using MentalHealthBar.Api.Domain.Assessments;
using MentalHealthBar.Api.Domain.EventLabels;
using MentalHealthBar.Api.Domain.HealthMetrics;
using MentalHealthBar.Api.Domain.MoodEntries;
using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentTemplate> AssessmentTemplates => Set<AssessmentTemplate>();
    public DbSet<MoodEntry> MoodEntries => Set<MoodEntry>();
    public DbSet<EventLabel> EventLabels => Set<EventLabel>();
    public DbSet<MoodEntryEventLabel> MoodEntryEventLabels => Set<MoodEntryEventLabel>();
    public DbSet<HealthMetric> HealthMetrics => Set<HealthMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
