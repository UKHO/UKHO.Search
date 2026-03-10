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
    public sealed class RulesEngineEndToEndExampleTests
    {
        [Fact]
        public void Mime_type_app_s63_enriches_keywords_and_search_text()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile(GetExampleRulesJson());

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequest(
                id: "doc-1",
                properties: Array.Empty<IngestionProperty>(),
                files: new IngestionFileList
                {
                    new IngestionFile("f1", 1, DateTimeOffset.UtcNow, "app/s63")
                });

            var document = CanonicalDocument.CreateMinimal("doc-1", request);
            engine.Apply("file-share", request, document);

            document.Keywords.ShouldContain("exchange-set");
            document.SearchText.ShouldBe("exchange set exchangeset");
        }

        [Fact]
        public void Property_abcdef_equals_a_value_adds_keywords()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile(GetExampleRulesJson());

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequest(
                id: "doc-2",
                properties:
                [
                    new IngestionProperty { Name = "abcdef", Type = IngestionPropertyType.String, Value = "a value" }
                ],
                files: new IngestionFileList());

            var document = CanonicalDocument.CreateMinimal("doc-2", request);
            engine.Apply("file-share", request, document);

            document.Keywords.ShouldContain("key1");
            document.Keywords.ShouldContain("key2");
        }

        [Fact]
        public void Property_abcdef_exists_adds_facet_with_path_value()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile(GetExampleRulesJson());

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequest(
                id: "doc-3",
                properties:
                [
                    new IngestionProperty { Name = "abcdef", Type = IngestionPropertyType.String, Value = "another" }
                ],
                files: new IngestionFileList());

            var document = CanonicalDocument.CreateMinimal("doc-3", request);
            engine.Apply("file-share", request, document);

            document.Facets.ShouldContainKey("facet 1");
            document.Facets["facet 1"].ShouldBe(new[] { "another" });
            document.Keywords.ShouldNotContain("key1");
            document.Keywords.ShouldNotContain("key2");
        }

        private static ServiceProvider CreateProvider(string contentRootPath)
        {
            var services = new ServiceCollection();

            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { ContentRootPath = contentRootPath });
            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug));
            services.AddIngestionServices();

            return services.BuildServiceProvider();
        }

        private static IngestionRequest CreateRequest(string id, IReadOnlyList<IngestionProperty> properties, IngestionFileList files)
        {
            var addItem = new AddItemRequest(
                id: id,
                properties: properties,
                securityTokens: ["token"],
                timestamp: DateTimeOffset.UtcNow,
                files: files);

            return new IngestionRequest(IngestionRequestType.AddItem, addItem, updateItem: null, deleteItem: null, updateAcl: null);
        }

        private static string GetExampleRulesJson()
        {
            return """
            {
              "schemaVersion": "1.0",
              "rules": {
                "file-share": [
                  {
                    "id": "mime-app-s63",
                    "description": "When any file is app/s63, enrich as exchange set",
                    "enabled": true,
                    "if": {
                      "files[*].mimeType": "app/s63"
                    },
                    "then": {
                      "keywords": { "add": ["exchange-set"] },
                      "searchText": { "add": ["exchange set", "exchangeset"] }
                    }
                  },
                  {
                    "id": "prop-abcdef-keywords",
                    "description": "When properties.abcdef equals 'a value', add key1/key2",
                    "enabled": true,
                    "if": {
                      "properties[\"abcdef\"]": "a value"
                    },
                    "then": {
                      "keywords": { "add": ["key1", "key2"] }
                    }
                  },
                  {
                    "id": "prop-abcdef-facet",
                    "description": "When properties.abcdef exists, add facet 1 with that value",
                    "enabled": true,
                    "if": {
                      "all": [
                        { "path": "properties[\"abcdef\"]", "exists": true }
                      ]
                    },
                    "then": {
                      "facets": {
                        "add": [
                          { "name": "facet 1", "value": "$path:properties[\"abcdef\"]" }
                        ]
                      }
                    }
                  }
                ]
              }
            }
            """;
        }
    }
}
