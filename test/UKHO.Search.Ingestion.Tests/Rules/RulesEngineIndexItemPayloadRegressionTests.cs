using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class RulesEngineIndexItemPayloadRegressionTests
    {
        [Fact]
        public void IndexItem_payload_is_used_for_rule_evaluation()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "payload-selection",
                                    "enabled": true,
                                    "if": { "id": "add-id" },
                                    "then": { "keywords": { "add": [ "matched" ] } }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = new IngestionRequest
            {
                RequestType = IngestionRequestType.IndexItem,
                IndexItem = new IndexRequest("add-id", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList())
            };

            var document = CanonicalDocument.CreateMinimal("doc-1", new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);

            engine.Apply("file-share", request, document);

            document.Keywords.ShouldContain("matched");
        }

        [Fact]
        public void IndexItem_payload_enrichment_populates_new_fields_on_the_canonical_document()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "additional-fields",
                                    "enabled": true,
                                    "if": { "id": "doc-1" },
                                    "then": {
                                      "authority": { "add": [ "UKHO" ] },
                                      "region": { "add": [ "$path:properties[\"region\"]" ] },
                                      "format": { "add": [ "PDF" ] },
                                      "majorVersion": { "add": [ 2 ] },
                                      "minorVersion": { "add": [ 10 ] },
                                      "category": { "add": [ "Charts" ] },
                                      "series": { "add": [ "A" ] },
                                      "instance": { "add": [ "1" ] }
                                    }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = new IngestionRequest
            {
                RequestType = IngestionRequestType.IndexItem,
                IndexItem = new IndexRequest(
                    "doc-1",
                    new[] { new IngestionProperty { Name = "region", Type = IngestionPropertyType.String, Value = "Europe" } },
                    new[] { "t1" },
                    DateTimeOffset.UnixEpoch,
                    new IngestionFileList())
            };

            var document = CanonicalDocument.CreateMinimal("doc-1", request.IndexItem!, request.IndexItem.Timestamp);

            engine.Apply("file-share", request, document);

            document.Authority.ShouldBe(new[] { "ukho" });
            document.Region.ShouldBe(new[] { "europe" });
            document.Format.ShouldBe(new[] { "pdf" });
            document.MajorVersion.ShouldBe(new[] { 2 });
            document.MinorVersion.ShouldBe(new[] { 10 });
            document.Category.ShouldBe(new[] { "charts" });
            document.Series.ShouldBe(new[] { "a" });
            document.Instance.ShouldBe(new[] { "1" });
        }

        private static ServiceProvider CreateProvider(string contentRootPath)
        {
            var services = new ServiceCollection();

            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { ContentRootPath = contentRootPath });
            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug));
            services.AddIngestionServices();

            return services.BuildServiceProvider();
        }
    }
}