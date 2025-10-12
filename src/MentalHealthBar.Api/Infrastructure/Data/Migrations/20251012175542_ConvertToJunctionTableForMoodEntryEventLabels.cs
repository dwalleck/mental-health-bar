using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalHealthBar.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToJunctionTableForMoodEntryEventLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, create the junction table
            migrationBuilder.CreateTable(
                name: "mood_entry_event_labels",
                columns: table => new
                {
                    MoodEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventLabelId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mood_entry_event_labels", x => new { x.MoodEntryId, x.EventLabelId });
                    table.ForeignKey(
                        name: "FK_mood_entry_event_labels_event_labels_EventLabelId",
                        column: x => x.EventLabelId,
                        principalTable: "event_labels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mood_entry_event_labels_mood_entries_MoodEntryId",
                        column: x => x.MoodEntryId,
                        principalTable: "mood_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_mood_entry_event_labels_EventLabelId",
                table: "mood_entry_event_labels",
                column: "EventLabelId");

            migrationBuilder.CreateIndex(
                name: "IX_mood_entry_event_labels_MoodEntryId",
                table: "mood_entry_event_labels",
                column: "MoodEntryId");

            // Since we're starting fresh, no data migration needed

            // Now drop the old column and index
            migrationBuilder.DropIndex(
                name: "idx_mood_entries_event_labels",
                table: "mood_entries");

            migrationBuilder.DropColumn(
                name: "event_label_ids",
                table: "mood_entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the junction table
            migrationBuilder.DropTable(
                name: "mood_entry_event_labels");

            // Recreate the column
            migrationBuilder.AddColumn<Guid[]>(
                name: "event_label_ids",
                table: "mood_entries",
                type: "uuid[]",
                nullable: true);

            // Recreate the index
            migrationBuilder.CreateIndex(
                name: "idx_mood_entries_event_labels",
                table: "mood_entries",
                column: "event_label_ids",
                filter: "\"IsDeleted\" = false")
                .Annotation("Npgsql:IndexMethod", "gin");
        }
    }
}
