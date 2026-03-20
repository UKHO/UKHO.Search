using System.Text.Json.Serialization;
using UKHO.Search.Geo;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Query;

namespace UKHO.Search.Ingestion.Pipeline.Documents
{
    public sealed record CanonicalDocument
    {
        public string Id { get; init; } = string.Empty;

        public required string Provider { get; init; }

        public IndexRequest Source { get; init; } = new();

        public DateTimeOffset Timestamp { get; init; }

        [JsonInclude]
        public SortedSet<string> Keywords { get; private set; } = new(StringComparer.Ordinal);

        [JsonInclude]
        public SortedSet<string> Authority { get; private set; } = new(StringComparer.Ordinal);

        [JsonInclude]
        public SortedSet<string> Region { get; private set; } = new(StringComparer.Ordinal);

        [JsonInclude]
        public SortedSet<string> Format { get; private set; } = new(StringComparer.Ordinal);

        [JsonInclude]
        public SortedSet<int> MajorVersion { get; private set; } = new();

        [JsonInclude]
        public SortedSet<int> MinorVersion { get; private set; } = new();

        [JsonInclude]
        public SortedSet<string> Category { get; private set; } = new(StringComparer.Ordinal);

        [JsonInclude]
        public SortedSet<string> Series { get; private set; } = new(StringComparer.Ordinal);

        [JsonInclude]
        public SortedSet<string> Instance { get; private set; } = new(StringComparer.Ordinal);

        [JsonInclude]
        public SortedSet<string> Title { get; private set; } = new(StringComparer.Ordinal);

        [JsonInclude]
        public string SearchText { get; private set; } = string.Empty;

        [JsonInclude]
        public string Content { get; private set; } = string.Empty;

        [JsonInclude]
        public List<GeoPolygon> GeoPolygons { get; private set; } = new();

        public void AddKeyword(string? keyword)
        {
            var normalized = NormalizeStringValue(keyword);
            if (normalized is null)
            {
                return;
            }

            Keywords.Add(normalized);
        }

        public void AddAuthority(string? authority)
        {
            AddNormalizedStringValue(Authority, authority);
        }

        public void AddRegion(string? region)
        {
            AddNormalizedStringValue(Region, region);
        }

        public void AddFormat(string? format)
        {
            AddNormalizedStringValue(Format, format);
        }

        public void AddMajorVersion(int? majorVersion)
        {
            AddIntValue(MajorVersion, majorVersion);
        }

        public void AddMinorVersion(int? minorVersion)
        {
            AddIntValue(MinorVersion, minorVersion);
        }

        public void AddCategory(string? category)
        {
            AddNormalizedStringValue(Category, category);
        }

        public void AddSeries(string? series)
        {
            AddNormalizedStringValue(Series, series);
        }

        public void AddInstance(string? instance)
        {
            AddNormalizedStringValue(Instance, instance);
        }

        public void AddTitle(string? title)
        {
            var normalized = NormalizeTitleValue(title);
            if (normalized is null)
            {
                return;
            }

            Title.Add(normalized);
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

        public void AddTitles(IEnumerable<string?>? titles)
        {
            if (titles is null)
            {
                return;
            }

            foreach (var title in titles)
            {
                AddTitle(title);
            }
        }

        public void AddKeywordsFromTokens(string? tokens)
        {
            if (string.IsNullOrWhiteSpace(tokens))
            {
                return;
            }

            var tokenNormalizer = new TokenNormalizer();
            foreach (var token in SplitKeywordTokens(tokens))
            {
                foreach (var normalizedToken in tokenNormalizer.NormalizeToken(token))
                {
                    AddKeyword(normalizedToken);
                }
            }
        }

        public void AddSearchText(string? text)
        {
            var normalized = NormalizeStringValue(text);
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

        public void AddContent(string? text)
        {
            var normalized = NormalizeStringValue(text);
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

        public static CanonicalDocument CreateMinimal(string id, string provider, IndexRequest source, DateTimeOffset timestamp)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            ArgumentNullException.ThrowIfNull(source);

            return new CanonicalDocument
            {
                Id = id,
                Provider = provider.Trim(),
                Source = source,
                Timestamp = timestamp
            };
        }

        public void AddGeoPolygon(GeoPolygon polygon)
        {
            ArgumentNullException.ThrowIfNull(polygon);
            GeoPolygons.Add(polygon);
        }

        public void AddGeoPolygons(IEnumerable<GeoPolygon>? polygons)
        {
            if (polygons is null)
            {
                return;
            }

            foreach (var polygon in polygons)
            {
                AddGeoPolygon(polygon);
            }
        }

        private static IEnumerable<string> SplitKeywordTokens(string tokens)
        {
            var startIndex = -1;
            for (var index = 0; index < tokens.Length; index++)
            {
                if (IsKeywordTokenDelimiter(tokens[index]))
                {
                    if (startIndex >= 0)
                    {
                        yield return tokens[startIndex..index];
                        startIndex = -1;
                    }

                    continue;
                }

                if (startIndex < 0)
                {
                    startIndex = index;
                }
            }

            if (startIndex >= 0)
            {
                yield return tokens[startIndex..];
            }
        }

        private static bool IsKeywordTokenDelimiter(char value)
        {
            return char.IsWhiteSpace(value) || value is ',' or ';';
        }

        private static string? NormalizeStringValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim()
                        .ToLowerInvariant();
        }

        private static void AddNormalizedStringValue(SortedSet<string> target, string? value)
        {
            var normalized = NormalizeStringValue(value);
            if (normalized is null)
            {
                return;
            }

            target.Add(normalized);
        }

        private static string? NormalizeTitleValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private static void AddIntValue(SortedSet<int> target, int? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            target.Add(value.Value);
        }
    }
}