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
    public sealed class RulesEngineEndToEndExampleTests
    {
        [Fact]
        public void Mime_type_app_s63_enriches_keywords_and_search_text()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "mime-app-s63", GetMimeAppS63RuleJson());
            temp.WriteRuleFile("file-share", "prop-abcdef-keywords", GetPropAbcdefKeywordsRuleJson());

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequest("doc-1", new IngestionPropertyList(), new IngestionFileList
            {
                new IngestionFile("f1", 1, DateTimeOffset.UtcNow, "app/s63")
            });

            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);
            engine.Apply("file-share", request, document);

            document.Title.ShouldBe(new[] { "Exchange Set app/s63" });
            document.Keywords.ShouldContain("exchange-set");
            document.SearchText.ShouldBe("exchange set exchangeset");
        }

        [Fact]
        public void Rule_title_is_added_to_canonical_document_without_lowercasing()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "title-only", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "title-only",
                                    "title": "Display Title $path:properties[\"abcdef\"]",
                                    "enabled": true,
                                    "if": {
                                      "properties[\"abcdef\"]": "a value"
                                    },
                                    "then": {
                                      "keywords": { "add": [ "key1" ] }
                                    }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequest("doc-3", new IngestionPropertyList
            {
                new IngestionProperty { Name = "abcdef", Type = IngestionPropertyType.String, Value = "a value" }
            }, new IngestionFileList());

            var document = CanonicalDocument.CreateMinimal("doc-3", "file-share", request.IndexItem!, request.IndexItem.Timestamp);
            engine.Apply("file-share", request, document);

            document.Title.ShouldBe(new[] { "Display Title a value" });
        }

        [Fact]
        public void Property_abcdef_equals_a_value_adds_keywords()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "mime-app-s63", GetMimeAppS63RuleJson());
            temp.WriteRuleFile("file-share", "prop-abcdef-keywords", GetPropAbcdefKeywordsRuleJson());

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequest("doc-2", new IngestionPropertyList
            {
                new IngestionProperty { Name = "abcdef", Type = IngestionPropertyType.String, Value = "a value" }
            }, new IngestionFileList());

            var document = CanonicalDocument.CreateMinimal("doc-2", "file-share", request.IndexItem!, request.IndexItem.Timestamp);
            engine.Apply("file-share", request, document);

            document.Keywords.ShouldContain("key1");
            document.Keywords.ShouldContain("key2");
        }
        private static ServiceProvider CreateProvider(string contentRootPath)
        {
            return IngestionRulesTestServiceProviderFactory.Create(contentRootPath);
        }

        private static IngestionRequest CreateRequest(string id, IngestionPropertyList properties, IngestionFileList files)
        {
            var indexRequest = new IndexRequest(id, properties, ["token"], DateTimeOffset.UtcNow, files);

            return new IngestionRequest(IngestionRequestType.IndexItem, indexRequest, null, null);
        }

        private static string GetMimeAppS63RuleJson()
        {
            return """
                   {
                     "schemaVersion": "1.0",
                     "rule": {
                       "id": "mime-app-s63",
                       "title": "Exchange Set $path:files[*].mimeType",
                       "description": "When any file is app/s63, enrich as exchange set",
                       "enabled": true,
                       "if": {
                         "files[*].mimeType": "app/s63"
                       },
                       "then": {
                         "keywords": { "add": ["exchange-set"] },
                         "searchText": { "add": ["exchange set", "exchangeset"] }
                       }
                     }
                   }
                   """;
        }

        private static string GetPropAbcdefKeywordsRuleJson()
        {
            return """
                   {
                     "schemaVersion": "1.0",
                     "rule": {
                       "id": "prop-abcdef-keywords",
                       "title": "Property abcdef keywords",
                       "description": "When properties.abcdef equals 'a value', add key1/key2",
                       "enabled": true,
                       "if": {
                         "properties[\"abcdef\"]": "a value"
                       },
                       "then": {
                         "keywords": { "add": ["key1", "key2"] }
                       }
                     }
                   }
                   """;
        }
    }
}