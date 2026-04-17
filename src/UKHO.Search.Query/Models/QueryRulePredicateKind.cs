namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Identifies the predicate behavior used by one validated query-rule condition.
    /// </summary>
    public enum QueryRulePredicateKind
    {
        /// <summary>
        /// Compares the resolved values at a path against one expected value.
        /// </summary>
        Equals = 0,

        /// <summary>
        /// Checks whether a cleaned text path contains a whole phrase.
        /// </summary>
        ContainsPhrase = 1,

        /// <summary>
        /// Matches when any child predicate matches.
        /// </summary>
        Any = 2
    }
}
