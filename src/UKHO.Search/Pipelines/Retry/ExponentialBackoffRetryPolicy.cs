using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.Retry
{
    public sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private readonly TimeSpan _baseDelay;
        private readonly double _jitterFactor;
        private readonly TimeSpan _maxDelay;
        private readonly Random _random;

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
            _baseDelay = baseDelay;
            _maxDelay = maxDelay;
            _jitterFactor = jitterFactor;
            _random = random ?? Random.Shared;
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
            var delayMs = _baseDelay.TotalMilliseconds * factor;

            var delay = TimeSpan.FromMilliseconds(delayMs);
            if (delay > _maxDelay)
            {
                delay = _maxDelay;
            }

            if (_jitterFactor <= 0)
            {
                return delay;
            }

            var jitterMultiplier = 1.0 + (_random.NextDouble() * 2.0 - 1.0) * _jitterFactor;
            var jittered = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * jitterMultiplier);
            if (jittered < TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            return jittered;
        }
    }
}