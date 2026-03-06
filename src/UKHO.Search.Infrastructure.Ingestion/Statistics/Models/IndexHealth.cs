namespace UKHO.Search.Infrastructure.Ingestion.Statistics.Models
{
    public sealed class IndexHealth
    {
        public string? Status { get; init; }

        public int? NumberOfNodes { get; init; }

        public int? NumberOfDataNodes { get; init; }

        public int? ActivePrimaryShards { get; init; }

        public int? ActiveShards { get; init; }

        public int? RelocatingShards { get; init; }

        public int? InitializingShards { get; init; }

        public int? UnassignedShards { get; init; }

        public double? ActiveShardsPercentAsNumber { get; init; }
    }
}
