using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plantify.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPlantSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plants_Users_SubmittedByUserId",
                table: "Plants");

            migrationBuilder.DropIndex(
                name: "IX_Plants_SubmittedByUserId",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "SubmissionStatus",
                table: "Plants");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "Plants");

            migrationBuilder.CreateTable(
                name: "PlantSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LightRequirement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Variety = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WateringInterval = table.Column<int>(type: "int", nullable: false),
                    FertilizingInterval = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantSubmissions_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantSubmissions_SubmittedByUserId",
                table: "PlantSubmissions",
                column: "SubmittedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantSubmissions");

            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "Plants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Plants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionStatus",
                table: "Plants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByUserId",
                table: "Plants",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plants_SubmittedByUserId",
                table: "Plants",
                column: "SubmittedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Plants_Users_SubmittedByUserId",
                table: "Plants",
                column: "SubmittedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
