using MentalHealthBar.Api.Domain.MoodEntries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalHealthBar.Api.Infrastructure.Data.Configurations;

public class MoodEntryEventLabelConfiguration : IEntityTypeConfiguration<MoodEntryEventLabel>
{
    public void Configure(EntityTypeBuilder<MoodEntryEventLabel> builder)
    {
        builder.ToTable("mood_entry_event_labels");

        // Composite primary key
        builder.HasKey(mel => new { mel.MoodEntryId, mel.EventLabelId });

        // Configure relationships
        builder.HasOne(mel => mel.MoodEntry)
            .WithMany(m => m.MoodEntryEventLabels)
            .HasForeignKey(mel => mel.MoodEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mel => mel.EventLabel)
            .WithMany(el => el.MoodEntryEventLabels)
            .HasForeignKey(mel => mel.EventLabelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(mel => mel.MoodEntryId);
        builder.HasIndex(mel => mel.EventLabelId);

        // Note: EventLabel already has its own query filter for soft deletes
        // We don't apply a filter here to avoid circular dependencies
    }
}