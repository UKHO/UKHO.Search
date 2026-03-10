using UKHO.Search.Pipelines.Nodes;

namespace UKHO.Search.Pipelines.Supervision
{
    public sealed class PipelineSupervisor : IPipelineFatalErrorReporter
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly List<INode> _nodes = new();
        private Task? _completion;
        private Exception? _fatalException;
        private string? _fatalNodeName;
        private int _fatalReported;
        private int _started;

        public PipelineSupervisor(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public PipelineSupervisor(IReadOnlyList<INode> nodes, CancellationToken cancellationToken) : this(cancellationToken)
        {
            foreach (var node in nodes)
            {
                AddNode(node);
            }
        }

        public IReadOnlyList<INode> Nodes => _nodes;

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public Exception? FatalException => _fatalException;

        public string? FatalNodeName => _fatalNodeName;

        public Task Completion => _completion ?? Task.CompletedTask;

        public void ReportFatal(string nodeName, Exception exception)
        {
            if (Interlocked.CompareExchange(ref _fatalReported, 1, 0) != 0)
            {
                return;
            }

            _fatalException = exception;
            _fatalNodeName = nodeName;
            Cancel();
        }

        public Task StartAsync()
        {
            Interlocked.Exchange(ref _started, 1);
            _completion ??= RunAsync();
            return Task.CompletedTask;
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        public void AddNode(INode node)
        {
            if (Volatile.Read(ref _started) != 0)
            {
                throw new InvalidOperationException("Cannot add nodes after the supervisor has started.");
            }

            if (_nodes.Any(n => string.Equals(n.Name, node.Name, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"A node with name '{node.Name}' has already been added. Node names must be unique.");
            }

            _nodes.Add(node);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Cancel();

            var nodesSnapshot = _nodes.ToArray();
            foreach (var node in nodesSnapshot)
            {
                await node.StopAsync(cancellationToken)
                          .ConfigureAwait(false);
            }
        }

        private async Task RunAsync()
        {
            var nodesSnapshot = _nodes.ToArray();
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