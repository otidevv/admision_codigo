using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddExamSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ExamScheduleId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FolderNumber",
                schema: "Infrastructure",
                table: "ExamAssignment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExamSchedule",
                schema: "Infrastructure",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ModalityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSchedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSchedule_Modality_ModalityId",
                        column: x => x.ModalityId,
                        principalSchema: "Modality",
                        principalTable: "Modality",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamSchedule_Terms_TermId",
                        column: x => x.TermId,
                        principalSchema: "Modality",
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamScheduleRoom",
                schema: "Infrastructure",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassroomId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: true),
                    TematicAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedCapacity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamScheduleRoom", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamScheduleRoom_Clasroom_ClassroomId",
                        column: x => x.ClassroomId,
                        principalSchema: "Infrastructure",
                        principalTable: "Clasroom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamScheduleRoom_ExamSchedule_ExamScheduleId",
                        column: x => x.ExamScheduleId,
                        principalSchema: "Infrastructure",
                        principalTable: "ExamSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamScheduleRoom_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalSchema: "Users",
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExamScheduleRoom_TematicArea_TematicAreaId",
                        column: x => x.TematicAreaId,
                        principalSchema: "Modality",
                        principalTable: "TematicArea",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignment_ExamScheduleId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                column: "ExamScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignment_TeacherId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchedule_ModalityId",
                schema: "Infrastructure",
                table: "ExamSchedule",
                column: "ModalityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchedule_TermId",
                schema: "Infrastructure",
                table: "ExamSchedule",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScheduleRoom_ClassroomId",
                schema: "Infrastructure",
                table: "ExamScheduleRoom",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScheduleRoom_ExamScheduleId_ClassroomId",
                schema: "Infrastructure",
                table: "ExamScheduleRoom",
                columns: new[] { "ExamScheduleId", "ClassroomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamScheduleRoom_TeacherId",
                schema: "Infrastructure",
                table: "ExamScheduleRoom",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamScheduleRoom_TematicAreaId",
                schema: "Infrastructure",
                table: "ExamScheduleRoom",
                column: "TematicAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAssignment_ExamSchedule_ExamScheduleId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                column: "ExamScheduleId",
                principalSchema: "Infrastructure",
                principalTable: "ExamSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamAssignment_Teachers_TeacherId",
                schema: "Infrastructure",
                table: "ExamAssignment",
                column: "TeacherId",
                principalSchema: "Users",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamAssignment_ExamSchedule_ExamScheduleId",
                schema: "Infrastructure",
                table: "ExamAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamAssignment_Teachers_TeacherId",
                schema: "Infrastructure",
                table: "ExamAssignment");

            migrationBuilder.DropTable(
                name: "ExamScheduleRoom",
                schema: "Infrastructure");

            migrationBuilder.DropTable(
                name: "ExamSchedule",
                schema: "Infrastructure");

            migrationBuilder.DropIndex(
                name: "IX_ExamAssignment_ExamScheduleId",
                schema: "Infrastructure",
                table: "ExamAssignment");

            migrationBuilder.DropIndex(
                name: "IX_ExamAssignment_TeacherId",
                schema: "Infrastructure",
                table: "ExamAssignment");

            migrationBuilder.DropColumn(
                name: "ExamScheduleId",
                schema: "Infrastructure",
                table: "ExamAssignment");

            migrationBuilder.DropColumn(
                name: "FolderNumber",
                schema: "Infrastructure",
                table: "ExamAssignment");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                schema: "Infrastructure",
                table: "ExamAssignment");
        }
    }
}
