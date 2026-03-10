namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public interface IQueueMessageAcker
    {
        ValueTask DeleteAsync(CancellationToken cancellationToken);

        ValueTask UpdateVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken);

        ValueTask MoveToPoisonAsync(IQueueClient poisonQueue, string poisonMessageBody, CancellationToken cancellationToken);
    }
}