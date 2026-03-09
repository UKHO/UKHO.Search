using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.Metrics
{
    public sealed class NodeMetrics : IDisposable
    {
        public const string MeterName = "UKHO.Search.Pipelines.Playground";

        private static readonly Meter _meter = new(MeterName);

        private static readonly Counter<long> _nodeIn = _meter.CreateCounter<long>("ukho.pipeline.node.in");
        private static readonly Counter<long> _nodeOut = _meter.CreateCounter<long>("ukho.pipeline.node.out");
        private static readonly Counter<long> _nodeFailed = _meter.CreateCounter<long>("ukho.pipeline.node.failed");
        private static readonly Counter<long> _nodeDropped = _meter.CreateCounter<long>("ukho.pipeline.node.dropped");
        private static readonly Histogram<double> _nodeDurationMs = _meter.CreateHistogram<double>("ukho.pipeline.node.duration_ms");

        private static readonly ConcurrentDictionary<string, Func<long>> _inFlightProviders = new();
        private static readonly ConcurrentDictionary<string, Func<long>> _queueDepthProviders = new();

        private readonly string _nodeName;
        private readonly Func<long> _queueDepthProvider;
        private long _inFlight;

        public NodeMetrics(string nodeName, Func<long>? queueDepthProvider = null)
        {
            _nodeName = nodeName;
            _queueDepthProvider = queueDepthProvider ?? (() => 0);

            _inFlightProviders[nodeName] = () => Volatile.Read(ref _inFlight);
            _queueDepthProviders[nodeName] = _queueDepthProvider;
        }

        public void Dispose()
        {
            _inFlightProviders.TryRemove(_nodeName, out var _);
            _queueDepthProviders.TryRemove(_nodeName, out var _);
        }

        public void RecordIn(object? item)
        {
            _nodeIn.Add(1, new KeyValuePair<string, object?>("node", _nodeName));
        }

        public void RecordOut(object? item)
        {
            _nodeOut.Add(1, new KeyValuePair<string, object?>("node", _nodeName));

            if (item is IEnvelope envelope)
            {
                switch (envelope.Status)
                {
                    case MessageStatus.Failed:
                        _nodeFailed.Add(1, new KeyValuePair<string, object?>("node", _nodeName));
                        break;
                    case MessageStatus.Dropped:
                        _nodeDropped.Add(1, new KeyValuePair<string, object?>("node", _nodeName));
                        break;
                }
            }
        }

        public void RecordDuration(TimeSpan duration)
        {
            _nodeDurationMs.Record(duration.TotalMilliseconds, new KeyValuePair<string, object?>("node", _nodeName));
        }

        public void IncrementInFlight()
        {
            Interlocked.Increment(ref _inFlight);
        }

        public void DecrementInFlight()
        {
            Interlocked.Decrement(ref _inFlight);
        }

        private static IEnumerable<Measurement<long>> ObserveInFlight()
        {
            foreach (var kvp in _inFlightProviders)
            {
                yield return new Measurement<long>(kvp.Value(), new KeyValuePair<string, object?>("node", kvp.Key));
            }
        }

        private static IEnumerable<Measurement<long>> ObserveQueueDepth()
        {
            foreach (var kvp in _queueDepthProviders)
            {
                yield return new Measurement<long>(kvp.Value(), new KeyValuePair<string, object?>("node", kvp.Key));
            }
        }

        // Gauges must be singletons per meter name; they return measurements for all nodes via tags.
#pragma warning disable IDE0052
        private static readonly ObservableGauge<long> _inFlightGauge = _meter.CreateObservableGauge("ukho.pipeline.node.inflight", ObserveInFlight);

        private static readonly ObservableGauge<long> _queueDepthGauge = _meter.CreateObservableGauge("ukho.pipeline.node.queue_depth", ObserveQueueDepth);
#pragma warning restore IDE0052
    }
}