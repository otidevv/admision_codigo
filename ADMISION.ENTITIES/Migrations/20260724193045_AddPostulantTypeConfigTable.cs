using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddPostulantTypeConfigTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostulantTypeConfig",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TypePostulantInscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostulantTypeConfig", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostulantTypeConfig_Career_CareerId",
                        column: x => x.CareerId,
                        principalSchema: "Modality",
                        principalTable: "Career",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PostulantTypeConfig_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostulantTypeConfig_TypePostulantInscription_TypePostulantI~",
                        column: x => x.TypePostulantInscriptionId,
                        principalSchema: "Postulant",
                        principalTable: "TypePostulantInscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostulantTypeConfig_CareerId",
                schema: "Exam",
                table: "PostulantTypeConfig",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_PostulantTypeConfig_TermId_Index",
                schema: "Exam",
                table: "PostulantTypeConfig",
                columns: new[] { "TermId", "Index" });

            migrationBuilder.CreateIndex(
                name: "IX_PostulantTypeConfig_TypePostulantInscriptionId",
                schema: "Exam",
                table: "PostulantTypeConfig",
                column: "TypePostulantInscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostulantTypeConfig",
                schema: "Exam");
        }
    }
}
