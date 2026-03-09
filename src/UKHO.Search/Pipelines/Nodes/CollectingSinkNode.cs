using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class CollectingSinkNode<TPayload> : SinkNodeBase<Envelope<TPayload>>
    {
        private readonly object _gate = new();
        private readonly List<Envelope<TPayload>> _items = new();
        private readonly TimeSpan _perMessageDelay;

        public CollectingSinkNode(string name, ChannelReader<Envelope<TPayload>> input, TimeSpan? perMessageDelay = null, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input, logger, fatalErrorReporter)
        {
            _perMessageDelay = perMessageDelay ?? TimeSpan.Zero;
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

        protected override async ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);
            item.Context.MarkTimeUtc($"received:{Name}", DateTimeOffset.UtcNow);

            if (_perMessageDelay > TimeSpan.Zero)
            {
                await Task.Delay(_perMessageDelay, cancellationToken)
                          .ConfigureAwait(false);
            }

            lock (_gate)
            {
                _items.Add(item);
            }
        }
    }
}