using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Shouldly;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
    public sealed class MetricsDetailedTests
    {
        [Fact]
        public async Task Failed_counter_increments_for_failed_envelope_outputs()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var seen = new ConcurrentDictionary<string, long>();
            using var listener = CreateListener(seen);

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var validate = new ValidateNode<int>("validate", input.Reader, output.Writer, forwardFailedToMainOutput: true, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", output.Reader, fatalErrorReporter: supervisor);

            supervisor.AddNode(validate);
            supervisor.AddNode(sink);

            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>(string.Empty, 1), cts.Token);
            input.Writer.TryComplete();

            await supervisor.Completion.WaitAsync(cts.Token);
            listener.RecordObservableInstruments();

            seen.TryGetValue("ukho.pipeline.node.failed|validate", out var failed)
                .ShouldBeTrue();
            failed.ShouldBeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public async Task Dropped_counter_increments_for_dropped_envelope_outputs()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var seen = new ConcurrentDictionary<string, long>();
            using var listener = CreateListener(seen);

            var supervisor = new PipelineSupervisor(cts.Token);
            var input = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);
            var output = BoundedChannelFactory.Create<Envelope<int>>(4, true, true);

            var drop = new DropNode<int>("drop", input.Reader, output.Writer, fatalErrorReporter: supervisor);

            var sink = new CollectingSinkNode<int>("sink", output.Reader, fatalErrorReporter: supervisor);

            supervisor.AddNode(drop);
            supervisor.AddNode(sink);

            await supervisor.StartAsync();

            await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);
            input.Writer.TryComplete();

            await supervisor.Completion.WaitAsync(cts.Token);
            listener.RecordObservableInstruments();

            seen.TryGetValue("ukho.pipeline.node.dropped|drop", out var dropped)
                .ShouldBeTrue();
            dropped.ShouldBeGreaterThanOrEqualTo(1);
        }

        private static MeterListener CreateListener(ConcurrentDictionary<string, long> seen)
        {
            var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == NodeMetrics.MeterName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };

            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
            {
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

                seen.AddOrUpdate($"{instrument.Name}|{node}", measurement, (_, v) => v + measurement);
            });

            listener.Start();
            return listener;
        }
    }
}