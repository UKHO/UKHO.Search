namespace UKHO.Search.Infrastructure.Ingestion.Statistics.Models
{
    public sealed class IndexFieldCapabilities
    {
        public bool IsSearchable { get; init; }

        public bool IsAggregatable { get; init; }

        public IReadOnlyCollection<string> Types { get; init; } = Array.Empty<string>();
    }
}