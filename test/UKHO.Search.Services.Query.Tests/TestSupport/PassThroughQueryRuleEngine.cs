using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Tests.TestSupport
{
    /// <summary>
    /// Provides a deterministic no-op rule engine for service-layer tests.
    /// </summary>
    internal sealed class PassThroughQueryRuleEngine : IQueryRuleEngine
    {
        /// <summary>
        /// Returns the default rule-evaluation result for the supplied input snapshot and canonical model.
        /// </summary>
        /// <param name="input">The normalized input snapshot supplied by the planner under test.</param>
        /// <param name="extracted">The typed extraction output supplied by the planner under test.</param>
        /// <param name="model">The canonical model supplied by the planner under test.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the test.</param>
        /// <returns>The default rule-evaluation result.</returns>
        public Task<QueryRuleEvaluationResult> EvaluateAsync(QueryInputSnapshot input, QueryExtractedSignals extracted, CanonicalQueryModel model, CancellationToken cancellationToken)
        {
            // Validate the test inputs so service-layer tests fail clearly if the planner passes broken state.
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(extracted);
            ArgumentNullException.ThrowIfNull(model);

            // Return the default residual-preserving result because work item one has not introduced rule loading yet.
            return Task.FromResult(QueryRuleEvaluationResult.CreateDefault(input, extracted, model));
        }
    }
}
