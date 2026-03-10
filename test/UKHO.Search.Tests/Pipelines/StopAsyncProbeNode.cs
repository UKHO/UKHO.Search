using UKHO.Search.Pipelines.Nodes;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class StopAsyncProbeNode : INode
    {
        private int _stopCalls;

        public StopAsyncProbeNode(string name)
        {
            Name = name;
        }

        public int StopCalls => Volatile.Read(ref _stopCalls);

        public string Name { get; }

        public Task Completion => Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _stopCalls);
            return ValueTask.CompletedTask;
        }
    }
}