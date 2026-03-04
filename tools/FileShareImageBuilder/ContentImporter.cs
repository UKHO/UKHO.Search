using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.Search.Configuration;

namespace FileShareImageBuilder;

public class ContentImporter
{
    private readonly IFileShareReadOnlyClient _fileShareClient;

    public ContentImporter(IFileShareReadOnlyClient fileShareClient)
    {
        _fileShareClient = fileShareClient;
    }

    public async Task ImportAsync(CancellationToken cancellationToken = default)
    {
        var environmentName = Environment.GetEnvironmentVariable("environment");
        if (string.IsNullOrWhiteSpace(environmentName))
            throw new InvalidOperationException("Missing required environment variable 'environment'.");

        var dataImagePath = ConfigurationReader.GetDataImagePath();
        var binDirectory = Path.Combine(dataImagePath, "bin");
        var contentDirectory = Path.Combine(binDirectory, "content");

        // Recreate content directory to ensure the on-disk set matches the current run.
        RecreateEmptyDirectory(contentDirectory);

        var invalidFilePath = Path.Combine(dataImagePath, "invalid.json");
        var invalidBatchIds = await ReadInvalidBatchIdsAsync(invalidFilePath, cancellationToken).ConfigureAwait(false);

        await using var cancellationRegistration = cancellationToken.Register(() =>
        {
            try
            {
                WriteInvalidBatchIds(invalidFilePath, invalidBatchIds);
            }
            catch
            {
            }
        });

        var maxBytes = ConfigurationReader.GetDataImageBinSizeGB() * 1024L * 1024L * 1024L;
        long totalBytesDownloaded = 0;

        var maxBatchCount = ConfigurationReader.GetDataImageCount();

        var targetConnectionString =
            ConfigurationReader.GetTargetDatabaseConnectionString(StorageNames.FileShareEmulatorDatabase);

        var failedBatchIds = new HashSet<Guid>();

        const int pageSize = 1000;
        DateTime? lastCreatedOn = null;
        Guid? lastId = null;
        var totalBatchesDownloaded = 0;
        var totalBatchesFailed = 0;
        var totalBatchesProcessed = 0;
        var pageNumber = 0;

        while (totalBytesDownloaded < maxBytes)
        {
            if (totalBatchesDownloaded >= maxBatchCount)
            {
                Console.WriteLine($"[ContentImporter] Reached batch count limit. Downloaded {totalBatchesDownloaded}, failed {totalBatchesFailed}, {totalBytesDownloaded:N0}/{maxBytes:N0} bytes.");
                break;
            }

            pageNumber++;

            var batchIds = await GetBatchIdsPageAsync(
                targetConnectionString,
                pageSize,
                lastCreatedOn,
                lastId,
                invalidBatchIds,
                cancellationToken).ConfigureAwait(false);

            if (batchIds.Count == 0)
            {
                Console.WriteLine($"[ContentImporter] No more batches found. Downloaded {totalBatchesDownloaded} batches, {totalBatchesFailed} failed, {totalBytesDownloaded:N0} bytes.");
                break;
            }

            foreach (var batch in batchIds)
            {
                if (totalBatchesDownloaded >= maxBatchCount)
                {
                    Console.WriteLine($"[ContentImporter] Reached batch count limit. Downloaded {totalBatchesDownloaded}, failed {totalBatchesFailed}, {totalBytesDownloaded:N0}/{maxBytes:N0} bytes.");
                    break;
                }

                var batchIdString = batch.Id.ToString("D");
                var batchId = batch.Id;

                // Ensure we never retry batches already marked invalid.
                // Skip batches already known to be invalid so they are not retried across pages/runs.
                if (invalidBatchIds.Contains(batchId))
                {
                    totalBatchesProcessed++;
                    lastCreatedOn = batch.CreatedOn;
                    lastId = batch.Id;

                    continue;
                }

                var shard = GetMostSignificantByteHex(batch.Id);
                var shardDirectory = Path.Combine(contentDirectory, shard);
                Directory.CreateDirectory(shardDirectory);

                var zipPath = Path.Combine(shardDirectory, $"{batchIdString}.zip");

                try
                {
                    var result = await _fileShareClient.DownloadZipFileAsync(batchIdString, cancellationToken)
                        .ConfigureAwait(false);
                    if (!result.IsSuccess(out var stream, out var error) || stream is null)
                    {
                        totalBatchesFailed++;
                        Console.WriteLine($"[ContentImporter] Failed batch '{batchIdString}': {error}");

                        failedBatchIds.Add(batchId);
                        invalidBatchIds.Add(batchId);

                        await WriteInvalidBatchIdsAsync(invalidFilePath, invalidBatchIds, cancellationToken)
                            .ConfigureAwait(false);

                        totalBatchesProcessed++;
                        lastCreatedOn = batch.CreatedOn;
                        
                        lastId = batch.Id;
                        continue;
                    }

                    await using (stream.ConfigureAwait(false))
                    await using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write,
                                     FileShare.None, 128 * 1024, true))
                    {
                        await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    totalBatchesFailed++;
                    Console.WriteLine($"[ContentImporter] Failed batch '{batchIdString}': {ex.GetType().Name}: {ex.Message}");

                    failedBatchIds.Add(batchId);
                    invalidBatchIds.Add(batchId);
                    
                    await WriteInvalidBatchIdsAsync(invalidFilePath, invalidBatchIds, cancellationToken)
                        .ConfigureAwait(false);
                    
                    totalBatchesProcessed++;
                    lastCreatedOn = batch.CreatedOn;
                    lastId = batch.Id;
                    
                    continue;
                }

                var fileLength = new FileInfo(zipPath).Length;
                totalBytesDownloaded += fileLength;
                totalBatchesDownloaded++;
                totalBatchesProcessed++;

                lastCreatedOn = batch.CreatedOn;
                lastId = batch.Id;

                if (totalBatchesProcessed % 100 == 0)
                {
                    Console.WriteLine(
                        $"[ContentImporter] Progress: downloaded {totalBatchesDownloaded}, failed {totalBatchesFailed} (processed {totalBatchesProcessed}), {totalBytesDownloaded:N0}/{maxBytes:N0} bytes.");
                }

                if (totalBytesDownloaded >= maxBytes)
                {
                    Console.WriteLine($"[ContentImporter] Reached download limit. Downloaded {totalBatchesDownloaded}, failed {totalBatchesFailed}, {totalBytesDownloaded:N0}/{maxBytes:N0} bytes.");
                    break;
                }
            }

            if (totalBatchesDownloaded >= maxBatchCount || totalBytesDownloaded >= maxBytes) break;

            // lastCreatedOn/lastId are now updated per-batch to avoid retrying failed ones.
        }

        await WriteInvalidBatchIdsAsync(invalidFilePath, invalidBatchIds, cancellationToken).ConfigureAwait(false);
    }

    private static void WriteInvalidBatchIds(string invalidFilePath, HashSet<Guid> invalidBatchIds)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(invalidFilePath)!);

        var options = new JsonSerializerOptions { WriteIndented = true };

        File.WriteAllText(invalidFilePath, JsonSerializer.Serialize(invalidBatchIds.OrderBy(x => x).ToList(), options));
    }

    private static async Task<HashSet<Guid>> ReadInvalidBatchIdsAsync(string invalidFilePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(invalidFilePath)) return new HashSet<Guid>();

        await using var stream = File.OpenRead(invalidFilePath);

        var ids = await JsonSerializer.DeserializeAsync<List<Guid>>(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return ids is { Count: > 0 }
            ? new HashSet<Guid>(ids)
            : new HashSet<Guid>();
    }

    private static async Task WriteInvalidBatchIdsAsync(string invalidFilePath, HashSet<Guid> invalidBatchIds,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(invalidFilePath)!);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        await using var stream = File.Create(invalidFilePath);

        await JsonSerializer
            .SerializeAsync(stream, invalidBatchIds.OrderBy(x => x).ToList(), options, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<List<(Guid Id, DateTime CreatedOn)>> GetBatchIdsPageAsync(
        string targetConnectionString,
        int pageSize,
        DateTime? lastCreatedOn,
        Guid? lastId,
        HashSet<Guid> invalidBatchIds,
        CancellationToken cancellationToken)
    {
        await using var sqlConnection = new SqlConnection(targetConnectionString);
        await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = sqlConnection.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandTimeout = 30;

        if (lastCreatedOn is null || lastId is null)
        {
            cmd.CommandText = @"SELECT TOP (@pageSize) [Id], [CreatedOn]
FROM [Batch]
WHERE [Status] = 3
ORDER BY [CreatedOn] DESC, [Id] DESC;";
        }
        else
        {
            cmd.CommandText = @"SELECT TOP (@pageSize) [Id], [CreatedOn]
FROM [Batch]
WHERE [Status] = 3
  AND (([CreatedOn] < @lastCreatedOn) OR ([CreatedOn] = @lastCreatedOn AND [Id] < @lastId))
ORDER BY [CreatedOn] DESC, [Id] DESC;";

            cmd.Parameters.Add(new SqlParameter("@lastCreatedOn", SqlDbType.DateTime2) { Value = lastCreatedOn.Value });
            cmd.Parameters.Add(new SqlParameter("@lastId", SqlDbType.UniqueIdentifier) { Value = lastId.Value });
        }

        cmd.Parameters.Add(new SqlParameter("@pageSize", SqlDbType.Int) { Value = pageSize });

        var results = new List<(Guid Id, DateTime CreatedOn)>(pageSize);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var id = reader.GetGuid(0);
            var createdOn = reader.GetDateTime(1);
            if (!invalidBatchIds.Contains(id)) results.Add((id, createdOn));
        }

        return results;
    }

    private static void RecreateEmptyDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath)) Directory.Delete(directoryPath, true);

        Directory.CreateDirectory(directoryPath);
    }

    private static string GetMostSignificantByteHex(Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!guid.TryWriteBytes(bytes)) bytes = guid.ToByteArray();

        return bytes[0].ToString("X2");
    }
}