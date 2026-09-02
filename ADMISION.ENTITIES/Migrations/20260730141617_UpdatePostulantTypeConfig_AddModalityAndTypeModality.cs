using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePostulantTypeConfig_AddModalityAndTypeModality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostulantTypeConfig_TypePostulantInscription_TypePostulantI~",
                schema: "Exam",
                table: "PostulantTypeConfig");

            migrationBuilder.RenameColumn(
                name: "TypePostulantInscriptionId",
                schema: "Exam",
                table: "PostulantTypeConfig",
                newName: "TypeModalityId");

            migrationBuilder.RenameIndex(
                name: "IX_PostulantTypeConfig_TypePostulantInscriptionId",
                schema: "Exam",
                table: "PostulantTypeConfig",
                newName: "IX_PostulantTypeConfig_TypeModalityId");

            migrationBuilder.AddColumn<Guid>(
                name: "ModalityId",
                schema: "Exam",
                table: "PostulantTypeConfig",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostulantTypeConfig_ModalityId",
                schema: "Exam",
                table: "PostulantTypeConfig",
                column: "ModalityId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostulantTypeConfig_Modality_ModalityId",
                schema: "Exam",
                table: "PostulantTypeConfig",
                column: "ModalityId",
                principalSchema: "Modality",
                principalTable: "Modality",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PostulantTypeConfig_TypeModality_TypeModalityId",
                schema: "Exam",
                table: "PostulantTypeConfig",
                column: "TypeModalityId",
                principalSchema: "Modality",
                principalTable: "TypeModality",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostulantTypeConfig_Modality_ModalityId",
                schema: "Exam",
                table: "PostulantTypeConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_PostulantTypeConfig_TypeModality_TypeModalityId",
                schema: "Exam",
                table: "PostulantTypeConfig");

            migrationBuilder.DropIndex(
                name: "IX_PostulantTypeConfig_ModalityId",
                schema: "Exam",
                table: "PostulantTypeConfig");

            migrationBuilder.DropColumn(
                name: "ModalityId",
                schema: "Exam",
                table: "PostulantTypeConfig");

            migrationBuilder.RenameColumn(
                name: "TypeModalityId",
                schema: "Exam",
                table: "PostulantTypeConfig",
                newName: "TypePostulantInscriptionId");

            migrationBuilder.RenameIndex(
                name: "IX_PostulantTypeConfig_TypeModalityId",
                schema: "Exam",
                table: "PostulantTypeConfig",
                newName: "IX_PostulantTypeConfig_TypePostulantInscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostulantTypeConfig_TypePostulantInscription_TypePostulantI~",
                schema: "Exam",
                table: "PostulantTypeConfig",
                column: "TypePostulantInscriptionId",
                principalSchema: "Postulant",
                principalTable: "TypePostulantInscription",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
