using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddConsolidadoIngresantesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsolidadoIngresantesVersion",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsLatest = table.Column<bool>(type: "boolean", nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidadoIngresantesVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsolidadoIngresantesVersion_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidadoIngresantesRecord",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CodigoEstudiante = table.Column<string>(type: "text", nullable: false),
                    CodigoCarrera = table.Column<string>(type: "text", nullable: false),
                    SegundaCarrera = table.Column<string>(type: "text", nullable: true),
                    Semestre = table.Column<string>(type: "text", nullable: true),
                    Nombres = table.Column<string>(type: "text", nullable: false),
                    Paterno = table.Column<string>(type: "text", nullable: false),
                    Materno = table.Column<string>(type: "text", nullable: false),
                    TypeDNI = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Celular = table.Column<string>(type: "text", nullable: true),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    FechaNacimiento = table.Column<string>(type: "text", nullable: true),
                    Sexo = table.Column<string>(type: "text", nullable: true),
                    EstadoCivil = table.Column<string>(type: "text", nullable: true),
                    Ubigeo = table.Column<string>(type: "text", nullable: true),
                    TipoPostulante = table.Column<string>(type: "text", nullable: true),
                    TipoObs = table.Column<string>(type: "text", nullable: true),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    Nro = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidadoIngresantesRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsolidadoIngresantesRecord_ConsolidadoIngresantesVersion_~",
                        column: x => x.VersionId,
                        principalSchema: "Exam",
                        principalTable: "ConsolidadoIngresantesVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsolidadoIngresantesRecord_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ConsolidadoIngresantesRecord_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidadoIngresantesRecord_CreatedBy_CreatedAt",
                schema: "Exam",
                table: "ConsolidadoIngresantesRecord",
                columns: new[] { "CreatedBy", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidadoIngresantesRecord_InscriptionId",
                schema: "Exam",
                table: "ConsolidadoIngresantesRecord",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidadoIngresantesRecord_TermId",
                schema: "Exam",
                table: "ConsolidadoIngresantesRecord",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidadoIngresantesRecord_VersionId",
                schema: "Exam",
                table: "ConsolidadoIngresantesRecord",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidadoIngresantesVersion_TermId_VersionNumber",
                schema: "Exam",
                table: "ConsolidadoIngresantesVersion",
                columns: new[] { "TermId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsolidadoIngresantesRecord",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ConsolidadoIngresantesVersion",
                schema: "Exam");
        }
    }
}
