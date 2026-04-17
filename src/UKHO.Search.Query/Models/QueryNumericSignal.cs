namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one numeric signal recognized from normalized query text.
    /// </summary>
    public sealed class QueryNumericSignal
    {
        /// <summary>
        /// Gets the original query text fragment that matched the numeric recognizer.
        /// </summary>
        public string MatchedText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the repository-owned normalized numeric value retained on the query plan.
        /// </summary>
        public string NormalizedValue { get; init; } = string.Empty;
    }
}
