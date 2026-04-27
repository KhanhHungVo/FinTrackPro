using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrackPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalDismissedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DismissedAt",
                table: "Signals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Signals_DismissedAt",
                table: "Signals",
                column: "DismissedAt",
                filter: "\"DismissedAt\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Signals_DismissedAt",
                table: "Signals");

            migrationBuilder.DropColumn(
                name: "DismissedAt",
                table: "Signals");
        }
    }
}
