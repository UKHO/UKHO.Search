using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class MetricsLifecycleTests
    {
        [Fact]
        public async Task Node_metrics_providers_are_removed_after_node_completion()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var seenNodes = new ConcurrentDictionary<string, long>();
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

                var node = string.Empty;
                for (var i = 0; i < tags.Length; i++)
                {
                    if (tags[i].Key == "node")
                    {
                        node = tags[i]
                               .Value?.ToString() ?? string.Empty;
                        break;
                    }
                }

                seenNodes[node] = measurement;
            });

            listener.Start();

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var nodeName = $"drop-{Guid.NewGuid():N}";

            var drop = new DropNode<int>(nodeName, input.Reader, output.Writer, fatalErrorReporter: supervisor);

            supervisor.AddNode(drop);
            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);
            input.Writer.TryComplete();

            await supervisor.Completion.WaitAsync(cts.Token);

            listener.RecordObservableInstruments();

            seenNodes.ContainsKey(nodeName)
                     .ShouldBeFalse();
        }
    }
}