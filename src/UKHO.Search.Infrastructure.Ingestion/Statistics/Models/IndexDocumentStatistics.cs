namespace UKHO.Search.Infrastructure.Ingestion.Statistics.Models
{
    public sealed class IndexDocumentStatistics
    {
        public long? Count { get; init; }

        public string? PrimaryStoreSize { get; init; }

        public string? TotalStoreSize { get; init; }
    }
}