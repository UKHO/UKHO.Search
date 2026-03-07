using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class MergeNode<TIn> : MultiInputNodeBase<Envelope<TIn>, Envelope<TIn>, Envelope<TIn>>
    {
        public MergeNode(string name, ChannelReader<Envelope<TIn>> input1, ChannelReader<Envelope<TIn>> input2, ChannelWriter<Envelope<TIn>> output, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input1, input2, output, logger, fatalErrorReporter)
        {
        }

        protected override ValueTask HandleInput1Async(Envelope<TIn> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);
            return WriteAsync(item, cancellationToken);
        }

        protected override ValueTask HandleInput2Async(Envelope<TIn> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);
            return WriteAsync(item, cancellationToken);
        }
    }
}