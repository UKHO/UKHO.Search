namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment
{
    public interface IFileShareZipDownloader
    {
        Task<Stream> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken = default);
    }
}