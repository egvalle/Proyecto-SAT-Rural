using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SatRural.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialAlertRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AlertRules",
                columns: new[] { "Id", "Description", "Operator", "Phenomenon", "RiskLevel", "SensorType", "ThresholdValue" },
                values: new object[,]
                {
                    { 1, "Temperatura elevada", ">=", "Ola de calor", "YELLOW", "TEMPERATURE", 32m },
                    { 2, "Temperatura muy elevada", ">=", "Ola de calor", "ORANGE", "TEMPERATURE", 35m },
                    { 3, "Temperatura extremadamente elevada", ">=", "Ola de calor", "RED", "TEMPERATURE", 38m },
                    { 4, "Temperatura baja", "<=", "Helada", "YELLOW", "TEMPERATURE", 8m },
                    { 5, "Temperatura muy baja", "<=", "Helada", "ORANGE", "TEMPERATURE", 4m },
                    { 6, "Temperatura extremadamente baja", "<=", "Helada", "RED", "TEMPERATURE", 0m },
                    { 7, "Humedad baja", "<=", "Sequía", "YELLOW", "HUMIDITY", 40m },
                    { 8, "Humedad muy baja", "<=", "Sequía", "ORANGE", "HUMIDITY", 30m },
                    { 9, "Humedad extremadamente baja", "<=", "Sequía", "RED", "HUMIDITY", 20m },
                    { 10, "Velocidad del viento elevada", ">=", "Tormenta", "YELLOW", "WIND_SPEED", 30m },
                    { 11, "Velocidad del viento muy elevada", ">=", "Tormenta", "ORANGE", "WIND_SPEED", 50m },
                    { 12, "Velocidad del viento extremadamente elevada", ">=", "Tormenta", "RED", "WIND_SPEED", 70m },
                    { 13, "Nivel de lluvia elevado", ">=", "Inundación", "YELLOW", "RAINFALL", 15m },
                    { 14, "Nivel de lluvia muy elevado", ">=", "Inundación", "ORANGE", "RAINFALL", 30m },
                    { 15, "Nivel de lluvia extremadamente elevado", ">=", "Inundación", "RED", "RAINFALL", 50m },
                    { 16, "Nivel del río elevado", ">=", "Inundación", "YELLOW", "RIVER_LEVEL", 60m },
                    { 17, "Nivel del río muy elevado", ">=", "Inundación", "ORANGE", "RIVER_LEVEL", 75m },
                    { 18, "Nivel del río extremadamente elevado", ">=", "Inundación", "RED", "RIVER_LEVEL", 90m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "AlertRules",
                keyColumn: "Id",
                keyValue: 18);
        }
    }
}
