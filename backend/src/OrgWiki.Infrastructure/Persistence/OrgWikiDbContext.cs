using Microsoft.EntityFrameworkCore;
using OrgWiki.Domain.Ingestion;
using OrgWiki.Domain.Analysis;
using OrgWiki.Domain.Authentication;
using OrgWiki.Domain.TeamSpaces;

namespace OrgWiki.Infrastructure.Persistence;

public sealed class OrgWikiDbContext(DbContextOptions<OrgWikiDbContext> options) : DbContext(options)
{
    public DbSet<Upload> Uploads => Set<Upload>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<KnowledgeAnalysis> KnowledgeAnalyses => Set<KnowledgeAnalysis>();
    public DbSet<KnowledgeGeneration> KnowledgeGenerations => Set<KnowledgeGeneration>();
    public DbSet<GeneratedArticle> GeneratedArticles => Set<GeneratedArticle>();
    public DbSet<GeneratedArticleCitation> GeneratedArticleCitations => Set<GeneratedArticleCitation>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TeamSpace> TeamSpaces => Set<TeamSpace>();
    public DbSet<TeamSpaceArticle> TeamSpaceArticles => Set<TeamSpaceArticle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Upload>(entity =>
        {
            entity.ToTable("uploads");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            entity.Property(x => x.UploadedBy).HasMaxLength(128);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.Status);
            entity.HasMany(x => x.Documents).WithOne(x => x.Upload)
                .HasForeignKey(x => x.UploadId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.OriginalPath).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.FileExtension).HasMaxLength(16).IsRequired();
            entity.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ProcessingStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.UploadId);
        });
        modelBuilder.Entity<KnowledgeAnalysis>(entity =>
        {
            entity.ToTable("knowledge_analyses"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.AiMode).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(128).IsRequired();
            entity.Property(x => x.IsCurrent).IsRequired();
            entity.HasIndex(x => x.UploadId).HasFilter("\"IsCurrent\" = TRUE").IsUnique();
            entity.HasIndex(x => new { x.UploadId, x.Status });
            entity.HasOne<Upload>().WithMany().HasForeignKey(x => x.UploadId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<KnowledgeGeneration>(entity =>
        {
            entity.ToTable("knowledge_generations"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.AiMode).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(128).IsRequired(); entity.Property(x => x.IsCurrent).IsRequired();
            entity.HasIndex(x => x.AnalysisId).HasFilter("\"IsCurrent\" = TRUE").IsUnique();
            entity.HasOne<KnowledgeAnalysis>().WithMany().HasForeignKey(x => x.AnalysisId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<GeneratedArticle>(entity =>
        {
            entity.ToTable("generated_articles"); entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(128).IsRequired(); entity.Property(x => x.Title).HasMaxLength(512).IsRequired();
            entity.Property(x => x.Summary).IsRequired(); entity.Property(x => x.MarkdownContent).IsRequired();
            entity.Property(x => x.Difficulty).HasMaxLength(32).IsRequired(); entity.Property(x => x.TagsJson).IsRequired(); entity.Property(x => x.RelatedArticleKeysJson).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired(); entity.HasIndex(x => new { x.GenerationId, x.Key }).IsUnique();
            entity.Property(x => x.ReviewedBy).HasMaxLength(128); entity.Property(x => x.LastEditedBy).HasMaxLength(128);
            entity.Property(x => x.PublishedBy).HasMaxLength(128);
            entity.HasOne<KnowledgeGeneration>().WithMany().HasForeignKey(x => x.GenerationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<GeneratedArticleCitation>(entity =>
        {
            entity.ToTable("generated_article_citations"); entity.HasKey(x => x.Id); entity.Property(x => x.EvidenceSnippet).IsRequired();
            entity.HasIndex(x => x.GeneratedArticleId); entity.HasOne(x => x.Article).WithMany(x => x.Citations).HasForeignKey(x => x.GeneratedArticleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<OrgWiki.Domain.Ingestion.Document>().WithMany().HasForeignKey(x => x.SourceDocumentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TeamSpace>(entity =>
        {
            entity.ToTable("team_spaces");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1024).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();
        });
        modelBuilder.Entity<TeamSpaceArticle>(entity =>
        {
            entity.ToTable("team_space_articles");
            entity.HasKey(x => new { x.TeamSpaceId, x.GeneratedArticleId });
            entity.HasIndex(x => x.GeneratedArticleId);
            entity.HasOne(x => x.TeamSpace).WithMany(x => x.ArticleAssignments).HasForeignKey(x => x.TeamSpaceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Article).WithMany(x => x.TeamSpaceAssignments).HasForeignKey(x => x.GeneratedArticleId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
