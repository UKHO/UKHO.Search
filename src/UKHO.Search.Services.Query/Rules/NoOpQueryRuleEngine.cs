using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Rules
{
    /// <summary>
    /// Provides the slice-one rule-engine implementation that leaves the canonical model and residual content unchanged.
    /// </summary>
    public sealed class NoOpQueryRuleEngine : IQueryRuleEngine
    {
        /// <summary>
        /// Returns the default no-op rule result for the supplied normalized input and canonical query model.
        /// </summary>
        /// <param name="input">The normalized query input snapshot that would normally be evaluated by rules.</param>
        /// <param name="extracted">The typed extraction output that would normally be inspected by rules.</param>
        /// <param name="model">The canonical query model that should flow through unchanged.</param>
        /// <param name="cancellationToken">The cancellation token that stops evaluation when the caller no longer needs the result.</param>
        /// <returns>The default no-op rule-evaluation result.</returns>
        public Task<QueryRuleEvaluationResult> EvaluateAsync(QueryInputSnapshot input, QueryExtractedSignals extracted, CanonicalQueryModel model, CancellationToken cancellationToken)
        {
            // Validate the inbound contracts explicitly so the application service receives deterministic failures for broken wiring.
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(extracted);
            ArgumentNullException.ThrowIfNull(model);

            // Slice one keeps rule evaluation injectable while proving the end-to-end path before rule loading arrives.
            return Task.FromResult(QueryRuleEvaluationResult.CreateDefault(input, extracted, model));
        }
    }
}
