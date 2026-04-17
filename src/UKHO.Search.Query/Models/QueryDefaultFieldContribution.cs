namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents one default query contribution that should be mapped onto a canonical index field.
    /// </summary>
    public sealed class QueryDefaultFieldContribution
    {
        /// <summary>
        /// Gets the canonical index field name that the contribution targets.
        /// </summary>
        public required string FieldName { get; init; }

        /// <summary>
        /// Gets the matching mode that tells the infrastructure mapper how to translate this contribution.
        /// </summary>
        public required QueryDefaultMatchingMode MatchingMode { get; init; }

        /// <summary>
        /// Gets the analyzed text to submit when the contribution represents a text match.
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// Gets the exact keyword terms to submit when the contribution represents keyword matching.
        /// </summary>
        public IReadOnlyCollection<string> Terms { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the boost applied by the infrastructure mapper when this contribution participates in scoring.
        /// </summary>
        public double Boost { get; init; } = 1.0d;
    }
}
