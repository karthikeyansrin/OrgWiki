using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrgWiki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamSpaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "team_spaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_spaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "team_space_articles",
                columns: table => new
                {
                    TeamSpaceId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneratedArticleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_team_space_articles", x => new { x.TeamSpaceId, x.GeneratedArticleId });
                    table.ForeignKey(
                        name: "FK_team_space_articles_generated_articles_GeneratedArticleId",
                        column: x => x.GeneratedArticleId,
                        principalTable: "generated_articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_team_space_articles_team_spaces_TeamSpaceId",
                        column: x => x.TeamSpaceId,
                        principalTable: "team_spaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_team_space_articles_GeneratedArticleId",
                table: "team_space_articles",
                column: "GeneratedArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_team_spaces_Slug",
                table: "team_spaces",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "team_space_articles");

            migrationBuilder.DropTable(
                name: "team_spaces");
        }
    }
}
