using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class SplitUbigeoLugarNacimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UbigeoLugarNacimiento",
                schema: "Exam",
                table: "CepreImportRecord",
                newName: "Ubigeo");

            migrationBuilder.AddColumn<string>(
                name: "LugarNacimiento",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LugarNacimiento",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.RenameColumn(
                name: "Ubigeo",
                schema: "Exam",
                table: "CepreImportRecord",
                newName: "UbigeoLugarNacimiento");
        }
    }
}
