using UKHO.ADDS.Clients.FileShareService.ReadOnly;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment
{
    public sealed class FileShareZipDownloader : IFileShareZipDownloader
    {
        private readonly IFileShareReadOnlyClient _fileShareClient;

        public FileShareZipDownloader(IFileShareReadOnlyClient fileShareClient)
        {
            ArgumentNullException.ThrowIfNull(fileShareClient);
            _fileShareClient = fileShareClient;
        }

        public async Task<Stream> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(batchId);

            var result = await _fileShareClient.DownloadZipFileAsync(batchId, cancellationToken).ConfigureAwait(false);
            if (!result.IsSuccess(out var stream, out var error) || stream is null)
            {
                throw new InvalidOperationException($"Failed to download ZIP from FileShare for batch '{batchId}': {error}");
            }

            return stream;
        }
    }
}
