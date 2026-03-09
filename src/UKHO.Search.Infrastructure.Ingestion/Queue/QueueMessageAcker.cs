using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public sealed class QueueMessageAcker : IQueueMessageAcker
    {
        private readonly ILogger _logger;
        private readonly string _messageId;
        private readonly string _messageText;
        private readonly IQueueClient _queue;
        private readonly CancellationTokenSource _renewalCts = new();
        private int _deleted;
        private string _popReceipt;

        public QueueMessageAcker(IQueueClient queue, string messageId, string popReceipt, string messageText, ILogger logger)
        {
            _queue = queue;
            _messageId = messageId;
            _popReceipt = popReceipt;
            _messageText = messageText;
            _logger = logger;
        }

        public Task? VisibilityRenewalTask { get; private set; }

        public async ValueTask DeleteAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _deleted, 1, 0) != 0)
            {
                return;
            }

            _renewalCts.Cancel();

            if (VisibilityRenewalTask is not null)
            {
                try
                {
                    await VisibilityRenewalTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }

            await _queue.DeleteMessageAsync(_messageId, _popReceipt, cancellationToken)
                        .ConfigureAwait(false);
        }

        public async ValueTask UpdateVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            var receipt = await _queue.UpdateMessageAsync(_messageId, _popReceipt, _messageText, visibilityTimeout, cancellationToken)
                                      .ConfigureAwait(false);

            _popReceipt = receipt.PopReceipt;
        }

        public async ValueTask MoveToPoisonAsync(IQueueClient poisonQueue, string poisonMessageBody, CancellationToken cancellationToken)
        {
            await poisonQueue.SendMessageAsync(poisonMessageBody, cancellationToken)
                             .ConfigureAwait(false);

            await DeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public void StartVisibilityRenewal(TimeSpan visibilityTimeout, TimeSpan renewalInterval, CancellationToken pipelineCancellationToken)
        {
            if (VisibilityRenewalTask is not null)
            {
                return;
            }

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_renewalCts.Token, pipelineCancellationToken);
            VisibilityRenewalTask = Task.Run(() => RunRenewalLoopAsync(visibilityTimeout, renewalInterval, linkedCts.Token), CancellationToken.None);
        }

        private async Task RunRenewalLoopAsync(TimeSpan visibilityTimeout, TimeSpan renewalInterval, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    await Task.Delay(renewalInterval, cancellationToken)
                              .ConfigureAwait(false);

                    await UpdateVisibilityAsync(visibilityTimeout, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Queue visibility renewal failed. MessageId={MessageId}", _messageId);
                throw;
            }
        }
    }
}