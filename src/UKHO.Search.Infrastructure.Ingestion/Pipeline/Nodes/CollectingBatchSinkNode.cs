using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes
{
    public sealed class CollectingBatchSinkNode<TPayload> : SinkNodeBase<BatchEnvelope<TPayload>>
    {
        private readonly object _gate = new();
        private readonly List<Envelope<TPayload>> _items = new();
        private readonly ILogger? _logger;

        public CollectingBatchSinkNode(string name, ChannelReader<BatchEnvelope<TPayload>> input, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input, logger, fatalErrorReporter)
        {
            _logger = logger;
        }

        public IReadOnlyList<Envelope<TPayload>> Items
        {
            get
            {
                lock (_gate)
                {
                    return _items.ToArray();
                }
            }
        }

        protected override ValueTask HandleItemAsync(BatchEnvelope<TPayload> batch, CancellationToken cancellationToken)
        {
            foreach (var envelope in batch.Items)
            {
                envelope.Context.AddBreadcrumb(Name);
                envelope.Context.MarkTimeUtc($"received:{Name}", DateTimeOffset.UtcNow);

                _logger?.LogInformation("Stub indexed message. NodeName={NodeName} PartitionId={PartitionId} Key={Key} MessageId={MessageId} Attempt={Attempt}", Name, batch.PartitionId, envelope.Key, envelope.MessageId, envelope.Attempt);

                lock (_gate)
                {
                    _items.Add(envelope);
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}