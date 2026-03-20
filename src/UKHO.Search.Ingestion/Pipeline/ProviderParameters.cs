namespace UKHO.Search.Ingestion.Pipeline
{
    public sealed class ProviderParameters
    {
        public ProviderParameters(string provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);

            Provider = provider.Trim();
        }

        public string Provider { get; }
    }
}
