using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatRural.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeparateAlertsFromLegacyEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Alerts_AlertId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_AlertId",
                table: "Events");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Events_AlertId",
                table: "Events",
                column: "AlertId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Alerts_AlertId",
                table: "Events",
                column: "AlertId",
                principalTable: "Alerts",
                principalColumn: "Id");
        }
    }
}
