using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddInscriptionOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InscriptionOrder",
                schema: "Postulant",
                table: "Inscription",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgramNumber",
                schema: "Modality",
                table: "Career",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InscriptionOrder",
                schema: "Postulant",
                table: "Inscription");

            migrationBuilder.DropColumn(
                name: "ProgramNumber",
                schema: "Modality",
                table: "Career");
        }
    }
}
