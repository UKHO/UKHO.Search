using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class BulkIndexRequest<TDocument>
    {
        public required Guid BatchId { get; init; }

        public required int PartitionId { get; init; }

        public required IReadOnlyList<Envelope<TDocument>> Items { get; init; }
    }
}