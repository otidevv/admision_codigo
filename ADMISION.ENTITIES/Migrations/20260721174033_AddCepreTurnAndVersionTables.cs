using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddCepreTurnAndVersionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CepreImportRecord_ExamScoreRecord_ExamResultId",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_CepreImportRecord_Inscription_InscriptionId",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropIndex(
                name: "IX_CepreImportRecord_ExamResultId",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropIndex(
                name: "IX_CepreImportRecord_InscriptionId",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropColumn(
                name: "ExamResultId",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropColumn(
                name: "InscriptionId",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.AddColumn<Guid>(
                name: "VersionId",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "CepreImportVersion",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    IsLatest = table.Column<bool>(type: "boolean", nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CepreImportVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CepreImportVersion_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CepreTurn",
                schema: "Exam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CepreTurn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CepreTurn_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CepreTurn_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CepreImportRecord_VersionId",
                schema: "Exam",
                table: "CepreImportRecord",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CepreImportVersion_TermId_VersionNumber",
                schema: "Exam",
                table: "CepreImportVersion",
                columns: new[] { "TermId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CepreTurn_TermId_UserId",
                schema: "Exam",
                table: "CepreTurn",
                columns: new[] { "TermId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CepreTurn_UserId",
                schema: "Exam",
                table: "CepreTurn",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CepreImportRecord_CepreImportVersion_VersionId",
                schema: "Exam",
                table: "CepreImportRecord",
                column: "VersionId",
                principalSchema: "Exam",
                principalTable: "CepreImportVersion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CepreImportRecord_CepreImportVersion_VersionId",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropTable(
                name: "CepreImportVersion",
                schema: "Exam");

            migrationBuilder.DropTable(
                name: "CepreTurn",
                schema: "Exam");

            migrationBuilder.DropIndex(
                name: "IX_CepreImportRecord_VersionId",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.DropColumn(
                name: "VersionId",
                schema: "Exam",
                table: "CepreImportRecord");

            migrationBuilder.AddColumn<Guid>(
                name: "ExamResultId",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InscriptionId",
                schema: "Exam",
                table: "CepreImportRecord",
                type: "uuid",
                nullable: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_CepreImportRecord_ExamScoreRecord_ExamResultId",
                schema: "Exam",
                table: "CepreImportRecord",
                column: "ExamResultId",
                principalSchema: "Exam",
                principalTable: "ExamScoreRecord",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CepreImportRecord_Inscription_InscriptionId",
                schema: "Exam",
                table: "CepreImportRecord",
                column: "InscriptionId",
                principalSchema: "Postulant",
                principalTable: "Inscription",
                principalColumn: "Id");
        }
    }
}
