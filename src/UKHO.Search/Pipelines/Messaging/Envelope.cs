using UKHO.Search.Pipelines.Errors;

namespace UKHO.Search.Pipelines.Messaging
{
    public sealed class Envelope<TPayload> : IEnvelope
    {
        public Envelope(string key, TPayload payload)
        {
            MessageId = Guid.NewGuid();
            Key = key;
            TimestampUtc = DateTimeOffset.UtcNow;
            Attempt = 1;
            Headers = new Dictionary<string, string>();
            Payload = payload;
            Context = new MessageContext();
            Status = MessageStatus.Ok;
        }

        public DateTimeOffset TimestampUtc { get; init; }

        public string? CorrelationId { get; init; }

        public IReadOnlyDictionary<string, string> Headers { get; init; }

        public TPayload Payload { get; init; }

        public MessageContext Context { get; init; }

        public Guid MessageId { get; init; }

        public string Key { get; init; }

        public int Attempt { get; set; }

        public MessageStatus Status { get; private set; }

        public PipelineError? Error { get; private set; }

        public Envelope<TPayload> MarkFailed(PipelineError error)
        {
            Status = MessageStatus.Failed;
            Error = error;
            return this;
        }

        public Envelope<TPayload> MarkRetrying(PipelineError error)
        {
            Status = MessageStatus.Retrying;
            Error = error;
            return this;
        }

        public Envelope<TPayload> MarkOk()
        {
            Status = MessageStatus.Ok;
            Error = null;
            return this;
        }

        public Envelope<TPayload> MarkDropped(string reason, string nodeName)
        {
            Status = MessageStatus.Dropped;
            Error = new PipelineError
            {
                Category = PipelineErrorCategory.Unknown,
                Code = "DROPPED",
                Message = reason,
                ExceptionType = null,
                ExceptionMessage = null,
                StackTrace = null,
                IsTransient = false,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                NodeName = nodeName,
                Details = new Dictionary<string, string>()
            };
            return this;
        }

        public Envelope<TOut> MapPayload<TOut>(TOut payload)
        {
            return new Envelope<TOut>(Key, payload)
            {
                MessageId = MessageId,
                TimestampUtc = TimestampUtc,
                CorrelationId = CorrelationId,
                Attempt = Attempt,
                Headers = Headers,
                Context = Context,
                Status = Status,
                Error = Error
            };
        }

        public Envelope<TPayload> Clone(bool cloneContext = true)
        {
            var clone = new Envelope<TPayload>(Key, Payload)
            {
                MessageId = MessageId,
                TimestampUtc = TimestampUtc,
                CorrelationId = CorrelationId,
                Attempt = Attempt,
                Headers = new Dictionary<string, string>(Headers),
                Context = cloneContext ? Context.Clone() : Context
            };

            clone.Status = Status;
            clone.Error = Error;
            return clone;
        }
    }
}