using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatRural.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandSensorForPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Sensors",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InstallationDate",
                table: "Sensors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Sensors",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "InstallationDate", "Location" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "InstallationDate", "Location" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "InstallationDate", "Location" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "InstallationDate", "Location" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "InstallationDate", "Location" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "InstallationDate",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Sensors");
        }
    }
}
