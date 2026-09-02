using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADMISION.ENTITIES.Migrations
{
    /// <inheritdoc />
    public partial class AddModalityStartEndTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                schema: "Modality",
                table: "Modality",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(23, 59, 59));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                schema: "Modality",
                table: "Modality",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                schema: "Modality",
                table: "Modality");

            migrationBuilder.DropColumn(
                name: "StartTime",
                schema: "Modality",
                table: "Modality");
        }
    }
}
