using UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline
{
    public sealed record IngestionPipelineGraph
    {
        public required PipelineSupervisor Supervisor { get; init; }

        public IReadOnlyList<CollectingBatchSinkNode<IndexOperation>> LaneSinks { get; init; } = Array.Empty<CollectingBatchSinkNode<IndexOperation>>();
    }
}