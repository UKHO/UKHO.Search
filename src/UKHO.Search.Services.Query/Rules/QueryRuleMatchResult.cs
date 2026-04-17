namespace UKHO.Search.Services.Query.Rules
{
    /// <summary>
    /// Represents the outcome of evaluating one validated query-rule predicate.
    /// </summary>
    internal sealed class QueryRuleMatchResult
    {
        /// <summary>
        /// Gets a value indicating whether the predicate matched.
        /// </summary>
        public bool IsMatch { get; init; }

        /// <summary>
        /// Gets the specific value that satisfied the predicate when the predicate matched.
        /// </summary>
        public string MatchedValue { get; init; } = string.Empty;

        /// <summary>
        /// Creates a successful predicate match result.
        /// </summary>
        /// <param name="matchedValue">The specific value that satisfied the predicate.</param>
        /// <returns>The successful predicate match result.</returns>
        public static QueryRuleMatchResult Match(string matchedValue)
        {
            // Return one compact result object so predicate evaluators can pass both match state and the matched value together.
            return new QueryRuleMatchResult
            {
                IsMatch = true,
                MatchedValue = matchedValue
            };
        }

        /// <summary>
        /// Creates a no-match predicate result.
        /// </summary>
        /// <returns>The no-match predicate result.</returns>
        public static QueryRuleMatchResult NoMatch()
        {
            // Return the shared no-match shape so callers can branch without null checks.
            return new QueryRuleMatchResult();
        }
    }
}
