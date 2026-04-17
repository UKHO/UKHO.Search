using UKHO.Search.Query.Models;

namespace UKHO.Search.Query.Abstractions
{
    /// <summary>
    /// Defines the abstraction that evaluates query rules against normalized input and typed signals.
    /// </summary>
    public interface IQueryRuleEngine
    {
        /// <summary>
        /// Evaluates query rules for the supplied normalized input and typed signals.
        /// </summary>
        /// <param name="input">The normalized query input snapshot that the rule engine should inspect.</param>
        /// <param name="extracted">The typed signals that were extracted from the input snapshot.</param>
        /// <param name="model">The canonical query model that the rule engine may shape or replace.</param>
        /// <param name="cancellationToken">The cancellation token that stops evaluation when the caller no longer needs the result.</param>
        /// <returns>The rule-evaluation result that should flow into default matching, including updated extracted signals, canonical model values, execution directives, and residual content.</returns>
        Task<QueryRuleEvaluationResult> EvaluateAsync(QueryInputSnapshot input, QueryExtractedSignals extracted, CanonicalQueryModel model, CancellationToken cancellationToken);
    }
}
