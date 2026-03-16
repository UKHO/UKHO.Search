using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;
using Shouldly;
using Xunit;

namespace UKHO.Search.Tests.FileShareImageBuilder
{
    public sealed class DataCleanerIngestionModeTests
    {
        [Fact]
        public async Task BestEffort_does_not_delete_committed_batches_when_no_downloaded_files_exist()
        {
            var tempDbName = $"fsib_{Guid.NewGuid():N}";
            await using var sql = await CreateDatabaseAsync(tempDbName);
            await CreateSchemaAsync(sql);

            var committedBatchId = Guid.NewGuid();
            await InsertBatchAsync(sql, committedBatchId, status: 3);

            var tempRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"fsib-{Guid.NewGuid():N}"));
            try
            {
                Environment.SetEnvironmentVariable("ingestionmode", "BestEffort");
                Environment.SetEnvironmentVariable($"ConnectionStrings__{UKHO.Search.Configuration.StorageNames.FileShareEmulatorDatabase}", sql.ConnectionString);

                await using var overrideConfig = CreateOverrideConfiguration(tempRoot.FullName);
                var cleaner = new global::FileShareImageBuilder.DataCleaner();

                await cleaner.CleanAsync();

                (await BatchExistsAsync(sql, committedBatchId)).ShouldBeTrue();
            }
            finally
            {
                Environment.SetEnvironmentVariable("ingestionmode", null);
                Environment.SetEnvironmentVariable($"ConnectionStrings__{UKHO.Search.Configuration.StorageNames.FileShareEmulatorDatabase}", null);
                try { tempRoot.Delete(recursive: true); } catch { }
            }
        }

        [Fact]
        public async Task Strict_deletes_committed_batches_when_no_downloaded_files_exist()
        {
            var tempDbName = $"fsib_{Guid.NewGuid():N}";

            await using var sql = await CreateDatabaseAsync(tempDbName);
            await CreateSchemaAsync(sql);

            var committedBatchId = Guid.NewGuid();
            await InsertBatchAsync(sql, committedBatchId, status: 3);

            var deleted = await DataCleanerTestHooks.DeleteCommittedBatchesNotDownloadedStrictAsync(sql, downloadedBatchIds: new HashSet<Guid>());

            deleted.ShouldBeGreaterThan(0);
            (await BatchExistsAsync(sql, committedBatchId)).ShouldBeFalse();
        }

        [Fact]
        public async Task Both_modes_delete_non_committed_batches()
        {
            var tempDbName = $"fsib_{Guid.NewGuid():N}";

            await using var sql = await CreateDatabaseAsync(tempDbName);
            await CreateSchemaAsync(sql);

            var nonCommittedBatchId = Guid.NewGuid();
            await InsertBatchAsync(sql, nonCommittedBatchId, status: 2);

            var deleted = await DataCleanerTestHooks.DeleteNonCommittedBatchesAsync(sql);

            deleted.ShouldBeGreaterThan(0);
            (await BatchExistsAsync(sql, nonCommittedBatchId)).ShouldBeFalse();
        }

        private static async Task<SqlConnection> CreateDatabaseAsync(string databaseName)
        {
            var master = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;MultipleActiveResultSets=true");
            await master.OpenAsync();

            await using (var cmd = master.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = $"CREATE DATABASE [{databaseName}]";
                await cmd.ExecuteNonQueryAsync();
            }

            var db = new SqlConnection($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true");
            await db.OpenAsync();
            return db;
        }

        private static async Task CreateSchemaAsync(SqlConnection sql)
        {
            await using var cmd = sql.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
CREATE TABLE [Batch] (
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [Status] int NOT NULL
);
CREATE TABLE [File] (
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
    [BatchId] uniqueidentifier NOT NULL
);
CREATE TABLE [FileAttribute] (
    [FileId] uniqueidentifier NOT NULL
);
CREATE TABLE [BatchReadGroup] (
    [BatchId] uniqueidentifier NOT NULL
);
CREATE TABLE [BatchReadUser] (
    [BatchId] uniqueidentifier NOT NULL
);
CREATE TABLE [BatchAttribute] (
    [BatchId] uniqueidentifier NOT NULL
);
";
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task InsertBatchAsync(SqlConnection sql, Guid id, int status)
        {
            await using var cmd = sql.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "INSERT INTO [Batch] ([Id], [Status]) VALUES (@id, @status);";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@status", status);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<bool> BatchExistsAsync(SqlConnection sql, Guid id)
        {
            await using var cmd = sql.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT COUNT(1) FROM [Batch] WHERE [Id] = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            var scalar = await cmd.ExecuteScalarAsync();
            var count = Convert.ToInt32(scalar);
            return count > 0;
        }

        private static FileStream CreateOverrideConfiguration(string rootPath)
        {
            var toolBaseDir = Path.GetDirectoryName(typeof(global::FileShareImageBuilder.DataCleaner).Assembly.Location)!;
            var configPath = Path.Combine(toolBaseDir, "configuration.override.json");
            var backupPath = configPath + ".bak." + Guid.NewGuid().ToString("N");

            if (File.Exists(configPath))
            {
                File.Move(configPath, backupPath);
            }

            var escapedRoot = rootPath.Replace("\\", "\\\\");
            var payload = $"{{\"remoteService\":\"https://example.test\",\"environment\":\"test\",\"sourceDatabase\":\"Server=(localdb)\\\\MSSQLLocalDB;Database=master;Trusted_Connection=True;\",\"dataImagePath\":\"{escapedRoot}\",\"dataImageBinSizeGB\":1,\"dataImageCount\":1 }}";
            File.WriteAllText(configPath, payload);

            return new RestoreOnDisposeFileStream(configPath, backupPath);
        }
    }

    internal sealed class RestoreOnDisposeFileStream : FileStream
    {
        private readonly string _configPath;
        private readonly string _backupPath;

        public RestoreOnDisposeFileStream(string configPath, string backupPath)
            : base(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        {
            _configPath = configPath;
            _backupPath = backupPath;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            try
            {
                File.Delete(_configPath);
            }
            catch
            {
            }

            if (!string.IsNullOrWhiteSpace(_backupPath) && File.Exists(_backupPath))
            {
                try
                {
                    File.Move(_backupPath, _configPath);
                }
                catch
                {
                }
            }
        }
    }

    internal static class DataCleanerTestHooks
    {
        public static Task<int> DeleteNonCommittedBatchesAsync(SqlConnection sql)
        {
            var method = typeof(global::FileShareImageBuilder.DataCleaner)
                .GetMethod("DeleteNonCommittedBatchesAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.ShouldNotBeNull();
            return (Task<int>)method!.Invoke(null, new object?[] { sql, default(System.Threading.CancellationToken) })!;
        }

        public static async Task<int> DeleteCommittedBatchesNotDownloadedStrictAsync(SqlConnection sql, HashSet<Guid> downloadedBatchIds)
        {
            var method = typeof(global::FileShareImageBuilder.DataCleaner)
                .GetMethod("DeleteCommittedBatchesNotDownloadedAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method.ShouldNotBeNull();
            var task = (Task<int>)method!.Invoke(null, new object?[] { sql, downloadedBatchIds, default(System.Threading.CancellationToken) })!;
            return await task;
        }

    }
}
