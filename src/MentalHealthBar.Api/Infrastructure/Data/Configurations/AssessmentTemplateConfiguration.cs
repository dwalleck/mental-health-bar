using System.Text.Json;
using MentalHealthBar.Api.Domain.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MentalHealthBar.Api.Infrastructure.Data.Configurations;

public class AssessmentTemplateConfiguration : IEntityTypeConfiguration<AssessmentTemplate>
{
    public void Configure(EntityTypeBuilder<AssessmentTemplate> builder)
    {
        builder.ToTable("assessment_templates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Questions)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Question>>(v, (JsonSerializerOptions?)null)
                     ?? new List<Question>())
            .IsRequired();

        builder.Property(t => t.ScoringRules)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<ScoringRules>(v, (JsonSerializerOptions?)null)!)
            .IsRequired();

        // Unique constraint on Type
        builder.HasIndex(t => t.Type)
            .IsUnique()
            .HasDatabaseName("uq_assessment_templates_type");
    }
}
