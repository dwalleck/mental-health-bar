using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MentalHealthBar.Api.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsDeletedFilterFromEventLabelUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_event_labels_name",
                table: "event_labels");

            migrationBuilder.CreateIndex(
                name: "idx_event_labels_name",
                table: "event_labels",
                column: "Name",
                unique: true)
                .Annotation("Npgsql:IndexMethod", "btree");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_event_labels_name",
                table: "event_labels");

            migrationBuilder.CreateIndex(
                name: "idx_event_labels_name",
                table: "event_labels",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false")
                .Annotation("Npgsql:IndexMethod", "btree");
        }
    }
}
