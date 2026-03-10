using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.Retry
{
    public interface IRetryPolicy
    {
        int MaxAttempts { get; }

        bool ShouldRetry<TPayload>(Envelope<TPayload> envelope, PipelineError error);

        TimeSpan GetDelay(int attempt);
    }
}