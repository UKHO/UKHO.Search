using UKHO.Search.Infrastructure.Ingestion.Queue;

namespace UKHO.Search.Ingestion.Tests.TestQueues
{
    public sealed class FakeQueueClientFactory : IQueueClientFactory
    {
        private readonly Dictionary<string, FakeQueueClient> _queues = new(StringComparer.OrdinalIgnoreCase);

        public List<string> RequestedQueueNames { get; } = new();

        public IQueueClient GetQueueClient(string queueName)
        {
            RequestedQueueNames.Add(queueName);
            return GetOrAdd(queueName);
        }

        public FakeQueueClient GetOrAdd(string queueName)
        {
            if (!_queues.TryGetValue(queueName, out var queue))
            {
                queue = new FakeQueueClient(queueName);
                _queues.Add(queueName, queue);
            }

            return queue;
        }
    }
}