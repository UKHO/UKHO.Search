using System.IO.Compression;
using Microsoft.Extensions.Logging;
using UKHO.Search.Configuration;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Providers.FileShare.Enrichment
{
    public sealed class BatchContentEnricher : IIngestionEnricher
    {
        private readonly ILogger<BatchContentEnricher> _logger;
        private readonly IFileShareZipDownloader _zipDownloader;
        private readonly IEnumerable<IBatchContentHandler> _handlers;
        private readonly IngestionModeOptions _ingestionModeOptions;

        public BatchContentEnricher(IFileShareZipDownloader zipDownloader, IEnumerable<IBatchContentHandler> handlers, ILogger<BatchContentEnricher> logger, IngestionModeOptions ingestionModeOptions)
        {
            ArgumentNullException.ThrowIfNull(zipDownloader);
            ArgumentNullException.ThrowIfNull(handlers);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(ingestionModeOptions);

            _zipDownloader = zipDownloader;
            _handlers = handlers;
            _logger = logger;
            _ingestionModeOptions = ingestionModeOptions;
        }

        public int Ordinal => 100;

        public async Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            var batchId = request.IndexItem?.Id;
            if (string.IsNullOrWhiteSpace(batchId))
            {
                return;
            }

            var workingDirectory = CreateWorkingDirectory(batchId);
            try
            {
                var zipFilePath = Path.Combine(workingDirectory, "batch.zip");
                var extractDirectory = Path.Combine(workingDirectory, "unzipped");

                var downloaded = await TryDownloadZipFileAsync(batchId, zipFilePath, cancellationToken)
                    .ConfigureAwait(false);

                if (!downloaded)
                {
                    return;
                }
                try
                {
                    ExtractZipFileSafely(zipFilePath, extractDirectory);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to unzip downloaded batch ZIP. BatchId={BatchId}", batchId);
                    throw;
                }

                try
                {
                    ExpandNestedZips(extractDirectory, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to unzip nested ZIP(s) within extracted batch. BatchId={BatchId}", batchId);
                    throw;
                }

                var paths = Directory.EnumerateFiles(extractDirectory, "*", SearchOption.AllDirectories)
                                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                     .ToList();

                foreach (var handler in _handlers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await handler.HandleFiles(paths, request, document, cancellationToken)
                                     .ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Batch content handler failed. BatchId={BatchId} Handler={HandlerType}", batchId, handler.GetType().FullName);
                    }
                }
            }
            finally
            {
                TryDeleteDirectory(workingDirectory);
            }
        }

        private static string CreateWorkingDirectory(string batchId)
        {
            var basePath = Path.Combine(Path.GetTempPath(), "ukho-search", "file-share", batchId, Guid.NewGuid()
                                                                                                                  .ToString("N"));
            Directory.CreateDirectory(basePath);
            return basePath;
        }

        private async Task<bool> TryDownloadZipFileAsync(string batchId, string zipFilePath, CancellationToken cancellationToken)
        {
            try
            {
                await using var stream = await _zipDownloader.DownloadZipFileAsync(batchId, cancellationToken)
                                                             .ConfigureAwait(false);

                await using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, System.IO.FileShare.None, 128 * 1024, true))
                {
                    await stream.CopyToAsync(fileStream, cancellationToken)
                                .ConfigureAwait(false);
                }

                return true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (_ingestionModeOptions.Mode == IngestionMode.BestEffort && IsZipNotFound(ex))
                {
                    _logger.LogWarning(ex, "ZIP not found in FileShare; skipping ZIP enrichment. BatchId={BatchId} IngestionMode={IngestionMode}", batchId, _ingestionModeOptions.Mode);
                    return false;
                }

                _logger.LogError(ex, "Failed to download ZIP from FileShare. BatchId={BatchId}", batchId);
                throw;
            }
        }

        private static bool IsZipNotFound(Exception ex)
        {
            // FileShare client currently surfaces "NotFoundHttpError" via the string representation of the failure.
            // We keep this detection narrowly scoped to avoid masking other errors.
            if (ex is InvalidOperationException && ex.Message.Contains("NotFoundHttpError", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
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
                entry.ExtractToFile(destinationPath, true);
            }
        }

        private void ExpandNestedZips(string extractDirectory, CancellationToken cancellationToken)
        {
            const int maxIterations = 25;

            var extracted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var extractedCount = 0;

            for (var i = 0; i < maxIterations; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var newExtractionsThisPass = 0;

                var zipFiles = Directory.EnumerateFiles(extractDirectory, "*.zip", SearchOption.AllDirectories)
                                         .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                                         .ToList();

                foreach (var zipFile in zipFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!extracted.Add(zipFile))
                    {
                        continue;
                    }

                    var destinationDirectory = Path.Combine(Path.GetDirectoryName(zipFile)!, Path.GetFileNameWithoutExtension(zipFile));

                    if (Directory.Exists(destinationDirectory) && Directory.EnumerateFileSystemEntries(destinationDirectory).Any())
                    {
                        _logger.LogWarning("Skipping nested ZIP extraction because destination directory already exists and is not empty. ZipFile={ZipFile} DestinationDirectory={DestinationDirectory}", zipFile, destinationDirectory);
                        continue;
                    }

                    try
                    {
                        ExtractZipFileSafely(zipFile, destinationDirectory);
                        extractedCount++;
                        newExtractionsThisPass++;

                        _logger.LogDebug("Extracted nested ZIP. ZipFile={ZipFile} DestinationDirectory={DestinationDirectory}", zipFile, destinationDirectory);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        throw new InvalidOperationException($"Failed to extract nested ZIP '{zipFile}'.", ex);
                    }
                }

                if (newExtractionsThisPass == 0)
                {
                    break;
                }
            }

            if (extractedCount > 0)
            {
                _logger.LogInformation("Extracted {NestedZipCount} nested ZIP(s) under '{ExtractDirectory}'.", extractedCount, extractDirectory);
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
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary working directory '{Directory}'.", directory);
            }
        }
    }
}
