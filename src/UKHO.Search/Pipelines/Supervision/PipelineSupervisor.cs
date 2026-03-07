using UKHO.Search.Pipelines.Nodes;

namespace UKHO.Search.Pipelines.Supervision
{
    public sealed class PipelineSupervisor : IPipelineFatalErrorReporter
    {
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly List<INode> nodes = new();
        private Task? completion;
        private Exception? fatalException;
        private string? fatalNodeName;
        private int fatalReported;
        private int started;

        public PipelineSupervisor(CancellationToken cancellationToken)
        {
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public PipelineSupervisor(IReadOnlyList<INode> nodes, CancellationToken cancellationToken) : this(cancellationToken)
        {
            foreach (var node in nodes)
            {
                AddNode(node);
            }
        }

        public IReadOnlyList<INode> Nodes => nodes;

        public CancellationToken CancellationToken => cancellationTokenSource.Token;

        public Exception? FatalException => fatalException;

        public string? FatalNodeName => fatalNodeName;

        public Task Completion => completion ?? Task.CompletedTask;

        public void ReportFatal(string nodeName, Exception exception)
        {
            if (Interlocked.CompareExchange(ref fatalReported, 1, 0) != 0)
            {
                return;
            }

            fatalException = exception;
            fatalNodeName = nodeName;
            Cancel();
        }

        public Task StartAsync()
        {
            Interlocked.Exchange(ref started, 1);
            completion ??= RunAsync();
            return Task.CompletedTask;
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        public void AddNode(INode node)
        {
            if (Volatile.Read(ref started) != 0)
            {
                throw new InvalidOperationException("Cannot add nodes after the supervisor has started.");
            }

            if (nodes.Any(n => string.Equals(n.Name, node.Name, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"A node with name '{node.Name}' has already been added. Node names must be unique.");
            }

            nodes.Add(node);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Cancel();

            var nodesSnapshot = nodes.ToArray();
            foreach (var node in nodesSnapshot)
            {
                await node.StopAsync(cancellationToken)
                          .ConfigureAwait(false);
            }
        }

        private async Task RunAsync()
        {
            var nodesSnapshot = nodes.ToArray();
            foreach (var node in nodesSnapshot)
            {
                await node.StartAsync(CancellationToken)
                          .ConfigureAwait(false);
            }

            var completionTasks = nodesSnapshot.Select(n => n.Completion)
                                               .ToArray();

            try
            {
                while (completionTasks.Length > 0)
                {
                    var finished = await Task.WhenAny(completionTasks)
                                             .ConfigureAwait(false);

                    if (finished.IsFaulted)
                    {
                        Cancel();
                        break;
                    }

                    completionTasks = completionTasks.Where(t => t != finished)
                                                     .ToArray();
                }
            }
            finally
            {
                await Task.WhenAll(nodesSnapshot.Select(n => n.Completion))
                          .ConfigureAwait(false);
            }
        }
    }
}