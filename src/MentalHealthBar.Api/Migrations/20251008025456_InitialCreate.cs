using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalHealthBar.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assessment_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Questions = table.Column<string>(type: "jsonb", nullable: false),
                    ScoringRules = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessment_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Responses = table.Column<string>(type: "jsonb", nullable: false),
                    TotalScore = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "event_labels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_labels", x => x.Id);
                    table.CheckConstraint("CK_EventLabel_Name_Length", "LENGTH(\"Name\") > 0 AND LENGTH(\"Name\") <= 50");
                });

            migrationBuilder.CreateTable(
                name: "health_metrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    RecordedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_metrics", x => x.Id);
                    table.CheckConstraint("CK_HealthMetric_Value_Positive", "\"Value\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "mood_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MoodScore = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mood_entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "uq_assessment_templates_type",
                table: "assessment_templates",
                column: "Type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_assessments_completed",
                table: "assessments",
                column: "CompletedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_assessments_type_completed",
                table: "assessments",
                columns: new[] { "Type", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_event_labels_name",
                table: "event_labels",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false")
                .Annotation("Npgsql:IndexMethod", "btree");

            migrationBuilder.CreateIndex(
                name: "idx_health_metrics_date",
                table: "health_metrics",
                column: "RecordedDate",
                descending: new bool[0],
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "idx_health_metrics_type_date",
                table: "health_metrics",
                columns: new[] { "Type", "RecordedDate" },
                unique: true,
                descending: new bool[0],
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "idx_mood_entries_recorded",
                table: "mood_entries",
                column: "RecordedAt",
                descending: new bool[0],
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "idx_mood_entries_tags",
                table: "mood_entries",
                column: "Tags",
                filter: "\"IsDeleted\" = false")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assessment_templates");

            migrationBuilder.DropTable(
                name: "assessments");

            migrationBuilder.DropTable(
                name: "event_labels");

            migrationBuilder.DropTable(
                name: "health_metrics");

            migrationBuilder.DropTable(
                name: "mood_entries");
        }
    }
}
