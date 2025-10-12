using System.Text.Json;
using MentalHealthBar.Api.Domain.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalHealthBar.Api.Infrastructure.Data.Configurations;

public class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("assessments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever(); // Using NewId

        builder.Property(a => a.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.Severity)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.CompletedAt)
            .IsRequired();

        builder.Property(a => a.Responses)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions?)null)
                     ?? new Dictionary<string, int>())
            .IsRequired();

        builder.Property(a => a.TotalScore)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Indexes for common queries
        builder.HasIndex(a => new { a.Type, a.CompletedAt })
            .HasDatabaseName("idx_assessments_type_completed");

        builder.HasIndex(a => a.CompletedAt)
            .IsDescending()
            .HasDatabaseName("idx_assessments_completed");
    }
}
