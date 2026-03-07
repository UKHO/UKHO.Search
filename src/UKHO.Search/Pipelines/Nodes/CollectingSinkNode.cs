using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class CollectingSinkNode<TPayload> : SinkNodeBase<Envelope<TPayload>>
    {
        private readonly object gate = new();
        private readonly List<Envelope<TPayload>> items = new();
        private readonly TimeSpan perMessageDelay;

        public CollectingSinkNode(string name, ChannelReader<Envelope<TPayload>> input, TimeSpan? perMessageDelay = null, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input, logger, fatalErrorReporter)
        {
            this.perMessageDelay = perMessageDelay ?? TimeSpan.Zero;
        }

        public IReadOnlyList<Envelope<TPayload>> Items
        {
            get
            {
                lock (gate)
                {
                    return items.ToArray();
                }
            }
        }

        protected override async ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);
            item.Context.MarkTimeUtc($"received:{Name}", DateTimeOffset.UtcNow);

            if (perMessageDelay > TimeSpan.Zero)
            {
                await Task.Delay(perMessageDelay, cancellationToken)
                          .ConfigureAwait(false);
            }

            lock (gate)
            {
                items.Add(item);
            }
        }
    }
}