namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one temporal match recognized from the query text that is richer than a simple year value.
    /// </summary>
    public sealed class QueryTemporalDateSignal
    {
        /// <summary>
        /// Gets the original query text fragment that matched the temporal recognizer.
        /// </summary>
        public string MatchedText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the recognizer category that explains what kind of temporal match was found.
        /// </summary>
        public string Kind { get; init; } = string.Empty;
    }
}
