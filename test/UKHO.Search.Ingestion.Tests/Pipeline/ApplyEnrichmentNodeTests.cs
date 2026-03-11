using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Pipeline.Nodes;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Rules;
using UKHO.Search.Ingestion.Tests.TestEnrichers;
using UKHO.Search.Ingestion.Tests.TestSupport;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Messaging;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class ApplyEnrichmentNodeTests
    {
        private static ServiceProvider CreateProvider(params IIngestionEnricher[] enrichers)
        {
            var services = new ServiceCollection();

            foreach (var enricher in enrichers)
            {
                services.AddScoped<IIngestionEnricher>(_ => enricher);
            }

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task Upsert_executes_enrichers_in_deterministic_order()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var calls = new List<string>();
            var enrichers = new IIngestionEnricher[]
            {
                new RecordingEnricherB(calls, 10),
                new RecordingEnricherA(calls, 10)
            };

            await using var provider = CreateProvider(enrichers);
            var node = new ApplyEnrichmentNode("enrich", input.Reader, output.Writer, deadLetter.Writer, provider.GetRequiredService<IServiceScopeFactory>());

            await node.StartAsync(CancellationToken.None);

            var add = new AddItemRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList());
            var request = new IngestionRequest(IngestionRequestType.AddItem, add, null, null, null);
            var doc = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            var ctx = new IngestionPipelineContext
            {
                Request = request,
                Operation = new UpsertOperation("doc-1", doc)
            };

            await input.Writer.WriteAsync(new Envelope<IngestionPipelineContext>("doc-1", ctx));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var _)
                  .ShouldBeTrue();

            deadLetter.Reader.TryRead(out var _)
                      .ShouldBeFalse();

            calls.ShouldBe(new[] { nameof(RecordingEnricherA), nameof(RecordingEnricherB) });
        }

        [Fact]
        public async Task Provider_name_is_set_in_ingestion_provider_context_for_enrichers()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var providerNames = new List<string?>();

            var services = new ServiceCollection();
            services.AddScoped<IIngestionProviderContext, TestIngestionProviderContext>();
            services.AddSingleton(providerNames);
            services.AddScoped<IIngestionEnricher, ProviderContextRecordingEnricher>();

            await using var provider = services.BuildServiceProvider();

            var node = new ApplyEnrichmentNode("enrich", input.Reader, output.Writer, deadLetter.Writer, provider.GetRequiredService<IServiceScopeFactory>(), providerName: "file-share");

            await node.StartAsync(CancellationToken.None);

            var add = new AddItemRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList());
            var request = new IngestionRequest(IngestionRequestType.AddItem, add, null, null, null);
            var doc = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await input.Writer.WriteAsync(new Envelope<IngestionPipelineContext>("doc-1", new IngestionPipelineContext
            {
                Request = request,
                Operation = new UpsertOperation("doc-1", doc)
            }));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            providerNames.ShouldBe(new[] { "file-share" });
        }

        [Fact]
        public async Task Non_upsert_operation_does_not_call_enrichers()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var calls = new List<string>();
            var enrichers = new IIngestionEnricher[]
            {
                new RecordingEnricherA(calls, 10)
            };

            await using var provider = CreateProvider(enrichers);
            var node = new ApplyEnrichmentNode("enrich", input.Reader, output.Writer, deadLetter.Writer, provider.GetRequiredService<IServiceScopeFactory>());

            await node.StartAsync(CancellationToken.None);

            var request = new IngestionRequest(IngestionRequestType.DeleteItem, null, null, new DeleteItemRequest("doc-1"), null);
            var ctx = new IngestionPipelineContext
            {
                Request = request,
                Operation = new DeleteOperation("doc-1")
            };

            await input.Writer.WriteAsync(new Envelope<IngestionPipelineContext>("doc-1", ctx));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var _)
                  .ShouldBeTrue();

            calls.ShouldBeEmpty();
        }

        [Fact]
        public async Task Enricher_can_access_request_and_document_via_ingestion_pipeline_context()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var enricher = new RequestEchoEnricher();

            await using var provider = CreateProvider(enricher);
            var node = new ApplyEnrichmentNode("enrich", input.Reader, output.Writer, deadLetter.Writer, provider.GetRequiredService<IServiceScopeFactory>());

            await node.StartAsync(CancellationToken.None);

            var request = new IngestionRequest(IngestionRequestType.AddItem, new AddItemRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null, null);
            var doc = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await input.Writer.WriteAsync(new Envelope<IngestionPipelineContext>("doc-1", new IngestionPipelineContext
            {
                Request = request,
                Operation = new UpsertOperation("doc-1", doc)
            }));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var outEnvelope)
                  .ShouldBeTrue();

            var upsert = outEnvelope.Payload.ShouldBeOfType<UpsertOperation>();
            upsert.Document.DocumentType.ShouldBe("AddItem");
            upsert.Document.Facets["enrichment_documentid"]
                  .ShouldBe(new[] { "doc-1" });
        }

        [Fact]
        public async Task Envelope_metadata_is_preserved_when_mapping_to_index_operation()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var messageId = Guid.NewGuid();
            var headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["h"] = "v" };
            var context = new MessageContext();
            context.AddBreadcrumb("upstream");

            var request = new IngestionRequest(IngestionRequestType.DeleteItem, null, null, new DeleteItemRequest("doc-1"), null);
            var payload = new IngestionPipelineContext
            {
                Request = request,
                Operation = new DeleteOperation("doc-1")
            };

            var envelope = new Envelope<IngestionPipelineContext>("doc-1", payload)
            {
                MessageId = messageId,
                CorrelationId = "corr",
                Attempt = 7,
                Headers = headers,
                Context = context
            };

            await using var provider = CreateProvider();
            var node = new ApplyEnrichmentNode("enrich", input.Reader, output.Writer, deadLetter.Writer, provider.GetRequiredService<IServiceScopeFactory>());

            await node.StartAsync(CancellationToken.None);

            await input.Writer.WriteAsync(envelope);
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var outEnvelope)
                  .ShouldBeTrue();

            outEnvelope.MessageId.ShouldBe(messageId);
            outEnvelope.CorrelationId.ShouldBe("corr");
            outEnvelope.Attempt.ShouldBe(7);
            outEnvelope.Headers.ShouldBeSameAs(headers);
            outEnvelope.Context.ShouldBeSameAs(context);

            outEnvelope.Context.Breadcrumbs.ShouldContain("upstream");
            outEnvelope.Context.Breadcrumbs.ShouldContain("enrich");
        }

        [Fact]
        public async Task Transient_exception_is_retried_until_success()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var delays = new List<TimeSpan>();

            Task Delay(TimeSpan delay, CancellationToken ct)
            {
                delays.Add(delay);
                return Task.CompletedTask;
            }

            var enricher = new FailingEnricher(10, 2, _ => new TimeoutException("timeout"));

            await using var provider = CreateProvider(enricher);

            var node = new ApplyEnrichmentNode("enrich", input.Reader, output.Writer, deadLetter.Writer, provider.GetRequiredService<IServiceScopeFactory>(), retryMaxAttempts: 5, retryBaseDelay: TimeSpan.FromMilliseconds(200), retryMaxDelay: TimeSpan.FromMilliseconds(5000), retryJitter: TimeSpan.Zero, delay: Delay);

            await node.StartAsync(CancellationToken.None);

            var request = new IngestionRequest(IngestionRequestType.AddItem, new AddItemRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null, null);
            var doc = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await input.Writer.WriteAsync(new Envelope<IngestionPipelineContext>("doc-1", new IngestionPipelineContext
            {
                Request = request,
                Operation = new UpsertOperation("doc-1", doc)
            }));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            enricher.CallCount.ShouldBe(3);
            delays.ShouldBe(new[] { TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(400) });

            output.Reader.TryRead(out var _)
                  .ShouldBeTrue();
            deadLetter.Reader.TryRead(out var _)
                      .ShouldBeFalse();
        }

        [Fact]
        public async Task Non_transient_exception_is_not_retried_and_is_dead_lettered()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var enricher = new FailingEnricher(10, 1, _ => new InvalidOperationException("boom"));

            await using var provider = CreateProvider(enricher);

            var node = new ApplyEnrichmentNode("enrich", input.Reader, output.Writer, deadLetter.Writer, provider.GetRequiredService<IServiceScopeFactory>(), retryMaxAttempts: 5, retryJitter: TimeSpan.Zero, delay: (_, _) => Task.CompletedTask);

            await node.StartAsync(CancellationToken.None);

            var request = new IngestionRequest(IngestionRequestType.AddItem, new AddItemRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null, null);
            var doc = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await input.Writer.WriteAsync(new Envelope<IngestionPipelineContext>("doc-1", new IngestionPipelineContext
            {
                Request = request,
                Operation = new UpsertOperation("doc-1", doc)
            }));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            enricher.CallCount.ShouldBe(1);

            output.Reader.TryRead(out var _)
                  .ShouldBeFalse();

            deadLetter.Reader.TryRead(out var failed)
                      .ShouldBeTrue();
            failed.Status.ShouldBe(MessageStatus.Failed);
        }

        [Fact]
        public async Task Transient_retry_exhaustion_is_dead_lettered()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var delays = new List<TimeSpan>();

            Task Delay(TimeSpan delay, CancellationToken ct)
            {
                delays.Add(delay);
                return Task.CompletedTask;
            }

            var enricher = new FailingEnricher(10, int.MaxValue, _ => new TimeoutException("timeout"));

            await using var provider = CreateProvider(enricher);

            var node = new ApplyEnrichmentNode("enrich", input.Reader, output.Writer, deadLetter.Writer, provider.GetRequiredService<IServiceScopeFactory>(), retryMaxAttempts: 5, retryBaseDelay: TimeSpan.FromMilliseconds(200), retryMaxDelay: TimeSpan.FromMilliseconds(5000), retryJitter: TimeSpan.Zero, delay: Delay);

            await node.StartAsync(CancellationToken.None);

            var request = new IngestionRequest(IngestionRequestType.AddItem, new AddItemRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null, null);
            var doc = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await input.Writer.WriteAsync(new Envelope<IngestionPipelineContext>("doc-1", new IngestionPipelineContext
            {
                Request = request,
                Operation = new UpsertOperation("doc-1", doc)
            }));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            enricher.CallCount.ShouldBe(6);
            delays.Count.ShouldBe(5);

            output.Reader.TryRead(out var _)
                  .ShouldBeFalse();

            deadLetter.Reader.TryRead(out var failed)
                      .ShouldBeTrue();
            failed.Status.ShouldBe(MessageStatus.Failed);
            failed.Error.ShouldNotBeNull();
            failed.Error!.Code.ShouldBe("ENRICHMENT_RETRIES_EXHAUSTED");
        }

        [Fact]
        public async Task TaskCanceledException_is_transient_when_token_is_not_cancelled()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionPipelineContext>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IndexOperation>>(1, true, true);

            var enricher = new FailingEnricher(10, 1, _ => new TaskCanceledException("tce"));

            await using var provider = CreateProvider(enricher);

            var node = new ApplyEnrichmentNode("enrich", input.Reader, output.Writer, deadLetter.Writer, provider.GetRequiredService<IServiceScopeFactory>(), retryMaxAttempts: 5, retryBaseDelay: TimeSpan.FromMilliseconds(200), retryJitter: TimeSpan.Zero, delay: (_, _) => Task.CompletedTask);

            await node.StartAsync(CancellationToken.None);

            var request = new IngestionRequest(IngestionRequestType.AddItem, new AddItemRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null, null);
            var doc = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await input.Writer.WriteAsync(new Envelope<IngestionPipelineContext>("doc-1", new IngestionPipelineContext
            {
                Request = request,
                Operation = new UpsertOperation("doc-1", doc)
            }));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            enricher.CallCount.ShouldBe(2);
            output.Reader.TryRead(out var _)
                  .ShouldBeTrue();
            deadLetter.Reader.TryRead(out var _)
                      .ShouldBeFalse();
        }

        [Fact]
        public void OperationCanceledException_is_not_transient_when_token_is_cancelled()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            ApplyEnrichmentNode.IsTransientException(new OperationCanceledException(), cts.Token)
                               .ShouldBeFalse();
        }
    }
}