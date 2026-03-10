using System.Threading.Channels;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Ingestion.Providers.FileShare
{
    internal sealed class IngestionRequestIngressChannel
    {
        private readonly CountingChannel<Envelope<IngestionRequest>> _channel;

        public IngestionRequestIngressChannel(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Ingress channel capacity must be > 0.");
            }

            _channel = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(capacity, true, false);
        }

        public ChannelReader<Envelope<IngestionRequest>> Reader => _channel.Reader;

        public ChannelWriter<Envelope<IngestionRequest>> Writer => _channel.Writer;
    }
}