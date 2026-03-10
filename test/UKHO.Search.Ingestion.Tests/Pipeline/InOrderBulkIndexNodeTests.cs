using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class InOrderBulkIndexNodeTests
    {
        [Fact]
        public async Task Retries_transient_failures_inline_and_increments_attempt()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var input = BoundedChannelFactory.Create<BatchEnvelope<IndexOperation>>(1, true, true);
            var success = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var envelope = new Envelope<IndexOperation>("doc-1", new DeleteOperation("doc-1"));

            var client = new SequencedBulkClient(new[]
            {
                new BulkIndexResponse
                {
                    Items = new[] { new BulkIndexItemResult { MessageId = envelope.MessageId, StatusCode = 503, ErrorType = "unavailable", ErrorReason = "transient" } }
                },
                new BulkIndexResponse
                {
                    Items = new[] { new BulkIndexItemResult { MessageId = envelope.MessageId, StatusCode = 200 } }
                }
            });

            var node = new InOrderBulkIndexNode("bulk", input.Reader, client, success.Writer, deadLetter.Writer, 3, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, static (_, _) => Task.CompletedTask);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new BatchEnvelope<IndexOperation>
            {
                BatchId = Guid.NewGuid(),
                PartitionId = 0,
                Items = new[] { envelope },
                CreatedUtc = DateTimeOffset.UtcNow,
                FlushedUtc = DateTimeOffset.UtcNow
            }, cts.Token);
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(cts.Token);

            var successEnvelope = await success.Reader.ReadAsync(cts.Token);
            successEnvelope.Attempt.ShouldBe(2);
            successEnvelope.Status.ShouldBe(MessageStatus.Ok);

            deadLetter.Reader.TryRead(out var _)
                      .ShouldBeFalse();
        }

        [Fact]
        public async Task Routes_non_transient_failures_to_deadletter_without_retry()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var input = BoundedChannelFactory.Create<BatchEnvelope<IndexOperation>>(1, true, true);
            var success = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var envelope = new Envelope<IndexOperation>("doc-1", new DeleteOperation("doc-1"));

            var client = new SequencedBulkClient(new[]
            {
                new BulkIndexResponse
                {
                    Items = new[] { new BulkIndexItemResult { MessageId = envelope.MessageId, StatusCode = 400, ErrorType = "bad_request", ErrorReason = "permanent" } }
                }
            });

            var node = new InOrderBulkIndexNode("bulk", input.Reader, client, success.Writer, deadLetter.Writer, 3, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, static (_, _) => Task.CompletedTask);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new BatchEnvelope<IndexOperation>
            {
                BatchId = Guid.NewGuid(),
                PartitionId = 0,
                Items = new[] { envelope },
                CreatedUtc = DateTimeOffset.UtcNow,
                FlushedUtc = DateTimeOffset.UtcNow
            }, cts.Token);
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(cts.Token);

            success.Reader.TryRead(out var _)
                   .ShouldBeFalse();

            var failed = await deadLetter.Reader.ReadAsync(cts.Token);
            failed.Status.ShouldBe(MessageStatus.Failed);
            failed.Error.ShouldNotBeNull();
            failed.Error!.Category.ShouldBe(PipelineErrorCategory.BulkIndex);
            failed.Error.IsTransient.ShouldBeFalse();
        }

        [Fact]
        public async Task Lane_is_blocked_while_waiting_to_retry_a_transient_failure()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var input = BoundedChannelFactory.Create<BatchEnvelope<IndexOperation>>(4, true, true);
            var success = BoundedChannelFactory.Create<Envelope<IndexOperation>>(4, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(4, true, true);

            var e1 = new Envelope<IndexOperation>("doc-1", new DeleteOperation("doc-1"));
            var e2 = new Envelope<IndexOperation>("doc-2", new DeleteOperation("doc-2"));

            var delayEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseDelay = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            Task Delay(TimeSpan _, CancellationToken ct)
            {
                delayEntered.TrySetResult();
                return releaseDelay.Task.WaitAsync(ct);
            }

            var client = new KeyedBulkClient(new Dictionary<Guid, Queue<BulkIndexResponse>>
            {
                [e1.MessageId] = new(new[]
                {
                    new BulkIndexResponse
                    {
                        Items = new[] { new BulkIndexItemResult { MessageId = e1.MessageId, StatusCode = 503, ErrorType = "unavailable", ErrorReason = "transient" } }
                    },
                    new BulkIndexResponse
                    {
                        Items = new[] { new BulkIndexItemResult { MessageId = e1.MessageId, StatusCode = 200 } }
                    }
                }),
                [e2.MessageId] = new(new[]
                {
                    new BulkIndexResponse
                    {
                        Items = new[] { new BulkIndexItemResult { MessageId = e2.MessageId, StatusCode = 200 } }
                    }
                })
            });

            var node = new InOrderBulkIndexNode("bulk", input.Reader, client, success.Writer, deadLetter.Writer, 3, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), TimeSpan.Zero, Delay);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new BatchEnvelope<IndexOperation>
            {
                BatchId = Guid.NewGuid(),
                PartitionId = 0,
                Items = new[] { e1 },
                CreatedUtc = DateTimeOffset.UtcNow,
                FlushedUtc = DateTimeOffset.UtcNow
            }, cts.Token);

            await input.Writer.WriteAsync(new BatchEnvelope<IndexOperation>
            {
                BatchId = Guid.NewGuid(),
                PartitionId = 0,
                Items = new[] { e2 },
                CreatedUtc = DateTimeOffset.UtcNow,
                FlushedUtc = DateTimeOffset.UtcNow
            }, cts.Token);

            await delayEntered.Task.WaitAsync(cts.Token);

            client.Calls.Select(c => c.Key)
                  .ToArray()
                  .ShouldBe(new[] { "doc-1" });

            releaseDelay.TrySetResult();

            input.Writer.TryComplete();
            await node.Completion.WaitAsync(cts.Token);

            client.Calls.Select(c => c.Key)
                  .ToArray()
                  .ShouldBe(new[] { "doc-1", "doc-1", "doc-2" });
        }

        private sealed class SequencedBulkClient : IBulkIndexClient<IndexOperation>
        {
            private readonly Queue<BulkIndexResponse> _responses;

            public SequencedBulkClient(IEnumerable<BulkIndexResponse> responses)
            {
                _responses = new Queue<BulkIndexResponse>(responses);
            }

            public ValueTask<BulkIndexResponse> BulkIndexAsync(BulkIndexRequest<IndexOperation> request, CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(_responses.Dequeue());
            }
        }

        private sealed class KeyedBulkClient : IBulkIndexClient<IndexOperation>
        {
            private readonly Dictionary<Guid, Queue<BulkIndexResponse>> _responses;

            public KeyedBulkClient(Dictionary<Guid, Queue<BulkIndexResponse>> responses)
            {
                _responses = responses;
            }

            public List<(string Key, Guid MessageId)> Calls { get; } = new();

            public ValueTask<BulkIndexResponse> BulkIndexAsync(BulkIndexRequest<IndexOperation> request, CancellationToken cancellationToken)
            {
                var first = request.Items[0];
                Calls.Add((first.Key, first.MessageId));

                var queue = _responses[first.MessageId];
                return ValueTask.FromResult(queue.Dequeue());
            }
        }
    }
}