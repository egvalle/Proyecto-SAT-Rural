using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatRural.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandCommunityForPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Communities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Communities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Communities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Municipality",
                table: "Communities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Communities",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Country", "Department", "Description", "Municipality" },
                values: new object[] { "Guatemala", "Guatemala", "Comunidad inicial del sistema de monitoreo.", "Guatemala" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Communities");

            migrationBuilder.DropColumn(
                name: "Municipality",
                table: "Communities");
        }
    }
}
