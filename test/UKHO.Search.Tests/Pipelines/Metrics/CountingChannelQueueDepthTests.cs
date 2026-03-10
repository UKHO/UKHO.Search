using Shouldly;
using UKHO.Search.Pipelines.Channels;
using Xunit;

namespace UKHO.Search.Tests.Pipelines.Metrics
{
    public sealed class CountingChannelQueueDepthTests
    {
        [Fact]
        public async Task Depth_increments_on_write_and_decrements_on_read()
        {
            var channel = BoundedChannelFactory.Create<int>(8, true, true);
            var depth = channel.Reader as IQueueDepthProvider;
            depth.ShouldNotBeNull();

            depth!.QueueDepth.ShouldBe(0);

            await channel.Writer.WriteAsync(1);
            depth.QueueDepth.ShouldBe(1);

            await channel.Writer.WriteAsync(2);
            depth.QueueDepth.ShouldBe(2);

            channel.Reader.TryRead(out var first)
                   .ShouldBeTrue();
            first.ShouldBe(1);
            depth.QueueDepth.ShouldBe(1);

            var second = await channel.Reader.ReadAsync();
            second.ShouldBe(2);
            depth.QueueDepth.ShouldBe(0);

            channel.Reader.TryRead(out var _)
                   .ShouldBeFalse();
            depth.QueueDepth.ShouldBe(0);
        }

        [Fact]
        public async Task Depth_does_not_go_negative_and_eventually_returns_to_zero_after_completion()
        {
            var channel = BoundedChannelFactory.Create<int>(8, true, true);
            var depth = (IQueueDepthProvider)channel.Reader;

            channel.Reader.TryRead(out var _)
                   .ShouldBeFalse();
            depth.QueueDepth.ShouldBe(0);

            for (var i = 0; i < 3; i++)
            {
                await channel.Writer.WriteAsync(i);
            }

            depth.QueueDepth.ShouldBe(3);

            channel.Writer.TryComplete()
                   .ShouldBeTrue();

            while (channel.Reader.TryRead(out var _))
            {
            }

            depth.QueueDepth.ShouldBe(0);
        }
    }
}