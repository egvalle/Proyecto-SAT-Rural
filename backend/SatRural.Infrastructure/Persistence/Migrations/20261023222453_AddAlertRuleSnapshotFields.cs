using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatRural.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertRuleSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlertRuleId",
                table: "Alerts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DetectedValue",
                table: "Alerts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ThresholdValue",
                table: "Alerts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlertRuleId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "DetectedValue",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ThresholdValue",
                table: "Alerts");
        }
    }
}
