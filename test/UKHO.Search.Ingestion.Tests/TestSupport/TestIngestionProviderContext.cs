using UKHO.Search.Ingestion.Rules;

namespace UKHO.Search.Ingestion.Tests.TestSupport
{
    internal sealed class TestIngestionProviderContext : IIngestionProviderContext
    {
        public string? ProviderName { get; set; }
    }
}
