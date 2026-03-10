using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Retry;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Tests.Pipelines
{
    internal sealed class RetryPipelineGraph
    {
        private RetryPipelineGraph(PipelineSupervisor supervisor, CollectingSinkNode<int> sink, DeadLetterSinkNode<int> deadLetterSink, string deadLetterFilePath)
        {
            Supervisor = supervisor;
            Sink = sink;
            DeadLetterSink = deadLetterSink;
            DeadLetterFilePath = deadLetterFilePath;
        }

        public PipelineSupervisor Supervisor { get; }

        public CollectingSinkNode<int> Sink { get; }

        public DeadLetterSinkNode<int> DeadLetterSink { get; }

        public string DeadLetterFilePath { get; }

        public static RetryPipelineGraph Create(int messageCount, int capacity, IRetryPolicy retryPolicy, TimeSpan? sinkDelay, Func<int, CancellationToken, ValueTask<int>> transform, Func<Exception, bool> isTransientException, string deadLetterFilePath, CancellationToken cancellationToken)
        {
            var supervisor = new PipelineSupervisor(cancellationToken);

            var srcToPartition = BoundedChannelFactory.Create<Envelope<int>>(capacity, true, true);
            var partitionToTransform = BoundedChannelFactory.Create<Envelope<int>>(capacity, true, true);
            var transformToSink = BoundedChannelFactory.Create<Envelope<int>>(capacity, true, true);
            var transformToDeadLetter = BoundedChannelFactory.Create<Envelope<int>>(capacity, true, true);

            var source = new SyntheticSourceNode<int>("source", srcToPartition.Writer, messageCount, 1, i => i, fatalErrorReporter: supervisor);

            var partition = new KeyPartitionNode<int>("partition", srcToPartition.Reader, new[] { partitionToTransform.Writer }, fatalErrorReporter: supervisor);

            var transformNode = new RetryingTransformNode<int, int>("retrying-transform", partitionToTransform.Reader, transformToSink.Writer, transform, retryPolicy, isTransientException, transformToDeadLetter.Writer, false, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", transformToSink.Reader, sinkDelay, fatalErrorReporter: supervisor);

            var deadLetterSink = new DeadLetterSinkNode<int>("dead-letter", transformToDeadLetter.Reader, deadLetterFilePath, true, fatalErrorReporter: supervisor);

            supervisor.AddNode(source);
            supervisor.AddNode(partition);
            supervisor.AddNode(transformNode);
            supervisor.AddNode(sink);
            supervisor.AddNode(deadLetterSink);

            return new RetryPipelineGraph(supervisor, sink, deadLetterSink, deadLetterFilePath);
        }
    }
}