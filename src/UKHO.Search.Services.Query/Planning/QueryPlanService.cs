using Microsoft.Extensions.Logging;
using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;
using UKHO.Search.Services.Query.Abstractions;
using UKHO.Search.Services.Query.Normalization;

namespace UKHO.Search.Services.Query.Planning
{
    /// <summary>
    /// Produces repository-owned query plans from raw user query text.
    /// </summary>
    public sealed class QueryPlanService : IQueryPlanService
    {
        private readonly IQueryTextNormalizer _normalizer;
        private readonly ITypedQuerySignalExtractor _typedQuerySignalExtractor;
        private readonly IQueryRuleEngine _queryRuleEngine;
        private readonly ILogger<QueryPlanService> _logger;

        /// <summary>
        /// Initializes the query planning service with the collaborators needed for normalization, typed extraction, and rule evaluation.
        /// </summary>
        /// <param name="normalizer">The normalizer that converts raw query text into a deterministic planning snapshot.</param>
        /// <param name="typedQuerySignalExtractor">The typed signal extractor that can add recognizer-derived signals to the plan.</param>
        /// <param name="queryRuleEngine">The rule engine that can mutate the canonical query model and consume residual content.</param>
        /// <param name="logger">The logger used to emit structured planning diagnostics and failures.</param>
        public QueryPlanService(IQueryTextNormalizer normalizer, ITypedQuerySignalExtractor typedQuerySignalExtractor, IQueryRuleEngine queryRuleEngine, ILogger<QueryPlanService> logger)
        {
            // Capture the injected collaborators once so each planning request runs through the same repository-owned pipeline.
            _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
            _typedQuerySignalExtractor = typedQuerySignalExtractor ?? throw new ArgumentNullException(nameof(typedQuerySignalExtractor));
            _queryRuleEngine = queryRuleEngine ?? throw new ArgumentNullException(nameof(queryRuleEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Produces a repository-owned query plan from the supplied raw query text.
        /// </summary>
        /// <param name="queryText">The raw query text submitted by the caller.</param>
        /// <param name="cancellationToken">The cancellation token that stops planning when the caller no longer needs the result.</param>
        /// <returns>The repository-owned query plan that should later be executed.</returns>
        public async Task<QueryPlan> PlanAsync(string? queryText, CancellationToken cancellationToken)
        {
            try
            {
                // Normalize first so every downstream stage receives the same cleaned, tokenized view of the query.
                var input = _normalizer.Normalize(queryText);
                _logger.LogInformation(
                    "Normalized query text for planning. RawLength={RawLength} CleanedLength={CleanedLength} TokenCount={TokenCount}",
                    input.RawText.Length,
                    input.CleanedText.Length,
                    input.Tokens.Count);

                // Run typed extraction before rules so recognizer-backed signals are available both to the canonical model seed and to later rule evaluation.
                var extracted = await ExtractSignalsSafelyAsync(input, cancellationToken)
                    .ConfigureAwait(false);

                // Seed the canonical query model from the extracted signals so recognized years already appear on the inward query contract.
                var model = CreateSeedModel(extracted);
                var ruleEvaluation = await _queryRuleEngine.EvaluateAsync(input, extracted, model, cancellationToken)
                    .ConfigureAwait(false);

                // Build the slice-one default contributions from whatever residual content remains after rule evaluation.
                var defaults = CreateDefaultContributions(ruleEvaluation.ResidualText, ruleEvaluation.ResidualTokens);

                var plan = new QueryPlan
                {
                    Input = input,
                    Extracted = ruleEvaluation.Extracted,
                    Model = ruleEvaluation.Model,
                    Defaults = defaults,
                    Execution = ruleEvaluation.Execution,
                    Diagnostics = ruleEvaluation.Diagnostics
                };

                _logger.LogInformation(
                    "Generated query plan. ResidualTextLength={ResidualTextLength} DefaultContributionCount={DefaultContributionCount} MatchedRuleCount={MatchedRuleCount}",
                    ruleEvaluation.ResidualText.Length,
                    defaults.Items.Count,
                    ruleEvaluation.Diagnostics.MatchedRuleIds.Count);

                return plan;
            }
            catch (Exception ex)
            {
                // Log the failure at the application-service boundary so host callers get useful diagnostics when planning breaks.
                _logger.LogError(ex, "Failed to generate a query plan for the supplied query text.");
                throw;
            }
        }

        /// <summary>
        /// Builds the slice-one default contributions from the residual query content.
        /// </summary>
        /// <param name="residualText">The residual cleaned text that remains available for analyzed matching.</param>
        /// <param name="residualTokens">The residual cleaned tokens that remain available for keyword matching.</param>
        /// <returns>The default query contributions derived from the residual content.</returns>
        internal static QueryDefaultContributions CreateDefaultContributions(string residualText, IReadOnlyCollection<string> residualTokens)
        {
            // Keep the residual token and text handling deterministic so the infrastructure mapper receives stable clause ordering.
            var items = new List<QueryDefaultFieldContribution>();

            if (residualTokens.Count > 0)
            {
                items.Add(new QueryDefaultFieldContribution
                {
                    FieldName = "keywords",
                    MatchingMode = QueryDefaultMatchingMode.ExactTerms,
                    Terms = residualTokens.ToArray(),
                    Boost = 1.0d
                });
            }

            if (!string.IsNullOrWhiteSpace(residualText))
            {
                items.Add(new QueryDefaultFieldContribution
                {
                    FieldName = "searchText",
                    MatchingMode = QueryDefaultMatchingMode.AnalyzedText,
                    Text = residualText,
                    Boost = 2.0d
                });

                items.Add(new QueryDefaultFieldContribution
                {
                    FieldName = "content",
                    MatchingMode = QueryDefaultMatchingMode.AnalyzedText,
                    Text = residualText,
                    Boost = 1.0d
                });
            }

            return new QueryDefaultContributions
            {
                Items = items
            };
        }

        /// <summary>
        /// Runs the typed extractor and degrades safely when recognizer-backed extraction fails.
        /// </summary>
        /// <param name="input">The normalized query input snapshot that should be inspected for typed signals.</param>
        /// <param name="cancellationToken">The cancellation token that stops extraction when the caller no longer needs the result.</param>
        /// <returns>The extracted typed signals, or an empty result when extraction fails.</returns>
        private async Task<QueryExtractedSignals> ExtractSignalsSafelyAsync(QueryInputSnapshot input, CancellationToken cancellationToken)
        {
            // Let cancellation surface normally so callers can stop planning without turning it into a silent fallback.
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Execute the recognizer-backed adapter and log the resulting shape so manual verification can trace what the planner observed.
                var extracted = await _typedQuerySignalExtractor.ExtractAsync(input, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation(
                    "Extracted typed query signals. YearCount={YearCount} DateCount={DateCount} NumberCount={NumberCount}",
                    extracted.Temporal.Years.Count,
                    extracted.Temporal.Dates.Count,
                    extracted.Numbers.Count);

                return extracted;
            }
            catch (OperationCanceledException)
            {
                // Preserve normal cooperative cancellation semantics rather than hiding them behind an empty extraction result.
                throw;
            }
            catch (Exception ex)
            {
                // Fail soft here so query planning still produces a deterministic default-only plan when recognizer invocation breaks.
                _logger.LogError(ex, "Typed query signal extraction failed. The planner will continue with an empty extracted-signal contract.");
                return new QueryExtractedSignals();
            }
        }

        /// <summary>
        /// Creates the initial canonical query model from typed extraction output before rules run.
        /// </summary>
        /// <param name="extracted">The extracted signals that should seed the canonical query model.</param>
        /// <returns>The initial canonical query model for the current planning request.</returns>
        private static CanonicalQueryModel CreateSeedModel(QueryExtractedSignals extracted)
        {
            ArgumentNullException.ThrowIfNull(extracted);

            // Project recognized years into majorVersion now so later slices can treat typed extraction as first-class canonical query intent.
            return new CanonicalQueryModel
            {
                MajorVersion = extracted.Temporal.Years
                    .Distinct()
                    .OrderBy(static year => year)
                    .ToArray()
            };
        }
    }
}
