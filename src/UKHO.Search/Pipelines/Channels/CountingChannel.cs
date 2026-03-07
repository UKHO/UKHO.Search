using System.Threading.Channels;

namespace UKHO.Search.Pipelines.Channels
{
    public sealed class CountingChannel<T>
    {
        private readonly Channel<T> inner;
        private readonly CountingChannelReader<T> reader;
        private readonly CountingChannelWriter<T> writer;
        private long depth;

        public CountingChannel(Channel<T> inner)
        {
            this.inner = inner;
            reader = new CountingChannelReader<T>(inner.Reader, GetDepth, Decrement);
            writer = new CountingChannelWriter<T>(inner.Writer, Increment);
        }

        public ChannelReader<T> Reader => reader;

        public ChannelWriter<T> Writer => writer;

        private long GetDepth()
        {
            return Volatile.Read(ref depth);
        }

        private void Increment()
        {
            Interlocked.Increment(ref depth);
        }

        private void Decrement()
        {
            var newValue = Interlocked.Decrement(ref depth);
            if (newValue < 0)
            {
                Interlocked.Exchange(ref depth, 0);
            }
        }
    }
}