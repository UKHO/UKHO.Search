using System.Threading.Channels;
using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class RouteNodeTests
    {
        [Fact]
        public async Task Routes_messages_to_expected_outputs()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);
            var even = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);
            var odd = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);
            var errors = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);

            static string GetRoute(Envelope<int> env)
            {
                return env.Payload % 2 == 0 ? "even" : "odd";
            }

            var routes = new Dictionary<string, ChannelWriter<Envelope<int>>>
            {
                ["even"] = even.Writer,
                ["odd"] = odd.Writer
            };

            var node = new RouteNode<int>("route", input.Reader, routes, GetRoute, errors.Writer);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new Envelope<int>("k", 2), cts.Token);
            await input.Writer.WriteAsync(new Envelope<int>("k", 3), cts.Token);
            input.Writer.TryComplete();

            (await even.Reader.ReadAsync(cts.Token)).Payload.ShouldBe(2);
            (await odd.Reader.ReadAsync(cts.Token)).Payload.ShouldBe(3);
            errors.Reader.TryRead(out var _)
                  .ShouldBeFalse();

            await node.Completion.WaitAsync(cts.Token);
            await even.Reader.Completion.WaitAsync(cts.Token);
            await odd.Reader.Completion.WaitAsync(cts.Token);
            await errors.Reader.Completion.WaitAsync(cts.Token);
        }

        [Fact]
        public async Task Missing_route_marks_message_failed_and_sends_to_error_output()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);
            var errors = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);

            static string GetRoute(Envelope<int> env)
            {
                return "missing";
            }

            var routes = new Dictionary<string, ChannelWriter<Envelope<int>>>
            {
                ["ok"] = output.Writer
            };

            var node = new RouteNode<int>("route", input.Reader, routes, GetRoute, errors.Writer);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new Envelope<int>("k", 1), cts.Token);
            input.Writer.TryComplete();

            var failed = await errors.Reader.ReadAsync(cts.Token);
            failed.Status.ShouldBe(MessageStatus.Failed);
            failed.Error.ShouldNotBeNull();
            failed.Error.Category.ShouldBe(PipelineErrorCategory.Validation);
            failed.Error.Code.ShouldBe("ROUTE_NOT_FOUND");
            failed.Error.NodeName.ShouldBe("route");

            output.Reader.TryRead(out var _)
                  .ShouldBeFalse();

            await node.Completion.WaitAsync(cts.Token);
        }

        [Fact]
        public async Task Missing_route_faults_when_error_output_not_configured()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var input = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(8, true, true);

            static string GetRoute(Envelope<int> env)
            {
                return "missing";
            }

            var routes = new Dictionary<string, ChannelWriter<Envelope<int>>>
            {
                ["ok"] = output.Writer
            };

            var node = new RouteNode<int>("route", input.Reader, routes, GetRoute);

            await node.StartAsync(cts.Token);

            await input.Writer.WriteAsync(new Envelope<int>("k", 1), cts.Token);
            input.Writer.TryComplete();

            await Should.ThrowAsync<KeyNotFoundException>(async () => await node.Completion.WaitAsync(cts.Token));
        }
    }
}