using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrgWiki.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using OrgWiki.Application.Ingestion;
using OrgWiki.Infrastructure.Ingestion;
using OrgWiki.Infrastructure.Storage;
using OrgWiki.Application.Analysis;
using OrgWiki.Infrastructure.Analysis;
using OrgWiki.Application.Review;
using OrgWiki.Infrastructure.Review;
using OrgWiki.Application.KnowledgeBase;
using OrgWiki.Infrastructure.KnowledgeBase;

namespace OrgWiki.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration["DATABASE_URL"]
            ?? configuration.GetConnectionString("OrgWiki")
            ?? throw new InvalidOperationException("A PostgreSQL connection string must be configured.");

        services.AddDbContext<OrgWikiDbContext>(options => options.UseNpgsql(connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(DependencyInjection).Assembly.GetName().Name)));
        services.Configure<IngestionOptions>(options =>
        {
            var section = configuration.GetSection(IngestionOptions.SectionName);
            if (long.TryParse(section[nameof(IngestionOptions.MaxArchiveBytes)], out var maxArchive)) options.MaxArchiveBytes = maxArchive;
            if (int.TryParse(section[nameof(IngestionOptions.MaxSupportedDocuments)], out var maxCount)) options.MaxSupportedDocuments = maxCount;
            if (long.TryParse(section[nameof(IngestionOptions.MaxIndividualFileBytes)], out var maxFile)) options.MaxIndividualFileBytes = maxFile;
            if (long.TryParse(section[nameof(IngestionOptions.MaxTotalExtractedBytes)], out var maxTotal)) options.MaxTotalExtractedBytes = maxTotal;
            if (int.TryParse(section[nameof(IngestionOptions.MaxNormalizedCharactersPerDocument)], out var maxDocumentCharacters)) options.MaxNormalizedCharactersPerDocument = maxDocumentCharacters;
            if (int.TryParse(section[nameof(IngestionOptions.MaxTotalNormalizedCharacters)], out var maxCorpusCharacters)) options.MaxTotalNormalizedCharacters = maxCorpusCharacters;
            if (int.TryParse(section[nameof(IngestionOptions.MaxPdfPages)], out var maxPdfPages)) options.MaxPdfPages = maxPdfPages;
            options.LocalStoragePath = section[nameof(IngestionOptions.LocalStoragePath)] ?? options.LocalStoragePath;
        });
        services.AddHttpClient("OpenAI", (serviceProvider, client) =>
        {
            client.BaseAddress = new Uri("https://api.openai.com/");
            var timeout = serviceProvider.GetRequiredService<IOptions<KnowledgeAnalysisOptions>>().Value.TimeoutSeconds;
            if (timeout <= 0) throw new OptionsValidationException(nameof(KnowledgeAnalysisOptions), typeof(KnowledgeAnalysisOptions), ["OpenAI timeout must be positive."]);
            client.Timeout = TimeSpan.FromSeconds(timeout);
        });
        services.AddScoped<IKnowledgeDiscoveryProvider, ReplayKnowledgeDiscoveryProvider>();
        services.AddScoped<IKnowledgeDiscoveryProvider, OpenAiKnowledgeDiscoveryProvider>();
        services.AddScoped<IKnowledgeDiscoveryValidator, KnowledgeDiscoveryValidator>();
        services.AddScoped<DeterministicCorpusBuilder>();
        services.AddScoped<IKnowledgeAnalysisService, KnowledgeAnalysisService>();
        services.AddScoped<IKnowledgeGenerationProvider, ReplayKnowledgeGenerationProvider>();
        services.AddScoped<IKnowledgeGenerationProvider, OpenAiKnowledgeGenerationProvider>();
        services.AddScoped<KnowledgeGenerationContextBuilder>();
        services.AddScoped<IKnowledgeGenerationValidator, KnowledgeGenerationValidator>();
        services.AddScoped<IKnowledgeGenerationService, KnowledgeGenerationService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IZipArchiveExtractor, SafeZipArchiveExtractor>();
        services.AddScoped<IContentNormalizer, ContentNormalizer>();
        services.AddScoped<IDocumentParser, PdfDocumentParser>();
        services.AddScoped<IDocumentParser, DocxDocumentParser>();
        services.AddScoped<IDocumentParser, MarkdownDocumentParser>();
        services.AddScoped<IDocumentParser, TextDocumentParser>();
        services.AddScoped<IIngestionService, IngestionService>();

        return services;
    }
}
