using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Tests.Pipelines
{
    internal sealed class DelayedThrowNode : INode
    {
        private readonly TimeSpan delay;
        private readonly Exception exception;
        private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
        private Task? completion;

        public DelayedThrowNode(string name, TimeSpan delay, Exception exception, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            this.delay = delay;
            this.exception = exception;
            this.fatalErrorReporter = fatalErrorReporter;
        }

        public string Name { get; }

        public Task Completion => completion ?? Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
            return Task.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken)
                      .ConfigureAwait(false);
            fatalErrorReporter?.ReportFatal(Name, exception);
            throw exception;
        }
    }
}