namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the temporal signals recognized from normalized query text.
    /// </summary>
    public sealed class QueryTemporalSignals
    {
        /// <summary>
        /// Gets the four-digit year values recognized from the query text.
        /// </summary>
        public IReadOnlyCollection<int> Years { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Gets the non-year temporal matches recognized from the query text.
        /// </summary>
        public IReadOnlyCollection<QueryTemporalDateSignal> Dates { get; init; } = Array.Empty<QueryTemporalDateSignal>();
    }
}
