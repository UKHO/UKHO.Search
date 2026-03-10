using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class DeadLetterConcurrencyTests
    {
        [Fact]
        public async Task Multiple_dead_letter_sinks_can_append_to_the_same_file_safely()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var filePath = Path.Combine(Path.GetTempPath(), "ukho-search", Guid.NewGuid()
                                                                               .ToString("N"), "shared.jsonl");

            var supervisor = new PipelineSupervisor(cts.Token);
            var input1 = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);
            var input2 = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);

            var dl1 = new DeadLetterSinkNode<int>("dl-1", input1.Reader, filePath, true, fatalErrorReporter: supervisor);
            var dl2 = new DeadLetterSinkNode<int>("dl-2", input2.Reader, filePath, true, fatalErrorReporter: supervisor);

            supervisor.AddNode(dl1);
            supervisor.AddNode(dl2);
            await supervisor.StartAsync();

            for (var i = 0; i < 5; i++)
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

                await input1.Writer.WriteAsync(env, cts.Token);
                await input2.Writer.WriteAsync(env, cts.Token);
            }

            input1.Writer.TryComplete();
            input2.Writer.TryComplete();

            await supervisor.Completion.WaitAsync(cts.Token);

            var lines = await File.ReadAllLinesAsync(filePath, cts.Token);
            lines.Length.ShouldBe(10);
        }
    }
}