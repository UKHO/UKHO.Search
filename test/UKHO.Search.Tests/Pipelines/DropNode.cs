using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Tests.Pipelines
{
    internal sealed class DropNode<TPayload> : NodeBase<Envelope<TPayload>, Envelope<TPayload>>
    {
        public DropNode(string name, ChannelReader<Envelope<TPayload>> input, ChannelWriter<Envelope<TPayload>> output, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input, output, logger, fatalErrorReporter)
        {
        }

        protected override ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);
            item.MarkDropped("Dropped for testing.", Name);
            return WriteAsync(item, cancellationToken);
        }
    }
}