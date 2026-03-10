using System.Threading.Channels;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline
{
    public sealed record FileShareIngestionProcessingGraphFactories
    {
        public required Func<string, ChannelReader<Envelope<IngestionRequest>>, PipelineSupervisor, INode> CreateRequestDeadLetterSinkNode { get; init; }

        public required Func<string, ChannelReader<Envelope<IndexOperation>>, PipelineSupervisor, INode> CreateIndexDeadLetterSinkNode { get; init; }

        public required Func<string, ChannelReader<Envelope<IndexOperation>>, PipelineSupervisor, INode> CreateDiagnosticsSinkNode { get; init; }

        public required Func<string, int, ChannelReader<BatchEnvelope<IndexOperation>>, ChannelWriter<Envelope<IndexOperation>>, ChannelWriter<Envelope<IndexOperation>>, PipelineSupervisor, INode> CreateBulkIndexNode { get; init; }

        public required Func<string, int, ChannelReader<Envelope<IndexOperation>>, PipelineSupervisor, INode> CreateAckNode { get; init; }
    }
}