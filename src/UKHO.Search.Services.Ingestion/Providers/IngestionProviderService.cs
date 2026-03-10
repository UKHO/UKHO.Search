using UKHO.Search.Ingestion.Providers;

namespace UKHO.Search.Services.Ingestion.Providers
{
    public sealed class IngestionProviderService : IIngestionProviderService
    {
        private readonly IReadOnlyDictionary<string, IIngestionDataProviderFactory> _providers;

        public IngestionProviderService(IEnumerable<IIngestionDataProviderFactory> providers)
        {
            ArgumentNullException.ThrowIfNull(providers);

            _providers = providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<IIngestionDataProviderFactory> GetAllProviders()
        {
            return _providers.Values;
        }

        public IIngestionDataProviderFactory GetProvider(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            if (!_providers.TryGetValue(name, out var provider))
            {
                throw new KeyNotFoundException($"No ingestion provider registered with name '{name}'.");
            }

            return provider;
        }
    }
}