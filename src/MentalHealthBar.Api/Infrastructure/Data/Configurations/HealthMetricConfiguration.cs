using MentalHealthBar.Api.Domain.HealthMetrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalHealthBar.Api.Infrastructure.Data.Configurations;

public class HealthMetricConfiguration : IEntityTypeConfiguration<HealthMetric>
{
    public void Configure(EntityTypeBuilder<HealthMetric> builder)
    {
        builder.ToTable("health_metrics");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever(); // Using NewId

        builder.Property(h => h.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(h => h.Value)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(h => h.RecordedDate)
            .IsRequired();

        builder.Property(h => h.CreatedAt)
            .IsRequired();

        builder.Property(h => h.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique constraint for Type + RecordedDate
        builder.HasIndex(h => new { h.Type, h.RecordedDate })
            .IsUnique()
            .HasDatabaseName("uq_health_metrics_type_date");

        // Indexes for common queries
        builder.HasIndex(h => h.RecordedDate)
            .IsDescending()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("idx_health_metrics_date");

        builder.HasIndex(h => new { h.Type, h.RecordedDate })
            .IsDescending()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("idx_health_metrics_type_date");

        // Check constraint for positive values
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_HealthMetric_Value_Positive", "\"Value\" >= 0");
        });

        // Global query filter for soft deletes
        builder.HasQueryFilter(h => !h.IsDeleted);
    }
}
