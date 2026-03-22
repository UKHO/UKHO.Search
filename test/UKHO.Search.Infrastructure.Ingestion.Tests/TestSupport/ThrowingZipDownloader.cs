using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;

namespace UKHO.Search.Ingestion.Tests.TestSupport
{
    internal sealed class ThrowingZipDownloader : IFileShareZipDownloader
    {
        private readonly Exception _exception;

        public ThrowingZipDownloader(Exception? exception = null)
        {
            _exception = exception ?? new InvalidOperationException("ZIP download is not used in this test.");
        }

        public Task<Stream> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken = default)
        {
            throw _exception;
        }
    }
}