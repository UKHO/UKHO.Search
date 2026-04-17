using Shouldly;
using UKHO.Search.Query.Models;
using Xunit;

namespace UKHO.Search.Query.Tests
{
    /// <summary>
    /// Verifies the repository-owned query plan contracts introduced for the query-side planning pipeline.
    /// </summary>
    public sealed class QueryPlanContractTests
    {
        /// <summary>
        /// Verifies that the canonical query model defaults to empty query-owned discovery values rather than null collections.
        /// </summary>
        [Fact]
        public void CanonicalQueryModel_defaults_to_empty_query_owned_values()
        {
            // Construct the canonical query model directly because the contract itself must guarantee non-null defaults.
            var model = new CanonicalQueryModel();

            model.Keywords.ShouldBeEmpty();
            model.Authority.ShouldBeEmpty();
            model.Region.ShouldBeEmpty();
            model.Format.ShouldBeEmpty();
            model.MajorVersion.ShouldBeEmpty();
            model.MinorVersion.ShouldBeEmpty();
            model.Category.ShouldBeEmpty();
            model.Series.ShouldBeEmpty();
            model.Instance.ShouldBeEmpty();
            model.Title.ShouldBeEmpty();
            model.SearchText.ShouldBeEmpty();
            model.Content.ShouldBeEmpty();
        }

        /// <summary>
        /// Verifies that the default rule-evaluation result preserves the residual input surface and emits empty execution diagnostics.
        /// </summary>
        [Fact]
        public void QueryRuleEvaluationResult_CreateDefault_preserves_residual_input_and_empty_diagnostics()
        {
            // Build the normalized input snapshot explicitly so the factory can be verified without involving the services layer.
            var input = new QueryInputSnapshot
            {
                RawText = "Latest Notices",
                NormalizedText = "latest notices",
                CleanedText = "latest notices",
                Tokens = ["latest", "notices"],
                ResidualTokens = ["latest", "notices"],
                ResidualText = "latest notices"
            };

            var extracted = new QueryExtractedSignals();
            var model = new CanonicalQueryModel();

            // Create the default rule-evaluation result because slice one wires a no-op rule engine.
            var result = QueryRuleEvaluationResult.CreateDefault(input, extracted, model);

            result.Extracted.ShouldBe(extracted);
            result.Model.ShouldBe(model);
            result.ResidualText.ShouldBe("latest notices");
            result.ResidualTokens.ShouldBe(["latest", "notices"]);
            result.Execution.Sorts.ShouldBeEmpty();
            result.Diagnostics.MatchedRuleIds.ShouldBeEmpty();
        }

        /// <summary>
        /// Verifies that the extracted-signal contract defaults to non-null temporal and numeric collections.
        /// </summary>
        [Fact]
        public void QueryExtractedSignals_defaults_to_empty_temporal_and_numeric_values()
        {
            // Construct the extracted-signal contract directly because its defaults must be safe before any recognizer adapter runs.
            var extracted = new QueryExtractedSignals();

            extracted.Temporal.Years.ShouldBeEmpty();
            extracted.Temporal.Dates.ShouldBeEmpty();
            extracted.Numbers.ShouldBeEmpty();
            extracted.Concepts.ShouldBeEmpty();
            extracted.SortHints.ShouldBeEmpty();
        }
    }
}
