namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one concept signal derived during query planning.
    /// </summary>
    public sealed class QueryConceptSignal
    {
        /// <summary>
        /// Gets the stable concept identifier emitted by the rule engine.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the query text that caused the concept to be recognized.
        /// </summary>
        public string MatchedText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the keyword expansions contributed by the recognized concept.
        /// </summary>
        public IReadOnlyCollection<string> KeywordExpansions { get; init; } = Array.Empty<string>();
    }
}
