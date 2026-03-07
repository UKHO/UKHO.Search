using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.Retry
{
    public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private readonly TimeSpan baseDelay;
        private readonly double jitterFactor;
        private readonly TimeSpan maxDelay;
        private readonly Random random;

        public ExponentialBackoffRetryPolicy(int maxAttempts, TimeSpan baseDelay, TimeSpan maxDelay, double jitterFactor = 0.2, Random? random = null)
        {
            if (maxAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            }

            if (baseDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDelay));
            }

            if (maxDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelay));
            }

            if (jitterFactor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(jitterFactor));
            }

            MaxAttempts = maxAttempts;
            this.baseDelay = baseDelay;
            this.maxDelay = maxDelay;
            this.jitterFactor = jitterFactor;
            this.random = random ?? Random.Shared;
        }

        public int MaxAttempts { get; }

        public bool ShouldRetry<TPayload>(Envelope<TPayload> envelope, PipelineError error)
        {
            if (!error.IsTransient)
            {
                return false;
            }

            return envelope.Attempt < MaxAttempts;
        }

        public TimeSpan GetDelay(int attempt)
        {
            if (attempt < 2)
            {
                return TimeSpan.Zero;
            }

            var exponent = attempt - 2;
            var factor = Math.Pow(2, exponent);
            var delayMs = baseDelay.TotalMilliseconds * factor;

            var delay = TimeSpan.FromMilliseconds(delayMs);
            if (delay > maxDelay)
            {
                delay = maxDelay;
            }

            if (jitterFactor <= 0)
            {
                return delay;
            }

            var jitterMultiplier = 1.0 + (random.NextDouble() * 2.0 - 1.0) * jitterFactor;
            var jittered = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * jitterMultiplier);
            if (jittered < TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            return jittered;
        }
    }
}