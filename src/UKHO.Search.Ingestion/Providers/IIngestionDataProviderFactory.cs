namespace UKHO.Search.Ingestion.Providers
{
    public interface IIngestionDataProviderFactory
    {
        string Name { get; }

        string QueueName { get; }

        IIngestionDataProvider CreateProvider();
    }
}