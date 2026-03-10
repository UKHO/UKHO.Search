using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class ValidateNodeRoutingTests
    {
        [Fact]
        public async Task Failed_messages_can_be_routed_to_error_output_without_leaking_to_main_output()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);
            var errors = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);

            var validate = new ValidateNode<int>("validate", input.Reader, output.Writer, errors.Writer, false, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", output.Reader, fatalErrorReporter: supervisor);
            var errorSink = new CollectingSinkNode<int>("error-sink", errors.Reader, fatalErrorReporter: supervisor);

            supervisor.AddNode(validate);
            supervisor.AddNode(sink);
            supervisor.AddNode(errorSink);

            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>(" ", 1), cts.Token);
            await input.Writer.WriteAsync(new Envelope<int>("key-0", 2), cts.Token);
            input.Writer.TryComplete();

            await supervisor.Completion.WaitAsync(cts.Token);

            sink.Items.Select(i => i.Payload)
                .ToArray()
                .ShouldBe(new[] { 2 });
            errorSink.Items.Count.ShouldBe(1);
            errorSink.Items[0]
                     .Status.ShouldBe(MessageStatus.Failed);
            errorSink.Items[0]
                     .Error.ShouldNotBeNull();
            errorSink.Items[0].Error!.Category.ShouldBe(PipelineErrorCategory.Validation);
        }

        [Fact]
        public async Task Already_failed_envelopes_are_forwarded_without_overwriting_error_details()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var validate = new ValidateNode<int>("validate", input.Reader, output.Writer, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", output.Reader, fatalErrorReporter: supervisor);

            supervisor.AddNode(validate);
            supervisor.AddNode(sink);

            await supervisor.StartAsync();

            var upstreamFailed = new Envelope<int>(string.Empty, 1).MarkFailed(new PipelineError
            {
                Category = PipelineErrorCategory.Unknown,
                Code = "UPSTREAM",
                Message = "upstream",
                ExceptionType = null,
                ExceptionMessage = null,
                StackTrace = null,
                IsTransient = false,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                NodeName = "upstream",
                Details = new Dictionary<string, string>()
            });

            await input.Writer.WriteAsync(upstreamFailed, cts.Token);
            input.Writer.TryComplete();

            await supervisor.Completion.WaitAsync(cts.Token);

            sink.Items.Count.ShouldBe(1);
            sink.Items[0]
                .Status.ShouldBe(MessageStatus.Failed);
            sink.Items[0]
                .Error.ShouldNotBeNull();
            sink.Items[0].Error!.Code.ShouldBe("UPSTREAM");
        }
    }
}