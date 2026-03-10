using System.Threading.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class PassThroughNode : NodeBase<Envelope<int>, Envelope<int>>
    {
        public PassThroughNode(string name, ChannelReader<Envelope<int>> input, ChannelWriter<Envelope<int>> output, CancellationMode cancellationMode) : base(name, input, output, null, null, cancellationMode)
        {
        }

        protected override ValueTask HandleItemAsync(Envelope<int> item, CancellationToken cancellationToken)
        {
            return WriteAsync(item, cancellationToken);
        }
    }
}