using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScoringProfile",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsWeighted = table.Column<bool>(type: "boolean", nullable: false),
                    PuntosCorrecta = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    PuntosBlanco = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    PuntosIncorrecta = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    NotaMinimaIngreso = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    AplicarVigesimal = table.Column<bool>(type: "boolean", nullable: false),
                    ManejoAnuladas = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    TypeModalityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringProfile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoringProfile_Career_CareerId",
                        column: x => x.CareerId,
                        principalSchema: "Modality",
                        principalTable: "Career",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ScoringProfile_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ScoringProfile_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ScoringProfile_TypeModality_TypeModalityId",
                        column: x => x.TypeModalityId,
                        principalSchema: "Modality",
                        principalTable: "TypeModality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ScoringProfileRange",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScoringProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromQuestion = table.Column<int>(type: "integer", nullable: false),
                    ToQuestion = table.Column<int>(type: "integer", nullable: false),
                    PuntosCorrecta = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringProfileRange", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoringProfileRange_ScoringProfile_ScoringProfileId",
                        column: x => x.ScoringProfileId,
                        principalSchema: "Exam",
                        principalTable: "ScoringProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoringProfile_CareerId",
                schema: "Exam",
                table: "ScoringProfile",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoringProfile_IsActive_TermId",
                schema: "Exam",
                table: "ScoringProfile",
                columns: new[] { "IsActive", "TermId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScoringProfile_ModalityId",
                schema: "Exam",
                table: "ScoringProfile",
                column: "ModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoringProfile_TermId",
                schema: "Exam",
                table: "ScoringProfile",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoringProfile_TypeModalityId",
                schema: "Exam",
                table: "ScoringProfile",
                column: "TypeModalityId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoringProfileRange_ScoringProfileId_DisplayOrder",
                schema: "Exam",
                table: "ScoringProfileRange",
                columns: new[] { "ScoringProfileId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoringProfileRange",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "ScoringProfile",
                schema: "Exam");
        }
    }
}
