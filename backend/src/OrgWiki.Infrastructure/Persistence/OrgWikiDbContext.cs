using Microsoft.EntityFrameworkCore;
using OrgWiki.Domain.Ingestion;

namespace OrgWiki.Infrastructure.Persistence;

public sealed class OrgWikiDbContext(DbContextOptions<OrgWikiDbContext> options) : DbContext(options)
{
    public DbSet<Upload> Uploads => Set<Upload>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Upload>(entity =>
        {
            entity.ToTable("uploads");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.Status);
            entity.HasMany(x => x.Documents).WithOne(x => x.Upload)
                .HasForeignKey(x => x.UploadId).OnDelete(DeleteBehavior.Cascade);
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
    }
}
