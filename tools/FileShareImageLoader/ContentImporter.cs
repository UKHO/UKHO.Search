using Azure;
using Azure.Storage.Blobs;

namespace FileShareImageLoader
{
    public sealed class ContentImporter
    {
        private readonly BlobServiceClient _blobServiceClient;

        public ContentImporter(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        public async Task ImportAsync(string environmentName, CancellationToken cancellationToken = default)
        {
            var dataRoot = "/data";
            var contentRoot = Path.Combine(dataRoot, "content");

            if (!Directory.Exists(contentRoot))
            {
                Console.WriteLine($"[ContentImporter] No seeded content directory found at '{contentRoot}'. Skipping copy.");
                return;
            }

            var containerName = environmentName;

            Console.WriteLine($"[ContentImporter] Copying seeded content to container '{containerName}'...");

            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

            try
            {
                Console.WriteLine("[ContentImporter] Ensuring container exists...");
                await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException ex)
            {
                LogRequestFailedException("CreateIfNotExists", ex);
                throw;
            }

            var zipFiles = Directory.EnumerateFiles(contentRoot, "*.zip", SearchOption.AllDirectories).ToList();

            Console.WriteLine($"[ContentImporter] Found {zipFiles.Count:N0} zip files.");

            var uploadedFiles = 0;
            long uploadedBytes = 0;
            var started = DateTimeOffset.UtcNow;

            foreach (var zipPath in zipFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var name = Path.GetFileNameWithoutExtension(zipPath);
                if (!Guid.TryParse(name, out _)) continue;

                var blobName = $"{name}/{name}.zip";
                var blobClient = containerClient.GetBlobClient(blobName);

                if (await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                var fileInfo = new FileInfo(zipPath);
                try
                {
                    await using var fileStream = File.OpenRead(zipPath);
                    await blobClient.UploadAsync(fileStream, overwrite: true, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (RequestFailedException ex)
                {
                    LogRequestFailedException($"Upload:{blobName}", ex);
                    throw;
                }

                uploadedFiles++;
                uploadedBytes += fileInfo.Length;

                if (uploadedFiles % 100 == 0)
                {
                    var elapsed = DateTimeOffset.UtcNow - started;
                    Console.WriteLine(
                        $"[ContentImporter] Uploaded {uploadedFiles} files ({uploadedBytes:N0} bytes) in {elapsed.TotalMinutes:N1} min");
                }

            }

            var totalElapsed = DateTimeOffset.UtcNow - started;
            Console.WriteLine(
                $"[ContentImporter] Completed. Uploaded {uploadedFiles} files ({uploadedBytes:N0} bytes) in {totalElapsed.TotalMinutes:N1} min");
        }

        private static void LogRequestFailedException(string operation, RequestFailedException ex)
        {
            Console.Error.WriteLine($"[ContentImporter] Storage request failed during '{operation}'.");
            Console.Error.WriteLine($"[ContentImporter] Status={ex.Status} ErrorCode='{ex.ErrorCode}'");

            if (!string.IsNullOrWhiteSpace(ex.Message))
            {
                Console.Error.WriteLine($"[ContentImporter] Message: {ex.Message}");
            }

            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                Console.Error.WriteLine(ex.StackTrace);
            }

            if (!string.IsNullOrWhiteSpace(ex.ErrorCode))
            {
                Console.Error.WriteLine($"[ContentImporter] ErrorCode: {ex.ErrorCode}");
            }

            // Note: Response headers/body are not consistently exposed on all Azure.Core versions.
        }

    }
}
