namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public interface IQueueClient
    {
        ValueTask CreateIfNotExistsAsync(CancellationToken cancellationToken);

        ValueTask<IReadOnlyList<QueueReceivedMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken);

        ValueTask SendMessageAsync(string messageText, CancellationToken cancellationToken);

        ValueTask DeleteMessageAsync(string messageId, string popReceipt, CancellationToken cancellationToken);

        ValueTask<QueueUpdateReceipt> UpdateMessageAsync(string messageId, string popReceipt, string messageText, TimeSpan visibilityTimeout, CancellationToken cancellationToken);
    }
}