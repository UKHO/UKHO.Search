namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one sort-intent signal derived during query planning.
    /// </summary>
    public sealed class QuerySortHintSignal
    {
        /// <summary>
        /// Gets the stable sort-hint identifier emitted by the rule engine.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets the query text that caused the sort intent to be recognized.
        /// </summary>
        public string MatchedText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the canonical fields that should be used when materializing the sort hint.
        /// </summary>
        public IReadOnlyCollection<string> Fields { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the textual sort order retained on the extracted signal contract.
        /// </summary>
        public string Order { get; init; } = "desc";
    }
}
