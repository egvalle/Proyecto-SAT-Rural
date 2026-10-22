using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SatRural.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClaseAndAttachmentClassId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdClase",
                table: "Attachments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Clase = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clases", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_IdClase",
                table: "Attachments",
                column: "IdClase");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Clases_IdClase",
                table: "Attachments",
                column: "IdClase",
                principalTable: "Clases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Clases_IdClase",
                table: "Attachments");

            migrationBuilder.DropTable(
                name: "Clases");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_IdClase",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "IdClase",
                table: "Attachments");
        }
    }
}
