using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plantify.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantSubmissionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
