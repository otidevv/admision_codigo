using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class DropOldExamAndCreateNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamAnswerKey",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ExamAreaConfig",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ExamParameters",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ExamScoreResult",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "PostulantAnswer",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "PostulantAnswerSheet",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ExamSession",
                schema: "Exam");

            migrationBuilder.CreateTable(
                name: "ExamScoreRecord",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TematicAreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Correctas = table.Column<int>(type: "integer", nullable: false),
                    Blancas = table.Column<int>(type: "integer", nullable: false),
                    Puntaje = table.Column<decimal>(type: "numeric", nullable: false),
                    Nota = table.Column<decimal>(type: "numeric", nullable: true),
                    EsIngresante = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamScoreRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamScoreRecord_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamScoreRecord_TematicArea_TematicAreaId",
                        column: x => x.TematicAreaId,
                        principalSchema: "Modality",
                        principalTable: "TematicArea",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AdmissionResultImportRecord",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExamResultId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nro = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    ApellidosNombres = table.Column<string>(type: "text", nullable: true),
                    CarreraProfesional = table.Column<string>(type: "text", nullable: true),
                    Grupo = table.Column<string>(type: "text", nullable: true),
                    Correctas = table.Column<string>(type: "text", nullable: true),
                    Blancas = table.Column<string>(type: "text", nullable: true),
                    Puntaje = table.Column<string>(type: "text", nullable: true),
                    Nota = table.Column<string>(type: "text", nullable: true),
                    Condicion = table.Column<string>(type: "text", nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionResultImportRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionResultImportRecord_ExamScoreRecord_ExamResultId",
                        column: x => x.ExamResultId,
                        principalSchema: "Exam",
                        principalTable: "ExamScoreRecord",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AdmissionResultImportRecord_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CepreImportRecord",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExamResultId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nro = table.Column<int>(type: "integer", nullable: false),
                    Ciclo = table.Column<string>(type: "text", nullable: true),
                    Codigo = table.Column<string>(type: "text", nullable: true),
                    Dni = table.Column<string>(type: "text", nullable: true),
                    Apaterno = table.Column<string>(type: "text", nullable: true),
                    Amaterno = table.Column<string>(type: "text", nullable: true),
                    Nombres = table.Column<string>(type: "text", nullable: true),
                    ApellidosNombres = table.Column<string>(type: "text", nullable: true),
                    Sexo = table.Column<string>(type: "text", nullable: true),
                    FechaNacimiento = table.Column<string>(type: "text", nullable: true),
                    Direccion = table.Column<string>(type: "text", nullable: true),
                    EstadoCivil = table.Column<string>(type: "text", nullable: true),
                    AnioEgreso = table.Column<string>(type: "text", nullable: true),
                    Correo = table.Column<string>(type: "text", nullable: true),
                    Celular = table.Column<string>(type: "text", nullable: true),
                    Colegio = table.Column<string>(type: "text", nullable: true),
                    NombreColegio = table.Column<string>(type: "text", nullable: true),
                    UbigeoColegio = table.Column<string>(type: "text", nullable: true),
                    DireccionColegio = table.Column<string>(type: "text", nullable: true),
                    UbigeoLugarNacimiento = table.Column<string>(type: "text", nullable: true),
                    Modalidad = table.Column<string>(type: "text", nullable: true),
                    CodigoCarrera = table.Column<string>(type: "text", nullable: true),
                    CarreraProfesional = table.Column<string>(type: "text", nullable: true),
                    Grupo = table.Column<string>(type: "text", nullable: true),
                    ModalidadPago = table.Column<string>(type: "text", nullable: true),
                    Monto = table.Column<decimal>(type: "numeric", nullable: true),
                    Nota01 = table.Column<decimal>(type: "numeric", nullable: true),
                    Nota02 = table.Column<decimal>(type: "numeric", nullable: true),
                    Nota03 = table.Column<decimal>(type: "numeric", nullable: true),
                    NotaFinal = table.Column<decimal>(type: "numeric", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationError = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CepreImportRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CepreImportRecord_ExamScoreRecord_ExamResultId",
                        column: x => x.ExamResultId,
                        principalSchema: "Exam",
                        principalTable: "ExamScoreRecord",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CepreImportRecord_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionResultImportRecord_CreatedBy_CreatedAt",
                schema: "Exam",
                table: "AdmissionResultImportRecord",
                columns: new[] { "CreatedBy", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionResultImportRecord_ExamResultId",
                schema: "Exam",
                table: "AdmissionResultImportRecord",
                column: "ExamResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionResultImportRecord_InscriptionId",
                schema: "Exam",
                table: "AdmissionResultImportRecord",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CepreImportRecord_CreatedBy_CreatedAt",
                schema: "Exam",
                table: "CepreImportRecord",
                columns: new[] { "CreatedBy", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CepreImportRecord_ExamResultId",
                schema: "Exam",
                table: "CepreImportRecord",
                column: "ExamResultId");

            migrationBuilder.CreateIndex(
                name: "IX_CepreImportRecord_InscriptionId",
                schema: "Exam",
                table: "CepreImportRecord",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScoreRecord_InscriptionId_TematicAreaId",
                schema: "Exam",
                table: "ExamScoreRecord",
                columns: new[] { "InscriptionId", "TematicAreaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamScoreRecord_Source",
                schema: "Exam",
                table: "ExamScoreRecord",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScoreRecord_TematicAreaId",
                schema: "Exam",
                table: "ExamScoreRecord",
                column: "TematicAreaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionResultImportRecord",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "CepreImportRecord",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ExamScoreRecord",
                schema: "Exam");

            migrationBuilder.CreateTable(
                name: "ExamSession",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    State = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSession_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamSession_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamAnswerKey",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TematicAreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsAnulada = table.Column<bool>(type: "boolean", nullable: false),
                    NumeroPregunta = table.Column<int>(type: "integer", nullable: false),
                    PuntosOverride = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                    RespuestaCorrecta = table.Column<string>(type: "text", nullable: false),
                    Tema = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAnswerKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAnswerKey_ExamSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Exam",
                        principalTable: "ExamSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAnswerKey_TematicArea_TematicAreaId",
                        column: x => x.TematicAreaId,
                        principalSchema: "Modality",
                        principalTable: "TematicArea",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamAreaConfig",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TematicAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    NumeroFin = table.Column<int>(type: "integer", nullable: false),
                    NumeroInicio = table.Column<int>(type: "integer", nullable: false),
                    PesoRelativo = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAreaConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAreaConfig_ExamSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Exam",
                        principalTable: "ExamSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAreaConfig_TematicArea_TematicAreaId",
                        column: x => x.TematicAreaId,
                        principalSchema: "Modality",
                        principalTable: "TematicArea",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamParameters",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AplicarBonificacion = table.Column<bool>(type: "boolean", nullable: false),
                    AplicarVigesimal = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    CriterioDesempate = table.Column<string>(type: "text", nullable: false),
                    ManejoAnuladas = table.Column<string>(type: "text", nullable: false),
                    NotaMinimaIngreso = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    PuntosBlanco = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    PuntosCorrecta = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    PuntosIncorrecta = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    TotalPreguntas = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamParameters_ExamSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Exam",
                        principalTable: "ExamSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostulantAnswerSheet",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodePostulant = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FileRowNumber = table.Column<int>(type: "integer", nullable: false),
                    HasIssues = table.Column<bool>(type: "boolean", nullable: false),
                    IssueMessage = table.Column<string>(type: "text", nullable: true),
                    Tema = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostulantAnswerSheet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostulantAnswerSheet_ExamSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Exam",
                        principalTable: "ExamSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostulantAnswerSheet_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamScoreResult",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Anuladas = table.Column<int>(type: "integer", nullable: false),
                    AreaScoresJson = table.Column<string>(type: "text", nullable: true),
                    Blancas = table.Column<int>(type: "integer", nullable: false),
                    Correctas = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EsIngresante = table.Column<bool>(type: "boolean", nullable: false),
                    Incorrectas = table.Column<int>(type: "integer", nullable: false),
                    Multiples = table.Column<int>(type: "integer", nullable: false),
                    PuntajeBruto = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PuntajeFinal = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    RankingCarrera = table.Column<int>(type: "integer", nullable: true),
                    RankingModalidad = table.Column<int>(type: "integer", nullable: true),
                    Vigesimal = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamScoreResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamScoreResult_ExamSession_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "Exam",
                        principalTable: "ExamSession",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamScoreResult_Inscription_InscriptionId",
                        column: x => x.InscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "Inscription",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamScoreResult_PostulantAnswerSheet_SheetId",
                        column: x => x.SheetId,
                        principalSchema: "Exam",
                        principalTable: "PostulantAnswerSheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostulantAnswer",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroPregunta = table.Column<int>(type: "integer", nullable: false),
                    RespuestaMarcada = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostulantAnswer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostulantAnswer_PostulantAnswerSheet_SheetId",
                        column: x => x.SheetId,
                        principalSchema: "Exam",
                        principalTable: "PostulantAnswerSheet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerKey_SessionId_Tema_NumeroPregunta",
                schema: "Exam",
                table: "ExamAnswerKey",
                columns: new[] { "SessionId", "Tema", "NumeroPregunta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamAnswerKey_TematicAreaId",
                schema: "Exam",
                table: "ExamAnswerKey",
                column: "TematicAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAreaConfig_SessionId",
                schema: "Exam",
                table: "ExamAreaConfig",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAreaConfig_TematicAreaId",
                schema: "Exam",
                table: "ExamAreaConfig",
                column: "TematicAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamParameters_SessionId",
                schema: "Exam",
                table: "ExamParameters",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamScoreResult_InscriptionId",
                schema: "Exam",
                table: "ExamScoreResult",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScoreResult_SessionId_EsIngresante",
                schema: "Exam",
                table: "ExamScoreResult",
                columns: new[] { "SessionId", "EsIngresante" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamScoreResult_SheetId",
                schema: "Exam",
                table: "ExamScoreResult",
                column: "SheetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSession_ModalityId",
                schema: "Exam",
                table: "ExamSession",
                column: "ModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSession_TermId",
                schema: "Exam",
                table: "ExamSession",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_PostulantAnswer_SheetId_NumeroPregunta",
                schema: "Exam",
                table: "PostulantAnswer",
                columns: new[] { "SheetId", "NumeroPregunta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostulantAnswerSheet_InscriptionId",
                schema: "Exam",
                table: "PostulantAnswerSheet",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PostulantAnswerSheet_SessionId",
                schema: "Exam",
                table: "PostulantAnswerSheet",
                column: "SessionId");
        }
    }
}
