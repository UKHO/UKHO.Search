using System.Text.Json;

namespace UKHO.Search.Query.Results
{
    /// <summary>
    /// Represents one search hit returned from the query execution path.
    /// </summary>
    public sealed class QuerySearchHit
    {
        /// <summary>
        /// Gets the display title selected from the indexed canonical document.
        /// </summary>
        public required string Title { get; init; }

        /// <summary>
        /// Gets the best available type-like value projected for the query UI.
        /// </summary>
        public string? Type { get; init; }

        /// <summary>
        /// Gets the best available region value projected for the query UI.
        /// </summary>
        public string? Region { get; init; }

        /// <summary>
        /// Gets the named query clauses that matched this hit when Elasticsearch returned matched-query metadata.
        /// </summary>
        public IReadOnlyCollection<string> MatchedFields { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Gets the raw source payload for developer-facing inspection in the query UI.
        /// </summary>
        public JsonElement? Raw { get; init; }
    }
}
