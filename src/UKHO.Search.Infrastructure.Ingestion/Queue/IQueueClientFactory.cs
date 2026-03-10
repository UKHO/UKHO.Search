namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public interface IQueueClientFactory
    {
        IQueueClient GetQueueClient(string queueName);
    }
}