using System.Text.Json.Serialization;
using UKHO.Search.Geo;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Query;

namespace UKHO.Search.Ingestion.Pipeline.Documents
{
    /// <summary>
    /// Represents the provider-independent canonical search document that ingestion enrichers and indexing infrastructure share.
    /// </summary>
    public sealed record CanonicalDocument
    {
        /// <summary>
        /// Gets the stable document identifier used throughout the ingestion and indexing pipeline.
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Gets the canonical provider identifier that produced this document.
        /// </summary>
        public required string Provider { get; init; }

        /// <summary>
        /// Gets the preserved source request used as a traceability copy for later diagnostics and dead-letter output.
        /// </summary>
        public IndexRequest Source { get; init; } = new();

        /// <summary>
        /// Gets the source timestamp captured when the canonical document was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; init; }

        /// <summary>
        /// Gets the normalized keyword set used for exact-match discovery behaviour.
        /// </summary>
        [JsonInclude]
        public SortedSet<string> Keywords { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the normalized canonical security token set used for exact-match security and filtering behaviour.
        /// </summary>
        [JsonInclude]
        public SortedSet<string> SecurityTokens { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the normalized authority taxonomy values.
        /// </summary>
        [JsonInclude]
        public SortedSet<string> Authority { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the normalized region taxonomy values.
        /// </summary>
        [JsonInclude]
        public SortedSet<string> Region { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the normalized format taxonomy values.
        /// </summary>
        [JsonInclude]
        public SortedSet<string> Format { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the canonical major-version taxonomy values.
        /// </summary>
        [JsonInclude]
        public SortedSet<int> MajorVersion { get; private set; } = new();

        /// <summary>
        /// Gets the canonical minor-version taxonomy values.
        /// </summary>
        [JsonInclude]
        public SortedSet<int> MinorVersion { get; private set; } = new();

        /// <summary>
        /// Gets the normalized category taxonomy values.
        /// </summary>
        [JsonInclude]
        public SortedSet<string> Category { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the normalized series taxonomy values.
        /// </summary>
        [JsonInclude]
        public SortedSet<string> Series { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the normalized instance taxonomy values.
        /// </summary>
        [JsonInclude]
        public SortedSet<string> Instance { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the display-oriented title set that preserves authored casing.
        /// </summary>
        [JsonInclude]
        public SortedSet<string> Title { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the normalized analyzed search text built from additive enrichment input.
        /// </summary>
        [JsonInclude]
        public string SearchText { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the normalized extracted content body built from additive enrichment input.
        /// </summary>
        [JsonInclude]
        public string Content { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the geographic coverage shapes retained on the canonical document.
        /// </summary>
        [JsonInclude]
        public List<GeoPolygon> GeoPolygons { get; private set; } = new();

        /// <summary>
        /// Adds a keyword to the canonical keyword set after normalization.
        /// </summary>
        /// <param name="keyword">The candidate keyword to normalize and retain.</param>
        public void AddKeyword(string? keyword)
        {
            // Normalize the input before adding it so the canonical set stays deduplicated and deterministic.
            var normalized = NormalizeStringValue(keyword);
            if (normalized is null)
            {
                return;
            }

            Keywords.Add(normalized);
        }

        /// <summary>
        /// Adds a security token to the canonical security token set after normalization.
        /// </summary>
        /// <param name="securityToken">The candidate security token to normalize and retain.</param>
        public void AddSecurityToken(string? securityToken)
        {
            // Reuse the shared normalized string path so security tokens follow the same lowercasing,
            // trimming, ordering, and de-duplication rules as other exact-match canonical fields.
            AddNormalizedStringValue(SecurityTokens, securityToken);
        }

        /// <summary>
        /// Adds multiple security tokens to the canonical security token set.
        /// </summary>
        /// <param name="securityTokens">The candidate security tokens to normalize and retain.</param>
        public void AddSecurityToken(IEnumerable<string?>? securityTokens)
        {
            if (securityTokens is null)
            {
                return;
            }

            // Funnel every value through the single-token overload so normalization stays consistent.
            foreach (var securityToken in securityTokens)
            {
                AddSecurityToken(securityToken);
            }
        }

        /// <summary>
        /// Adds an authority taxonomy value after normalization.
        /// </summary>
        /// <param name="authority">The candidate authority value.</param>
        public void AddAuthority(string? authority)
        {
            AddNormalizedStringValue(Authority, authority);
        }

        /// <summary>
        /// Adds a region taxonomy value after normalization.
        /// </summary>
        /// <param name="region">The candidate region value.</param>
        public void AddRegion(string? region)
        {
            AddNormalizedStringValue(Region, region);
        }

        /// <summary>
        /// Adds a format taxonomy value after normalization.
        /// </summary>
        /// <param name="format">The candidate format value.</param>
        public void AddFormat(string? format)
        {
            AddNormalizedStringValue(Format, format);
        }

        /// <summary>
        /// Adds a major-version taxonomy value when one is supplied.
        /// </summary>
        /// <param name="majorVersion">The candidate major-version value.</param>
        public void AddMajorVersion(int? majorVersion)
        {
            AddIntValue(MajorVersion, majorVersion);
        }

        /// <summary>
        /// Adds a minor-version taxonomy value when one is supplied.
        /// </summary>
        /// <param name="minorVersion">The candidate minor-version value.</param>
        public void AddMinorVersion(int? minorVersion)
        {
            AddIntValue(MinorVersion, minorVersion);
        }

        /// <summary>
        /// Adds a category taxonomy value after normalization.
        /// </summary>
        /// <param name="category">The candidate category value.</param>
        public void AddCategory(string? category)
        {
            AddNormalizedStringValue(Category, category);
        }

        /// <summary>
        /// Adds a series taxonomy value after normalization.
        /// </summary>
        /// <param name="series">The candidate series value.</param>
        public void AddSeries(string? series)
        {
            AddNormalizedStringValue(Series, series);
        }

        /// <summary>
        /// Adds an instance taxonomy value after normalization.
        /// </summary>
        /// <param name="instance">The candidate instance value.</param>
        public void AddInstance(string? instance)
        {
            AddNormalizedStringValue(Instance, instance);
        }

        /// <summary>
        /// Adds a display title while preserving authored casing.
        /// </summary>
        /// <param name="title">The candidate title value.</param>
        public void AddTitle(string? title)
        {
            // Titles preserve display casing, so they use the title-specific normalization path instead of lowercasing.
            var normalized = NormalizeTitleValue(title);
            if (normalized is null)
            {
                return;
            }

            Title.Add(normalized);
        }

        /// <summary>
        /// Adds multiple keywords to the canonical keyword set.
        /// </summary>
        /// <param name="keywords">The candidate keywords to normalize and retain.</param>
        public void AddKeywords(IEnumerable<string?>? keywords)
        {
            if (keywords is null)
            {
                return;
            }

            // Reuse the single-keyword path so all normalization rules stay in one place.
            foreach (var keyword in keywords)
            {
                AddKeyword(keyword);
            }
        }

        /// <summary>
        /// Adds multiple titles to the canonical title set.
        /// </summary>
        /// <param name="titles">The candidate title values to retain.</param>
        public void AddTitles(IEnumerable<string?>? titles)
        {
            if (titles is null)
            {
                return;
            }

            // Reuse the single-title path so trimming and de-duplication stay consistent.
            foreach (var title in titles)
            {
                AddTitle(title);
            }
        }

        /// <summary>
        /// Tokenizes free-form keyword text and adds the normalized results to the canonical keyword set.
        /// </summary>
        /// <param name="tokens">The delimited keyword text to tokenize.</param>
        public void AddKeywordsFromTokens(string? tokens)
        {
            if (string.IsNullOrWhiteSpace(tokens))
            {
                return;
            }

            // Use the shared token normalizer so alias expansion and token normalization remain centralized.
            var tokenNormalizer = new TokenNormalizer();
            foreach (var token in SplitKeywordTokens(tokens))
            {
                foreach (var normalizedToken in tokenNormalizer.NormalizeToken(token))
                {
                    AddKeyword(normalizedToken);
                }
            }
        }

        /// <summary>
        /// Appends normalized search text to the analyzed search surface.
        /// </summary>
        /// <param name="text">The candidate search text to append.</param>
        public void AddSearchText(string? text)
        {
            var normalized = NormalizeStringValue(text);
            if (normalized is null)
            {
                return;
            }

            // The first retained value becomes the whole field; later values are appended with a single separator.
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                SearchText = normalized;
                return;
            }

            SearchText = string.Concat(SearchText, " ", normalized);
        }

        /// <summary>
        /// Appends normalized content text to the extracted content surface.
        /// </summary>
        /// <param name="text">The candidate content text to append.</param>
        public void AddContent(string? text)
        {
            var normalized = NormalizeStringValue(text);
            if (normalized is null)
            {
                return;
            }

            // The first retained value becomes the whole field; later values are appended with a single separator.
            if (string.IsNullOrWhiteSpace(Content))
            {
                Content = normalized;
                return;
            }

            Content = string.Concat(Content, " ", normalized);
        }

        /// <summary>
        /// Creates the minimal canonical document shape from the inbound request and provider context.
        /// </summary>
        /// <param name="id">The document identifier to retain in canonical state.</param>
        /// <param name="provider">The provider identifier responsible for the request.</param>
        /// <param name="source">The traceability copy of the inbound request.</param>
        /// <param name="timestamp">The canonical timestamp to retain on the document.</param>
        /// <returns>A minimal canonical document seeded with request-derived security tokens.</returns>
        public static CanonicalDocument CreateMinimal(string id, string provider, IndexRequest source, DateTimeOffset timestamp)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            ArgumentNullException.ThrowIfNull(source);

            // Build the minimal canonical state first, preserving the original request payload for traceability.
            var document = new CanonicalDocument
            {
                Id = id,
                Provider = provider.Trim(),
                Source = source,
                Timestamp = timestamp
            };

            // Copy request security tokens through the canonical mutator path so lowercasing, trimming,
            // de-duplication, and ordering match later enricher-added tokens.
            document.AddSecurityToken(source.SecurityTokens);

            return document;
        }

        /// <summary>
        /// Adds a geo polygon to the canonical geographic coverage collection.
        /// </summary>
        /// <param name="polygon">The polygon to retain.</param>
        public void AddGeoPolygon(GeoPolygon polygon)
        {
            ArgumentNullException.ThrowIfNull(polygon);
            GeoPolygons.Add(polygon);
        }

        /// <summary>
        /// Adds multiple geo polygons to the canonical geographic coverage collection.
        /// </summary>
        /// <param name="polygons">The polygons to retain.</param>
        public void AddGeoPolygons(IEnumerable<GeoPolygon>? polygons)
        {
            if (polygons is null)
            {
                return;
            }

            // Reuse the single-polygon path so null-guarding stays centralized.
            foreach (var polygon in polygons)
            {
                AddGeoPolygon(polygon);
            }
        }

        private static IEnumerable<string> SplitKeywordTokens(string tokens)
        {
            // Walk the source text manually so repeated delimiters do not create empty tokens.
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

            // Lowercase exact-match fields so indexing and filtering stay case-insensitive.
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