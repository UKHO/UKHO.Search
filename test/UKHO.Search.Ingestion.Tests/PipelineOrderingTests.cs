using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests
{
    public sealed class PipelineOrderingTests
    {
        [Fact]
        public async Task KeyPartitioning_preserves_per_key_order_end_to_end()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                                          {
                                                              ["ingestion:laneCount"] = "4",
                                                              ["ingestion:channelCapacityPrePartition"] = "64",
                                                              ["ingestion:channelCapacityPerLane"] = "16",
                                                              ["ingestion:channelCapacityMicrobatchOut"] = "8",
                                                              ["ingestion:microbatchMaxItems"] = "10",
                                                              ["ingestion:microbatchMaxDelayMilliseconds"] = "1"
                                                          })
                                                          .Build();

            using var loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));

            var builder = new IngestionPipelineBuilder(configuration, loggerFactory);
            var graph = builder.BuildSynthetic(CancellationToken.None);

            await graph.Supervisor.StartAsync();
            await graph.Supervisor.Completion.WaitAsync(TimeSpan.FromSeconds(10));

            var allReceived = graph.LaneSinks.SelectMany((sink, sinkIndex) => sink.Items.Select(e => (sinkIndex, envelope: e)))
                                   .ToArray();

            allReceived.Length.ShouldBeGreaterThan(0);

            var lanePerKey = allReceived.GroupBy(x => x.envelope.Key)
                                        .ToDictionary(g => g.Key, g => g.Select(x => x.sinkIndex)
                                                                        .Distinct()
                                                                        .ToArray());

            foreach (var kvp in lanePerKey)
            {
                kvp.Value.Length.ShouldBe(1);
            }

            var sequencesByKey = allReceived.GroupBy(x => x.envelope.Key)
                                            .ToDictionary(g => g.Key, g => g.Select(x => GetSequence(x.envelope.Payload))
                                                                            .ToArray());

            foreach (var kvp in sequencesByKey)
            {
                kvp.Value.ShouldBe(kvp.Value.OrderBy(x => x)
                                      .ToArray());
            }
        }

        private static int GetSequence(IndexOperation operation)
        {
            var upsert = operation.ShouldBeOfType<UpsertOperation>();
            var request = upsert.Document.Source;
            request.ShouldNotBeNull();

            var properties = request.UpdateItem?.Properties ?? Array.Empty<IngestionProperty>();
            var seq = properties.FirstOrDefault(p => string.Equals(p.Name, "sequence", StringComparison.Ordinal));
            seq.ShouldNotBeNull();
            seq!.Type.ShouldBe(IngestionPropertyType.Integer);
            return Convert.ToInt32(seq.Value);
        }
    }
}