using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes
{
    public sealed class BatchFlattenNode<TPayload> : INode
    {
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private readonly ChannelReader<BatchEnvelope<TPayload>> _input;
        private readonly ILogger? _logger;
        private readonly NodeMetrics _metrics;
        private readonly ChannelWriter<Envelope<TPayload>> _output;
        private Task? _completion;

        public BatchFlattenNode(string name, ChannelReader<BatchEnvelope<TPayload>> input, ChannelWriter<Envelope<TPayload>> output, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            _input = input;
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

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (await _input.WaitToReadAsync(cancellationToken)
                                   .ConfigureAwait(false))
                {
                    while (_input.TryRead(out var batch))
                    {
                        _metrics.RecordIn(batch);
                        _metrics.IncrementInFlight();
                        var started = Stopwatch.GetTimestamp();
                        try
                        {
                            foreach (var item in batch.Items)
                            {
                                item.Context.AddBreadcrumb(Name);

                                _logger?.LogInformation("Stub indexed message. NodeName={NodeName} PartitionId={PartitionId} Key={Key} MessageId={MessageId} Attempt={Attempt}", Name, batch.PartitionId, item.Key, item.MessageId, item.Attempt);

                                await _output.WriteAsync(item, cancellationToken)
                                             .ConfigureAwait(false);
                                _metrics.RecordOut(item);
                            }
                        }
                        finally
                        {
                            var elapsed = Stopwatch.GetElapsedTime(started);
                            _metrics.RecordDuration(elapsed);
                            _metrics.DecrementInFlight();
                        }
                    }
                }

                await _input.Completion.ConfigureAwait(false);
                _output.TryComplete();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _output.TryComplete();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Node '{NodeName}' failed.", Name);
                _output.TryComplete(ex);
                _fatalErrorReporter?.ReportFatal(Name, ex);
                throw;
            }
            finally
            {
                _metrics.Dispose();
            }
        }
    }
}