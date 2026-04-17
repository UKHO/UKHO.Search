using UKHO.Search.Query.Models;

namespace UKHO.Search.Services.Query.Normalization
{
    /// <summary>
    /// Normalizes raw query text into the deterministic representation consumed by the query planner.
    /// </summary>
    public sealed class QueryTextNormalizer : IQueryTextNormalizer
    {
        /// <summary>
        /// Normalizes the supplied raw query text.
        /// </summary>
        /// <param name="queryText">The raw query text submitted by the caller.</param>
        /// <returns>The normalized query snapshot used by planning.</returns>
        public QueryInputSnapshot Normalize(string? queryText)
        {
            // Preserve the caller-supplied text first so diagnostics can always refer back to the original request.
            var rawText = queryText ?? string.Empty;

            // Lowercase before further cleanup so tokenization and later matching remain deterministic and case-insensitive.
            var normalizedText = rawText.ToLowerInvariant();

            // Collapse repeated whitespace by splitting on all whitespace and joining with a single separator.
            var cleanedText = string.Join(" ", normalizedText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

            // Tokenize the cleaned query once so the planner and later stages can reuse the same stable token stream.
            var tokens = string.IsNullOrEmpty(cleanedText)
                ? Array.Empty<string>()
                : cleanedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // The first slice uses the cleaned query as the full residual surface because no typed extraction or rules consume content yet.
            return new QueryInputSnapshot
            {
                RawText = rawText,
                NormalizedText = normalizedText,
                CleanedText = cleanedText,
                Tokens = tokens,
                ResidualTokens = tokens.ToArray(),
                ResidualText = cleanedText
            };
        }
    }
}
