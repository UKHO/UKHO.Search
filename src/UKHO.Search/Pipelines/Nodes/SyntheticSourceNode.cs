using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class SyntheticSourceNode<TPayload> : SourceNodeBase<Envelope<TPayload>>
    {
        private readonly int keyCardinality;
        private readonly Func<int, string>? keyFactory;
        private readonly int messageCount;
        private readonly Func<int, TPayload> payloadFactory;

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

            this.messageCount = messageCount;
            this.keyCardinality = keyCardinality;
            this.payloadFactory = payloadFactory;
            this.keyFactory = keyFactory;
        }

        protected override async ValueTask ProduceAsync(ChannelWriter<Envelope<TPayload>> output, CancellationToken cancellationToken)
        {
            for (var i = 0; i < messageCount; i++)
            {
                var key = keyFactory is not null ? keyFactory(i) : $"key-{i % keyCardinality}";
                var payload = payloadFactory(i);
                var envelope = new Envelope<TPayload>(key, payload);
                envelope.Context.AddBreadcrumb(Name);

                await WriteAsync(output, envelope, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}