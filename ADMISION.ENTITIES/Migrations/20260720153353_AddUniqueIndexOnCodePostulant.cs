using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnCodePostulant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inscription_ModalityId",
                schema: "Postulant",
                table: "Inscription");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_ModalityId_CodePostulant",
                schema: "Postulant",
                table: "Inscription",
                columns: new[] { "ModalityId", "CodePostulant" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inscription_ModalityId_CodePostulant",
                schema: "Postulant",
                table: "Inscription");

            migrationBuilder.CreateIndex(
                name: "IX_Inscription_ModalityId",
                schema: "Postulant",
                table: "Inscription",
                column: "ModalityId");
        }
    }
}
