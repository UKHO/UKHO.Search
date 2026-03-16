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
    public sealed class RulesEngineSlice4ActionsIntegrationTests
    {
        [Fact]
        public void Matching_rules_apply_all_actions_with_templating_and_dedupe()
        {
            using var temp = new TempRulesRoot();

            temp.WriteRuleFile("file-share", "r1", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "r1",
                                    "if": { "id": "doc-1" },
                                    "then": {
                                      "keywords": { "add": [ "Key1", "$nope" ] },
                                      "searchText": { "add": [ "First" ] },
                                      "content": { "add": [ "C1" ] }
                                    }
                                  }
                                }
                                """);

            temp.WriteRuleFile("file-share", "r2", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "r2",
                                    "if": { "id": "doc-1" },
                                    "then": {
                                      "searchText": { "add": [ "First", "Second" ] }
                                    }
                                  }
                                }
                                """);

            temp.WriteRuleFile("file-share", "r3", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "r3",
                                    "if": { "files[*].mimeType": "app/s63" },
                                    "then": {
                                      "keywords": { "add": [ "mime-$val" ] }
                                    }
                                  }
                                }
                                """);

            temp.WriteRuleFile("file-share", "r4", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "r4",
                                    "if": { "all": [ { "path": "properties[\"abcdef\"]", "exists": true } ] },
                                    "then": { }
                                  }
                                }
                                """);

            temp.WriteRuleFile("file-share", "r6", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "r6",
                                    "if": { "all": [ { "path": "files[*].mimeType", "exists": true } ] },
                                    "then": {
                                      "keywords": { "add": [ "all-$val" ] }
                                    }
                                  }
                                }
                                """);

            temp.WriteRuleFile("other-provider", "other", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "other",
                                    "if": { "id": "doc-1" },
                                    "then": { "keywords": { "add": [ "should-not-apply" ] } }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequest();
            var document = CanonicalDocument.CreateMinimal("doc-1", request.IndexItem!, request.IndexItem.Timestamp);

            engine.Apply("file-share", request, document);

            document.Keywords.ShouldContain("key1");
            document.Keywords.ShouldContain("mime-app/s63");
            document.Keywords.ShouldContain("all-app/s63");
            document.Keywords.ShouldContain("all-text/plain");
            document.Keywords.ShouldNotContain("$nope");
            document.Keywords.ShouldNotContain("should-not-apply");

            document.SearchText.ShouldBe("first second");
            document.Content.ShouldBe("c1");
        }

        private static ServiceProvider CreateProvider(string contentRootPath)
        {
            var services = new ServiceCollection();

            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { ContentRootPath = contentRootPath });
            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug));
            services.AddIngestionServices();

            return services.BuildServiceProvider();
        }

        private static IngestionRequest CreateRequest()
        {
            var indexRequest = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "abcdef", Type = IngestionPropertyType.String, Value = "a value" }
            ], ["token"], DateTimeOffset.UtcNow, new IngestionFileList
            {
                new IngestionFile("f1", 1, DateTimeOffset.UtcNow, "app/s63"),
                new IngestionFile("f2", 1, DateTimeOffset.UtcNow, "text/plain")
            });

            return new IngestionRequest(IngestionRequestType.IndexItem, indexRequest, null, null);
        }
    }
}