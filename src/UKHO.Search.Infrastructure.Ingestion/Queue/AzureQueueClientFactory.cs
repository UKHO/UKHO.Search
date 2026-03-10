using Azure.Storage.Queues;

namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public sealed class AzureQueueClientFactory : IQueueClientFactory
    {
        private readonly QueueServiceClient _queueServiceClient;

        public AzureQueueClientFactory(QueueServiceClient queueServiceClient)
        {
            _queueServiceClient = queueServiceClient;
        }

        public IQueueClient GetQueueClient(string queueName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

            var queueClient = _queueServiceClient.GetQueueClient(queueName);
            return new AzureQueueClient(queueClient);
        }
    }
}