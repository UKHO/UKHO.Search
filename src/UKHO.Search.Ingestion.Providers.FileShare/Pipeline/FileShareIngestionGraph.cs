using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Pipeline.Nodes;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Nodes;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Batching;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline
{
    public static class FileShareIngestionGraph
    {
        public static FileShareIngestionGraphHandle BuildAzureQueueBacked(FileShareIngestionGraphDependencies dependencies, CancellationToken cancellationToken)
        {
            return BuildAzureQueueBacked(dependencies, null, cancellationToken);
        }

        public static FileShareIngestionGraphHandle BuildAzureQueueBacked(FileShareIngestionGraphDependencies dependencies, string? providerName, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(dependencies);
            ArgumentNullException.ThrowIfNull(dependencies.Configuration);
            ArgumentNullException.ThrowIfNull(dependencies.LoggerFactory);
            ArgumentNullException.ThrowIfNull(dependencies.Factories);

            var configuration = dependencies.Configuration;
            var loggerFactory = dependencies.LoggerFactory;
            var factories = dependencies.Factories;

            var laneCount = configuration.GetValue<int>("ingestion:laneCount");
            var channelCapacityPrePartition = configuration.GetValue<int>("ingestion:channelCapacityPrePartition");
            var channelCapacityPerLane = configuration.GetValue<int>("ingestion:channelCapacityPerLane");
            var channelCapacityMicrobatchOut = configuration.GetValue<int>("ingestion:channelCapacityMicrobatchOut");
            var microbatchMaxItems = configuration.GetValue<int>("ingestion:microbatchMaxItems");
            var microbatchMaxDelayMs = configuration.GetValue<int>("ingestion:microbatchMaxDelayMilliseconds");

            var enrichmentRetryMaxAttempts = configuration.GetValue("ingestion:enrichmentRetryMaxAttempts", 5);
            var enrichmentRetryBaseDelayMs = configuration.GetValue("ingestion:enrichmentRetryBaseDelayMilliseconds", 200);
            var enrichmentRetryMaxDelayMs = configuration.GetValue("ingestion:enrichmentRetryMaxDelayMilliseconds", 5000);
            var enrichmentRetryJitterMs = configuration.GetValue("ingestion:enrichmentRetryJitterMilliseconds", 250);

            if (laneCount <= 0)
            {
                throw new InvalidOperationException("ingestion:laneCount must be > 0.");
            }

            var supervisor = new PipelineSupervisor(cancellationToken);

            var prePartition = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(channelCapacityPrePartition, true, true);
            var validated = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(channelCapacityPrePartition, true, true);

            var source = factories.CreateSourceNode("ingestion-source-queue", prePartition.Writer, supervisor);

            var validateDeadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(channelCapacityPrePartition, true, true);

            var validate = new IngestionRequestValidateNode("ingestion-validate", prePartition.Reader, validated.Writer, validateDeadLetter.Writer, loggerFactory.CreateLogger("ingestion-validate"), supervisor, providerName);

            var laneDispatchChannels = new List<CountingChannel<Envelope<IngestionRequest>>>(laneCount);
            var laneDispatchWriters = new List<ChannelWriter<Envelope<IngestionRequest>>>(laneCount);

            for (var lane = 0; lane < laneCount; lane++)
            {
                var laneDispatch = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(channelCapacityPerLane, true, true);
                laneDispatchChannels.Add(laneDispatch);
                laneDispatchWriters.Add(laneDispatch.Writer);
            }

            var partition = new KeyPartitionNode<IngestionRequest>("ingestion-partition", validated.Reader, laneDispatchWriters, loggerFactory.CreateLogger("ingestion-partition"), supervisor, providerName);

            var requestDeadLetterReaders = new List<ChannelReader<Envelope<IngestionRequest>>>(1 + laneCount)
            {
                validateDeadLetter.Reader
            };

            var indexDeadLetterReaders = new List<ChannelReader<Envelope<IndexOperation>>>(laneCount);
            var diagnosticsReaders = new List<ChannelReader<Envelope<IndexOperation>>>(laneCount);

            var canonicalBuilder = new CanonicalDocumentBuilder();

            for (var lane = 0; lane < laneCount; lane++)
            {
                var laneRequestDeadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(channelCapacityPrePartition, true, true);
                requestDeadLetterReaders.Add(laneRequestDeadLetter.Reader);

                var laneIndexDeadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(channelCapacityPrePartition, true, true);
                indexDeadLetterReaders.Add(laneIndexDeadLetter.Reader);

                var laneDispatchOut = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(channelCapacityPerLane, true, true);
                var laneEnrichedOps = BoundedChannelFactory.Create<Envelope<IndexOperation>>(channelCapacityPerLane, true, true);
                var laneOps = BoundedChannelFactory.Create<Envelope<IndexOperation>>(channelCapacityPerLane, true, true);
                var laneBatches = BoundedChannelFactory.Create<BatchEnvelope<IndexOperation>>(channelCapacityMicrobatchOut, true, true);
                var laneIndexedOkFromBulk = BoundedChannelFactory.Create<Envelope<IndexOperation>>(channelCapacityPerLane, true, true);
                var laneIndexedOk = BoundedChannelFactory.Create<Envelope<IndexOperation>>(channelCapacityPerLane, true, true);

                var laneDispatchDiagnostics = BoundedChannelFactory.Create<Envelope<IndexOperation>>(channelCapacityPrePartition, true, true);
                var laneBulkDiagnostics = BoundedChannelFactory.Create<Envelope<IndexOperation>>(channelCapacityPrePartition, true, true);

                var laneDiagnosticsMerged = BoundedChannelFactory.Create<Envelope<IndexOperation>>(channelCapacityPrePartition, true, true);

                var laneDiagnosticsMerge = new MergeNode<IndexOperation>($"ingestion-diagnostics-lane-{lane}-merge", laneDispatchDiagnostics.Reader, laneBulkDiagnostics.Reader, laneDiagnosticsMerged.Writer, loggerFactory.CreateLogger($"ingestion-diagnostics-lane-{lane}-merge"), supervisor, providerName);

                diagnosticsReaders.Add(laneDiagnosticsMerged.Reader);

                var dispatch = new IngestionRequestDispatchNode($"ingestion-dispatch-{lane}", laneDispatchChannels[lane].Reader, laneDispatchOut.Writer, laneRequestDeadLetter.Writer, canonicalBuilder, loggerFactory.CreateLogger($"ingestion-dispatch-{lane}"), supervisor, providerName);

                var enrich = new ApplyEnrichmentNode($"ingestion-enrich-{lane}", laneDispatchOut.Reader, laneEnrichedOps.Writer, laneIndexDeadLetter.Writer, dependencies.ScopeFactory, loggerFactory.CreateLogger($"ingestion-enrich-{lane}"), supervisor, enrichmentRetryMaxAttempts, TimeSpan.FromMilliseconds(enrichmentRetryBaseDelayMs), TimeSpan.FromMilliseconds(enrichmentRetryMaxDelayMs),
                    TimeSpan.FromMilliseconds(enrichmentRetryJitterMs), providerName: providerName);

                var dispatchBroadcast = new BroadcastNode<IndexOperation>($"ingestion-dispatch-diagtee-{lane}", laneEnrichedOps.Reader, new[] { laneOps.Writer }, new[] { laneDispatchDiagnostics.Writer }, BroadcastMode.BestEffort, loggerFactory.CreateLogger($"ingestion-dispatch-diagtee-{lane}"), supervisor, providerName);

                var microBatch = new MicroBatchNode<IndexOperation>($"ingestion-microbatch-{lane}", lane, laneOps.Reader, laneBatches.Writer, microbatchMaxItems, TimeSpan.FromMilliseconds(microbatchMaxDelayMs), logger: loggerFactory.CreateLogger($"ingestion-microbatch-{lane}"), fatalErrorReporter: supervisor, cancellationMode: CancellationMode.Drain, providerName: providerName);

                var bulkIndex = factories.CreateBulkIndexNode($"ingestion-bulk-index-{lane}", lane, laneBatches.Reader, laneIndexedOkFromBulk.Writer, laneIndexDeadLetter.Writer, supervisor);

                var bulkIndexOkBroadcast = new BroadcastNode<IndexOperation>($"ingestion-bulk-index-diagtee-{lane}", laneIndexedOkFromBulk.Reader, new[] { laneIndexedOk.Writer }, new[] { laneBulkDiagnostics.Writer }, BroadcastMode.BestEffort, loggerFactory.CreateLogger($"ingestion-bulk-index-diagtee-{lane}"), supervisor, providerName);

                var ack = factories.CreateAckNode($"ingestion-ack-{lane}", lane, laneIndexedOk.Reader, supervisor);

                supervisor.AddNode(dispatch);
                supervisor.AddNode(enrich);
                supervisor.AddNode(dispatchBroadcast);
                supervisor.AddNode(microBatch);
                supervisor.AddNode(bulkIndex);
                supervisor.AddNode(bulkIndexOkBroadcast);
                supervisor.AddNode(ack);
                supervisor.AddNode(laneDiagnosticsMerge);
            }

            var requestDeadLetterMerged = CreateMergedReader("ingestion-deadletter-request", requestDeadLetterReaders, channelCapacityPrePartition, loggerFactory, supervisor, providerName);

            var indexDeadLetterMerged = CreateMergedReader("ingestion-deadletter-index", indexDeadLetterReaders, channelCapacityPrePartition, loggerFactory, supervisor, providerName);

            var diagnosticsMerged = CreateMergedReader("ingestion-diagnostics", diagnosticsReaders, channelCapacityPrePartition, loggerFactory, supervisor, providerName);

            var requestDeadLetterSink = factories.CreateRequestDeadLetterSinkNode("ingestion-deadletter-request", requestDeadLetterMerged, supervisor);

            var indexDeadLetterSink = factories.CreateIndexDeadLetterSinkNode("ingestion-deadletter-index", indexDeadLetterMerged, supervisor);

            var diagnosticsSink = factories.CreateDiagnosticsSinkNode("ingestion-diagnostics", diagnosticsMerged, supervisor);

            supervisor.AddNode(source);
            supervisor.AddNode(validate);
            supervisor.AddNode(partition);
            supervisor.AddNode(requestDeadLetterSink);
            supervisor.AddNode(indexDeadLetterSink);
            supervisor.AddNode(diagnosticsSink);

            return new FileShareIngestionGraphHandle
            {
                Supervisor = supervisor
            };
        }

        private static ChannelReader<Envelope<TPayload>> CreateMergedReader<TPayload>(string namePrefix, IReadOnlyList<ChannelReader<Envelope<TPayload>>> inputs, int capacity, ILoggerFactory loggerFactory, PipelineSupervisor supervisor, string? providerName)
        {
            if (inputs.Count == 0)
            {
                throw new InvalidOperationException($"Cannot merge zero inputs for '{namePrefix}'.");
            }

            if (inputs.Count == 1)
            {
                return inputs[0];
            }

            var current = inputs[0];

            for (var i = 1; i < inputs.Count; i++)
            {
                var merged = BoundedChannelFactory.Create<Envelope<TPayload>>(capacity, true, true);

                var merge = new MergeNode<TPayload>($"{namePrefix}-merge-{i}", current, inputs[i], merged.Writer, loggerFactory.CreateLogger($"{namePrefix}-merge-{i}"), supervisor, providerName);

                supervisor.AddNode(merge);
                current = merged.Reader;
            }

            return current;
        }
    }
}