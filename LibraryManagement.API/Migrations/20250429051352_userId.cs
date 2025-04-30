using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class userId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRefreshTokens_Users_UserId1",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_UserId1",
                table: "UserRefreshTokens");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "UserRefreshTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "UserRefreshTokens",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_UserId1",
                table: "UserRefreshTokens",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRefreshTokens_Users_UserId1",
                table: "UserRefreshTokens",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
