using System.Text.Json;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class OperatorEvaluatorCasingMatrixTests
    {
        [Theory]
        [InlineData("foo", "FOO")]
        [InlineData("FOO", "foo")]
        [InlineData("FoO", "fOo")]
        public void Eq_matches_regardless_of_case(string ruleValue, string payloadValue)
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ruleValue));

            IngestionRulesOperatorEvaluator.Evaluate("eq", new[] { payloadValue }, doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBe(new[] { payloadValue });
        }

        [Theory]
        [InlineData("s63", "APP/S63")]
        [InlineData("S63", "app/s63")]
        [InlineData("S63", "ApP/S63")]
        public void Contains_matches_regardless_of_case(string ruleValue, string payloadValue)
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ruleValue));

            IngestionRulesOperatorEvaluator.Evaluate("contains", new[] { payloadValue }, doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBe(new[] { payloadValue });
        }

        [Theory]
        [InlineData("app/", "APP/S63")]
        [InlineData("APP/", "app/s63")]
        [InlineData("ApP/", "aPp/s63")]
        public void StartsWith_matches_regardless_of_case(string ruleValue, string payloadValue)
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ruleValue));

            IngestionRulesOperatorEvaluator.Evaluate("startsWith", new[] { payloadValue }, doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBe(new[] { payloadValue });
        }

        [Theory]
        [InlineData("plain", "text/plain")]
        [InlineData("PLAIN", "text/plain")]
        [InlineData("PlAiN", "TEXT/PLAIN")]
        public void EndsWith_matches_regardless_of_case(string ruleValue, string payloadValue)
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ruleValue));

            IngestionRulesOperatorEvaluator.Evaluate("endsWith", new[] { payloadValue }, doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBe(new[] { payloadValue });
        }

        [Theory]
        [InlineData("b", "B")]
        [InlineData("B", "b")]
        [InlineData("b", "b")]
        public void In_matches_regardless_of_case(string setValue, string payloadValue)
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new[] { setValue }));

            IngestionRulesOperatorEvaluator.Evaluate("in", new[] { payloadValue }, doc.RootElement, out var matched)
                                           .ShouldBeTrue();

            matched.ShouldBe(new[] { payloadValue });
        }

        [Theory]
        [InlineData("foo", "bar")]
        [InlineData("s63", "text/plain")]
        [InlineData("app/", "text/plain")]
        [InlineData("plain", "APP/S63")]
        public void Operators_do_not_match_when_value_is_different(string ruleValue, string payloadValue)
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ruleValue));

            IngestionRulesOperatorEvaluator.Evaluate("eq", new[] { payloadValue }, doc.RootElement, out _)
                                           .ShouldBeFalse();
        }
    }
}
