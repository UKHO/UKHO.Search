namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the complete query plan that moves from normalization through execution mapping.
    /// </summary>
    public sealed class QueryPlan
    {
        /// <summary>
        /// Gets the normalized input snapshot retained for deterministic planning and diagnostics.
        /// </summary>
        public required QueryInputSnapshot Input { get; init; }

        /// <summary>
        /// Gets the typed extraction output retained by the planner.
        /// </summary>
        public required QueryExtractedSignals Extracted { get; init; }

        /// <summary>
        /// Gets the query-owned canonical model that mirrors the discovery-facing canonical index fields.
        /// </summary>
        public required CanonicalQueryModel Model { get; init; }

        /// <summary>
        /// Gets the default contributions derived from the residual query content.
        /// </summary>
        public required QueryDefaultContributions Defaults { get; init; }

        /// <summary>
        /// Gets the execution-time directives that accompany the canonical query model.
        /// </summary>
        public required QueryExecutionDirectives Execution { get; init; }

        /// <summary>
        /// Gets the developer-facing diagnostics emitted while planning the query.
        /// </summary>
        public required QueryPlanDiagnostics Diagnostics { get; init; }
    }
}
