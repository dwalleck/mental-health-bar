using Microsoft.EntityFrameworkCore;

namespace MentalHealthBar.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets will be added when domain models are created
    // public DbSet<Assessment> Assessments => Set<Assessment>();
    // public DbSet<MoodEntry> MoodEntries => Set<MoodEntry>();
    // public DbSet<EventLabel> EventLabels => Set<EventLabel>();
    // public DbSet<HealthMetric> HealthMetrics => Set<HealthMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Entity configurations will be applied here
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
