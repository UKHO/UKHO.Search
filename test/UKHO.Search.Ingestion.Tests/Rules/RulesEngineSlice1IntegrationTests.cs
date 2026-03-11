using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Injection;
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
                var rulesPath = Path.Combine(tempRoot, "ingestion-rules.json");
                await File.WriteAllTextAsync(rulesPath, """
                                                        {
                                                          "schemaVersion": "1.0",
                                                          "rules": {
                                                            "file-share": [
                                                              {
                                                                "id": "slice1-test-rule",
                                                                "if": { "any": [ { "path": "id", "exists": true } ] },
                                                                "then": { "keywords": { "add": [ "slice1-keyword" ] } }
                                                              }
                                                            ]
                                                          }
                                                        }
                                                        """);

                var env = new TestHostEnvironment { ContentRootPath = tempRoot };

                var services = new ServiceCollection();
                services.AddSingleton<IHostEnvironment>(env);
                services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug));

                services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                                                {
                                                                                    ["ingestion:fileContentExtractionAllowedExtensions"] = string.Empty
                                                                                })
                                                                                .Build());

                services.AddIngestionServices();

                services.AddScoped<IFileShareZipDownloader>(_ => new ThrowingZipDownloader());

                await using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();

                var providerContext = scope.ServiceProvider.GetRequiredService<IIngestionProviderContext>();
                providerContext.ProviderName = "file-share";

                var enrichers = scope.ServiceProvider.GetServices<IIngestionEnricher>()
                                     .ToArray();
                var rulesEnricher = enrichers.Single(x => string.Equals(x.GetType()
                                                                         .Name, "IngestionRulesEnricher", StringComparison.Ordinal));

                var request = new IngestionRequest(IngestionRequestType.AddItem, new AddItemRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null, null);

                var doc = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

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