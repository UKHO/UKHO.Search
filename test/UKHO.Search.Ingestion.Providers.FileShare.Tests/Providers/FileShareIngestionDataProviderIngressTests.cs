using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Providers.FileShare;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Messaging;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Providers
{
    public sealed class FileShareIngestionDataProviderIngressTests
    {
        [Fact]
        public async Task ProcessIngestionRequestAsync_enqueues_original_envelope_and_returns()
        {
            var provider = new FileShareIngestionDataProvider(1, NullLogger<FileShareIngestionDataProvider>.Instance);

            var request = new IngestionRequest(IngestionRequestType.IndexItem, new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null);

            var envelope = new Envelope<IngestionRequest>("doc-1", request);

            var testContextItem = new object();
            envelope.Context.SetItem("test", testContextItem);

            await provider.ProcessIngestionRequestAsync(envelope);

            provider.IngressReader.TryRead(out var queued)
                    .ShouldBeTrue();

            queued.ShouldBeSameAs(envelope);

            queued.Context.TryGetItem<object>("test", out var queuedContextItem)
                  .ShouldBeTrue();
            queuedContextItem.ShouldBeSameAs(testContextItem);

            queued.Context.TryGetItem<ProviderParameters>(ProviderEnvelopeContextKeys.ProviderParameters, out var providerParameters)
                  .ShouldBeTrue();
            providerParameters.ShouldNotBeNull();
            providerParameters!.Provider.ShouldBe(FileShareIngestionDataProviderFactory.ProviderName);
        }

        [Fact]
        public async Task ProcessIngestionRequestAsync_applies_backpressure_and_honours_cancellation()
        {
            var provider = new FileShareIngestionDataProvider(1, NullLogger<FileShareIngestionDataProvider>.Instance);

            var request = new IngestionRequest(IngestionRequestType.IndexItem, new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()), null, null);

            var envelope1 = new Envelope<IngestionRequest>("doc-1", request);
            var envelope2 = new Envelope<IngestionRequest>("doc-2", request);

            await provider.ProcessIngestionRequestAsync(envelope1);

            using var cts = new CancellationTokenSource();

            var blockedWrite = provider.ProcessIngestionRequestAsync(envelope2, cts.Token)
                                       .AsTask();

            await Task.Delay(50);
            blockedWrite.IsCompleted.ShouldBeFalse();

            cts.Cancel();

            await Should.ThrowAsync<OperationCanceledException>(async () => await blockedWrite);
        }
    }
}