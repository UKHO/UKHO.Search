using System.Threading.Channels;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Ingestion.Tests.TestNodes
{
    public sealed class RecordingBulkIndexNode : INode
    {
        private readonly ChannelWriter<Envelope<IndexOperation>> _deadLetterOutput;
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private readonly ChannelReader<BatchEnvelope<IndexOperation>> _input;
        private readonly ChannelWriter<Envelope<IndexOperation>> _successOutput;
        private Task? _completion;

        public RecordingBulkIndexNode(string name, ChannelReader<BatchEnvelope<IndexOperation>> input, ChannelWriter<Envelope<IndexOperation>> successOutput, ChannelWriter<Envelope<IndexOperation>> deadLetterOutput, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            _input = input;
            _successOutput = successOutput;
            _deadLetterOutput = deadLetterOutput;
            _fatalErrorReporter = fatalErrorReporter;
        }

        public string Name { get; }

        public List<Envelope<IndexOperation>> Received { get; } = new();

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
                        foreach (var envelope in batch.Items)
                        {
                            Received.Add(envelope);
                            await _successOutput.WriteAsync(envelope, cancellationToken)
                                                .ConfigureAwait(false);
                        }
                    }
                }

                _successOutput.TryComplete();
                _deadLetterOutput.TryComplete();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _successOutput.TryComplete();
                _deadLetterOutput.TryComplete();
            }
            catch (Exception ex)
            {
                _successOutput.TryComplete(ex);
                _deadLetterOutput.TryComplete(ex);
                _fatalErrorReporter?.ReportFatal(Name, ex);
                throw;
            }
        }
    }
}
