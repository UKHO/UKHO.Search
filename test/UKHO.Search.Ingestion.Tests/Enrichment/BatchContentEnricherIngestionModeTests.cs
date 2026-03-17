using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Configuration;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Enrichment
{
    public sealed class BatchContentEnricherIngestionModeTests
    {
        [Fact]
        public async Task BestEffort_missing_zip_skips_enrichment_and_does_not_throw()
        {
            var downloader = new FakeZipDownloader(_ => throw new InvalidOperationException("Failed to download ZIP from FileShare for batch 'batch-1': NotFoundHttpError { Message = \"The requested resource was not found\" }"));

            var handler = new RecordingHandler(_ => throw new InvalidOperationException("should not be called"));

            var enricher = new BatchContentEnricher(downloader, new[] { handler }, NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.BestEffort));

            var request = CreateAddRequest("batch-1");
            var document = CanonicalDocument.CreateMinimal("doc-1", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            handler.CallCount.ShouldBe(0);
        }

        [Fact]
        public async Task BestEffort_non_notfound_failure_still_throws()
        {
            var downloader = new FakeZipDownloader(_ => throw new InvalidOperationException("some other failure"));

            var handler = new RecordingHandler(_ => throw new InvalidOperationException("should not be called"));

            var enricher = new BatchContentEnricher(downloader, new[] { handler }, NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.BestEffort));

            var request = CreateAddRequest("batch-1");
            var document = CanonicalDocument.CreateMinimal("doc-1", request.IndexItem!, request.IndexItem.Timestamp);

            await Should.ThrowAsync<InvalidOperationException>(() => enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None));

            handler.CallCount.ShouldBe(0);
        }

        [Fact]
        public async Task Strict_notfound_failure_throws()
        {
            var downloader = new FakeZipDownloader(_ => throw new InvalidOperationException("Failed to download ZIP from FileShare for batch 'batch-1': NotFoundHttpError { Message = \"The requested resource was not found\" }"));

            var handler = new RecordingHandler(_ => throw new InvalidOperationException("should not be called"));

            var enricher = new BatchContentEnricher(downloader, new[] { handler }, NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-1");
            var document = CanonicalDocument.CreateMinimal("doc-1", request.IndexItem!, request.IndexItem.Timestamp);

            await Should.ThrowAsync<InvalidOperationException>(() => enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None));

            handler.CallCount.ShouldBe(0);
        }

        private static IngestionRequest CreateAddRequest(string batchId)
        {
            var add = new IndexRequest(batchId, Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList());
            return new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);
        }

        private sealed class FakeZipDownloader : IFileShareZipDownloader
        {
            private readonly Func<string, Stream> _download;

            public FakeZipDownloader(Func<string, Stream> download)
            {
                _download = download;
            }

            public Task<Stream> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_download(batchId));
            }
        }

        private sealed class RecordingHandler : IBatchContentHandler
        {
            private readonly Action<IEnumerable<string>> _capture;

            public RecordingHandler(Action<IEnumerable<string>> capture)
            {
                _capture = capture;
            }

            public int CallCount { get; private set; }

            public Task HandleFiles(IEnumerable<string> paths, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
            {
                CallCount++;
                _capture(paths);
                return Task.CompletedTask;
            }
        }
    }
}
