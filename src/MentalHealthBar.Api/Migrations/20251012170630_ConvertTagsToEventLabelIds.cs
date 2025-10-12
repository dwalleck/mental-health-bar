using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalHealthBar.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertTagsToEventLabelIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_mood_entries_tags",
                table: "mood_entries");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "mood_entries");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "event_label_ids",
                table: "mood_entries",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "idx_mood_entries_event_labels",
                table: "mood_entries",
                column: "event_label_ids",
                filter: "\"IsDeleted\" = false")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_mood_entries_event_labels",
                table: "mood_entries");

            migrationBuilder.DropColumn(
                name: "event_label_ids",
                table: "mood_entries");

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "mood_entries",
                type: "text[]",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "idx_mood_entries_tags",
                table: "mood_entries",
                column: "Tags",
                filter: "\"IsDeleted\" = false")
                .Annotation("Npgsql:IndexMethod", "gin");
        }
    }
}
