using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class RulesEngineAdditionalFieldsIntegrationTests
    {
        [Fact]
        public void Matching_rules_add_multiple_values_to_new_fields_with_dedupe_and_sorting()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "tax1",
                                    "if": { "id": "doc-1" },
                                    "then": {
                                      "authority": { "add": [ "UKHO", "Admiralty", "ukho" ] },
                                      "region": { "add": [ "Europe", "europe", "  " ] },
                                      "format": { "add": [ "PDF" ] },
                                      "majorVersion": { "add": [ 2, 1, 2 ] },
                                      "minorVersion": { "add": [ 10, 2 ] },
                                      "category": { "add": [ "Charts" ] },
                                      "series": { "add": [ "B", "a" ] },
                                      "instance": { "add": [ "2", "1" ] }
                                    }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = new IngestionRequest
            {
                RequestType = IngestionRequestType.IndexItem,
                IndexItem = new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList())
            };

            var document = CanonicalDocument.CreateMinimal("doc-1", request.IndexItem!, request.IndexItem.Timestamp);

            engine.Apply("file-share", request, document);

            document.Authority.ShouldBe(new[] { "admiralty", "ukho" });
            document.Region.ShouldBe(new[] { "europe" });
            document.Format.ShouldBe(new[] { "pdf" });
            document.MajorVersion.ShouldBe(new[] { 1, 2 });
            document.MinorVersion.ShouldBe(new[] { 2, 10 });
            document.Category.ShouldBe(new[] { "charts" });
            document.Series.ShouldBe(new[] { "a", "b" });
            document.Instance.ShouldBe(new[] { "1", "2" });
        }

        private static ServiceProvider CreateProvider(string contentRootPath)
        {
            return IngestionRulesTestServiceProviderFactory.Create(contentRootPath);
        }
    }
}
