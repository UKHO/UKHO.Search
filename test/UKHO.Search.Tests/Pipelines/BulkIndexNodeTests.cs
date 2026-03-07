using Shouldly;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class BulkIndexNodeTests
    {
        [Fact]
        public async Task Classifies_items_into_success_retry_and_permanent_failure()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<BatchEnvelope<int>>(4, true, true);
            var success = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var retry = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var errors = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var e1 = new Envelope<int>("k1", 1);
            var e2 = new Envelope<int>("k2", 2);
            var e3 = new Envelope<int>("k3", 3);

            var client = new FakeBulkIndexClient<int>(new BulkIndexResponse
            {
                Items = new[]
                {
                    new BulkIndexItemResult { MessageId = e1.MessageId, StatusCode = 201 },
                    new BulkIndexItemResult { MessageId = e2.MessageId, StatusCode = 429, ErrorType = "too_many_requests", ErrorReason = "throttled" },
                    new BulkIndexItemResult { MessageId = e3.MessageId, StatusCode = 400, ErrorType = "bad_request", ErrorReason = "invalid" }
                }
            });

            var node = new BulkIndexNode<int>("bulk", input.Reader, client, success.Writer, retry.Writer, errors.Writer);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new BatchEnvelope<int>
            {
                BatchId = Guid.NewGuid(),
                PartitionId = 7,
                Items = new[] { e1, e2, e3 },
                CreatedUtc = DateTimeOffset.UtcNow,
                FlushedUtc = DateTimeOffset.UtcNow
            }, cts.Token);

            input.Writer.TryComplete();
            await node.Completion.WaitAsync(cts.Token);

            var successItem = await success.Reader.ReadAsync(cts.Token);
            successItem.Payload.ShouldBe(1);
            successItem.Status.ShouldBe(MessageStatus.Ok);

            var retryItem = await retry.Reader.ReadAsync(cts.Token);
            retryItem.Payload.ShouldBe(2);
            retryItem.Status.ShouldBe(MessageStatus.Retrying);
            retryItem.Error.ShouldNotBeNull();
            retryItem.Error.IsTransient.ShouldBeTrue();
            retryItem.Error.Category.ShouldBe(PipelineErrorCategory.BulkIndex);

            var errorItem = await errors.Reader.ReadAsync(cts.Token);
            errorItem.Payload.ShouldBe(3);
            errorItem.Status.ShouldBe(MessageStatus.Failed);
            errorItem.Error.ShouldNotBeNull();
            errorItem.Error.IsTransient.ShouldBeFalse();
            errorItem.Error.Category.ShouldBe(PipelineErrorCategory.BulkIndex);
        }

        [Fact]
        public async Task Completes_outputs_on_input_completion()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<BatchEnvelope<int>>(4, true, true);
            var success = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var client = new FakeBulkIndexClient<int>(new BulkIndexResponse { Items = Array.Empty<BulkIndexItemResult>() });

            var node = new BulkIndexNode<int>("bulk", input.Reader, client, success.Writer);

            await node.StartAsync(cts.Token);
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(cts.Token);
            await success.Reader.Completion.WaitAsync(cts.Token);
        }

        private sealed class FakeBulkIndexClient<T> : IBulkIndexClient<T>
        {
            private readonly BulkIndexResponse response;

            public FakeBulkIndexClient(BulkIndexResponse response)
            {
                this.response = response;
            }

            public ValueTask<BulkIndexResponse> BulkIndexAsync(BulkIndexRequest<T> request, CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(response);
            }
        }
    }
}