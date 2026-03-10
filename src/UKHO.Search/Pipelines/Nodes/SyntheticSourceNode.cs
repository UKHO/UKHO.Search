using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class SyntheticSourceNode<TPayload> : SourceNodeBase<Envelope<TPayload>>
    {
        private readonly int _keyCardinality;
        private readonly Func<int, string>? _keyFactory;
        private readonly int _messageCount;
        private readonly Func<int, TPayload> _payloadFactory;

        public SyntheticSourceNode(string name, ChannelWriter<Envelope<TPayload>> output, int messageCount, int keyCardinality, Func<int, TPayload> payloadFactory, Func<int, string>? keyFactory = null, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, output, logger, fatalErrorReporter)
        {
            if (messageCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(messageCount));
            }

            if (keyCardinality <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(keyCardinality));
            }

            _messageCount = messageCount;
            _keyCardinality = keyCardinality;
            _payloadFactory = payloadFactory;
            _keyFactory = keyFactory;
        }

        protected override async ValueTask ProduceAsync(ChannelWriter<Envelope<TPayload>> output, CancellationToken cancellationToken)
        {
            for (var i = 0; i < _messageCount; i++)
            {
                var key = _keyFactory is not null ? _keyFactory(i) : $"key-{i % _keyCardinality}";
                var payload = _payloadFactory(i);
                var envelope = new Envelope<TPayload>(key, payload);
                envelope.Context.AddBreadcrumb(Name);

                await WriteAsync(output, envelope, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}