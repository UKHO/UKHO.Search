using System.Threading.Channels;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline.Channels
{
    public sealed class RefCountedChannelWriter<T> : ChannelWriter<T>
    {
        private readonly RefCountedCompletion _completion;
        private readonly ChannelWriter<T> _inner;

        public RefCountedChannelWriter(ChannelWriter<T> inner, RefCountedCompletion completion)
        {
            _inner = inner;
            _completion = completion;
        }

        public override bool TryComplete(Exception? error = null)
        {
            return _completion.TryComplete(_inner, error);
        }

        public override bool TryWrite(T item)
        {
            return _inner.TryWrite(item);
        }

        public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
        {
            return _inner.WaitToWriteAsync(cancellationToken);
        }

        public override ValueTask WriteAsync(T item, CancellationToken cancellationToken = default)
        {
            return _inner.WriteAsync(item, cancellationToken);
        }
    }
}