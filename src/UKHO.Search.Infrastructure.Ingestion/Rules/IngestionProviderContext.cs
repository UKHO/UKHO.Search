using UKHO.Search.Ingestion.Rules;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class IngestionProviderContext : IIngestionProviderContext
    {
        public string? ProviderName { get; set; }
    }
}
