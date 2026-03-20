using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class IngestionRulesApplyReportIntegrationTests
    {
        [Fact]
        public void ApplyWithReport_returns_matched_rules_in_execution_order()
        {
            using var temp = new TempRulesRoot();

            temp.WriteRuleFile("file-share", "r1", """
                                                {
                                                  "schemaVersion": "1.0",
                                                  "rule": {
                                                    "id": "r1",
                                                    "description": "First rule",
                                                    "if": { "id": "doc-1" },
                                                    "then": {
                                                      "category": { "add": [ "charts" ] }
                                                    }
                                                  }
                                                }
                                                """);

            temp.WriteRuleFile("file-share", "r2", """
                                                {
                                                  "schemaVersion": "1.0",
                                                  "rule": {
                                                    "id": "r2",
                                                    "description": "Second rule",
                                                    "if": { "id": "doc-1" },
                                                    "then": {
                                                      "series": { "add": [ "series-a" ] }
                                                    }
                                                  }
                                                }
                                                """);

            using var provider = IngestionRulesTestServiceProviderFactory.Create(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = CreateRequest();
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            var report = engine.ApplyWithReport("file-share", request, document);

            report.MatchedRules.Select(x => x.RuleId)
                .ShouldBe(new[] { "r1", "r2" });

            report.MatchedRules[0].Description.ShouldBe("First rule");
            report.MatchedRules[1].Description.ShouldBe("Second rule");
            report.MatchedRules.All(x => !string.IsNullOrWhiteSpace(x.Summary)).ShouldBeTrue();

            document.Category.ShouldContain("charts");
            document.Series.ShouldContain("series-a");
        }

        private static IngestionRequest CreateRequest()
        {
            var indexRequest = new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "token" }, DateTimeOffset.UtcNow, new IngestionFileList());
            return new IngestionRequest(IngestionRequestType.IndexItem, indexRequest, null, null);
        }
    }
}
