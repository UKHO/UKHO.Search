using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Tests.TestSupport;
using UKHO.Search.ProviderModel;
using UKHO.Search.ProviderModel.Injection;
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
                                    "title": "Rule 1 title",
                                    "if": { "id": "doc-1" },
                                    "then": {
                                      "keywords": { "add": [ "Key1", "S-100", "$nope" ] },
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
                                    "title": "Rule 2 title",
                                    "if": { "id": "doc-1" },
                                    "then": {
                                      "searchText": { "add": [ "First", "Second" ] },
                                      "content": { "add": [ "C1", "SecondContent" ] }
                                    }
                                  }
                                }
                                """);

            temp.WriteRuleFile("file-share", "r3", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "r3",
                                    "title": "Rule 3 title",
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
                                    "title": "Rule 4 title",
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
                                    "title": "Rule 6 title",
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
                                    "title": "Other provider rule",
                                    "if": { "id": "doc-1" },
                                    "then": { "keywords": { "add": [ "should-not-apply" ] } }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequest();
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            engine.Apply("file-share", request, document);

            document.Keywords.ShouldContain("key1");
            document.Keywords.ShouldContain("s-100");
            document.Keywords.ShouldContain("s100");
            document.Keywords.ShouldContain("mime-app/s63");
            document.Keywords.ShouldContain("all-app/s63");
            document.Keywords.ShouldContain("all-text/plain");
            document.Keywords.ShouldNotContain("$nope");
            document.Keywords.ShouldNotContain("should-not-apply");

            document.SearchText.ShouldBe("first second");
            document.Content.ShouldBe("c1 secondcontent");
        }

        [Fact]
        public void Matching_rules_lowercase_all_additive_string_fields_during_ingestion()
        {
            using var temp = new TempRulesRoot();

            temp.WriteRuleFile("file-share", "lowercase-all-fields", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "lowercase-all-fields",
                                    "title": "Lowercase all fields",
                                    "if": { "id": "doc-1" },
                                    "then": {
                                      "keywords": { "add": [ "S-100" ] },
                                      "searchText": { "add": [ "Mixed Search" ] },
                                      "content": { "add": [ "Mixed Content" ] },
                                      "authority": { "add": [ "UKHO" ] },
                                      "region": { "add": [ "Europe" ] },
                                      "format": { "add": [ "PDF" ] },
                                      "category": { "add": [ "Charts" ] },
                                      "series": { "add": [ "SeriesA" ] },
                                      "instance": { "add": [ "Instance1" ] }
                                    }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequest();
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            engine.Apply("file-share", request, document);

            document.Keywords.ShouldContain("s-100");
            document.Keywords.ShouldContain("s100");
            document.SearchText.ShouldBe("mixed search");
            document.Content.ShouldBe("mixed content");
            document.Authority.ShouldBe(new[] { "ukho" });
            document.Region.ShouldBe(new[] { "europe" });
            document.Format.ShouldBe(new[] { "pdf" });
            document.Category.ShouldBe(new[] { "charts" });
            document.Series.ShouldBe(new[] { "seriesa" });
            document.Instance.ShouldBe(new[] { "instance1" });
        }

        [Fact]
        public void Matching_rule_can_set_minorVersion_using_toInt_path_with_spaced_property_key()
        {
            using var temp = new TempRulesRoot();

            temp.WriteRuleFile("file-share", "minorversion", """
                                {
                                  "schemaVersion": "1.0",
                                  "rule": {
                                    "id": "minorversion",
                                    "title": "Minor version rule",
                                    "if": { "id": "doc-1" },
                                    "then": {
                                      "minorVersion": { "add": [ "toInt($path:properties[\"week number\"])" ] }
                                    }
                                  }
                                }
                                """);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequestWithWeekNumber();
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            engine.Apply("file-share", request, document);

            document.MinorVersion.ShouldBe(new[] { 10 });
        }

        [Fact]
        public void Repository_style_s100_rule_with_exists_false_matches_when_product_code_is_missing()
        {
            const string ruleJson = """
                                    {
                                      "schemaVersion": "1.0",
                                      "rule": {
                                        "id": "bu-adds-s100-2-data-product-product-identifier",
                                        "context": "adds-s100",
                                        "title": "ADDS-S100 data product $path:properties[\"product name\"]",
                                        "description": "ADDS-S100 data product using product identifier.",
                                        "enabled": true,
                                        "if": {
                                          "all": [
                                            {
                                              "path": "properties[\"businessunitname\"]",
                                              "eq": "adds-s100"
                                            },
                                            {
                                              "path": "properties[\"product type\"]",
                                              "exists": true
                                            },
                                            {
                                              "path": "properties[\"product code\"]",
                                              "exists": false
                                            },
                                            {
                                              "path": "properties[\"product name\"]",
                                              "exists": true
                                            }
                                          ]
                                        },
                                        "then": {
                                          "keywords": {
                                            "add": [
                                              "s100",
                                              "product"
                                            ]
                                          },
                                          "authority": {
                                            "add": [
                                              "$path:properties[\"producing agency\"]"
                                            ]
                                          },
                                          "majorVersion": {
                                            "add": [
                                              "toInt($path:properties[\"edition number\"])"
                                            ]
                                          },
                                          "minorVersion": {
                                            "add": [
                                              "toInt($path:properties[\"update number\"])"
                                            ]
                                          },
                                          "category": {
                                            "add": [
                                              "data product"
                                            ]
                                          },
                                          "series": {
                                            "add": [
                                              "s-100",
                                              "s100",
                                              "s-101",
                                              "s101"
                                            ]
                                          },
                                          "instance": {
                                            "add": [
                                              "$path:properties[\"product name\"]"
                                            ]
                                          },
                                          "searchText": {
                                            "add": [
                                              "s100 product $path:properties[\"product name\"] from $path:properties[\"producing agency\"] edition $path:properties[\"edition number\"] update $path:properties[\"update number\"]"
                                            ]
                                          }
                                        }
                                      }
                                    }
                                    """;

            var (document, report) = EvaluateSingleRule(ruleJson, CreateAddsS100RequestMissingProductCode());

            report.MatchedRules.Select(x => x.RuleId)
                  .ShouldBe(new[] { "bu-adds-s100-2-data-product-product-identifier" });
            document.Title.ShouldBe(new[] { "ADDS-S100 data product 101GB005TST23" });
            document.Keywords.ShouldContain("s100");
            document.Keywords.ShouldContain("product");
            document.Authority.ShouldBe(new[] { "gb00" });
            document.MajorVersion.ShouldBe(new[] { 0 });
            document.MinorVersion.ShouldBe(new[] { 5 });
            document.Category.ShouldBe(new[] { "data product" });
            document.Series.ShouldBe(new[] { "s-100", "s-101", "s100", "s101" });
            document.Instance.ShouldBe(new[] { "101gb005tst23" });
            document.SearchText.ShouldBe("s100 product 101gb005tst23 from gb00 edition 0 update 5");
        }

        [Fact]
        public void Repository_style_exists_false_rule_matches_same_as_explicit_not_exists_true()
        {
            const string existsFalseRuleJson = """
                                               {
                                                 "schemaVersion": "1.0",
                                                 "rule": {
                                                   "id": "bu-adds-3-s63-cell-source-no-agency",
                                                   "context": "adds",
                                                   "title": "ADDS S63 cell $path:properties[\"cellname\"] source $path:properties[\"source\"]",
                                                   "description": "ADDS S63 cell with source and no agency.",
                                                   "enabled": true,
                                                   "if": {
                                                     "all": [
                                                       {
                                                         "path": "properties[\"businessunitname\"]",
                                                         "eq": "adds"
                                                       },
                                                       {
                                                         "path": "properties[\"agency\"]",
                                                         "exists": false
                                                       },
                                                       {
                                                         "path": "properties[\"source\"]",
                                                         "exists": true
                                                       },
                                                       {
                                                         "path": "properties[\"traceid\"]",
                                                         "exists": true
                                                       }
                                                     ]
                                                   },
                                                   "then": {
                                                     "authority": { "add": [ "ukho" ] },
                                                     "category": { "add": [ "enc" ] },
                                                     "series": { "add": [ "s63" ] },
                                                     "instance": { "add": [ "$path:properties[\"cellname\"]" ] },
                                                     "searchText": { "add": [ "s63 enc cell $path:properties[\"cellname\"] source $path:properties[\"source\"] edition $path:properties[\"editionnumber\"] update $path:properties[\"updatenumber\"]" ] }
                                                   }
                                                 }
                                               }
                                               """;

            const string notExistsTrueRuleJson = """
                                                 {
                                                   "schemaVersion": "1.0",
                                                   "rule": {
                                                     "id": "bu-adds-3-s63-cell-source-no-agency-not",
                                                     "context": "adds",
                                                     "title": "ADDS S63 cell $path:properties[\"cellname\"] source $path:properties[\"source\"]",
                                                     "description": "ADDS S63 cell with source and no agency.",
                                                     "enabled": true,
                                                     "if": {
                                                       "all": [
                                                         {
                                                           "path": "properties[\"businessunitname\"]",
                                                           "eq": "adds"
                                                         },
                                                         {
                                                           "not": {
                                                             "path": "properties[\"agency\"]",
                                                             "exists": true
                                                           }
                                                         },
                                                         {
                                                           "path": "properties[\"source\"]",
                                                           "exists": true
                                                         },
                                                         {
                                                           "path": "properties[\"traceid\"]",
                                                           "exists": true
                                                         }
                                                       ]
                                                     },
                                                     "then": {
                                                       "authority": { "add": [ "ukho" ] },
                                                       "category": { "add": [ "enc" ] },
                                                       "series": { "add": [ "s63" ] },
                                                       "instance": { "add": [ "$path:properties[\"cellname\"]" ] },
                                                       "searchText": { "add": [ "s63 enc cell $path:properties[\"cellname\"] source $path:properties[\"source\"] edition $path:properties[\"editionnumber\"] update $path:properties[\"updatenumber\"]" ] }
                                                     }
                                                   }
                                                 }
                                                 """;

            var request = CreateAddsRequestWithSourceAndTraceIdButNoAgency();
            var (existsFalseDocument, existsFalseReport) = EvaluateSingleRule(existsFalseRuleJson, request);
            var (notExistsDocument, notExistsReport) = EvaluateSingleRule(notExistsTrueRuleJson, request);

            existsFalseReport.MatchedRules.Count.ShouldBe(1);
            notExistsReport.MatchedRules.Count.ShouldBe(1);
            existsFalseDocument.Title.ShouldBe(notExistsDocument.Title);
            existsFalseDocument.Authority.ShouldBe(notExistsDocument.Authority);
            existsFalseDocument.Category.ShouldBe(notExistsDocument.Category);
            existsFalseDocument.Series.ShouldBe(notExistsDocument.Series);
            existsFalseDocument.Instance.ShouldBe(notExistsDocument.Instance);
            existsFalseDocument.SearchText.ShouldBe(notExistsDocument.SearchText);
        }

        private static ServiceProvider CreateProvider(string contentRootPath)
        {
            return IngestionRulesTestServiceProviderFactory.Create(
                contentRootPath,
                configureServices: services => services.AddProviderDescriptor<OtherProviderRegistrationMarker>(
                    new ProviderDescriptor("other-provider", "Other Provider")));
        }

        private static (CanonicalDocument Document, IngestionRulesApplyReport Report) EvaluateSingleRule(string ruleJson, IngestionRequest request)
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "rule", ruleJson);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();
            var document = CanonicalDocument.CreateMinimal(request.IndexItem!.Id, "file-share", request.IndexItem, request.IndexItem.Timestamp);
            var report = engine.ApplyWithReport("file-share", request, document);

            return (document, report);
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

        private static IngestionRequest CreateRequestWithWeekNumber()
        {
            var indexRequest = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "week number", Type = IngestionPropertyType.String, Value = "10" }
            ], ["token"], DateTimeOffset.UtcNow, new IngestionFileList());

            return new IngestionRequest(IngestionRequestType.IndexItem, indexRequest, null, null);
        }

        private static IngestionRequest CreateAddsS100RequestMissingProductCode()
        {
            var indexRequest = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "edition number", Type = IngestionPropertyType.String, Value = "0" },
                new IngestionProperty { Name = "producing agency", Type = IngestionPropertyType.String, Value = "GB00" },
                new IngestionProperty { Name = "product identifier", Type = IngestionPropertyType.String, Value = "S-101" },
                new IngestionProperty { Name = "product name", Type = IngestionPropertyType.String, Value = "101GB005TST23" },
                new IngestionProperty { Name = "product type", Type = IngestionPropertyType.String, Value = "S-100" },
                new IngestionProperty { Name = "update number", Type = IngestionPropertyType.String, Value = "5" },
                new IngestionProperty { Name = "businessunitname", Type = IngestionPropertyType.String, Value = "ADDS-S100" }
            ], ["public"], DateTimeOffset.UtcNow, new IngestionFileList());

            return new IngestionRequest(IngestionRequestType.IndexItem, indexRequest, null, null);
        }

        private static IngestionRequest CreateAddsRequestWithSourceAndTraceIdButNoAgency()
        {
            var indexRequest = new IndexRequest("doc-1", [
                new IngestionProperty { Name = "businessunitname", Type = IngestionPropertyType.String, Value = "ADDS" },
                new IngestionProperty { Name = "cellname", Type = IngestionPropertyType.String, Value = "GB100001" },
                new IngestionProperty { Name = "source", Type = IngestionPropertyType.String, Value = "AVCS" },
                new IngestionProperty { Name = "traceid", Type = IngestionPropertyType.String, Value = "trace-1" },
                new IngestionProperty { Name = "editionnumber", Type = IngestionPropertyType.String, Value = "2" },
                new IngestionProperty { Name = "updatenumber", Type = IngestionPropertyType.String, Value = "7" }
            ], ["public"], DateTimeOffset.UtcNow, new IngestionFileList());

            return new IngestionRequest(IngestionRequestType.IndexItem, indexRequest, null, null);
        }
    }
}