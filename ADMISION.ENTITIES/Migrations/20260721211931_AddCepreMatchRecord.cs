using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddCepreMatchRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CepreMatchRecord",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    CepreVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExamResultId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nro = table.Column<int>(type: "integer", nullable: false),
                    Dni = table.Column<string>(type: "text", nullable: true),
                    CodigoCarrera = table.Column<string>(type: "text", nullable: true),
                    CarreraProfesional = table.Column<string>(type: "text", nullable: true),
                    ApellidosNombres = table.Column<string>(type: "text", nullable: true),
                    NotaFinal = table.Column<decimal>(type: "numeric", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: true),
                    IsAdmission = table.Column<bool>(type: "boolean", nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CepreMatchRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CepreMatchRecord_CepreImportVersion_CepreVersionId",
                        column: x => x.CepreVersionId,
                        principalSchema: "Exam",
                        principalTable: "CepreImportVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CepreMatchRecord_ExamScoreRecord_ExamResultId",
                        column: x => x.ExamResultId,
                        principalSchema: "Exam",
                        principalTable: "ExamScoreRecord",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CepreMatchRecord_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CepreMatchRecord_CepreVersionId",
                schema: "Exam",
                table: "CepreMatchRecord",
                column: "CepreVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CepreMatchRecord_CreatedBy_CreatedAt",
                schema: "Exam",
                table: "CepreMatchRecord",
                columns: new[] { "CreatedBy", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CepreMatchRecord_ExamResultId",
                schema: "Exam",
                table: "CepreMatchRecord",
                column: "ExamResultId");

            migrationBuilder.CreateIndex(
                name: "IX_CepreMatchRecord_InscriptionId",
                schema: "Exam",
                table: "CepreMatchRecord",
                column: "InscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CepreMatchRecord",
                schema: "Exam");
        }
    }
}
