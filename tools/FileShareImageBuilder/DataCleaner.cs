using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using UKHO.Search.Configuration;

namespace FileShareImageBuilder
{
    public sealed class DataCleaner
    {
        public async Task CleanAsync(CancellationToken cancellationToken = default)
        {
            var dataImagePath = ConfigurationReader.GetDataImagePath();
            var invalidFilePath = Path.Combine(dataImagePath, "invalid.json");

            // The local DB should contain only:
            // - committed batches that were successfully downloaded, and
            // - no batches explicitly marked invalid.
            var invalidIds = await ReadInvalidIdsAsync(invalidFilePath, cancellationToken)
                .ConfigureAwait(false);
            var downloadedBatchIds = GetDownloadedBatchIds(dataImagePath);

            Console.WriteLine($"[DataCleaner] Downloaded batch zip files found: {downloadedBatchIds.Count}");
            Console.WriteLine($"[DataCleaner] Invalid batch ids found: {invalidIds.Count}");

            var targetConnectionString = ConfigurationReader.GetTargetDatabaseConnectionString(StorageNames.FileShareEmulatorDatabase);

            await using var sqlConnection = new SqlConnection(targetConnectionString);
            await sqlConnection.OpenAsync(cancellationToken)
                               .ConfigureAwait(false);

            var deletedInvalidBatchIds = 0;
            var deletedInvalidRows = 0;
            foreach (var batchId in invalidIds)
            {
                var result = await DeleteBatchAsync(sqlConnection, batchId, cancellationToken)
                    .ConfigureAwait(false);
                if (result.BatchDeleted)
                {
                    deletedInvalidBatchIds++;
                }

                deletedInvalidRows += result.RowsAffected;
            }

            Console.WriteLine($"[DataCleaner] Deleted invalid batch ids: {deletedInvalidBatchIds}");
            Console.WriteLine($"[DataCleaner] Deleted invalid rows: {deletedInvalidRows}");

            if (invalidIds.Count > 0)
            {
                try
                {
                    File.Delete(invalidFilePath);
                    Console.WriteLine($"[DataCleaner] Deleted invalid file: {invalidFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DataCleaner] Failed to delete invalid file '{invalidFilePath}': {ex.GetType().Name}: {ex.Message}");
                }
            }

            var deletedNotDownloaded = await DeleteCommittedBatchesNotDownloadedAsync(sqlConnection, downloadedBatchIds, cancellationToken)
                .ConfigureAwait(false);
            Console.WriteLine($"[DataCleaner] Deleted committed batches not downloaded: {deletedNotDownloaded}");

            var deletedNonCommitted = await DeleteNonCommittedBatchesAsync(sqlConnection, cancellationToken)
                .ConfigureAwait(false);
            Console.WriteLine($"[DataCleaner] Deleted non-committed batches: {deletedNonCommitted}");
        }

        private static async Task<HashSet<Guid>> ReadInvalidIdsAsync(string invalidFilePath, CancellationToken cancellationToken)
        {
            if (!File.Exists(invalidFilePath))
            {
                return new HashSet<Guid>();
            }

            await using var stream = File.OpenRead(invalidFilePath);
            var ids = await JsonSerializer.DeserializeAsync<List<Guid>>(stream, cancellationToken: cancellationToken)
                                          .ConfigureAwait(false);
            return ids is { Count: > 0 } ? new HashSet<Guid>(ids) : new HashSet<Guid>();
        }

        private static HashSet<Guid> GetDownloadedBatchIds(string dataImagePath)
        {
            var contentDir = Path.Combine(dataImagePath, "bin", "content");
            if (!Directory.Exists(contentDir))
            {
                return new HashSet<Guid>();
            }

            var ids = new HashSet<Guid>();
            foreach (var file in Directory.EnumerateFiles(contentDir, "*.zip", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (Guid.TryParseExact(name, "D", out var id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private static async Task<DeleteBatchResult> DeleteBatchAsync(SqlConnection sqlConnection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"DELETE FA
FROM [FileAttribute] FA
JOIN [File] F ON F.[Id] = FA.[FileId]
WHERE F.[BatchId] = @id;

DELETE FROM [BatchReadGroup]
WHERE [BatchId] = @id;

DELETE FROM [BatchReadUser]
WHERE [BatchId] = @id;

DELETE FROM [BatchAttribute]
WHERE [BatchId] = @id;

DELETE FROM [File]
WHERE [BatchId] = @id;

DELETE FROM [Batch]
WHERE [Id] = @id;

SELECT @@ROWCOUNT;";

            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = batchId });
            var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken)
                                        .ConfigureAwait(false);

            // We can't reliably infer whether the Batch row was deleted from total affected rows.
            // Perform a targeted delete to detect the batch delete outcome without re-deleting dependents.
            await using var batchDeleteCmd = sqlConnection.CreateCommand();
            batchDeleteCmd.CommandType = CommandType.Text;
            batchDeleteCmd.CommandTimeout = 30;
            batchDeleteCmd.CommandText = @"DELETE FROM [Batch]
WHERE [Id] = @id;";
            batchDeleteCmd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = batchId });
            var batchDeleted = await batchDeleteCmd.ExecuteNonQueryAsync(cancellationToken)
                                                   .ConfigureAwait(false) > 0;

            return new DeleteBatchResult(batchDeleted, rowsAffected);
        }

        private static async Task<int> DeleteCommittedBatchesNotDownloadedAsync(SqlConnection sqlConnection, HashSet<Guid> downloadedBatchIds, CancellationToken cancellationToken)
        {
            await using var cmd = sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;

            if (downloadedBatchIds.Count == 0)
            {
                cmd.CommandText = @"DELETE FA
FROM [FileAttribute] FA
JOIN [File] F ON F.[Id] = FA.[FileId]
JOIN [Batch] B ON B.[Id] = F.[BatchId]
WHERE B.[Status] = 3;

DELETE FROM [BatchReadGroup]
WHERE [BatchId] IN (SELECT [Id] FROM [Batch] WHERE [Status] = 3);

DELETE FROM [BatchReadUser]
WHERE [BatchId] IN (SELECT [Id] FROM [Batch] WHERE [Status] = 3);

DELETE FROM [BatchAttribute]
WHERE [BatchId] IN (SELECT [Id] FROM [Batch] WHERE [Status] = 3);

DELETE FROM [File]
WHERE [BatchId] IN (SELECT [Id] FROM [Batch] WHERE [Status] = 3);

DELETE FROM [Batch]
WHERE [Status] = 3;";

                return await cmd.ExecuteNonQueryAsync(cancellationToken)
                                .ConfigureAwait(false);
            }

            // Use a temp table to avoid exceeding SQL Server's 2100 parameter limit.
            cmd.CommandText = @"CREATE TABLE #DownloadedBatchIds ([Id] uniqueidentifier NOT NULL PRIMARY KEY);
";

            await cmd.ExecuteNonQueryAsync(cancellationToken)
                     .ConfigureAwait(false);

            await BulkInsertDownloadedIdsAsync(sqlConnection, downloadedBatchIds, cancellationToken)
                .ConfigureAwait(false);

            await using var deleteCmd = sqlConnection.CreateCommand();
            deleteCmd.CommandType = CommandType.Text;
            deleteCmd.CommandTimeout = 0;

            deleteCmd.CommandText = @"DELETE FA
FROM [FileAttribute] FA
JOIN [File] F ON F.[Id] = FA.[FileId]
JOIN [Batch] B ON B.[Id] = F.[BatchId]
WHERE B.[Status] = 3
  AND NOT EXISTS (SELECT 1 FROM #DownloadedBatchIds D WHERE D.[Id] = B.[Id]);

DELETE BRG
FROM [BatchReadGroup] BRG
JOIN [Batch] B ON B.[Id] = BRG.[BatchId]
WHERE B.[Status] = 3
  AND NOT EXISTS (SELECT 1 FROM #DownloadedBatchIds D WHERE D.[Id] = B.[Id]);

DELETE BRU
FROM [BatchReadUser] BRU
JOIN [Batch] B ON B.[Id] = BRU.[BatchId]
WHERE B.[Status] = 3
  AND NOT EXISTS (SELECT 1 FROM #DownloadedBatchIds D WHERE D.[Id] = B.[Id]);

DELETE BA
FROM [BatchAttribute] BA
JOIN [Batch] B ON B.[Id] = BA.[BatchId]
WHERE B.[Status] = 3
  AND NOT EXISTS (SELECT 1 FROM #DownloadedBatchIds D WHERE D.[Id] = B.[Id]);

DELETE F
FROM [File] F
JOIN [Batch] B ON B.[Id] = F.[BatchId]
WHERE B.[Status] = 3
  AND NOT EXISTS (SELECT 1 FROM #DownloadedBatchIds D WHERE D.[Id] = B.[Id]);

DELETE FROM [Batch]
WHERE [Status] = 3
  AND NOT EXISTS (SELECT 1 FROM #DownloadedBatchIds D WHERE D.[Id] = [Batch].[Id]);

DROP TABLE #DownloadedBatchIds;";

            return await deleteCmd.ExecuteNonQueryAsync(cancellationToken)
                                  .ConfigureAwait(false);
        }

        private static async Task BulkInsertDownloadedIdsAsync(SqlConnection sqlConnection, HashSet<Guid> downloadedBatchIds, CancellationToken cancellationToken)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(Guid));
            foreach (var id in downloadedBatchIds)
            {
                table.Rows.Add(id);
            }

            using var bulkCopy = new SqlBulkCopy(sqlConnection)
            {
                DestinationTableName = "#DownloadedBatchIds",
                BulkCopyTimeout = 0
            };

            await bulkCopy.WriteToServerAsync(table, cancellationToken)
                          .ConfigureAwait(false);
        }

        private static async Task<int> DeleteNonCommittedBatchesAsync(SqlConnection sqlConnection, CancellationToken cancellationToken)
        {
            await using var cmd = sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 0;

            cmd.CommandText = @"DELETE FA
FROM [FileAttribute] FA
JOIN [File] F ON F.[Id] = FA.[FileId]
JOIN [Batch] B ON B.[Id] = F.[BatchId]
WHERE B.[Status] <> 3;

DELETE BRG
FROM [BatchReadGroup] BRG
JOIN [Batch] B ON B.[Id] = BRG.[BatchId]
WHERE B.[Status] <> 3;

DELETE BRU
FROM [BatchReadUser] BRU
JOIN [Batch] B ON B.[Id] = BRU.[BatchId]
WHERE B.[Status] <> 3;

DELETE BA
FROM [BatchAttribute] BA
JOIN [Batch] B ON B.[Id] = BA.[BatchId]
WHERE B.[Status] <> 3;

DELETE F
FROM [File] F
JOIN [Batch] B ON B.[Id] = F.[BatchId]
WHERE B.[Status] <> 3;

DELETE FROM [Batch]
WHERE [Status] <> 3;";

            return await cmd.ExecuteNonQueryAsync(cancellationToken)
                            .ConfigureAwait(false);
        }

        private sealed record DeleteBatchResult(bool BatchDeleted, int RowsAffected);
    }
}