using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Shouldly;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Nodes;
using Xunit;

namespace UKHO.Search.Tests.Pipelines
{
	public sealed class MetricsQueueDepthTests
	{
		[Fact]
		public async Task Microbatch_reports_queue_depth_while_buffering()
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

			var nodeName = $"microbatch-{Guid.NewGuid():N}";

			var seen = new ConcurrentDictionary<string, long>();
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
				string node = string.Empty;
				for (var i = 0; i < tags.Length; i++)
				{
					if (tags[i].Key == "node")
					{
						node = tags[i].Value?.ToString() ?? string.Empty;
						break;
					}
				}

				seen[$"{instrument.Name}|{node}"] = measurement;
			});

			listener.Start();

			var input = BoundedChannelFactory.Create<Envelope<int>>(capacity: 10, singleReader: true, singleWriter: true);
			var output = BoundedChannelFactory.Create<BatchEnvelope<int>>(capacity: 10, singleReader: true, singleWriter: true);

			var node = new MicroBatchNode<int>(
				nodeName,
				partitionId: 0,
				input.Reader,
				output.Writer,
				maxItems: 100,
				maxDelay: TimeSpan.FromSeconds(5));

			await node.StartAsync(cts.Token);

			await input.Writer.WriteAsync(new Envelope<int>("key-0", 1), cts.Token);
			var expectedKey = $"ukho.pipeline.node.queue_depth|{nodeName}";
			var sw = Stopwatch.StartNew();
			while (sw.Elapsed < TimeSpan.FromSeconds(2))
			{
				await Task.Delay(TimeSpan.FromMilliseconds(25), cts.Token);
				listener.RecordObservableInstruments();

				if (seen.TryGetValue(expectedKey, out var depth) && depth > 0)
				{
					break;
				}
			}

			seen.TryGetValue(expectedKey, out var finalDepth).ShouldBeTrue();
			finalDepth.ShouldBeGreaterThan(0);

			input.Writer.TryComplete();
			await node.Completion.WaitAsync(cts.Token);
		}
	}
}
