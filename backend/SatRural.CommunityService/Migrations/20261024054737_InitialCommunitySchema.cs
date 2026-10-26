using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SatRural.CommunityService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommunitySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Communities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Municipality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communities", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Communities",
                columns: new[] { "Id", "Country", "CreatedAt", "Department", "Description", "IsActive", "Latitude", "Longitude", "Municipality", "Name" },
                values: new object[] { 1, "Guatemala", new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Guatemala", "Comunidad inicial del sistema de monitoreo.", true, 14.634915m, -90.506882m, "Guatemala", "Comunidad El Pinar" });

            migrationBuilder.Sql(
                """
                SELECT setval(
                    pg_get_serial_sequence('"Communities"', 'Id'),
                    COALESCE((SELECT MAX("Id") FROM "Communities"), 1),
                    true
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Communities");
        }
    }
}
