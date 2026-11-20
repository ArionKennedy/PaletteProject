using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Palette.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMovieGenres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Genres",
                table: "MOVIES",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Genres",
                table: "MOVIES");
        }
    }
}
