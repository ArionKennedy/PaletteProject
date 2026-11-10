using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Palette.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameAuth0IdToClerkID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Auth0Id",
                table: "USERS",
                newName: "ClerkId");

            migrationBuilder.RenameIndex(
                name: "IX_USERS_Auth0Id",
                table: "USERS",
                newName: "IX_USERS_ClerkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ClerkId",
                table: "USERS",
                newName: "Auth0Id");

            migrationBuilder.RenameIndex(
                name: "IX_USERS_ClerkId",
                table: "USERS",
                newName: "IX_USERS_Auth0Id");
        }
    }
}
