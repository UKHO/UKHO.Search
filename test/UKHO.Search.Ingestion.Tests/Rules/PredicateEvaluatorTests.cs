using System.Text.Json;
using Shouldly;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class PredicateEvaluatorTests
    {
        [Fact]
        public void Leaf_exists_matches_when_value_present()
        {
            var payload = CreateAddItem();
            var evaluator = CreateEvaluator();

            using var doc = JsonDocument.Parse("""{ "path": "id", "exists": true }""");

            evaluator.Evaluate("r1", doc.RootElement, payload).IsMatch.ShouldBeTrue();
        }

        [Fact]
        public void Leaf_startsWith_and_endsWith_match_any_wildcard_value()
        {
            var payload = CreateAddItem();
            var evaluator = CreateEvaluator();

            using var starts = JsonDocument.Parse("""{ "path": "files[*].mimeType", "startsWith": "app/" }""");
            evaluator.Evaluate("r1", starts.RootElement, payload).IsMatch.ShouldBeTrue();

            using var ends = JsonDocument.Parse("""{ "path": "files[*].mimeType", "endsWith": "plain" }""");
            evaluator.Evaluate("r1", ends.RootElement, payload).IsMatch.ShouldBeTrue();
        }

        [Fact]
        public void Leaf_eq_matches_case_insensitive()
        {
            var payload = CreateAddItem();
            var evaluator = CreateEvaluator();

            using var doc = JsonDocument.Parse("""{ "path": "properties.abcdef", "eq": "A VALUE" }""");

            evaluator.Evaluate("r1", doc.RootElement, payload).IsMatch.ShouldBeTrue();
        }

        [Fact]
        public void Leaf_contains_matches_any_wildcard_value()
        {
            var payload = CreateAddItem();
            var evaluator = CreateEvaluator();

            using var doc = JsonDocument.Parse("""{ "path": "files[*].mimeType", "contains": "s63" }""");

            evaluator.Evaluate("r1", doc.RootElement, payload).IsMatch.ShouldBeTrue();
        }

        [Fact]
        public void Leaf_in_matches_any_wildcard_value()
        {
            var payload = CreateAddItem();
            var evaluator = CreateEvaluator();

            using var doc = JsonDocument.Parse("""{ "path": "files[*].mimeType", "in": ["text/plain"] }""");

            evaluator.Evaluate("r1", doc.RootElement, payload).IsMatch.ShouldBeTrue();
        }

        [Fact]
        public void Boolean_any_matches_first_matching_child()
        {
            var payload = CreateAddItem();
            var evaluator = CreateEvaluator();

            using var doc = JsonDocument.Parse("""
            {
              "any": [
                { "path": "id", "eq": "nope" },
                { "path": "id", "eq": "doc-1" }
              ]
            }
            """);

            evaluator.Evaluate("r1", doc.RootElement, payload).IsMatch.ShouldBeTrue();
        }

        [Fact]
        public void Boolean_all_with_nested_not_matches()
        {
            var payload = CreateAddItem();
            var evaluator = CreateEvaluator();

            using var doc = JsonDocument.Parse("""
            {
              "all": [
                { "path": "id", "exists": true },
                { "not": { "path": "id", "eq": "nope" } }
              ]
            }
            """);

            evaluator.Evaluate("r1", doc.RootElement, payload).IsMatch.ShouldBeTrue();
        }

        [Fact]
        public void Shorthand_and_requires_all_conditions()
        {
            var payload = CreateAddItem();
            var evaluator = CreateEvaluator();

            using var doc = JsonDocument.Parse("""{ "id": "doc-1", "properties.abcdef": "a value" }""");
            evaluator.Evaluate("r1", doc.RootElement, payload).IsMatch.ShouldBeTrue();

            using var docFail = JsonDocument.Parse("""{ "id": "doc-1", "properties.abcdef": "nope" }""");
            evaluator.Evaluate("r1", docFail.RootElement, payload).IsMatch.ShouldBeFalse();
        }

        [Fact]
        public void Missing_runtime_path_does_not_throw_and_does_not_match()
        {
            var payload = CreateAddItem();
            var evaluator = CreateEvaluator();

            using var doc = JsonDocument.Parse("""{ "path": "does.not.exist", "exists": true }""");

            evaluator.Evaluate("r1", doc.RootElement, payload).IsMatch.ShouldBeFalse();
        }

        private static IngestionRulesPredicateEvaluator CreateEvaluator()
        {
            var resolver = new IngestionRulesPathResolver();
            return new IngestionRulesPredicateEvaluator(resolver);
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
