using System.Text.Json;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Requests.Serialization;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Ingestion.Tests.TestProviders
{
    public sealed class RecordingIngestionDataProvider : IIngestionDataProvider
    {
        private static readonly JsonSerializerOptions _serializerOptions = IngestionJsonSerializerOptions.Create();
        private readonly TaskCompletionSource _releaseAck = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<Envelope<IngestionRequest>> EnvelopeReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Name { get; init; } = "test-provider";

        public ValueTask<IngestionRequest> DeserializeIngestionRequestAsync(string messageText, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                throw new JsonException("Queue message body is required.");
            }

            var request = JsonSerializer.Deserialize<IngestionRequest>(messageText, _serializerOptions);
            if (request is null)
            {
                throw new JsonException("Queue message could not be deserialized to IngestionRequest.");
            }

            return ValueTask.FromResult(request);
        }

        public ValueTask ProcessIngestionRequestAsync(Envelope<IngestionRequest> envelope, CancellationToken cancellationToken = default)
        {
            EnvelopeReceived.TrySetResult(envelope);

            _ = Task.Run(async () =>
            {
                await _releaseAck.Task.ConfigureAwait(false);

                if (envelope.Context.TryGetItem<IQueueMessageAcker>(QueueEnvelopeContextKeys.MessageAcker, out var acker) && acker is not null)
                {
                    await acker.DeleteAsync(CancellationToken.None)
                               .ConfigureAwait(false);
                }
            }, CancellationToken.None);

            return ValueTask.CompletedTask;
        }

        public void ReleaseAck()
        {
            _releaseAck.TrySetResult();
        }
    }
}