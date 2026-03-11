using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;

namespace UKHO.Search.Ingestion.Tests.TestSupport
{
    internal sealed class ThrowingZipDownloader : IFileShareZipDownloader
    {
        public Task<Stream> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("ZIP download is not used in this test.");
        }
    }
}