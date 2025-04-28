using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagement.API.Migrations
{
    /// <inheritdoc />
    public partial class updateKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "LogID",
                table: "UserActivityLogs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "RoleID",
                table: "Roles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "CategoryID",
                table: "Categories",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "BookID",
                table: "Books",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "RatingID",
                table: "BookRatings",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "RequestID",
                table: "BookBorrowingRequests",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "RequestDetailID",
                table: "BookBorrowingRequestDetails",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "UserActivityLogs",
                newName: "LogID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Roles",
                newName: "RoleID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Categories",
                newName: "CategoryID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Books",
                newName: "BookID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "BookRatings",
                newName: "RatingID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "BookBorrowingRequests",
                newName: "RequestID");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "BookBorrowingRequestDetails",
                newName: "RequestDetailID");
        }
    }
}
