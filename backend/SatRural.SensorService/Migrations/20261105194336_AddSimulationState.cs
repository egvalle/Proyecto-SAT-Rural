using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatRural.SensorService.Migrations
{
    /// <inheritdoc />
    public partial class AddSimulationState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ManualSimulationValue",
                table: "Sensors",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SimulationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Scenario = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationSettings", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 1,
                column: "ManualSimulationValue",
                value: null);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 2,
                column: "ManualSimulationValue",
                value: null);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 3,
                column: "ManualSimulationValue",
                value: null);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 4,
                column: "ManualSimulationValue",
                value: null);

            migrationBuilder.UpdateData(
                table: "Sensors",
                keyColumn: "Id",
                keyValue: 5,
                column: "ManualSimulationValue",
                value: null);

            migrationBuilder.InsertData(
                table: "SimulationSettings",
                columns: new[] { "Id", "Scenario" },
                values: new object[] { 1, "NORMAL" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SimulationSettings");

            migrationBuilder.DropColumn(
                name: "ManualSimulationValue",
                table: "Sensors");
        }
    }
}
