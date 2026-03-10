using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.Batching
{
    public sealed class BatchEnvelope<TPayload>
    {
        public required Guid BatchId { get; init; }

        public required int PartitionId { get; init; }

        public required IReadOnlyList<Envelope<TPayload>> Items { get; init; }

        public int ItemCount => Items.Count;

        public int? TotalEstimatedBytes { get; init; }

        public DateTimeOffset? MinItemTimestampUtc { get; init; }

        public DateTimeOffset? MaxItemTimestampUtc { get; init; }

        public required DateTimeOffset CreatedUtc { get; init; }

        public required DateTimeOffset FlushedUtc { get; init; }
    }
}