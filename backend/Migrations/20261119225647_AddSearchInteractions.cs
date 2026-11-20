using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Palette.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SEARCH_INTERACTIONS",
                columns: table => new
                {
                    SearchInteractionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    MediaType = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEARCH_INTERACTIONS", x => x.SearchInteractionId);
                    table.ForeignKey(
                        name: "FK_SEARCH_INTERACTIONS_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "USERS",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SEARCH_INTERACTIONS_UserId_MediaType_ExternalId_Type",
                table: "SEARCH_INTERACTIONS",
                columns: new[] { "UserId", "MediaType", "ExternalId", "Type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SEARCH_INTERACTIONS");
        }
    }
}
