using System.Threading.Channels;

namespace UKHO.Search.Pipelines.Channels
{
    public sealed class CountingChannelWriter<T> : ChannelWriter<T>
    {
        private readonly Action _increment;
        private readonly ChannelWriter<T> _inner;

        public CountingChannelWriter(ChannelWriter<T> inner, Action increment)
        {
            _inner = inner;
            _increment = increment;
        }

        public override bool TryComplete(Exception? error = null)
        {
            return _inner.TryComplete(error);
        }

        public override bool TryWrite(T item)
        {
            if (_inner.TryWrite(item))
            {
                _increment();
                return true;
            }

            return false;
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
        {
            return _inner.WaitToWriteAsync(cancellationToken);
        }

        public override ValueTask WriteAsync(T item, CancellationToken cancellationToken = default)
        {
            var write = _inner.WriteAsync(item, cancellationToken);
            if (write.IsCompletedSuccessfully)
            {
                _increment();
                return write;
            }

            return AwaitWriteAsync(write);
        }

        private async ValueTask AwaitWriteAsync(ValueTask write)
        {
            await write.ConfigureAwait(false);
            _increment();
        }
    }
}