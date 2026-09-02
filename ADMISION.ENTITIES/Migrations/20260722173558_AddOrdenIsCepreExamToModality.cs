using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdenIsCepreExamToModality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCepreExam",
                schema: "Modality",
                table: "Modality",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Orden",
                schema: "Modality",
                table: "Modality",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCepreExam",
                schema: "Modality",
                table: "Modality");

            migrationBuilder.DropColumn(
                name: "Orden",
                schema: "Modality",
                table: "Modality");
        }
    }
}
