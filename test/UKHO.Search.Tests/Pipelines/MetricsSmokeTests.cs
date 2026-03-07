using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Shouldly;
using UKHO.Search.Pipelines.Metrics;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class MetricsSmokeTests
    {
        [Fact]
        public async Task Nodes_emit_basic_metrics()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var seen = new ConcurrentDictionary<string, int>();

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
                seen.AddOrUpdate(instrument.Name, 1, (_, v) => v + 1);
            });

            listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) =>
            {
                seen.AddOrUpdate(instrument.Name, 1, (_, v) => v + 1);
            });

            listener.Start();

            var graph = HelloPipelineGraph.Create(20, 5, 2, 4, null, (p, _) => ValueTask.FromResult(p), cts.Token);

            await graph.Supervisor.StartAsync();
            await graph.Supervisor.Completion.WaitAsync(cts.Token);

            listener.RecordObservableInstruments();

            seen.ContainsKey("ukho.pipeline.node.in")
                .ShouldBeTrue();
            seen.ContainsKey("ukho.pipeline.node.out")
                .ShouldBeTrue();
            seen.ContainsKey("ukho.pipeline.node.duration_ms")
                .ShouldBeTrue();
            seen.ContainsKey("ukho.pipeline.node.inflight")
                .ShouldBeTrue();
            seen.ContainsKey("ukho.pipeline.node.queue_depth")
                .ShouldBeTrue();
        }
    }
}