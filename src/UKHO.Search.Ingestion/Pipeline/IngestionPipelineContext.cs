using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Pipeline
{
    public sealed record IngestionPipelineContext
    {
        public required IngestionRequest Request { get; init; }

        public required IndexOperation Operation { get; init; }
    }
}
