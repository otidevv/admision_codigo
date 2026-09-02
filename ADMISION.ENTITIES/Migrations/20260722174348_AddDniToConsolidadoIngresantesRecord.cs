using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddDniToConsolidadoIngresantesRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TypeDNI",
                schema: "Exam",
                table: "ConsolidadoIngresantesRecord",
                newName: "DType");

            migrationBuilder.AddColumn<string>(
                name: "DNI",
                schema: "Exam",
                table: "ConsolidadoIngresantesRecord",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DNI",
                schema: "Exam",
                table: "ConsolidadoIngresantesRecord");

            migrationBuilder.RenameColumn(
                name: "DType",
                schema: "Exam",
                table: "ConsolidadoIngresantesRecord",
                newName: "TypeDNI");
        }
    }
}
