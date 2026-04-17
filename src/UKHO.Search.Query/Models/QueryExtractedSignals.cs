namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the normalized typed signal extraction output retained on the query plan.
    /// </summary>
    public sealed class QueryExtractedSignals
    {
        /// <summary>
        /// Gets the temporal signals recognized from the query text, including year values and richer date-like matches.
        /// </summary>
        public QueryTemporalSignals Temporal { get; init; } = new QueryTemporalSignals();

        /// <summary>
        /// Gets the numeric signals recognized from the query text.
        /// </summary>
        public IReadOnlyCollection<QueryNumericSignal> Numbers { get; init; } = Array.Empty<QueryNumericSignal>();

        /// <summary>
        /// Gets the concept signals derived by the query-rule engine.
        /// </summary>
        public IReadOnlyCollection<QueryConceptSignal> Concepts { get; init; } = Array.Empty<QueryConceptSignal>();

        /// <summary>
        /// Gets the sort-hint signals derived by the query-rule engine.
        /// </summary>
        public IReadOnlyCollection<QuerySortHintSignal> SortHints { get; init; } = Array.Empty<QuerySortHintSignal>();
    }
}
