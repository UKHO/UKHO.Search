using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class DeadLetterSinkNodeTests
    {
        [Fact]
        public async Task Non_fatal_dead_letter_persist_failures_are_swallowed_and_pipeline_completes()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var deadLetter = new DeadLetterSinkNode<int>("dead-letter", input.Reader, string.Empty, fatalErrorReporter: supervisor);

            supervisor.AddNode(deadLetter);
            await supervisor.StartAsync();

            for (var i = 0; i < 2; i++)
            {
                var env = new Envelope<int>("key-0", i);
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
            }

            input.Writer.TryComplete();
            await supervisor.Completion.WaitAsync(cts.Token);

            deadLetter.PersistedCount.ShouldBe(0);
        }
    }
}