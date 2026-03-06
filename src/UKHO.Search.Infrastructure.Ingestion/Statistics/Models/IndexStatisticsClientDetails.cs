namespace UKHO.Search.Infrastructure.Ingestion.Statistics.Models
{
    public sealed class IndexStatisticsClientDetails
    {
        public object? Exists { get; init; }

        public object? Mapping { get; init; }

        public object? Settings { get; init; }

        public object? Health { get; init; }

        public object? Count { get; init; }

        public object? Stats { get; init; }

        public object? FieldCaps { get; init; }
    }
}