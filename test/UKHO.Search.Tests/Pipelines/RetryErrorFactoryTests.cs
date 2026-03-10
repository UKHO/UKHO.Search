using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Retry;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class RetryErrorFactoryTests
    {
        [Fact]
        public async Task Transient_error_retries_and_eventually_succeeds_using_error_factory()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var supervisor = new PipelineSupervisor(cts.Token);

            var input = BoundedChannelFactory.Create<Envelope<int>>(32, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(32, true, true);

            var retryPolicy = new ExponentialBackoffRetryPolicy(5, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), 0);

            var failures = 0;

            ValueTask<int> Transform(int payload, CancellationToken cancellationToken)
            {
                if (payload == 2)
                {
                    failures++;
                    if (failures < 3)
                    {
                        throw new TimeoutException("transient");
                    }
                }

                return ValueTask.FromResult(payload);
            }

            PipelineError CreateError(Envelope<int> envelope, Exception ex)
            {
                return new PipelineError
                {
                    Category = PipelineErrorCategory.Transform,
                    Code = "TRANSFORM_ERROR",
                    Message = "Transform failed.",
                    ExceptionType = ex.GetType()
                                      .FullName,
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    IsTransient = ex is TimeoutException,
                    OccurredAtUtc = DateTimeOffset.UtcNow,
                    NodeName = "retrying-transform",
                    Details = new Dictionary<string, string>()
                };
            }

            var retryNode = new RetryingTransformNode<int, int>("retrying-transform", input.Reader, output.Writer, Transform, retryPolicy, CreateError, forwardFailedToMainOutput: true, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", output.Reader, fatalErrorReporter: supervisor);

            supervisor.AddNode(retryNode);
            supervisor.AddNode(sink);

            await supervisor.StartAsync();

            for (var i = 0; i < 4; i++)
            {
                await input.Writer.WriteAsync(new Envelope<int>("key-0", i), cts.Token);
            }

            input.Writer.TryComplete();
            await supervisor.Completion.WaitAsync(cts.Token);

            var payloads = sink.Items.Select(i => i.Payload)
                               .ToArray();
            payloads.ShouldBe(new[] { 0, 1, 2, 3 });

            var env2 = sink.Items.Single(i => i.Payload == 2);
            env2.Attempt.ShouldBe(3);
            env2.Status.ShouldBe(MessageStatus.Ok);
            env2.Error.ShouldBeNull();
        }

        [Fact]
        public async Task Non_transient_error_fails_fast_and_writes_to_error_output_using_error_factory()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var supervisor = new PipelineSupervisor(cts.Token);

            var input = BoundedChannelFactory.Create<Envelope<int>>(32, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(32, true, true);
            var errors = BoundedChannelFactory.Create<Envelope<int>>(32, true, true);

            var retryPolicy = new ExponentialBackoffRetryPolicy(5, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1), 0);

            ValueTask<int> Transform(int payload, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("boom");
            }

            PipelineError CreateError(Envelope<int> envelope, Exception ex)
            {
                return new PipelineError
                {
                    Category = PipelineErrorCategory.Transform,
                    Code = "TRANSFORM_ERROR",
                    Message = "Transform failed.",
                    ExceptionType = ex.GetType()
                                      .FullName,
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    IsTransient = false,
                    OccurredAtUtc = DateTimeOffset.UtcNow,
                    NodeName = "retrying-transform",
                    Details = new Dictionary<string, string>()
                };
            }

            var retryNode = new RetryingTransformNode<int, int>("retrying-transform", input.Reader, output.Writer, Transform, retryPolicy, CreateError, errors.Writer, false, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", output.Reader, fatalErrorReporter: supervisor);
            var errorSink = new CollectingSinkNode<int>("error-sink", errors.Reader, fatalErrorReporter: supervisor);

            supervisor.AddNode(retryNode);
            supervisor.AddNode(sink);
            supervisor.AddNode(errorSink);

            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 123), cts.Token);

            input.Writer.TryComplete();
            await supervisor.Completion.WaitAsync(cts.Token);

            sink.Items.Count.ShouldBe(0);
            errorSink.Items.Count.ShouldBe(1);
            errorSink.Items[0]
                     .Status.ShouldBe(MessageStatus.Failed);
            errorSink.Items[0]
                     .Error.ShouldNotBeNull();
            errorSink.Items[0].Error!.IsTransient.ShouldBeFalse();
        }
    }
}