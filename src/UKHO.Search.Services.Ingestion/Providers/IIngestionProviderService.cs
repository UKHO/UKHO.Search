using UKHO.Search.Ingestion.Providers;

namespace UKHO.Search.Services.Ingestion.Providers
{
    public interface IIngestionProviderService
    {
        IEnumerable<IIngestionDataProviderFactory> GetAllProviders();

        IIngestionDataProviderFactory GetProvider(string name);
    }
}