using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SatRural.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndMigrateUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear la tabla de roles
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    Descripcion = table.Column<string>(
                        type: "nvarchar(50)",
                        maxLength: 50,
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            // 2. Crear los roles iniciales
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Descripcion" },
                values: new object[,]
                {
                    { 1, "ADMIN" },
                    { 2, "USER" },
                    { 3, "USERCONSULTA" }
                });

            // 3. Agregar RolId temporalmente
            // Se permite NULL durante la migración para poder convertir
            // los datos existentes antes de crear la FK.
            migrationBuilder.AddColumn<int>(
                name: "RolId",
                table: "Users",
                type: "int",
                nullable: true);

            // 4. Migrar los roles existentes
            migrationBuilder.Sql("""
                UPDATE Users
                SET RolId =
                    CASE Role
                        WHEN 'ADMIN' THEN 1
                        WHEN 'USER' THEN 2
                        WHEN 'USERCONSULTA' THEN 3
                        ELSE 2
                    END
            """);

            // 5. Convertir RolId a NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // 6. Eliminar la columna antigua
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            // 7. Índice de Users.RolId
            migrationBuilder.CreateIndex(
                name: "IX_Users_RolId",
                table: "Users",
                column: "RolId");

            // 8. Índice único para Roles.Descripcion
            migrationBuilder.CreateIndex(
                name: "IX_Roles_Descripcion",
                table: "Roles",
                column: "Descripcion",
                unique: true);

            // 9. Crear relación Users → Roles
            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RolId",
                table: "Users",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RolId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Users_RolId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
