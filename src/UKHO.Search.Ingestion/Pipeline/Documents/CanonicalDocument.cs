using System.Text.Json.Serialization;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Pipeline.Documents
{
    public sealed record CanonicalDocument
    {
        public string DocumentId { get; init; } = string.Empty;

        public string DocumentType { get; set; } = string.Empty;

        public IngestionRequest Source { get; init; } = new();

        [JsonInclude]
        public SortedSet<string> Keywords { get; private set; } = new(StringComparer.Ordinal);

        [JsonInclude]
        public string SearchText { get; private set; } = string.Empty;

        [JsonInclude]
        public string Content { get; private set; } = string.Empty;

        [JsonInclude]
        public SortedDictionary<string, SortedSet<string>> Facets { get; private set; } = new(StringComparer.Ordinal);

        public void AddKeyword(string? keyword)
        {
            var normalized = NormalizeToken(keyword);
            if (normalized is null)
            {
                return;
            }

            Keywords.Add(normalized);
        }

        public void SetKeyword(string? keyword)
        {
            AddKeyword(keyword);
        }

        public void AddKeywords(IEnumerable<string?>? keywords)
        {
            if (keywords is null)
            {
                return;
            }

            foreach (var keyword in keywords)
            {
                AddKeyword(keyword);
            }
        }

        public void SetKeywordsFromTokens(string? tokens)
        {
            Keywords.Clear();

            if (string.IsNullOrWhiteSpace(tokens))
            {
                return;
            }

            var split = tokens.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            AddKeywords(split);
        }

        public void SetSearchText(string? text)
        {
            var normalized = NormalizeToken(text);
            if (normalized is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                SearchText = normalized;
                return;
            }

            SearchText = string.Concat(SearchText, " ", normalized);
        }

        public void SetContent(string? text)
        {
            var normalized = NormalizeToken(text);
            if (normalized is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Content))
            {
                Content = normalized;
                return;
            }

            Content = string.Concat(Content, " ", normalized);
        }

        public void AddFacetValue(string? name, string? value)
        {
            var normalizedName = NormalizeToken(name);
            var normalizedValue = NormalizeToken(value);

            if (normalizedName is null || normalizedValue is null)
            {
                return;
            }

            if (!Facets.TryGetValue(normalizedName, out var values))
            {
                values = new SortedSet<string>(StringComparer.Ordinal);
                Facets.Add(normalizedName, values);
            }

            values.Add(normalizedValue);
        }

        public void AddFacetValues(string? name, IEnumerable<string?>? values)
        {
            var normalizedName = NormalizeToken(name);
            if (normalizedName is null)
            {
                return;
            }

            if (values is null)
            {
                return;
            }

            foreach (var value in values)
            {
                AddFacetValue(normalizedName, value);
            }
        }

        public static CanonicalDocument CreateMinimal(string documentId, IngestionRequest source)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
            ArgumentNullException.ThrowIfNull(source);

            return new CanonicalDocument
            {
                DocumentId = documentId,
                Source = source
            };
        }

        private static string? NormalizeToken(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim()
                        .ToLowerInvariant();
        }
    }
}