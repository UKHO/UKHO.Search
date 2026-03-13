namespace QueryServiceHost.Models
{
    public class QueryResponse
    {
        public IReadOnlyList<Hit> Hits { get; init; } = Array.Empty<Hit>();

        public IReadOnlyList<FacetGroup> Facets { get; init; } = Array.Empty<FacetGroup>();

        public long Total { get; init; }

        public TimeSpan Duration { get; init; }
    }
}
