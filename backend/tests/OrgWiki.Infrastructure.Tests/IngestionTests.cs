using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Migrations;
using OrgWiki.Infrastructure.Persistence;
using OrgWiki.Application.Ingestion;
using OrgWiki.Infrastructure.Ingestion;
using OrgWiki.Domain.Ingestion;
using Xunit;

namespace OrgWiki.Infrastructure.Tests;

public sealed class IngestionTests
{
    [Fact]
    public void Normalizer_preserves_paragraphs_and_counts_words()
    {
        var result = new ContentNormalizer().Normalize("  First\r\n\r\n\r\nSecond  \0");
        Assert.Equal("First\n\nSecond", result.Content);
        Assert.Equal(2, result.WordCount);
    }

    [Fact]
    public async Task Text_parser_reads_utf8_content()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, "Leave policy – 30 days", Encoding.UTF8);
        try { var result = await new TextDocumentParser().ParseAsync(path, default); Assert.Contains("Leave policy", result.Content); }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Markdown_parser_preserves_code_blocks()
    {
        var path = Path.GetTempFileName() + ".md";
        await File.WriteAllTextAsync(path, "# API\n\n```bash\ncurl /health\n```");
        try { var result = await new MarkdownDocumentParser().ParseAsync(path, default); Assert.Contains("curl /health", result.Content); }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Zip_extractor_rejects_path_traversal()
    {
        await using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        await using (var writer = new StreamWriter(archive.CreateEntry("../escape.txt").Open())) await writer.WriteAsync("unsafe");
        stream.Position = 0;
        var extractor = new SafeZipArchiveExtractor(Options.Create(new IngestionOptions()));
        await Assert.ThrowsAsync<InvalidDataException>(() => extractor.ExtractSupportedFilesAsync(stream, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), default));
    }

    [Fact]
    public async Task Zip_extractor_limits_supported_documents_but_ignores_unsupported_files()
    {
        await using var tooMany = CreateZip(Enumerable.Range(0, 3).Select(i => ($"doc{i}.txt", "text")));
        var options = Options.Create(new IngestionOptions { MaxSupportedDocuments = 2 });
        await Assert.ThrowsAsync<InvalidDataException>(() => new SafeZipArchiveExtractor(options)
            .ExtractSupportedFilesAsync(tooMany, TempDirectory(), default));

        await using var mixed = CreateZip([("one.txt", "text"), ("image.png", new string('x', 1000))]);
        var result = await new SafeZipArchiveExtractor(options).ExtractSupportedFilesAsync(mixed, TempDirectory(), default);
        Assert.Single(result.Files);
        Assert.Equal(2, result.TotalFiles);
    }

    [Fact]
    public async Task Zip_extractor_records_oversized_supported_file_without_extracting_it()
    {
        await using var zip = CreateZip([("large.txt", "123456789")]);
        var result = await new SafeZipArchiveExtractor(Options.Create(new IngestionOptions { MaxIndividualFileBytes = 4 }))
            .ExtractSupportedFilesAsync(zip, TempDirectory(), default);
        Assert.Equal("Document exceeds the 2 MB MVP processing limit.", result.Files.Single().ExtractionError);
        Assert.False(File.Exists(result.Files.Single().FullPath));
    }

    [Fact]
    public async Task Zip_extractor_rejects_total_uncompressed_size_limit()
    {
        await using var zip = CreateZip([("one.txt", "12345"), ("two.txt", "67890")]);
        await Assert.ThrowsAsync<InvalidDataException>(() => new SafeZipArchiveExtractor(
            Options.Create(new IngestionOptions { MaxTotalExtractedBytes = 8 }))
            .ExtractSupportedFilesAsync(zip, TempDirectory(), default));
    }

    [Fact]
    public void Upload_can_be_completed_but_ineligible_without_mutating_document_content()
    {
        var upload = new Upload("knowledge.zip", "archive");
        upload.Complete(2, 2, 0, 301);
        upload.SetAnalysisEligibility(false, "The extracted knowledge corpus exceeds the 300,000 character MVP analysis limit.");
        Assert.Equal(301, upload.TotalCharacterCount);
        Assert.False(upload.IsEligibleForAnalysis);
        Assert.Equal("The extracted knowledge corpus exceeds the 300,000 character MVP analysis limit.", upload.AnalysisEligibilityReason);
    }

    [Fact]
    public void Phase2_migration_is_discoverable()
    {
        var migrations = typeof(OrgWikiDbContext).Assembly.GetTypes()
            .Where(type => typeof(Migration).IsAssignableFrom(type))
            .Select(type => (type, attribute: type.GetCustomAttributes(typeof(MigrationAttribute), false).Cast<MigrationAttribute>().SingleOrDefault()))
            .ToList();
        Assert.Contains(migrations, migration => migration.attribute?.Id == "20260714103000_Phase2DocumentIngestion");
    }

    private static string TempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "orgwiki-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static MemoryStream CreateZip(IEnumerable<(string Name, string Content)> files)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        foreach (var file in files)
        {
            using var writer = new StreamWriter(archive.CreateEntry(file.Name).Open());
            writer.Write(file.Content);
        }
        stream.Position = 0;
        return stream;
    }
}
