using System.Threading.Channels;

namespace UKHO.Search.Pipelines.Channels
{
    public sealed class CountingChannelReader<T> : ChannelReader<T>, IQueueDepthProvider
    {
        private readonly Action _decrement;
        private readonly Func<long> _getDepth;
        private readonly ChannelReader<T> _inner;

        public CountingChannelReader(ChannelReader<T> inner, Func<long> getDepth, Action decrement)
        {
            _inner = inner;
            _getDepth = getDepth;
            _decrement = decrement;
        }

        public override Task Completion => _inner.Completion;

        public long QueueDepth => _getDepth();

        public override bool TryRead(out T item)
        {
            if (_inner.TryRead(out item))
            {
                _decrement();
                return true;
            }

            return false;
        }

        public override bool TryPeek(out T item)
        {
            return _inner.TryPeek(out item);
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            return _inner.WaitToReadAsync(cancellationToken);
        }

        public override ValueTask<T> ReadAsync(CancellationToken cancellationToken = default)
        {
            var read = _inner.ReadAsync(cancellationToken);
            if (read.IsCompletedSuccessfully)
            {
                _decrement();
                return read;
            }

            return AwaitReadAsync(read);
        }

        private async ValueTask<T> AwaitReadAsync(ValueTask<T> read)
        {
            var item = await read.ConfigureAwait(false);
            _decrement();
            return item;
        }
    }
}