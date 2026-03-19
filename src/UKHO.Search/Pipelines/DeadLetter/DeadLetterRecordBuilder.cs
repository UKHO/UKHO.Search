using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.DeadLetter
{
    public static class DeadLetterRecordBuilder
    {
        public static DeadLetterRecord<TPayload> Create<TPayload>(string nodeName, Envelope<TPayload> envelope, IDeadLetterMetadataProvider metadataProvider, string? rawSnapshot = null, DeadLetterPayloadDiagnostics? payloadDiagnostics = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(nodeName);
            ArgumentNullException.ThrowIfNull(envelope);
            ArgumentNullException.ThrowIfNull(metadataProvider);

            return new DeadLetterRecord<TPayload>
            {
                DeadLetteredAtUtc = DateTimeOffset.UtcNow,
                NodeName = nodeName,
                Envelope = envelope,
                Error = envelope.Error,
                PayloadDiagnostics = payloadDiagnostics ?? DeadLetterPayloadDiagnosticsBuilder.Create(envelope.Payload),
                RawSnapshot = rawSnapshot,
                Metadata = CreateMetadata(metadataProvider)
            };
        }

        public static DeadLetterPersistenceFallbackRecord CreateFallback<TPayload>(DeadLetterRecord<TPayload> record, Exception serializationException)
        {
            ArgumentNullException.ThrowIfNull(record);
            ArgumentNullException.ThrowIfNull(serializationException);

            return new DeadLetterPersistenceFallbackRecord
            {
                DeadLetteredAtUtc = record.DeadLetteredAtUtc,
                NodeName = record.NodeName,
                Envelope = CreateFallbackEnvelope(record.Envelope),
                Error = record.Error,
                PayloadDiagnostics = record.PayloadDiagnostics,
                RawSnapshot = record.RawSnapshot,
                Metadata = record.Metadata,
                SerializationError = serializationException.ToString()
            };
        }

        private static DeadLetterMetadata CreateMetadata(IDeadLetterMetadataProvider metadataProvider)
        {
            return new DeadLetterMetadata
            {
                AppVersion = metadataProvider.AppVersion,
                CommitId = metadataProvider.CommitId,
                HostName = metadataProvider.HostName
            };
        }

        private static DeadLetterFallbackEnvelope CreateFallbackEnvelope<TPayload>(Envelope<TPayload> envelope)
        {
            return new DeadLetterFallbackEnvelope
            {
                TimestampUtc = envelope.TimestampUtc,
                CorrelationId = envelope.CorrelationId,
                Headers = envelope.Headers,
                MessageId = envelope.MessageId,
                Key = envelope.Key,
                Attempt = envelope.Attempt,
                Status = envelope.Status,
                Error = envelope.Error,
                Context = envelope.Context
            };
        }
    }
}
