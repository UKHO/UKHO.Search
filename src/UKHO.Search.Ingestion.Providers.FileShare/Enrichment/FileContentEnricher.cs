using System.IO.Compression;
using Kreuzberg;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment
{
    public sealed class FileContentEnricher : IIngestionEnricher
    {
        private readonly IFileShareZipDownloader _zipDownloader;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileContentEnricher> _logger;

        public FileContentEnricher(IFileShareZipDownloader zipDownloader, IConfiguration configuration, ILogger<FileContentEnricher> logger)
        {
            ArgumentNullException.ThrowIfNull(zipDownloader);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(logger);

            _zipDownloader = zipDownloader;
            _configuration = configuration;
            _logger = logger;
        }

        public int Ordinal => 100;

        public async Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            var batchId = request.AddItem?.Id ?? request.UpdateItem?.Id;
            if (string.IsNullOrWhiteSpace(batchId))
            {
                return;
            }

            var allowedExtensions = GetAllowedExtensions();
            if (allowedExtensions.Count == 0)
            {
                _logger.LogWarning("File content extraction disabled because configuration key '{ConfigKey}' is missing or empty. BatchId={BatchId}", "ingestion:fileContentExtractionAllowedExtensions", batchId);
                return;
            }

            var workingDirectory = CreateWorkingDirectory(batchId);
            try
            {
                var zipFilePath = Path.Combine(workingDirectory, "batch.zip");
                var extractDirectory = Path.Combine(workingDirectory, "unzipped");

                await DownloadZipFileAsync(batchId, zipFilePath, cancellationToken).ConfigureAwait(false);
                try
                {
                    ExtractZipFileSafely(zipFilePath, extractDirectory);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to unzip downloaded batch ZIP. BatchId={BatchId}", batchId);
                    throw;
                }

                await ExtractAndEnrichAsync(batchId, extractDirectory, allowedExtensions, document, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                TryDeleteDirectory(workingDirectory);
            }
        }

        private HashSet<string> GetAllowedExtensions()
        {
            var raw = _configuration["ingestion:fileContentExtractionAllowedExtensions"];
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new HashSet<string>(StringComparer.Ordinal);
            }

            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                var normalized = token.StartsWith(".", StringComparison.Ordinal) ? token : "." + token;
                set.Add(normalized.ToLowerInvariant());
            }

            return set;
        }

        private static string CreateWorkingDirectory(string batchId)
        {
            var basePath = Path.Combine(Path.GetTempPath(), "ukho-search", "fileshare", "kreuzberg", batchId, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(basePath);
            return basePath;
        }

        private async Task DownloadZipFileAsync(string batchId, string zipFilePath, CancellationToken cancellationToken)
        {
            try
            {
                await using var stream = await _zipDownloader.DownloadZipFileAsync(batchId, cancellationToken).ConfigureAwait(false);

                await using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, System.IO.FileShare.None, 128 * 1024, true))
                {
                    await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to download ZIP from FileShare. BatchId={BatchId}", batchId);
                throw;
            }
        }

        private static void ExtractZipFileSafely(string zipFilePath, string extractDirectory)
        {
            Directory.CreateDirectory(extractDirectory);

            var extractRootFullPath = Path.GetFullPath(extractDirectory);
            if (!extractRootFullPath.EndsWith(Path.DirectorySeparatorChar))
            {
                extractRootFullPath += Path.DirectorySeparatorChar;
            }

            using var zip = ZipFile.OpenRead(zipFilePath);

            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                var destinationPath = Path.GetFullPath(Path.Combine(extractDirectory, entry.FullName));
                if (!destinationPath.StartsWith(extractRootFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Zip entry path traversal detected: '{entry.FullName}'.");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, overwrite: true);
            }
        }

        private async Task ExtractAndEnrichAsync(string batchId, string extractDirectory, HashSet<string> allowedExtensions, CanonicalDocument document, CancellationToken cancellationToken)
        {
            var extractedFiles = Directory.EnumerateFiles(extractDirectory, "*", SearchOption.AllDirectories)
                                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                         .ToList();

            foreach (var filePath in extractedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
                {
                    continue;
                }

                try
                {
                    var result = await KreuzbergClient.ExtractFileAsync(filePath, config: null, cancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(result.Content))
                    {
                        continue;
                    }

                    document.SetContent(result.Content);

                    var keyword = Path.GetFileNameWithoutExtension(filePath);
                    document.SetKeyword(keyword);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to extract file content. BatchId={BatchId} FilePath={FilePath}", batchId, filePath);
                }
            }
        }

        private void TryDeleteDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary working directory '{Directory}'.", directory);
            }
        }
    }
}