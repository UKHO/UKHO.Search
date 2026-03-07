using System.Collections.ObjectModel;

namespace UKHO.Search.Infrastructure.Ingestion.Statistics.Models
{
    public sealed class IndexStatistics
    {
        public required string IndexName { get; init; }

        public bool Exists { get; init; }

        public IndexStatisticsClientDetails? Client { get; init; }

        public IndexHealth? Health { get; init; }

        public IndexMapping? Mapping { get; init; }

        public IndexSettings? Settings { get; init; }

        public IndexDocumentStatistics? Documents { get; init; }

        public IReadOnlyDictionary<string, IndexFieldCapabilities> FieldCapabilities { get; init; } = new ReadOnlyDictionary<string, IndexFieldCapabilities>(new Dictionary<string, IndexFieldCapabilities>());
    }
}