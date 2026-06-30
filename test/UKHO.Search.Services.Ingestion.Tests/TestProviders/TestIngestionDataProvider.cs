using System.Threading.Channels;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Ingestion.Contracts;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Services.Ingestion.Tests.TestProviders
{
    internal sealed class TestIngestionDataProvider : IIngestionDataProvider
    {
        public string Name => "test-provider";

        public ValueTask<IngestionRequest> DeserializeIngestionRequestAsync(string messageText, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask ProcessIngestionRequestAsync(Envelope<IngestionRequest> envelope, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
