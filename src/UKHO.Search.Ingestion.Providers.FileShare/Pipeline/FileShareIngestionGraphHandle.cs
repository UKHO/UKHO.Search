using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline
{
    public sealed record FileShareIngestionGraphHandle
    {
        public required PipelineSupervisor Supervisor { get; init; }
    }
}