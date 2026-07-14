using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OrgWiki.Infrastructure.Persistence;

#nullable disable

namespace OrgWiki.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OrgWikiDbContext))]
partial class OrgWikiDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.11");
        modelBuilder.Entity("OrgWiki.Domain.Ingestion.Document", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<int>("CharacterCount").HasColumnType("integer");
            b.Property<string>("Content").HasColumnType("text");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<string>("DocumentType").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("FileExtension").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)");
            b.Property<string>("FileName").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
            b.Property<string>("OriginalPath").IsRequired().HasMaxLength(1024).HasColumnType("character varying(1024)");
            b.Property<string>("ProcessingError").HasColumnType("text");
            b.Property<string>("ProcessingStatus").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<Guid>("UploadId").HasColumnType("uuid");
            b.Property<int>("WordCount").HasColumnType("integer");
            b.HasKey("Id"); b.HasIndex("UploadId"); b.ToTable("documents");
        });
        modelBuilder.Entity("OrgWiki.Domain.Ingestion.Upload", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid");
            b.Property<DateTime?>("CompletedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone");
            b.Property<int>("FailedFiles").HasColumnType("integer");
            b.Property<string>("AnalysisEligibilityReason").HasColumnType("text");
            b.Property<bool>("IsEligibleForAnalysis").HasColumnType("boolean");
            b.Property<string>("OriginalFileName").IsRequired().HasMaxLength(255).HasColumnType("character varying(255)");
            b.Property<string>("Status").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)");
            b.Property<string>("StorageKey").IsRequired().HasMaxLength(512).HasColumnType("character varying(512)");
            b.Property<int>("SupportedFiles").HasColumnType("integer"); b.Property<int>("TotalCharacterCount").HasColumnType("integer"); b.Property<int>("TotalFiles").HasColumnType("integer");
            b.HasKey("Id"); b.HasIndex("CreatedAtUtc"); b.HasIndex("Status"); b.ToTable("uploads");
        });
        modelBuilder.Entity("OrgWiki.Domain.Analysis.KnowledgeAnalysis", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<string>("AiMode").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<DateTime?>("CompletedAtUtc").HasColumnType("timestamp with time zone"); b.Property<long?>("DurationMilliseconds").HasColumnType("bigint"); b.Property<string>("ErrorMessage").HasColumnType("text"); b.Property<int?>("InputTokens").HasColumnType("integer"); b.Property<bool>("IsCurrent").HasColumnType("boolean"); b.Property<int?>("OutputTokens").HasColumnType("integer"); b.Property<string>("Model").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)"); b.Property<string>("ResultJson").HasColumnType("text"); b.Property<DateTime>("StartedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("Status").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)"); b.Property<int?>("TotalTokens").HasColumnType("integer"); b.Property<Guid>("UploadId").HasColumnType("uuid"); b.HasKey("Id"); b.HasIndex("UploadId").IsUnique().HasFilter("\"IsCurrent\" = TRUE"); b.HasIndex("UploadId", "Status"); b.ToTable("knowledge_analyses");
        });
        modelBuilder.Entity("OrgWiki.Domain.Analysis.KnowledgeGeneration", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<Guid>("AnalysisId").HasColumnType("uuid"); b.Property<string>("AiMode").IsRequired().HasMaxLength(16).HasColumnType("character varying(16)"); b.Property<DateTime>("CreatedAtUtc").HasColumnType("timestamp with time zone"); b.Property<DateTime?>("CompletedAtUtc").HasColumnType("timestamp with time zone"); b.Property<long?>("DurationMilliseconds").HasColumnType("bigint"); b.Property<string>("ErrorMessage").HasColumnType("text"); b.Property<int?>("InputTokens").HasColumnType("integer"); b.Property<bool>("IsCurrent").HasColumnType("boolean"); b.Property<string>("Model").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)"); b.Property<int?>("OutputTokens").HasColumnType("integer"); b.Property<string>("ResultJson").HasColumnType("text"); b.Property<DateTime>("StartedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("Status").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)"); b.Property<int?>("TotalTokens").HasColumnType("integer"); b.HasKey("Id"); b.HasIndex("AnalysisId").IsUnique().HasFilter("\"IsCurrent\" = TRUE"); b.ToTable("knowledge_generations");
        });
        modelBuilder.Entity("OrgWiki.Domain.Analysis.GeneratedArticle", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<double>("Confidence").HasColumnType("double precision"); b.Property<string>("Difficulty").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)"); b.Property<int>("EstimatedReadingMinutes").HasColumnType("integer"); b.Property<DateTime>("GeneratedAtUtc").HasColumnType("timestamp with time zone"); b.Property<Guid>("GenerationId").HasColumnType("uuid"); b.Property<string>("Key").IsRequired().HasMaxLength(128).HasColumnType("character varying(128)"); b.Property<DateTime?>("LastEditedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("LastEditedBy").HasMaxLength(128).HasColumnType("character varying(128)"); b.Property<string>("MarkdownContent").IsRequired().HasColumnType("text"); b.Property<string>("RelatedArticleKeysJson").IsRequired().HasColumnType("text"); b.Property<DateTime?>("ReviewedAtUtc").HasColumnType("timestamp with time zone"); b.Property<string>("ReviewedBy").HasMaxLength(128).HasColumnType("character varying(128)"); b.Property<string>("ReviewNotes").HasColumnType("text"); b.Property<string>("Status").IsRequired().HasMaxLength(32).HasColumnType("character varying(32)"); b.Property<string>("Summary").IsRequired().HasColumnType("text"); b.Property<string>("TagsJson").IsRequired().HasColumnType("text"); b.Property<string>("Title").IsRequired().HasMaxLength(512).HasColumnType("character varying(512)"); b.HasKey("Id"); b.HasIndex("GenerationId", "Key").IsUnique(); b.ToTable("generated_articles");
        });
        modelBuilder.Entity("OrgWiki.Domain.Analysis.GeneratedArticleCitation", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uuid"); b.Property<Guid>("GeneratedArticleId").HasColumnType("uuid"); b.Property<string>("EvidenceSnippet").IsRequired().HasColumnType("text"); b.Property<Guid>("SourceDocumentId").HasColumnType("uuid"); b.HasKey("Id"); b.HasIndex("GeneratedArticleId"); b.ToTable("generated_article_citations");
        });
        modelBuilder.Entity("OrgWiki.Domain.Ingestion.Document", b =>
            b.HasOne("OrgWiki.Domain.Ingestion.Upload", "Upload").WithMany("Documents").HasForeignKey("UploadId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("OrgWiki.Domain.Ingestion.Document", b => b.Navigation("Upload"));
        modelBuilder.Entity("OrgWiki.Domain.Ingestion.Upload", b => b.Navigation("Documents"));
        modelBuilder.Entity("OrgWiki.Domain.Analysis.KnowledgeAnalysis", b => b.HasOne("OrgWiki.Domain.Ingestion.Upload", null).WithMany().HasForeignKey("UploadId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("OrgWiki.Domain.Analysis.KnowledgeGeneration", b => b.HasOne("OrgWiki.Domain.Analysis.KnowledgeAnalysis", null).WithMany().HasForeignKey("AnalysisId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("OrgWiki.Domain.Analysis.GeneratedArticle", b => b.HasOne("OrgWiki.Domain.Analysis.KnowledgeGeneration", null).WithMany().HasForeignKey("GenerationId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("OrgWiki.Domain.Analysis.GeneratedArticleCitation", b => b.HasOne("OrgWiki.Domain.Analysis.GeneratedArticle", "Article").WithMany("Citations").HasForeignKey("GeneratedArticleId").OnDelete(DeleteBehavior.Cascade).IsRequired());
        modelBuilder.Entity("OrgWiki.Domain.Analysis.GeneratedArticleCitation", b => b.HasOne("OrgWiki.Domain.Ingestion.Document", null).WithMany().HasForeignKey("SourceDocumentId").OnDelete(DeleteBehavior.Restrict).IsRequired());
        modelBuilder.Entity("OrgWiki.Domain.Analysis.GeneratedArticleCitation", b => b.Navigation("Article"));
        modelBuilder.Entity("OrgWiki.Domain.Analysis.GeneratedArticle", b => b.Navigation("Citations"));
    }
}
