namespace UKHO.Search.Query.Models
{
    /// <summary>
    /// Represents the query-owned canonical model that mirrors the discovery-facing half of the indexed canonical document.
    /// </summary>
    public sealed class CanonicalQueryModel
    {
        /// <summary>
        /// Gets the exact-match keyword values that the query planner has derived for the canonical keyword field.
        /// </summary>
        public IReadOnlyCollection<string> Keywords { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the exact-match authority values derived for the canonical authority field.
        /// </summary>
        public IReadOnlyCollection<string> Authority { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the exact-match region values derived for the canonical region field.
        /// </summary>
        public IReadOnlyCollection<string> Region { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the exact-match format values derived for the canonical format field.
        /// </summary>
        public IReadOnlyCollection<string> Format { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the exact-match major-version values derived for the canonical major-version field.
        /// </summary>
        public IReadOnlyCollection<int> MajorVersion { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Gets the exact-match minor-version values derived for the canonical minor-version field.
        /// </summary>
        public IReadOnlyCollection<int> MinorVersion { get; init; } = Array.Empty<int>();

        /// <summary>
        /// Gets the exact-match category values derived for the canonical category field.
        /// </summary>
        public IReadOnlyCollection<string> Category { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the exact-match series values derived for the canonical series field.
        /// </summary>
        public IReadOnlyCollection<string> Series { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the exact-match instance values derived for the canonical instance field.
        /// </summary>
        public IReadOnlyCollection<string> Instance { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the analyzed search-text contribution owned by the canonical query model when rules or typed extraction explicitly target that field.
        /// </summary>
        public string SearchText { get; init; } = string.Empty;

        /// <summary>
        /// Gets the analyzed content contribution owned by the canonical query model when rules or typed extraction explicitly target that field.
        /// </summary>
        public string Content { get; init; } = string.Empty;

        /// <summary>
        /// Gets the optional title values derived for the canonical title field.
        /// </summary>
        public IReadOnlyCollection<string> Title { get; init; } = Array.Empty<string>();
    }
}
