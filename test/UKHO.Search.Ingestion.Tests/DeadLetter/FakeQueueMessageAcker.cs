using UKHO.Search.Infrastructure.Ingestion.Queue;

namespace UKHO.Search.Ingestion.Tests.DeadLetter
{
    public sealed class FakeQueueMessageAcker : IQueueMessageAcker
    {
        public int DeleteCalls { get; private set; }

        public int UpdateVisibilityCalls { get; private set; }

        public int MoveToPoisonCalls { get; private set; }

        public ValueTask DeleteAsync(CancellationToken cancellationToken)
        {
            DeleteCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask UpdateVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            UpdateVisibilityCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask MoveToPoisonAsync(IQueueClient poisonQueue, string poisonMessageBody, CancellationToken cancellationToken)
        {
            MoveToPoisonCalls++;
            return ValueTask.CompletedTask;
        }
    }
}