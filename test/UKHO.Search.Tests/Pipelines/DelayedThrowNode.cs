using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Tests.Pipelines
{
    internal sealed class DelayedThrowNode : INode
    {
        private readonly TimeSpan _delay;
        private readonly Exception _exception;
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private Task? _completion;

        public DelayedThrowNode(string name, TimeSpan delay, Exception exception, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            _delay = delay;
            _exception = exception;
            _fatalErrorReporter = fatalErrorReporter;
        }

        public string Name { get; }

        public Task Completion => _completion ?? Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
            return Task.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken)
                      .ConfigureAwait(false);
            _fatalErrorReporter?.ReportFatal(Name, _exception);
            throw _exception;
        }
    }
}