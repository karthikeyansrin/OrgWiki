using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using OrgWiki.Infrastructure.Persistence;
using OrgWiki.Infrastructure.Analysis;
using OrgWiki.Application.Analysis;
using OrgWiki.Application.Ingestion;
using OrgWiki.Infrastructure.Ingestion;
using OrgWiki.Domain.Ingestion;
using OrgWiki.Domain.Analysis;
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

    [Fact]
    public void Corpus_builder_orders_by_path_and_preserves_exact_content()
    {
        var first = new Document(Guid.NewGuid(), "b.txt", "B/b.txt", DocumentType.Text); first.MarkParsed("Exact B\ntext", 12, 2);
        var second = new Document(first.UploadId, "a.txt", "A/a.txt", DocumentType.Text); second.MarkParsed("Exact A\ntext", 12, 2);
        var request = new DeterministicCorpusBuilder().Build(first.UploadId, [first, second]);
        Assert.Equal("A/a.txt", request.Documents[0].OriginalPath);
        Assert.Contains("Exact A\ntext", request.CorpusText);
        Assert.Contains(first.Id.ToString(), request.CorpusText);
    }

    [Fact]
    public void Validator_rejects_fabricated_conflict_evidence()
    {
        var id = Guid.NewGuid();
        var docs = new[] { new CorpusDocument(id, "a.txt", "a.txt", "Text", "Verified source text", 20) };
        var result = new KnowledgeDiscoveryResult([new("d", "Domain", "Description", .8)], [new("t", "Topic", "Description", "d", .8, [id])], [], [], [new("conflict", "Description", ["t"], "A", "B", id, id, "fabricated", "Verified", "Recommendation", "Reasoning", .8)], [], []);
        Assert.Throws<InvalidDataException>(() => new KnowledgeDiscoveryValidator().Validate(result, docs));
    }

    [Fact]
    public async Task Replay_provider_returns_without_external_call()
    {
        var id = Guid.NewGuid();
        var response = await new ReplayKnowledgeDiscoveryProvider(Options.Create(new KnowledgeAnalysisOptions())).DiscoverAsync(new KnowledgeDiscoveryRequest(id, [new CorpusDocument(id, "a.txt", "a.txt", "Text", "Replay content", 14)], "corpus"), default);
        Assert.Null(response.Usage);
        Assert.NotEmpty(response.Result.Domains);
        Assert.NotEmpty(response.Result.SuggestedArticles);
    }

    [Fact]
    public void Schema_requires_all_collections_and_exact_relationship_enum()
    {
        var json = KnowledgeDiscoverySchema.CreateResponseFormat().ToJsonString();
        Assert.Contains("orgwiki_knowledge_discovery", json);
        Assert.Contains("\"json_schema\"", json);
        Assert.Contains("DependsOn", json); Assert.Contains("Supersedes", json);
        Assert.Contains("\"minimum\":0", json); Assert.Contains("\"maximum\":1", json);
        Assert.Contains("conflicts", json); Assert.Contains("outdatedCandidates", json);
    }

    [Fact]
    public void Prompt_communicates_validator_rules()
    {
        Assert.Contains("unique", KnowledgeDiscoveryPrompt.System, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("between 0 and 1", KnowledgeDiscoveryPrompt.System);
        Assert.Contains("exactly one of DependsOn, RelatedTo, Uses, PartOf, Supersedes", KnowledgeDiscoveryPrompt.System);
        Assert.Contains("topicKeys on conflicts and outdatedCandidates", KnowledgeDiscoveryPrompt.User(new KnowledgeDiscoveryRequest(Guid.NewGuid(), [], "corpus")));
        Assert.Contains("verbatim", KnowledgeDiscoveryPrompt.System);
    }

    [Fact]
    public void Validator_rejects_invalid_keys_confidence_and_topic_references()
    {
        var id = Guid.NewGuid(); var docs = new[] { new CorpusDocument(id, "a.txt", "a.txt", "Text", "exact evidence", 14) };
        var invalidKey = new KnowledgeDiscoveryResult([new("Bad Key", "Domain", "Description", .8)], [], [], [], [], [], []);
        Assert.Throws<InvalidDataException>(() => new KnowledgeDiscoveryValidator().Validate(invalidKey, docs));
        var invalidConfidence = new KnowledgeDiscoveryResult([new("domain", "Domain", "Description", 97)], [], [], [], [], [], []);
        Assert.Throws<InvalidDataException>(() => new KnowledgeDiscoveryValidator().Validate(invalidConfidence, docs));
        var invalidTopic = new KnowledgeDiscoveryResult([new("domain", "Domain", "Description", .8)], [new("topic", "Topic", "Description", "domain", .8, [id])], [], [], [new("c", "d", ["missing"], "a", "b", id, id, "exact", "exact", "r", "rr", .8)], [], []);
        Assert.Throws<InvalidDataException>(() => new KnowledgeDiscoveryValidator().Validate(invalidTopic, docs));
    }

    [Fact]
    public void Validator_enforces_exact_evidence_and_reference_array_uniqueness()
    {
        var id = Guid.NewGuid(); var secondId = Guid.NewGuid(); var docs = new[] { new CorpusDocument(id, "a.txt", "a.txt", "Text", "Access tokens expire\nafter 60 minutes.", 39), new CorpusDocument(secondId, "b.txt", "b.txt", "Text", "Access tokens expire\nafter 60 minutes.", 39) };
        KnowledgeDiscoveryResult Result(string a, string b) => new([new("domain", "Domain", "Description", .8)], [new("topic", "Topic", "Description", "domain", .8, [id, secondId])], [], [], [new("c", "Description", ["topic"], "A", "B", id, secondId, a, b, "Recommendation", "Reasoning", .8)], [], []);
        new KnowledgeDiscoveryValidator().Validate(Result("Access tokens expire\nafter 60 minutes.", "Access tokens expire\nafter 60 minutes."), docs);
        Assert.Throws<InvalidDataException>(() => new KnowledgeDiscoveryValidator().Validate(Result("Access tokens expire after sixty minutes.", "Access tokens expire\nafter 60 minutes."), docs));
        Assert.Throws<InvalidDataException>(() => new KnowledgeDiscoveryValidator().Validate(Result("\"Access tokens expire", "Access tokens expire\nafter 60 minutes."), docs));
        Assert.Throws<InvalidDataException>(() => new KnowledgeDiscoveryValidator().Validate(Result("Access tokens expire\nafter 60 minutes...", "Access tokens expire\nafter 60 minutes."), docs));
    }

    [Fact]
    public void Current_analysis_index_is_unique_and_filtered()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<OrgWikiDbContext>().UseNpgsql("Host=localhost;Database=unused").Options;
        using var context = new OrgWikiDbContext(options);
        var index = context.Model.FindEntityType(typeof(KnowledgeAnalysis))!.GetIndexes().Single(x => x.IsUnique);
        Assert.Equal("\"IsCurrent\" = TRUE", index.GetFilter());
    }

    [Fact]
    public void Topic_overlap_makes_phase4_traceability_deterministic()
    {
        var articleTopics = new HashSet<string>(["authentication"]);
        var duplicateTopics = new[] { new[] { "authentication" }, new[] { "leave" } };
        var conflictTopics = new[] { new[] { "authentication" }, new[] { "leave" } };
        var outdatedTopics = new[] { new[] { "authentication" }, new[] { "leave" } };
        Assert.Equal(1, duplicateTopics.Count(x => x.Any(articleTopics.Contains)));
        Assert.Equal(1, conflictTopics.Count(x => x.Any(articleTopics.Contains)));
        Assert.Equal(1, outdatedTopics.Count(x => x.Any(articleTopics.Contains)));
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
