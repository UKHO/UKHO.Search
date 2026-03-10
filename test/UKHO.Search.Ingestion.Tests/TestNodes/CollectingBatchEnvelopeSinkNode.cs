using System.Threading.Channels;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;

namespace UKHO.Search.Ingestion.Tests.TestNodes
{
    public sealed class CollectingBatchEnvelopeSinkNode<TPayload> : SinkNodeBase<BatchEnvelope<TPayload>>
    {
        private readonly object _gate = new();
        private readonly List<Envelope<TPayload>> _items = new();
        private readonly SemaphoreSlim _receivedSignal = new(0);

        public CollectingBatchEnvelopeSinkNode(string name, ChannelReader<BatchEnvelope<TPayload>> input) : base(name, input, cancellationMode: CancellationMode.Drain)
        {
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

        protected override ValueTask HandleItemAsync(BatchEnvelope<TPayload> batch, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                _items.AddRange(batch.Items);
            }

            _receivedSignal.Release(batch.Items.Count);
            return ValueTask.CompletedTask;
        }
    }
}