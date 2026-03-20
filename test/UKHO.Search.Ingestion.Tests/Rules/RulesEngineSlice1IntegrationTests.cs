using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Rules;
using UKHO.Search.Ingestion.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class RulesEngineSlice1IntegrationTests
    {
        [Fact]
        public async Task Rules_enricher_adds_keyword_for_file_share_provider()
        {
            var tempRoot = Path.Combine(Path.GetTempPath(), $"ukho-search-rules-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempRoot);

            try
            {
                var providerRulesRoot = Path.Combine(tempRoot, "Rules", "file-share");
                Directory.CreateDirectory(providerRulesRoot);
                var rulesPath = Path.Combine(providerRulesRoot, "slice1-test-rule.json");
                await File.WriteAllTextAsync(rulesPath, """
                                                        {
                                                          "schemaVersion": "1.0",
                                                          "rule": {
                                                            "id": "slice1-test-rule",
                                                            "title": "Slice 1 test rule",
                                                            "if": { "any": [ { "path": "id", "exists": true } ] },
                                                            "then": { "keywords": { "add": [ "slice1-keyword" ] } }
                                                          }
                                                        }
                                                        """);

                await using var provider = IngestionRulesTestServiceProviderFactory.Create(
                    tempRoot,
                    configureServices: services => services.AddScoped<IFileShareZipDownloader>(_ => new ThrowingZipDownloader()));
                using var scope = provider.CreateScope();

                var providerContext = scope.ServiceProvider.GetRequiredService<IIngestionProviderContext>();
                providerContext.ProviderName = "file-share";

                var enrichers = scope.ServiceProvider.GetServices<IIngestionEnricher>()
                                     .ToArray();
                var rulesEnricher = enrichers.Single(x => string.Equals(x.GetType()
                                                                         .Name, "IngestionRulesEnricher", StringComparison.Ordinal));

                var request = new IngestionRequest(IngestionRequestType.IndexItem, new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null);

                var doc = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

                await rulesEnricher.TryBuildEnrichmentAsync(request, doc, CancellationToken.None);

                doc.Keywords.ShouldContain("slice1-keyword");
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
}