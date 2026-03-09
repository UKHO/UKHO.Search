using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Ingestion.Tests.TestProviders
{
    public sealed class SingleProviderService : IIngestionProviderService
    {
        private readonly IIngestionDataProviderFactory _factory;

        public SingleProviderService(IIngestionDataProviderFactory factory)
        {
            _factory = factory;
        }

        public IEnumerable<IIngestionDataProviderFactory> GetAllProviders()
        {
            return new[] { _factory };
        }

        public IIngestionDataProviderFactory GetProvider(string name)
        {
            return _factory;
        }
    }
}