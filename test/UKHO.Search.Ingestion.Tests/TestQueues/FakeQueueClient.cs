using System.Collections.Concurrent;
using UKHO.Search.Infrastructure.Ingestion.Queue;

namespace UKHO.Search.Ingestion.Tests.TestQueues
{
    public sealed class FakeQueueClient : IQueueClient
    {
        private readonly ConcurrentQueue<QueueReceivedMessage> _messages = new();

        public FakeQueueClient(string queueName)
        {
            QueueName = queueName;
        }

        public string QueueName { get; }

        public List<string> SentMessages { get; } = new();

        public List<(string MessageId, string PopReceipt)> DeletedMessages { get; } = new();

        public List<(string MessageId, string PopReceipt, TimeSpan VisibilityTimeout)> UpdatedMessages { get; } = new();

        public int CreateCallCount { get; private set; }

        public TaskCompletionSource CreateCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SendCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource DeleteCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask CreateIfNotExistsAsync(CancellationToken cancellationToken)
        {
            CreateCallCount++;
            CreateCalled.TrySetResult();
            return ValueTask.CompletedTask;
        }

        public ValueTask<IReadOnlyList<QueueReceivedMessage>> ReceiveMessagesAsync(int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            var list = new List<QueueReceivedMessage>(maxMessages);
            while (list.Count < maxMessages && _messages.TryDequeue(out var msg))
            {
                list.Add(msg);
            }

            return ValueTask.FromResult<IReadOnlyList<QueueReceivedMessage>>(list);
        }

        public ValueTask SendMessageAsync(string messageText, CancellationToken cancellationToken)
        {
            SentMessages.Add(messageText);
            SendCalled.TrySetResult();
            return ValueTask.CompletedTask;
        }

        public ValueTask DeleteMessageAsync(string messageId, string popReceipt, CancellationToken cancellationToken)
        {
            DeletedMessages.Add((messageId, popReceipt));
            DeleteCalled.TrySetResult();
            return ValueTask.CompletedTask;
        }

        public ValueTask<QueueUpdateReceipt> UpdateMessageAsync(string messageId, string popReceipt, string messageText, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            UpdatedMessages.Add((messageId, popReceipt, visibilityTimeout));
            return ValueTask.FromResult(new QueueUpdateReceipt
            {
                PopReceipt = popReceipt,
                NextVisibleOnUtc = DateTimeOffset.UtcNow + visibilityTimeout
            });
        }

        public void Enqueue(QueueReceivedMessage message)
        {
            _messages.Enqueue(message);
        }
    }
}