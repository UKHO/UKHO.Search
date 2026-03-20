using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RulesWorkbench.Services;
using RulesWorkbench.Tests.TestSupport;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace RulesWorkbench.Tests
{
    public sealed class RuleEvaluationServiceTests
    {
        [Fact]
        public async Task EvaluateFileShareAsync_uses_shared_engine_for_exists_false_rule_matching()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRuleFile("file-share", "bu-adds-s100-2-data-product-product-identifier", """
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
                      }
                    }
                  }
                }
                """);

            using var provider = RulesEngineTestServiceProviderFactory.Create(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();
            var catalog = provider.GetRequiredService<IIngestionRulesCatalog>();
            var service = new RuleEvaluationService(engine, catalog, NullLogger<RuleEvaluationService>.Instance);

            var result = await service.EvaluateFileShareAsync(CreateAddsS100RequestMissingProductCode(), CancellationToken.None);

            result.ValidationErrors.ShouldBeEmpty();
            result.MatchedRules.Select(x => x.RuleId)
                  .ShouldBe(new[] { "bu-adds-s100-2-data-product-product-identifier" });
            result.FinalDocumentJson.ShouldContain("\"title\": [");
            result.FinalDocumentJson.ShouldContain("ADDS-S100 data product 101GB005TST23");
            result.FinalDocumentJson.ShouldContain("\"category\": [");
            result.FinalDocumentJson.ShouldContain("data product");
            result.FinalDocumentJson.ShouldContain("\"series\": [");
            result.FinalDocumentJson.ShouldContain("\"instance\": [");
        }

        private static IndexRequest CreateAddsS100RequestMissingProductCode()
        {
            return new IndexRequest("doc-1", [
                new IngestionProperty { Name = "edition number", Type = IngestionPropertyType.String, Value = "0" },
                new IngestionProperty { Name = "producing agency", Type = IngestionPropertyType.String, Value = "GB00" },
                new IngestionProperty { Name = "product identifier", Type = IngestionPropertyType.String, Value = "S-101" },
                new IngestionProperty { Name = "product name", Type = IngestionPropertyType.String, Value = "101GB005TST23" },
                new IngestionProperty { Name = "product type", Type = IngestionPropertyType.String, Value = "S-100" },
                new IngestionProperty { Name = "update number", Type = IngestionPropertyType.String, Value = "5" },
                new IngestionProperty { Name = "businessunitname", Type = IngestionPropertyType.String, Value = "ADDS-S100" }
            ], ["public"], DateTimeOffset.UtcNow, new IngestionFileList());
        }
    }
}
