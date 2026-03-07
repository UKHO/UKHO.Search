using System.Threading.Channels;

namespace UKHO.Search.Pipelines.Channels
{
    public sealed class CountingChannelWriter<T> : ChannelWriter<T>
    {
        private readonly Action increment;
        private readonly ChannelWriter<T> inner;

        public CountingChannelWriter(ChannelWriter<T> inner, Action increment)
        {
            this.inner = inner;
            this.increment = increment;
        }

        public override bool TryComplete(Exception? error = null)
        {
            return inner.TryComplete(error);
        }

        public override bool TryWrite(T item)
        {
            if (inner.TryWrite(item))
            {
                increment();
                return true;
            }

            return false;
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
        {
            return inner.WaitToWriteAsync(cancellationToken);
        }

        public override ValueTask WriteAsync(T item, CancellationToken cancellationToken = default)
        {
            var write = inner.WriteAsync(item, cancellationToken);
            if (write.IsCompletedSuccessfully)
            {
                increment();
                return write;
            }

            return AwaitWriteAsync(write);
        }

        private async ValueTask AwaitWriteAsync(ValueTask write)
        {
            await write.ConfigureAwait(false);
            increment();
        }
    }
}