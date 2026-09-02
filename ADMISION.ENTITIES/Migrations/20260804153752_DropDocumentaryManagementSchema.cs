using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class DropDocumentaryManagementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicYearName",
                schema: "DocumentaryManagement");

            migrationBuilder.DropTable(
                name: "DocumentHeaderConfig",
                schema: "DocumentaryManagement");

            migrationBuilder.DropTable(
                name: "DocumentIssued",
                schema: "DocumentaryManagement");

            migrationBuilder.DropTable(
                name: "DocumentType",
                schema: "DocumentaryManagement");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "DocumentaryManagement");

            migrationBuilder.CreateTable(
                name: "AcademicYearName",
                schema: "DocumentaryManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYearName", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentHeaderConfig",
                schema: "DocumentaryManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Dependency = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FooterText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InstitutionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    OfficeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Ruc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SecondaryLogoUrl = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentHeaderConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentType",
                schema: "DocumentaryManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CorrelativePadding = table.Column<int>(type: "integer", nullable: false),
                    CorrelativePrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    TemplateName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentIssued",
                schema: "DocumentaryManagement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Correlative = table.Column<int>(type: "integer", nullable: false),
                    CorrelativeDisplay = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    InscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostulantId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoragePath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    WatermarkText = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentIssued", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentIssued_DocumentType_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "DocumentaryManagement",
                        principalTable: "DocumentType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYearName_IsActive",
                schema: "DocumentaryManagement",
                table: "AcademicYearName",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYearName_Year",
                schema: "DocumentaryManagement",
                table: "AcademicYearName",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIssued_DocumentTypeId_Year_Correlative",
                schema: "DocumentaryManagement",
                table: "DocumentIssued",
                columns: new[] { "DocumentTypeId", "Year", "Correlative" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentIssued_PostulantId",
                schema: "DocumentaryManagement",
                table: "DocumentIssued",
                column: "PostulantId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentType_Code",
                schema: "DocumentaryManagement",
                table: "DocumentType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentType_TemplateName",
                schema: "DocumentaryManagement",
                table: "DocumentType",
                column: "TemplateName");
        }
    }
}
