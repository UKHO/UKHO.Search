using UKHO.Search.Pipelines.Nodes;

namespace UKHO.Search.Tests.Pipelines
{
    internal sealed class CompletedNode : INode
    {
        private Task? _completion;

        public CompletedNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Task Completion => _completion ?? Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _completion ??= Task.CompletedTask;
            return Task.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}