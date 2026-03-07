using System.Threading.Channels;

namespace UKHO.Search.Pipelines.Channels
{
    public sealed class CountingChannelReader<T> : ChannelReader<T>, IQueueDepthProvider
    {
        private readonly Action decrement;
        private readonly Func<long> getDepth;
        private readonly ChannelReader<T> inner;

        public CountingChannelReader(ChannelReader<T> inner, Func<long> getDepth, Action decrement)
        {
            this.inner = inner;
            this.getDepth = getDepth;
            this.decrement = decrement;
        }

        public override Task Completion => inner.Completion;

        public long QueueDepth => getDepth();

        public override bool TryRead(out T item)
        {
            if (inner.TryRead(out item))
            {
                decrement();
                return true;
            }

            return false;
        }

        public override bool TryPeek(out T item)
        {
            return inner.TryPeek(out item);
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            return inner.WaitToReadAsync(cancellationToken);
        }

        public override ValueTask<T> ReadAsync(CancellationToken cancellationToken = default)
        {
            var read = inner.ReadAsync(cancellationToken);
            if (read.IsCompletedSuccessfully)
            {
                decrement();
                return read;
            }

            return AwaitReadAsync(read);
        }

        private async ValueTask<T> AwaitReadAsync(ValueTask<T> read)
        {
            var item = await read.ConfigureAwait(false);
            decrement();
            return item;
        }
    }
}