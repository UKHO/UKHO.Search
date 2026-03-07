using System.Buffers;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class KeyPartitionNode<TPayload> : INode
    {
        private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
        private readonly ChannelReader<Envelope<TPayload>> input;
        private readonly ILogger? logger;
        private readonly NodeMetrics metrics;
        private readonly IReadOnlyList<ChannelWriter<Envelope<TPayload>>> outputs;
        private Task? completion;

        public KeyPartitionNode(string name, ChannelReader<Envelope<TPayload>> input, IReadOnlyList<ChannelWriter<Envelope<TPayload>>> outputs, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            if (outputs.Count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(outputs));
            }

            Name = name;
            this.input = input;
            this.outputs = outputs;
            this.logger = logger;
            this.fatalErrorReporter = fatalErrorReporter;
            metrics = new NodeMetrics(name);
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
            try
            {
                while (await input.WaitToReadAsync(cancellationToken)
                                  .ConfigureAwait(false))
                {
                    while (input.TryRead(out var item))
                    {
                        metrics.RecordIn(item);
                        metrics.IncrementInFlight();
                        var started = Stopwatch.GetTimestamp();
                        try
                        {
                            item.Context.AddBreadcrumb(Name);

                            var partition = GetPartition(item.Key, outputs.Count);
                            await outputs[partition]
                                  .WriteAsync(item, cancellationToken)
                                  .ConfigureAwait(false);
                            metrics.RecordOut(item);
                        }
                        finally
                        {
                            var elapsed = Stopwatch.GetElapsedTime(started);
                            metrics.RecordDuration(elapsed);
                            metrics.DecrementInFlight();
                        }
                    }
                }

                await input.Completion.ConfigureAwait(false);

                CompleteOutputs();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                CompleteOutputs();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Node '{NodeName}' failed.", Name);
                CompleteOutputs(ex);
                fatalErrorReporter?.ReportFatal(Name, ex);
                throw;
            }
            finally
            {
                metrics.Dispose();
            }
        }

        private void CompleteOutputs(Exception? error = null)
        {
            foreach (var output in outputs)
            {
                output.TryComplete(error);
            }
        }

        private static int GetPartition(string key, int partitions)
        {
            // Stable, deterministic 32-bit FNV-1a hash.
            unchecked
            {
                var hash = 2166136261;

                var byteCount = Encoding.UTF8.GetByteCount(key);
                byte[]? rented = null;
                var buffer = byteCount <= 256 ? stackalloc byte[byteCount] : (rented = ArrayPool<byte>.Shared.Rent(byteCount)).AsSpan(0, byteCount);

                try
                {
                    Encoding.UTF8.GetBytes(key.AsSpan(), buffer);
                    for (var i = 0; i < buffer.Length; i++)
                    {
                        hash ^= buffer[i];
                        hash *= 16777619;
                    }
                }
                finally
                {
                    if (rented is not null)
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                }

                return (int)(hash % (uint)partitions);
            }
        }
    }
}