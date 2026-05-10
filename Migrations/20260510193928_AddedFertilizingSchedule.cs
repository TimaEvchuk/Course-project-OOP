using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plantify.Migrations
{
    /// <inheritdoc />
    public partial class AddedFertilizingSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastFertilizedDate",
                table: "UserPlants",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "FertilizingInterval",
                table: "Plants",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastFertilizedDate",
                table: "UserPlants");

            migrationBuilder.DropColumn(
                name: "FertilizingInterval",
                table: "Plants");
        }
    }
}
