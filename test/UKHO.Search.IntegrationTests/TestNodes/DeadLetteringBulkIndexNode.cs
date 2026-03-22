using System.Threading.Channels;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Ingestion.Tests.TestNodes
{
    public sealed class DeadLetteringBulkIndexNode : INode
    {
        private readonly ChannelWriter<Envelope<IndexOperation>> _deadLetterOutput;
        private readonly IPipelineFatalErrorReporter? _fatalErrorReporter;
        private readonly ChannelReader<BatchEnvelope<IndexOperation>> _input;
        private readonly ChannelWriter<Envelope<IndexOperation>> _successOutput;
        private Task? _completion;

        public DeadLetteringBulkIndexNode(string name, ChannelReader<BatchEnvelope<IndexOperation>> input, ChannelWriter<Envelope<IndexOperation>> successOutput, ChannelWriter<Envelope<IndexOperation>> deadLetterOutput, IPipelineFatalErrorReporter? fatalErrorReporter = null)
        {
            Name = name;
            _input = input;
            _successOutput = successOutput;
            _deadLetterOutput = deadLetterOutput;
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
            try
            {
                while (await _input.WaitToReadAsync(cancellationToken)
                                   .ConfigureAwait(false))
                {
                    while (_input.TryRead(out var batch))
                    {
                        foreach (var envelope in batch.Items)
                        {
                            envelope.MarkFailed(new PipelineError
                            {
                                Category = PipelineErrorCategory.BulkIndex,
                                Code = "BULK_INDEX_ERROR",
                                Message = "Bulk index failed.",
                                ExceptionType = null,
                                ExceptionMessage = null,
                                StackTrace = null,
                                IsTransient = false,
                                OccurredAtUtc = DateTimeOffset.UtcNow,
                                NodeName = Name,
                                Details = new Dictionary<string, string>()
                            });

                            await _deadLetterOutput.WriteAsync(envelope, cancellationToken)
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