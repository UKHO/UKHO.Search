using UKHO.Search.Ingestion.Providers;

namespace UKHO.Search.Services.Ingestion.Providers
{
    public interface IIngestionProviderService
    {
        IEnumerable<IIngestionDataProvider> GetAllProviders();

        IIngestionDataProvider GetProvider(string name);
    }
}