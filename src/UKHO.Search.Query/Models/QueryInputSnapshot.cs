namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the immutable normalized snapshot of a user query as it enters query planning.
    /// </summary>
    public sealed class QueryInputSnapshot
    {
        /// <summary>
        /// Gets the raw query text exactly as the caller supplied it.
        /// </summary>
        public string RawText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the lower-cased form of the raw query text before whitespace cleanup is applied.
        /// </summary>
        public string NormalizedText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the cleaned query text after trimming and repeated-whitespace collapse have been applied.
        /// </summary>
        public string CleanedText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the tokenized cleaned query text in deterministic order.
        /// </summary>
        public IReadOnlyCollection<string> Tokens { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the residual tokens that remain available for default matching after typed extraction and rules have run.
        /// </summary>
        public IReadOnlyCollection<string> ResidualTokens { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the residual cleaned text that remains available for default analyzed matching after typed extraction and rules have run.
        /// </summary>
        public string ResidualText { get; init; } = string.Empty;
    }
}
