using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Tests.Pipelines
{
    internal sealed class HelloPipelineGraph
    {
        private HelloPipelineGraph(PipelineSupervisor supervisor, SyntheticSourceNode<int> source, ValidateNode<int> validate, KeyPartitionNode<int> partition, IReadOnlyList<TransformNode<int, int>> transforms, IReadOnlyList<CollectingSinkNode<int>> sinks, DeadLetterSinkNode<int>? deadLetterSink, string? deadLetterFilePath)
        {
            Supervisor = supervisor;
            Source = source;
            Validate = validate;
            Partition = partition;
            Transforms = transforms;
            Sinks = sinks;
            DeadLetterSink = deadLetterSink;
            DeadLetterFilePath = deadLetterFilePath;
        }

        public PipelineSupervisor Supervisor { get; }

        public SyntheticSourceNode<int> Source { get; }

        public ValidateNode<int> Validate { get; }

        public KeyPartitionNode<int> Partition { get; }

        public IReadOnlyList<TransformNode<int, int>> Transforms { get; }

        public IReadOnlyList<CollectingSinkNode<int>> Sinks { get; }

        public DeadLetterSinkNode<int>? DeadLetterSink { get; }

        public string? DeadLetterFilePath { get; }

        public static HelloPipelineGraph Create(int messageCount, int keyCardinality, int partitions, int capacity, TimeSpan? sinkDelay, Func<int, CancellationToken, ValueTask<int>> transform, CancellationToken cancellationToken, Func<int, string>? keyFactory = null, string? deadLetterFilePath = null, bool faultPipelineOnTransformException = false)
        {
            var supervisor = new PipelineSupervisor(cancellationToken);

            var srcToValidate = BoundedChannelFactory.Create<Envelope<int>>(capacity, true, true);
            var validateToPartition = BoundedChannelFactory.Create<Envelope<int>>(capacity, true, true);
            var validateToDeadLetter = deadLetterFilePath is not null ? BoundedChannelFactory.Create<Envelope<int>>(capacity, true, true) : null;

            var source = new SyntheticSourceNode<int>("source", srcToValidate.Writer, messageCount, keyCardinality, i => i, keyFactory, fatalErrorReporter: supervisor);

            var validate = new ValidateNode<int>("validate", srcToValidate.Reader, validateToPartition.Writer, validateToDeadLetter?.Writer, deadLetterFilePath is null, fatalErrorReporter: supervisor);

            var partitionOutputs = new CountingChannel<Envelope<int>>[partitions];
            var transformOutputs = new CountingChannel<Envelope<int>>[partitions];

            for (var i = 0; i < partitions; i++)
            {
                partitionOutputs[i] = BoundedChannelFactory.Create<Envelope<int>>(capacity, true, true);
                transformOutputs[i] = BoundedChannelFactory.Create<Envelope<int>>(capacity, true, true);
            }

            var partition = new KeyPartitionNode<int>("partition", validateToPartition.Reader, partitionOutputs.Select(c => c.Writer)
                                                                                                               .ToArray(), fatalErrorReporter: supervisor);

            var transforms = new List<TransformNode<int, int>>(partitions);
            var sinks = new List<CollectingSinkNode<int>>(partitions);

            for (var i = 0; i < partitions; i++)
            {
                transforms.Add(new TransformNode<int, int>($"transform-{i}", partitionOutputs[i].Reader, transformOutputs[i].Writer, transform, faultPipelineOnTransformException, fatalErrorReporter: supervisor));

                sinks.Add(new CollectingSinkNode<int>($"sink-{i}", transformOutputs[i].Reader, sinkDelay, fatalErrorReporter: supervisor));
            }

            DeadLetterSinkNode<int>? deadLetterSink = null;

            if (deadLetterFilePath is not null && validateToDeadLetter is not null)
            {
                deadLetterSink = new DeadLetterSinkNode<int>("dead-letter", validateToDeadLetter.Reader, deadLetterFilePath, fatalErrorReporter: supervisor);
            }

            supervisor.AddNode(source);
            supervisor.AddNode(validate);
            supervisor.AddNode(partition);

            foreach (var transformNode in transforms)
            {
                supervisor.AddNode(transformNode);
            }

            foreach (var sinkNode in sinks)
            {
                supervisor.AddNode(sinkNode);
            }

            if (deadLetterSink is not null)
            {
                supervisor.AddNode(deadLetterSink);
            }

            return new HelloPipelineGraph(supervisor, source, validate, partition, transforms, sinks, deadLetterSink, deadLetterFilePath);
        }
    }
}