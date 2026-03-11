using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Shouldly;
using UKHO.Search.Pipelines.Metrics;
using Xunit;

namespace UKHO.Search.Tests.Pipelines.Metrics
{
    public sealed class NodeMetricsProviderTaggingTests
    {
        [Fact]
        public void Instrument_names_are_unchanged()
        {
            var instruments = new HashSet<string>(StringComparer.Ordinal);

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == NodeMetrics.MeterName)
                {
                    instruments.Add(instrument.Name);
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };

            listener.Start();

            using (var _ = new NodeMetrics("probe"))
            {
                // Ensure observable instruments are also published/observed.
                listener.RecordObservableInstruments();
            }

            var expected = new HashSet<string>(StringComparer.Ordinal)
            {
                "ukho.pipeline.node.in",
                "ukho.pipeline.node.out",
                "ukho.pipeline.node.failed",
                "ukho.pipeline.node.dropped",
                "ukho.pipeline.node.duration_ms",
                "ukho.pipeline.node.inflight",
                "ukho.pipeline.node.queue_depth"
            };

            instruments.SetEquals(expected)
                       .ShouldBeTrue();
        }

        [Fact]
        public void Counters_include_provider_tag_when_provider_is_supplied()
        {
            var nodeName = $"node-{Guid.NewGuid():N}";
            const string providerName = "provider-a";

            var seen = new ConcurrentBag<(string Provider, string Node)>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == NodeMetrics.MeterName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };

            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            {
                if (instrument.Name != "ukho.pipeline.node.in")
                {
                    return;
                }

                var node = GetTagValue(tags, "node");
                if (!string.Equals(node, nodeName, StringComparison.Ordinal))
                {
                    return;
                }

                var provider = GetTagValue(tags, "provider");
                if (!string.IsNullOrWhiteSpace(provider))
                {
                    seen.Add((provider!, node!));
                }
            });

            listener.Start();

            using var metrics = new NodeMetrics(nodeName, providerName);
            metrics.RecordIn(null);

            SpinWait.SpinUntil(() => !seen.IsEmpty, TimeSpan.FromSeconds(2))
                    .ShouldBeTrue();

            seen.ShouldContain(x => x.Provider == providerName && x.Node == nodeName);
        }

        [Fact]
        public void Inflight_gauge_does_not_collide_across_providers()
        {
            var nodeName = $"node-{Guid.NewGuid():N}";

            var seen = new ConcurrentBag<(string Provider, string Node, long Value)>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == NodeMetrics.MeterName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };

            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            {
                if (instrument.Name != "ukho.pipeline.node.inflight")
                {
                    return;
                }

                var node = GetTagValue(tags, "node");
                if (!string.Equals(node, nodeName, StringComparison.Ordinal))
                {
                    return;
                }

                var provider = GetTagValue(tags, "provider") ?? string.Empty;
                seen.Add((provider, node!, measurement));
            });

            listener.Start();

            using var metricsA = new NodeMetrics(nodeName, "provider-a");
            using var metricsB = new NodeMetrics(nodeName, "provider-b");

            metricsA.IncrementInFlight();
            metricsB.IncrementInFlight();

            listener.RecordObservableInstruments();

            SpinWait.SpinUntil(() => seen.Count >= 2, TimeSpan.FromSeconds(2))
                    .ShouldBeTrue();

            seen.ShouldContain(x => x.Provider == "provider-a" && x.Node == nodeName && x.Value >= 1);
            seen.ShouldContain(x => x.Provider == "provider-b" && x.Node == nodeName && x.Value >= 1);
        }

        [Fact]
        public void Queue_depth_gauge_does_not_collide_across_providers()
        {
            var nodeName = $"node-{Guid.NewGuid():N}";

            var seen = new ConcurrentBag<(string Provider, string Node, long Value)>();

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == NodeMetrics.MeterName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };

            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            {
                if (instrument.Name != "ukho.pipeline.node.queue_depth")
                {
                    return;
                }

                var node = GetTagValue(tags, "node");
                if (!string.Equals(node, nodeName, StringComparison.Ordinal))
                {
                    return;
                }

                var provider = GetTagValue(tags, "provider") ?? string.Empty;
                seen.Add((provider, node!, measurement));
            });

            listener.Start();

            using var metricsA = new NodeMetrics(nodeName, "provider-a", () => 10);
            using var metricsB = new NodeMetrics(nodeName, "provider-b", () => 20);

            listener.RecordObservableInstruments();

            SpinWait.SpinUntil(() => seen.Count >= 2, TimeSpan.FromSeconds(2))
                    .ShouldBeTrue();

            seen.ShouldContain(x => x.Provider == "provider-a" && x.Node == nodeName && x.Value == 10);
            seen.ShouldContain(x => x.Provider == "provider-b" && x.Node == nodeName && x.Value == 20);
        }

        private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key)
        {
            for (var i = 0; i < tags.Length; i++)
            {
                if (string.Equals(tags[i].Key, key, StringComparison.Ordinal))
                {
                    return tags[i]
                           .Value?.ToString();
                }
            }

            return null;
        }
    }
}