using System.Globalization;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using UKHO.Search.Infrastructure.Ingestion.Rules.Templating;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class ToIntOperatorTests
    {
        [Theory]
        [InlineData("toInt(0)", 0)]
        [InlineData("toInt(1)", 1)]
        [InlineData("toInt(-1)", -1)]
        [InlineData("toInt(+2)", 2)]
        public void ExpandToInt_WhenValidInteger_ReturnsSingleValue(string template, int expected)
        {
            var expander = new IngestionRulesTemplateExpander();
            var context = CreateContext(matchedValues: Array.Empty<string>());

            var result = expander.ExpandToInt(template, context);

            result.Count.ShouldBe(1);
            result[0].ShouldBe(expected);
        }

        [Fact]
        public void ExpandToInt_TrimsWhitespace()
        {
            var expander = new IngestionRulesTemplateExpander();
            var context = CreateContext(matchedValues: Array.Empty<string>());

            var result = expander.ExpandToInt("toInt(  42  )", context);

            result.Count.ShouldBe(1);
            result[0].ShouldBe(42);
        }

        [Theory]
        [InlineData("toInt()")]
        [InlineData("toInt(   )")]
        [InlineData("toInt(abc)")]
        [InlineData("toInt(1.2)")]
        [InlineData("toInt(1,000)")]
        public void ExpandToInt_WhenInvalid_ReturnsNoValues(string template)
        {
            var expander = new IngestionRulesTemplateExpander();
            var context = CreateContext(matchedValues: Array.Empty<string>());

            expander.ExpandToInt(template, context).Count.ShouldBe(0);
        }

        [Fact]
        public void ExpandToInt_WhenOverflow_ReturnsNoValues()
        {
            var expander = new IngestionRulesTemplateExpander();
            var context = CreateContext(matchedValues: Array.Empty<string>());

            expander.ExpandToInt("toInt(999999999999999999999)", context).Count.ShouldBe(0);
        }

        [Fact]
        public void ExpandToInt_UsesInvariantCulture()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

                var expander = new IngestionRulesTemplateExpander();
                var context = CreateContext(matchedValues: Array.Empty<string>());

                var result = expander.ExpandToInt("toInt( 10 )", context);

                result.Count.ShouldBe(1);
                result[0].ShouldBe(10);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void ExpandToInt_ResolvesVariablesFirst()
        {
            var expander = new IngestionRulesTemplateExpander();
            var context = CreateContext(matchedValues: new[] { "2", "not-a-number" });

            var result = expander.ExpandToInt("toInt($val)", context);

            result.ShouldBe(new[] { 2 });
        }

        private static TemplateContext CreateContext(IReadOnlyList<string> matchedValues)
        {
            var payload = new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t" }, DateTimeOffset.UnixEpoch, new IngestionFileList());
            var resolver = new IngestionRulesPathResolver();
            return new TemplateContext(payload, resolver, matchedValues);
        }
    }
}
