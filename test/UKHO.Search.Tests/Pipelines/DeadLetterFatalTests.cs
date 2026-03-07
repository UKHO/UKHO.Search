using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class DeadLetterFatalTests
    {
        [Fact]
        public async Task Dead_letter_can_be_configured_to_be_fatal_if_it_cannot_persist()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var deadLetter = new DeadLetterSinkNode<int>("dead-letter", input.Reader, string.Empty, true, fatalErrorReporter: supervisor);

            supervisor.AddNode(deadLetter);
            await supervisor.StartAsync();

            var env = new Envelope<int>("key-0", 1);
            env.MarkFailed(new PipelineError
            {
                Category = PipelineErrorCategory.Validation,
                Code = "TEST",
                Message = "test",
                ExceptionType = null,
                ExceptionMessage = null,
                StackTrace = null,
                IsTransient = false,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                NodeName = "test",
                Details = new Dictionary<string, string>()
            });

            await input.Writer.WriteAsync(env, cts.Token);
            input.Writer.TryComplete();

            await Should.ThrowAsync<ArgumentException>(async () => await supervisor.Completion.WaitAsync(cts.Token));

            supervisor.CancellationToken.IsCancellationRequested.ShouldBeTrue();
            supervisor.FatalNodeName.ShouldBe("dead-letter");
        }
    }
}