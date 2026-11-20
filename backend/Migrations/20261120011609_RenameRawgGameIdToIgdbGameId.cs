using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Palette.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameRawgGameIdToIgdbGameId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RawgGameId",
                table: "GAMES",
                newName: "IgdbGameId");

            migrationBuilder.RenameIndex(
                name: "IX_GAMES_RawgGameId",
                table: "GAMES",
                newName: "IX_GAMES_IgdbGameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IgdbGameId",
                table: "GAMES",
                newName: "RawgGameId");

            migrationBuilder.RenameIndex(
                name: "IX_GAMES_IgdbGameId",
                table: "GAMES",
                newName: "IX_GAMES_RawgGameId");
        }
    }
}
