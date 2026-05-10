using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plantify.Migrations
{
    /// <inheritdoc />
    public partial class AddedCustomImagePathToUserPlant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Plants");

            migrationBuilder.RenameColumn(
                name: "Difficulty",
                table: "Plants",
                newName: "Variety");

            migrationBuilder.AddColumn<string>(
                name: "CustomImagePath",
                table: "UserPlants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlantSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantSections_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantSections_PlantId",
                table: "PlantSections",
                column: "PlantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantSections");

            migrationBuilder.DropColumn(
                name: "CustomImagePath",
                table: "UserPlants");

            migrationBuilder.RenameColumn(
                name: "Variety",
                table: "Plants",
                newName: "Difficulty");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Plants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
