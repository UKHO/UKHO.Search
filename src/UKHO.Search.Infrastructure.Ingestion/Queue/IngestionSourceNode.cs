using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Queue
{
    public sealed class IngestionSourceNode : INode
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IIngestionProviderService _providerService;
        private readonly IQueueClientFactory _queueClientFactory;
        private Task? _completion;

        public IngestionSourceNode(string name, IConfiguration configuration, IIngestionProviderService providerService, IQueueClientFactory queueClientFactory, ILogger logger)
        {
            Name = name;
            _configuration = configuration;
            _providerService = providerService;
            _queueClientFactory = queueClientFactory;
            _logger = logger;
        }

        public string Name { get; }

        public Task Completion => _completion ?? Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
            return Task.CompletedTask;
        }

        public ValueTask StopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                var providers = _providerService.GetAllProviders()
                                                .ToArray();

                if (providers.Length == 0)
                {
                    _logger.LogWarning("No ingestion providers registered; source node will be idle.");
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken)
                              .ConfigureAwait(false);
                    return;
                }

                var tasks = providers.Select(factory => PollProviderQueueAsync(factory, cancellationToken))
                                     .ToArray();

                await Task.WhenAll(tasks)
                          .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ingestion queue poller failed.");
                throw;
            }
        }

        private async Task PollProviderQueueAsync(IIngestionDataProviderFactory factory, CancellationToken cancellationToken)
        {
            var queueName = factory.QueueName;
            if (string.IsNullOrWhiteSpace(queueName))
            {
                _logger.LogWarning("Ingestion provider '{ProviderName}' has an empty queue name; skipping.", factory.Name);
                return;
            }

            var provider = factory.CreateProvider();

            try
            {
                var receiveBatchSize = _configuration.GetValue("ingestion:queueReceiveBatchSize", 16);
                var visibilityTimeoutSeconds = _configuration.GetValue("ingestion:queueVisibilityTimeoutSeconds", 300);
                var renewalSeconds = _configuration.GetValue("ingestion:queueVisibilityRenewalSeconds", 60);
                var pollingIntervalMs = _configuration.GetValue("ingestion:queuePollingIntervalMilliseconds", 1000);
                var maxDequeueCount = _configuration.GetValue("ingestion:queueMaxDequeueCount", 5);
                var poisonQueueSuffix = _configuration["ingestion:poisonQueueSuffix"] ?? "-poison";

                var visibilityTimeout = TimeSpan.FromSeconds(visibilityTimeoutSeconds);
                var renewalInterval = TimeSpan.FromSeconds(renewalSeconds);
                var pollingInterval = TimeSpan.FromMilliseconds(pollingIntervalMs);

                var queue = _queueClientFactory.GetQueueClient(queueName);
                var poisonQueue = _queueClientFactory.GetQueueClient(queueName + poisonQueueSuffix);

                _logger.LogInformation("Ensuring ingestion queues exist. ProviderName={ProviderName} QueueName={QueueName} PoisonQueueName={PoisonQueueName}", factory.Name, queueName, queueName + poisonQueueSuffix);

                await queue.CreateIfNotExistsAsync(cancellationToken)
                           .ConfigureAwait(false);
                await poisonQueue.CreateIfNotExistsAsync(cancellationToken)
                                 .ConfigureAwait(false);

                _logger.LogInformation("Starting ingestion queue poller. ProviderName={ProviderName} QueueName={QueueName}", factory.Name, queueName);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var messages = await queue.ReceiveMessagesAsync(receiveBatchSize, visibilityTimeout, cancellationToken)
                                              .ConfigureAwait(false);

                    if (messages.Count == 0)
                    {
                        await Task.Delay(pollingInterval, cancellationToken)
                                  .ConfigureAwait(false);
                        continue;
                    }

                    foreach (var message in messages)
                    {
                        if (message.DequeueCount > maxDequeueCount)
                        {
                            await MoveToPoisonAsync(queueName, queue, poisonQueue, message, cancellationToken)
                                .ConfigureAwait(false);
                            continue;
                        }

                        IngestionRequest request;
                        try
                        {
                            request = await provider.DeserializeIngestionRequestAsync(message.MessageText, cancellationToken)
                                                    .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to deserialize ingestion request. ProviderName={ProviderName} QueueName={QueueName} MessageId={MessageId} DequeueCount={DequeueCount}", factory.Name, queueName, message.MessageId, message.DequeueCount);
                            continue;
                        }

                        var requestId = GetRequestId(request);
                        if (string.IsNullOrWhiteSpace(requestId))
                        {
                            _logger.LogWarning("Ingestion request did not contain a valid Id. ProviderName={ProviderName} QueueName={QueueName} MessageId={MessageId}", factory.Name, queueName, message.MessageId);
                            continue;
                        }

                        var envelope = new Envelope<IngestionRequest>(requestId, request);
                        if (envelope.Headers is Dictionary<string, string> headers)
                        {
                            headers["queueName"] = queueName;
                            headers["queueMessageId"] = message.MessageId;
                            headers["dequeueCount"] = message.DequeueCount.ToString();
                            headers["providerName"] = factory.Name;
                        }

                        var acker = new QueueMessageAcker(queue, message.MessageId, message.PopReceipt, message.MessageText, _logger);
                        acker.StartVisibilityRenewal(visibilityTimeout, renewalInterval, cancellationToken);
                        envelope.Context.SetItem(QueueEnvelopeContextKeys.MessageAcker, acker);

                        try
                        {
                            await provider.ProcessIngestionRequestAsync(envelope, cancellationToken)
                                          .ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Provider failed to accept ingestion request. ProviderName={ProviderName} QueueName={QueueName} MessageId={MessageId} Key={Key}", factory.Name, queueName, message.MessageId, envelope.Key);
                        }
                    }
                }
            }
            finally
            {
                switch (provider)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync()
                                             .ConfigureAwait(false);
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
        }

        private async ValueTask MoveToPoisonAsync(string queueName, IQueueClient queue, IQueueClient poisonQueue, QueueReceivedMessage message, CancellationToken cancellationToken)
        {
            var poisonBody = JsonSerializer.Serialize(new
            {
                queueName,
                messageId = message.MessageId,
                dequeueCount = message.DequeueCount,
                insertedOnUtc = message.InsertedOnUtc,
                nextVisibleOnUtc = message.NextVisibleOnUtc,
                movedToPoisonUtc = DateTimeOffset.UtcNow,
                body = message.MessageText,
                reason = "MaxDequeueCountExceeded"
            });

            var acker = new QueueMessageAcker(queue, message.MessageId, message.PopReceipt, message.MessageText, _logger);

            await acker.MoveToPoisonAsync(poisonQueue, poisonBody, cancellationToken)
                       .ConfigureAwait(false);

            _logger.LogWarning("Moved message to poison queue. QueueName={QueueName} PoisonQueueName={PoisonQueueName} MessageId={MessageId} DequeueCount={DequeueCount}", queueName, queueName + (_configuration["ingestion:poisonQueueSuffix"] ?? "-poison"), message.MessageId, message.DequeueCount);
        }

        private static string? GetRequestId(IngestionRequest request)
        {
            return request.RequestType switch
            {
                IngestionRequestType.AddItem => request.AddItem?.Id,
                IngestionRequestType.UpdateItem => request.UpdateItem?.Id,
                IngestionRequestType.DeleteItem => request.DeleteItem?.Id,
                IngestionRequestType.UpdateAcl => request.UpdateAcl?.Id,
                var _ => null
            };
        }
    }
}