using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrgWiki.Infrastructure.Persistence.Migrations;

[Migration("20260714103000_Phase2DocumentIngestion")]
public partial class Phase2DocumentIngestion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "uploads",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                TotalFiles = table.Column<int>(type: "integer", nullable: false),
                SupportedFiles = table.Column<int>(type: "integer", nullable: false),
                FailedFiles = table.Column<int>(type: "integer", nullable: false),
                TotalCharacterCount = table.Column<int>(type: "integer", nullable: false),
                IsEligibleForAnalysis = table.Column<bool>(type: "boolean", nullable: false),
                AnalysisEligibilityReason = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_uploads", x => x.Id));

        migrationBuilder.CreateTable(
            name: "documents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UploadId = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                OriginalPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                FileExtension = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                DocumentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Content = table.Column<string>(type: "text", nullable: true),
                CharacterCount = table.Column<int>(type: "integer", nullable: false),
                WordCount = table.Column<int>(type: "integer", nullable: false),
                ProcessingStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ProcessingError = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_documents", x => x.Id);
                table.ForeignKey("FK_documents_uploads_UploadId", x => x.UploadId, "uploads", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_documents_UploadId", table: "documents", column: "UploadId");
        migrationBuilder.CreateIndex(name: "IX_uploads_CreatedAtUtc", table: "uploads", column: "CreatedAtUtc");
        migrationBuilder.CreateIndex(name: "IX_uploads_Status", table: "uploads", column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "documents");
        migrationBuilder.DropTable(name: "uploads");
    }
}
