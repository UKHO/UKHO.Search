using System.Text.Json.Serialization;
using UKHO.Search.Geo;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Pipeline.Documents
{
    public sealed record CanonicalDocument
    {
        public string Id { get; init; } = string.Empty;

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
        public string SearchText { get; private set; } = string.Empty;

        [JsonInclude]
        public string Content { get; private set; } = string.Empty;

        [JsonInclude]
        public List<GeoPolygon> GeoPolygons { get; private set; } = new();

        public void AddKeyword(string? keyword)
        {
            var normalized = NormalizeToken(keyword);
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

        public void SetAuthority(string? authority)
        {
            AddAuthority(authority);
        }

        public void AddRegion(string? region)
        {
            AddNormalizedStringValue(Region, region);
        }

        public void SetRegion(string? region)
        {
            AddRegion(region);
        }

        public void AddFormat(string? format)
        {
            AddNormalizedStringValue(Format, format);
        }

        public void SetFormat(string? format)
        {
            AddFormat(format);
        }

        public void AddMajorVersion(int? majorVersion)
        {
            AddIntValue(MajorVersion, majorVersion);
        }

        public void SetMajorVersion(int? majorVersion)
        {
            AddMajorVersion(majorVersion);
        }

        public void AddMinorVersion(int? minorVersion)
        {
            AddIntValue(MinorVersion, minorVersion);
        }

        public void SetMinorVersion(int? minorVersion)
        {
            AddMinorVersion(minorVersion);
        }

        public void AddCategory(string? category)
        {
            AddNormalizedStringValue(Category, category);
        }

        public void SetCategory(string? category)
        {
            AddCategory(category);
        }

        public void AddSeries(string? series)
        {
            AddNormalizedStringValue(Series, series);
        }

        public void SetSeries(string? series)
        {
            AddSeries(series);
        }

        public void AddInstance(string? instance)
        {
            AddNormalizedStringValue(Instance, instance);
        }

        public void SetInstance(string? instance)
        {
            AddInstance(instance);
        }

        public void SetKeyword(string? keyword)
        {
            AddKeyword(keyword);
        }

        public void AddKeywordToken(string? keyword)
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

        public void AddKeywordsFromTokens(string? tokens)
        {
            if (string.IsNullOrWhiteSpace(tokens))
            {
                return;
            }

            var split = tokens.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            AddKeywords(split);
        }

        public void SetKeywordsFromTokens(string? tokens)
        {
            AddKeywordsFromTokens(tokens);
        }

        public void AddSearchText(string? text)
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

        public void SetSearchText(string? text)
        {
            AddSearchText(text);
        }

        public void AddContent(string? text)
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

        public void SetContent(string? text)
        {
            AddContent(text);
        }

        public static CanonicalDocument CreateMinimal(string id, IndexRequest source, DateTimeOffset timestamp)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentNullException.ThrowIfNull(source);

            return new CanonicalDocument
            {
                Id = id,
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

        private static string? NormalizeToken(string? value)
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
            var normalized = NormalizeToken(value);
            if (normalized is null)
            {
                return;
            }

            target.Add(normalized);
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