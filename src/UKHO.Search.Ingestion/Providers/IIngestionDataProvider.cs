using UKHO.Search.Ingestion.Contracts;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Ingestion.Providers
{
    public interface IIngestionDataProvider
    {
        string Name { get; }

        ValueTask<IngestionRequest> DeserializeIngestionRequestAsync(string messageText, CancellationToken cancellationToken = default);

        ValueTask ProcessIngestionRequestAsync(Envelope<IngestionRequest> envelope, CancellationToken cancellationToken = default);
    }
}