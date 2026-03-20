using System.Text.Json;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class OperatorEvaluatorTests
    {
        [Fact]
        public void Eq_is_case_insensitive_and_trimmed()
        {
            using var doc = JsonDocument.Parse("\"foo\"");

            IngestionRulesOperatorEvaluator.Evaluate("eq", new[] { " Foo ", "bar" }, doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBe(new[] { " Foo " });
        }

        [Fact]
        public void Contains_is_case_insensitive_and_trimmed()
        {
            using var doc = JsonDocument.Parse("\"s63\"");

            IngestionRulesOperatorEvaluator.Evaluate("contains", new[] { "APP/S63", "text/plain" }, doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBe(new[] { "APP/S63" });
        }

        [Fact]
        public void StartsWith_and_endsWith_are_case_insensitive()
        {
            using var starts = JsonDocument.Parse("\"app/\"");
            using var ends = JsonDocument.Parse("\"plain\"");

            IngestionRulesOperatorEvaluator.Evaluate("startsWith", new[] { " APP/S63 ", "text/plain" }, starts.RootElement, out var startsMatched)
                                           .ShouldBeTrue();
            startsMatched.ShouldBe(new[] { " APP/S63 " });

            IngestionRulesOperatorEvaluator.Evaluate("endsWith", new[] { "APP/S63", "text/plain" }, ends.RootElement, out var endsMatched)
                                           .ShouldBeTrue();
            endsMatched.ShouldBe(new[] { "text/plain" });
        }

        [Fact]
        public void In_matches_any_value_in_set()
        {
            using var doc = JsonDocument.Parse("[\"a\", \"b\"]");

            IngestionRulesOperatorEvaluator.Evaluate("in", new[] { "c", " B " }, doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBe(new[] { " B " });
        }

        [Fact]
        public void Exists_matches_any_non_empty_value()
        {
            using var doc = JsonDocument.Parse("true");

            IngestionRulesOperatorEvaluator.Evaluate("exists", new[] { "", "  ", "x" }, doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBe(new[] { "x" });
        }

        [Fact]
        public void Exists_true_does_not_match_when_values_are_missing()
        {
            using var doc = JsonDocument.Parse("true");

            IngestionRulesOperatorEvaluator.Evaluate("exists", Array.Empty<string>(), doc.RootElement, out var matched)
                                           .ShouldBeFalse();

            matched.ShouldBeEmpty();
        }

        [Fact]
        public void Exists_true_does_not_match_when_values_are_only_whitespace()
        {
            using var doc = JsonDocument.Parse("true");

            IngestionRulesOperatorEvaluator.Evaluate("exists", new[] { string.Empty, "  " }, doc.RootElement, out var matched)
                                           .ShouldBeFalse();

            matched.ShouldBeEmpty();
        }

        [Fact]
        public void Exists_false_matches_when_values_are_missing()
        {
            using var doc = JsonDocument.Parse("false");

            IngestionRulesOperatorEvaluator.Evaluate("exists", Array.Empty<string>(), doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBeEmpty();
        }

        [Fact]
        public void Exists_false_matches_when_values_are_only_whitespace()
        {
            using var doc = JsonDocument.Parse("false");

            IngestionRulesOperatorEvaluator.Evaluate("exists", new[] { string.Empty, "  " }, doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBeEmpty();
        }

        [Fact]
        public void Exists_false_does_not_match_when_non_empty_value_is_present()
        {
            using var doc = JsonDocument.Parse("false");

            IngestionRulesOperatorEvaluator.Evaluate("exists", new[] { string.Empty, "x" }, doc.RootElement, out var matched)
                                           .ShouldBeFalse();

            matched.ShouldBeEmpty();
        }
    }
}