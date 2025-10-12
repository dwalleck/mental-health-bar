using MentalHealthBar.Api.Domain.EventLabels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalHealthBar.Api.Infrastructure.Data.Configurations;

public class EventLabelConfiguration : IEntityTypeConfiguration<EventLabel>
{
    public void Configure(EntityTypeBuilder<EventLabel> builder)
    {
        builder.ToTable("event_labels");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever(); // Using NewId

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(200);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Unique constraint on lowercase name (case-insensitive uniqueness)
        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("idx_event_labels_name")
            .HasMethod("btree");

        // Create expression index for case-insensitive uniqueness
        builder.ToTable(tb =>
        {
            tb.HasCheckConstraint("CK_EventLabel_Name_Length", "LENGTH(\"Name\") > 0 AND LENGTH(\"Name\") <= 50");
        });

        // Global query filter for soft deletes
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
