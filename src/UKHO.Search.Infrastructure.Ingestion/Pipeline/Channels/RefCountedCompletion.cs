using System.Threading.Channels;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Channels
{
    public sealed class RefCountedCompletion
    {
        private Exception? _error;
        private int _remaining;

        public RefCountedCompletion(int writers)
        {
            if (writers <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(writers));
            }

            _remaining = writers;
        }

        public bool TryComplete<T>(ChannelWriter<T> inner, Exception? completeError = null)
        {
            ArgumentNullException.ThrowIfNull(inner);

            if (completeError is not null)
            {
                Interlocked.CompareExchange(ref _error, completeError, null);
            }

            if (Interlocked.Decrement(ref _remaining) == 0)
            {
                return inner.TryComplete(_error);
            }

            return true;
        }
    }
}