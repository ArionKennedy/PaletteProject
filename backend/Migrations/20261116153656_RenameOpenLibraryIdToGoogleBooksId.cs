using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Palette.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameOpenLibraryIdToGoogleBooksId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpenLibraryId",
                table: "BOOKS",
                newName: "GoogleBooksId");

            migrationBuilder.RenameIndex(
                name: "IX_BOOKS_OpenLibraryId",
                table: "BOOKS",
                newName: "IX_BOOKS_GoogleBooksId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GoogleBooksId",
                table: "BOOKS",
                newName: "OpenLibraryId");

            migrationBuilder.RenameIndex(
                name: "IX_BOOKS_GoogleBooksId",
                table: "BOOKS",
                newName: "IX_BOOKS_OpenLibraryId");
        }
    }
}
