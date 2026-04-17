namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the output produced by the query rule engine before default matching is applied.
    /// </summary>
    public sealed class QueryRuleEvaluationResult
    {
        /// <summary>
        /// Gets the extracted-signal contract after the rule engine has appended rule-derived concepts and sort hints.
        /// </summary>
        public required QueryExtractedSignals Extracted { get; init; }

        /// <summary>
        /// Gets the canonical query model after rules have had a chance to shape it.
        /// </summary>
        public required CanonicalQueryModel Model { get; init; }

        /// <summary>
        /// Gets the residual text that remains available for default analyzed matching after rule consumption.
        /// </summary>
        public string ResidualText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the residual tokens that remain available for default keyword matching after rule consumption.
        /// </summary>
        public IReadOnlyCollection<string> ResidualTokens { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the execution-time directives emitted by the rule engine.
        /// </summary>
        public required QueryExecutionDirectives Execution { get; init; }

        /// <summary>
        /// Gets the diagnostics emitted by the rule engine.
        /// </summary>
        public required QueryPlanDiagnostics Diagnostics { get; init; }

        /// <summary>
        /// Creates the default no-op rule-evaluation result that preserves the current residual content.
        /// </summary>
        /// <param name="input">The normalized query input that still owns the residual content.</param>
        /// <param name="extracted">The extracted-signal contract that should flow through unchanged.</param>
        /// <param name="model">The canonical query model that should flow through unchanged.</param>
        /// <returns>The default rule-evaluation result for a no-op rule engine.</returns>
        public static QueryRuleEvaluationResult CreateDefault(QueryInputSnapshot input, QueryExtractedSignals extracted, CanonicalQueryModel model)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(extracted);
            ArgumentNullException.ThrowIfNull(model);

            return new QueryRuleEvaluationResult
            {
                Extracted = extracted,
                Model = model,
                ResidualText = input.ResidualText,
                ResidualTokens = input.ResidualTokens.ToArray(),
                Execution = new QueryExecutionDirectives(),
                Diagnostics = new QueryPlanDiagnostics()
            };
        }
    }
}
