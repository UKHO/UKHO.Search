using System.Threading.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;

namespace UKHO.Search.Ingestion.Tests.TestNodes
{
    public sealed class BlockingEnvelopeSinkNode<TPayload> : SinkNodeBase<Envelope<TPayload>>
    {
        private readonly int _blockAfterCount;
        private readonly object _gate = new();
        private readonly List<Envelope<TPayload>> _items = new();
        private readonly SemaphoreSlim _receivedSignal = new(0);
        private readonly TaskCompletionSource _releaseGate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingEnvelopeSinkNode(string name, ChannelReader<Envelope<TPayload>> input, int blockAfterCount) : base(name, input)
        {
            _blockAfterCount = blockAfterCount;
        }

        public IReadOnlyList<Envelope<TPayload>> Items
        {
            get
            {
                lock (_gate)
                {
                    return _items.ToArray();
                }
            }
        }

        public void ReleaseBlocking()
        {
            _releaseGate.TrySetResult();
        }

        public async Task WaitForCountAsync(int expectedCount, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);

            while (true)
            {
                lock (_gate)
                {
                    if (_items.Count >= expectedCount)
                    {
                        return;
                    }
                }

                await _receivedSignal.WaitAsync(cts.Token)
                                     .ConfigureAwait(false);
            }
        }

        protected override async ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _items.Add(item);
            }

            _receivedSignal.Release();

            if (_blockAfterCount > 0)
            {
                var shouldBlock = false;
                lock (_gate)
                {
                    shouldBlock = _items.Count == _blockAfterCount;
                }

                if (shouldBlock)
                {
                    await _releaseGate.Task.WaitAsync(cancellationToken)
                                      .ConfigureAwait(false);
                }
            }
        }
    }
}