using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Terminal
{
    public sealed class AckSinkNode<TPayload> : SinkNodeBase<Envelope<TPayload>>
    {
        private readonly ILogger _logger;

        public AckSinkNode(string name, ChannelReader<Envelope<TPayload>> input, ILogger logger, IPipelineFatalErrorReporter? fatalErrorReporter = null, string? providerName = null) : base(name, input, logger, fatalErrorReporter, providerName: providerName)
        {
            _logger = logger;
        }

        protected override async ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);

            if (!item.Context.TryGetItem<IQueueMessageAcker>(QueueEnvelopeContextKeys.MessageAcker, out var acker) || acker is null)
            {
                return;
            }

            try
            {
                await acker.DeleteAsync(cancellationToken)
                           .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete queue message on terminal ack. NodeName={NodeName} Key={Key} MessageId={MessageId} Attempt={Attempt}", Name, item.Key, item.MessageId, item.Attempt);
            }
        }
    }
}