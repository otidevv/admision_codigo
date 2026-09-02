using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddNewCepreImportColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Distrito",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provincia",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Puntaje",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Puntaje01",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Puntaje02",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Puntaje03",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TDocumento",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Departamento",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropColumn(
                name: "Distrito",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropColumn(
                name: "Provincia",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropColumn(
                name: "Puntaje",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropColumn(
                name: "Puntaje01",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropColumn(
                name: "Puntaje02",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropColumn(
                name: "Puntaje03",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropColumn(
                name: "TDocumento",
                schema: "Exam",
                table: "CepreImportRecord");
        }
    }
}
