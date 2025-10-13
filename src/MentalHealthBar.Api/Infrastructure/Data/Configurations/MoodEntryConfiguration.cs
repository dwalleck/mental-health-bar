using MentalHealthBar.Api.Domain.MoodEntries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalHealthBar.Api.Infrastructure.Data.Configurations;

public class MoodEntryConfiguration : IEntityTypeConfiguration<MoodEntry>
{
    public void Configure(EntityTypeBuilder<MoodEntry> builder)
    {
        builder.ToTable("mood_entries");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever(); // Using NewId

        builder.Property(m => m.MoodScore)
            .IsRequired();

        builder.Property(m => m.RecordedAt)
            .IsRequired();

        // EventLabelIds is now handled via the junction table MoodEntryEventLabel
        // The EventLabelIds property on the entity is computed from the navigation property

        builder.Property(m => m.Notes)
            .HasMaxLength(500);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes for common queries
        builder.HasIndex(m => m.RecordedAt)
            .IsDescending()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("idx_mood_entries_recorded");

        // The EventLabelIds index is no longer needed - queries will use the junction table indexes

        // Global query filter for soft deletes
        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
