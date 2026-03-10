using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class CancellationIdleTests
    {
        [Fact]
        public async Task Cancelling_supervisor_completes_idle_nodebase_nodes_without_input_completion()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var supervisor = new PipelineSupervisor(cts.Token);

            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var validate = new ValidateNode<int>("validate", input.Reader, output.Writer, fatalErrorReporter: supervisor);

            supervisor.AddNode(validate);

            await supervisor.StartAsync();

            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            supervisor.Cancel();

            await supervisor.Completion.WaitAsync(cts.Token);
            supervisor.CancellationToken.IsCancellationRequested.ShouldBeTrue();
        }

        [Fact]
        public async Task Cancelling_supervisor_completes_idle_sink_nodes_without_input_completion()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var supervisor = new PipelineSupervisor(cts.Token);

            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var deadLetter = new DeadLetterSinkNode<int>("dead-letter", input.Reader, Path.Combine(Path.GetTempPath(), "ukho-search", Guid.NewGuid()
                                                                                                                                          .ToString("N"), "idle.jsonl"), true, fatalErrorReporter: supervisor);

            supervisor.AddNode(deadLetter);

            await supervisor.StartAsync();

            await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            supervisor.Cancel();

            await supervisor.Completion.WaitAsync(cts.Token);
            supervisor.CancellationToken.IsCancellationRequested.ShouldBeTrue();
        }
    }
}