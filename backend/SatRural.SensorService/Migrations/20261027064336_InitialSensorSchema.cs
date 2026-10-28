using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SatRural.SensorService.Migrations
{
    /// <inheritdoc />
    public partial class InitialSensorSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sensors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommunityId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InstallationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "SIMULATED"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensorReadings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SensorId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "SIMULATED")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorReadings_Sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Sensors",
                columns: new[] { "Id", "Code", "CommunityId", "CreatedAt", "Description", "InstallationDate", "IsActive", "Location", "Name", "Source", "Type", "Unit" },
                values: new object[,]
                {
                    { 1, "TEMP-001", 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, null, "Temperatura Ambiente", "SIMULATED", "TEMPERATURE", "°C" },
                    { 2, "HUM-001", 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, null, "Humedad Relativa", "SIMULATED", "HUMIDITY", "%" },
                    { 3, "WIND-001", 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, null, "Velocidad del Viento", "SIMULATED", "WIND_SPEED", "km/h" },
                    { 4, "RAIN-001", 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, null, "Nivel de Lluvia", "SIMULATED", "RAINFALL", "mm/h" },
                    { 5, "RIVER-001", 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, null, "Nivel del Río", "SIMULATED", "RIVER_LEVEL", "%" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_SensorId_RecordedAt",
                table: "SensorReadings",
                columns: new[] { "SensorId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_Code",
                table: "Sensors",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensorReadings");

            migrationBuilder.DropTable(
                name: "Sensors");
        }
    }
}
