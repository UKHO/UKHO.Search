using System.Threading.Channels;

namespace UKHO.Search.Pipelines.Channels
{
    public sealed class CountingChannel<T>
    {
        private readonly Channel<T> _inner;
        private readonly CountingChannelReader<T> _reader;
        private readonly CountingChannelWriter<T> _writer;
        private long _depth;

        public CountingChannel(Channel<T> inner)
        {
            _inner = inner;
            _reader = new CountingChannelReader<T>(inner.Reader, GetDepth, Decrement);
            _writer = new CountingChannelWriter<T>(inner.Writer, Increment);
        }

        public ChannelReader<T> Reader => _reader;

        public ChannelWriter<T> Writer => _writer;

        private long GetDepth()
        {
            return Volatile.Read(ref _depth);
        }

        private void Increment()
        {
            Interlocked.Increment(ref _depth);
        }

        private void Decrement()
        {
            var newValue = Interlocked.Decrement(ref _depth);
            if (newValue < 0)
            {
                Interlocked.Exchange(ref _depth, 0);
            }
        }
    }
}