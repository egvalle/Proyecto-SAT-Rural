using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SatRural.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialMonitoringData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sensors_Communities_CommunityId",
                table: "Sensors");

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Sensors",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Sensors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Sensors",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Sensors",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Communities",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Communities",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Communities",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Communities",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Latitude", "Longitude", "Name" },
                values: new object[] { 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), true, 14.634915m, -90.506882m, "Comunidad El Pinar" });

            migrationBuilder.InsertData(
                table: "Sensors",
                columns: new[] { "Id", "Code", "CommunityId", "CreatedAt", "IsActive", "Name", "Type", "Unit" },
                values: new object[,]
                {
                    { 1, "TEMP-001", 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), true, "Temperatura Ambiente", "TEMPERATURE", "°C" },
                    { 2, "HUM-001", 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), true, "Humedad Relativa", "HUMIDITY", "%" },
                    { 3, "WIND-001", 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), true, "Velocidad del Viento", "WIND_SPEED", "km/h" },
                    { 4, "RAIN-001", 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), true, "Nivel de Lluvia", "RAINFALL", "mm/h" },
                    { 5, "RIVER-001", 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), true, "Nivel del Río", "RIVER_LEVEL", "%" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_Code",
                table: "Sensors",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sensors_Communities_CommunityId",
                table: "Sensors",
                column: "CommunityId",
                principalTable: "Communities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sensors_Communities_CommunityId",
                table: "Sensors");

            migrationBuilder.DropIndex(
                name: "IX_Sensors_Code",
                table: "Sensors");

            migrationBuilder.DeleteData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Communities",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "Unit",
                table: "Sensors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Sensors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Sensors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Sensors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Communities",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Communities",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Communities",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldPrecision: 9,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sensors_Communities_CommunityId",
                table: "Sensors",
                column: "CommunityId",
                principalTable: "Communities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
