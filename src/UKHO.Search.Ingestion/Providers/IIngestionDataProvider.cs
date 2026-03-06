namespace UKHO.Search.Ingestion.Providers
{
    public interface IIngestionDataProvider
    {
        string Name { get; }

        string QueueName { get; }
    }
}