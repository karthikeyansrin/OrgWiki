using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrgWiki.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOwnedWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_generated_articles_Key",
                table: "generated_articles");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "uploads",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_uploads_UserId",
                table: "uploads",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_uploads_users_UserId",
                table: "uploads",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_uploads_users_UserId",
                table: "uploads");

            migrationBuilder.DropIndex(
                name: "IX_uploads_UserId",
                table: "uploads");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "uploads");

            migrationBuilder.CreateIndex(
                name: "IX_generated_articles_Key",
                table: "generated_articles",
                column: "Key",
                unique: true,
                filter: "\"Status\" = 'Published'");
        }
    }
}
