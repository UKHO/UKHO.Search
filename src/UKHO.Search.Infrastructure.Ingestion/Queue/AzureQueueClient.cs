using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public sealed class AzureQueueClient : IQueueClient
    {
        private readonly QueueClient _inner;

        public AzureQueueClient(QueueClient inner)
        {
            _inner = inner;
        }

        public async ValueTask CreateIfNotExistsAsync(CancellationToken cancellationToken)
        {
            _ = await _inner.CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
        }

        public async ValueTask<IReadOnlyList<QueueReceivedMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            var response = await _inner.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken)
                                       .ConfigureAwait(false);

            return response.Value.Select(Map)
                           .ToArray();
        }

        public async ValueTask SendMessageAsync(string messageText, CancellationToken cancellationToken)
        {
            await _inner.SendMessageAsync(messageText, cancellationToken)
                        .ConfigureAwait(false);
        }

        public async ValueTask DeleteMessageAsync(string messageId, string popReceipt, CancellationToken cancellationToken)
        {
            await _inner.DeleteMessageAsync(messageId, popReceipt, cancellationToken)
                        .ConfigureAwait(false);
        }

        public async ValueTask<QueueUpdateReceipt> UpdateMessageAsync(string messageId, string popReceipt, string messageText, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            var response = await _inner.UpdateMessageAsync(messageId, popReceipt, messageText, visibilityTimeout, cancellationToken)
                                       .ConfigureAwait(false);

            return new QueueUpdateReceipt
            {
                PopReceipt = response.Value.PopReceipt,
                NextVisibleOnUtc = response.Value.NextVisibleOn.ToUniversalTime()
            };
        }

        private static QueueReceivedMessage Map(QueueMessage message)
        {
            return new QueueReceivedMessage
            {
                MessageId = message.MessageId,
                PopReceipt = message.PopReceipt,
                DequeueCount = (int)message.DequeueCount,
                MessageText = message.MessageText,
                InsertedOnUtc = message.InsertedOn?.ToUniversalTime(),
                NextVisibleOnUtc = message.NextVisibleOn?.ToUniversalTime()
            };
        }
    }
}