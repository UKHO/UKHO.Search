using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public abstract class SourceNodeBase<TOut> : INode
    {
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private readonly ILogger? _logger;
        private readonly NodeMetrics _metrics;
        private readonly ChannelWriter<TOut> _output;
        private Task? _completion;

        protected SourceNodeBase(string name, ChannelWriter<TOut> output, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            _output = output;
            _logger = logger;
            _fatalErrorReporter = fatalErrorReporter;
            _metrics = new NodeMetrics(name);
        }

        public string Name { get; }

        public Task Completion => _completion ?? Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
            return Task.CompletedTask;
        }

        public virtual ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected ValueTask WriteAsync(ChannelWriter<TOut> output, TOut item, CancellationToken cancellationToken)
        {
            return WriteCoreAsync(output, item, cancellationToken);
        }

        private async ValueTask WriteCoreAsync(ChannelWriter<TOut> output, TOut item, CancellationToken cancellationToken)
        {
            await output.WriteAsync(item, cancellationToken)
                        .ConfigureAwait(false);
            _metrics.RecordOut(item);
        }

        protected abstract ValueTask ProduceAsync(ChannelWriter<TOut> output, CancellationToken cancellationToken);

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                _metrics.IncrementInFlight();
                var started = Stopwatch.GetTimestamp();
                try
                {
                    await ProduceAsync(_output, cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    var elapsed = Stopwatch.GetElapsedTime(started);
                    _metrics.RecordDuration(elapsed);
                    _metrics.DecrementInFlight();
                }

                _output.TryComplete();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _output.TryComplete();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Node '{NodeName}' failed.", Name);
                _fatalErrorReporter?.ReportFatal(Name, ex);
                _output.TryComplete(ex);
                throw;
            }
            finally
            {
                _metrics.Dispose();
            }
        }
    }
}