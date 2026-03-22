using UKHO.Search.Ingestion.Providers;

namespace UKHO.Search.Ingestion.Tests.TestProviders
{
    public sealed class RecordingIngestionDataProviderFactory : IIngestionDataProviderFactory
    {
        public RecordingIngestionDataProviderFactory(string name, string queueName, RecordingIngestionDataProvider provider)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
            ArgumentNullException.ThrowIfNull(provider);

            Name = name;
            QueueName = queueName;
            Provider = provider;
        }

        public RecordingIngestionDataProvider Provider { get; }

        public string Name { get; }

        public string QueueName { get; }

        public IIngestionDataProvider CreateProvider()
        {
            return Provider;
        }
    }
}