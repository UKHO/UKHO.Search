using Shouldly;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using UKHO.Search.Infrastructure.Ingestion.Rules.Templating;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class TemplateExpanderTests
    {
        [Fact]
        public void Val_variable_expands_to_all_values()
        {
            var payload = CreateAddItem();
            var resolver = new IngestionRulesPathResolver();
            var expander = new IngestionRulesTemplateExpander();
            var context = new TemplateContext(payload, resolver, ["a", "b"]);

            expander.Expand("$val", context).ShouldBe(new[] { "a", "b" });
            expander.Expand("facet-$val", context).ShouldBe(new[] { "facet-a", "facet-b" });
        }

        [Fact]
        public void Path_variable_expands_to_resolved_values()
        {
            var payload = CreateAddItem();
            var resolver = new IngestionRulesPathResolver();
            var expander = new IngestionRulesTemplateExpander();
            var context = new TemplateContext(payload, resolver, ["ignored"]);

            expander.Expand("$path:id", context).ShouldBe(new[] { "doc-1" });
            expander.Expand("mime-$path:files[*].mimeType", context).ShouldBe(new[] { "mime-app/s63", "mime-text/plain" });
        }

        [Fact]
        public void Unknown_or_empty_variables_produce_no_values()
        {
            var payload = CreateAddItem();
            var resolver = new IngestionRulesPathResolver();
            var expander = new IngestionRulesTemplateExpander();

            expander.Expand("$nope", new TemplateContext(payload, resolver, ["a"])).ShouldBeEmpty();
            expander.Expand("$val", new TemplateContext(payload, resolver, Array.Empty<string>())).ShouldBeEmpty();
        }

        private static AddItemRequest CreateAddItem()
        {
            return new AddItemRequest(
                id: "doc-1",
                properties:
                [
                    new IngestionProperty { Name = "abcdef", Type = IngestionPropertyType.String, Value = "a value" }
                ],
                securityTokens: ["token"],
                timestamp: DateTimeOffset.UtcNow,
                files: new IngestionFileList
                {
                    new IngestionFile("f1", 1, DateTimeOffset.UtcNow, "app/s63"),
                    new IngestionFile("f2", 1, DateTimeOffset.UtcNow, "text/plain")
                });
        }
    }
}
