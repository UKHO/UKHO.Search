using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.Metrics
{
    public sealed class NodeMetrics : IDisposable
    {
        public const string MeterName = "UKHO.Search.Ingestion.Pipeline";

        private static readonly Meter _meter = new(MeterName);

        private static readonly Counter<long> _nodeIn = _meter.CreateCounter<long>("ukho.pipeline.node.in");
        private static readonly Counter<long> _nodeOut = _meter.CreateCounter<long>("ukho.pipeline.node.out");
        private static readonly Counter<long> _nodeFailed = _meter.CreateCounter<long>("ukho.pipeline.node.failed");
        private static readonly Counter<long> _nodeDropped = _meter.CreateCounter<long>("ukho.pipeline.node.dropped");
        private static readonly Histogram<double> _nodeDurationMs = _meter.CreateHistogram<double>("ukho.pipeline.node.duration_ms");

        private static readonly ConcurrentDictionary<(string Provider, string Node), GaugeProvider> _inFlightProviders = new();
        private static readonly ConcurrentDictionary<(string Provider, string Node), GaugeProvider> _queueDepthProviders = new();

        private readonly string _nodeName;
        private readonly (string Provider, string Node) _providerKey;
        private readonly string _providerName;
        private readonly Func<long> _queueDepthProvider;
        private readonly KeyValuePair<string, object?>[] _tags;
        private long _inFlight;

        public NodeMetrics(string nodeName, Func<long>? queueDepthProvider = null)
        {
            _nodeName = nodeName;
            _providerName = string.Empty;
            _queueDepthProvider = queueDepthProvider ?? (() => 0);
            _providerKey = (_providerName, _nodeName);
            _tags = CreateTags(_nodeName, null);

            _inFlightProviders[_providerKey] = new GaugeProvider(() => Volatile.Read(ref _inFlight), _tags);
            _queueDepthProviders[_providerKey] = new GaugeProvider(_queueDepthProvider, _tags);
        }

        public NodeMetrics(string nodeName, string? providerName, Func<long>? queueDepthProvider = null)
        {
            _nodeName = nodeName;
            _providerName = providerName ?? string.Empty;
            _queueDepthProvider = queueDepthProvider ?? (() => 0);
            _providerKey = (_providerName, _nodeName);
            _tags = CreateTags(_nodeName, providerName);

            _inFlightProviders[_providerKey] = new GaugeProvider(() => Volatile.Read(ref _inFlight), _tags);
            _queueDepthProviders[_providerKey] = new GaugeProvider(_queueDepthProvider, _tags);
        }

        public void Dispose()
        {
            _inFlightProviders.TryRemove(_providerKey, out var _);
            _queueDepthProviders.TryRemove(_providerKey, out var _);
        }

        public void RecordIn(object? item)
        {
            _nodeIn.Add(1, _tags);
        }

        public void RecordOut(object? item)
        {
            _nodeOut.Add(1, _tags);

            if (item is IEnvelope envelope)
            {
                switch (envelope.Status)
                {
                    case MessageStatus.Failed:
                        _nodeFailed.Add(1, _tags);
                        break;
                    case MessageStatus.Dropped:
                        _nodeDropped.Add(1, _tags);
                        break;
                }
            }
        }

        public void RecordDuration(TimeSpan duration)
        {
            _nodeDurationMs.Record(duration.TotalMilliseconds, _tags);
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
                yield return new Measurement<long>(kvp.Value.ValueProvider(), kvp.Value.Tags);
            }
        }

        private static IEnumerable<Measurement<long>> ObserveQueueDepth()
        {
            foreach (var kvp in _queueDepthProviders)
            {
                yield return new Measurement<long>(kvp.Value.ValueProvider(), kvp.Value.Tags);
            }
        }

        private static KeyValuePair<string, object?>[] CreateTags(string nodeName, string? providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                return
                [
                    new KeyValuePair<string, object?>("node", nodeName)
                ];
            }

            return
            [
                new KeyValuePair<string, object?>("provider", providerName),
                new KeyValuePair<string, object?>("node", nodeName)
            ];
        }

        private readonly record struct GaugeProvider(Func<long> ValueProvider, KeyValuePair<string, object?>[] Tags);

        // Gauges must be singletons per meter name; they return measurements for all nodes via tags.
#pragma warning disable IDE0052
        private static readonly ObservableGauge<long> _inFlightGauge = _meter.CreateObservableGauge("ukho.pipeline.node.inflight", ObserveInFlight);

        private static readonly ObservableGauge<long> _queueDepthGauge = _meter.CreateObservableGauge("ukho.pipeline.node.queue_depth", ObserveQueueDepth);
#pragma warning restore IDE0052
    }
}