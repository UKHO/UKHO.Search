using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Pipelines.Metrics
{
    public sealed class NodeMetrics : IDisposable
    {
        public const string MeterName = "UKHO.Search.Pipelines.Playground";

        private static readonly Meter Meter = new(MeterName);

        private static readonly Counter<long> NodeIn = Meter.CreateCounter<long>("ukho.pipeline.node.in");
        private static readonly Counter<long> NodeOut = Meter.CreateCounter<long>("ukho.pipeline.node.out");
        private static readonly Counter<long> NodeFailed = Meter.CreateCounter<long>("ukho.pipeline.node.failed");
        private static readonly Counter<long> NodeDropped = Meter.CreateCounter<long>("ukho.pipeline.node.dropped");
        private static readonly Histogram<double> NodeDurationMs = Meter.CreateHistogram<double>("ukho.pipeline.node.duration_ms");

        private static readonly ConcurrentDictionary<string, Func<long>> InFlightProviders = new();
        private static readonly ConcurrentDictionary<string, Func<long>> QueueDepthProviders = new();

        private readonly string nodeName;
        private readonly Func<long> queueDepthProvider;
        private long inFlight;

        public NodeMetrics(string nodeName, Func<long>? queueDepthProvider = null)
        {
            this.nodeName = nodeName;
            this.queueDepthProvider = queueDepthProvider ?? (() => 0);

            InFlightProviders[nodeName] = () => Volatile.Read(ref inFlight);
            QueueDepthProviders[nodeName] = this.queueDepthProvider;
        }

        public void Dispose()
        {
            InFlightProviders.TryRemove(nodeName, out var _);
            QueueDepthProviders.TryRemove(nodeName, out var _);
        }

        public void RecordIn(object? item)
        {
            NodeIn.Add(1, new KeyValuePair<string, object?>("node", nodeName));
        }

        public void RecordOut(object? item)
        {
            NodeOut.Add(1, new KeyValuePair<string, object?>("node", nodeName));

            if (item is IEnvelope envelope)
            {
                switch (envelope.Status)
                {
                    case MessageStatus.Failed:
                        NodeFailed.Add(1, new KeyValuePair<string, object?>("node", nodeName));
                        break;
                    case MessageStatus.Dropped:
                        NodeDropped.Add(1, new KeyValuePair<string, object?>("node", nodeName));
                        break;
                }
            }
        }

        public void RecordDuration(TimeSpan duration)
        {
            NodeDurationMs.Record(duration.TotalMilliseconds, new KeyValuePair<string, object?>("node", nodeName));
        }

        public void IncrementInFlight()
        {
            Interlocked.Increment(ref inFlight);
        }

        public void DecrementInFlight()
        {
            Interlocked.Decrement(ref inFlight);
        }

        private static IEnumerable<Measurement<long>> ObserveInFlight()
        {
            foreach (var kvp in InFlightProviders)
            {
                yield return new Measurement<long>(kvp.Value(), new KeyValuePair<string, object?>("node", kvp.Key));
            }
        }

        private static IEnumerable<Measurement<long>> ObserveQueueDepth()
        {
            foreach (var kvp in QueueDepthProviders)
            {
                yield return new Measurement<long>(kvp.Value(), new KeyValuePair<string, object?>("node", kvp.Key));
            }
        }

        // Gauges must be singletons per meter name; they return measurements for all nodes via tags.
#pragma warning disable IDE0052
        private static readonly ObservableGauge<long> InFlightGauge = Meter.CreateObservableGauge("ukho.pipeline.node.inflight", ObserveInFlight);

        private static readonly ObservableGauge<long> QueueDepthGauge = Meter.CreateObservableGauge("ukho.pipeline.node.queue_depth", ObserveQueueDepth);
#pragma warning restore IDE0052
    }
}