using System.Data;
using Humanizer;
using Microsoft.Data.SqlClient;

namespace FileShareEmulator.Services
{
    public sealed class StatisticsService
    {
        private static readonly IReadOnlyDictionary<string, string?> EmptyMetadata =
            new Dictionary<string, string?>(0, StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyDictionary<string, string> Labels =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(StatisticsSnapshot.BatchCount)] = HumanizeLabel(nameof(StatisticsSnapshot.BatchCount)),
                [nameof(StatisticsSnapshot.FileCount)] = HumanizeLabel(nameof(StatisticsSnapshot.FileCount)),
                [nameof(StatisticsSnapshot.BatchAttributeCount)] =
                    HumanizeLabel(nameof(StatisticsSnapshot.BatchAttributeCount)),
                [nameof(StatisticsSnapshot.FileAttributeCount)] =
                    HumanizeLabel(nameof(StatisticsSnapshot.FileAttributeCount)),
                [nameof(StatisticsSnapshot.BatchReadUserCount)] =
                    HumanizeLabel(nameof(StatisticsSnapshot.BatchReadUserCount)),
                [nameof(StatisticsSnapshot.BatchReadGroupCount)] =
                    HumanizeLabel(nameof(StatisticsSnapshot.BatchReadGroupCount))
            };

        private readonly SqlConnection _sqlConnection;

        public StatisticsService(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public async Task<IndexingStatus> GetIndexingStatusAsync(CancellationToken cancellationToken = default)
        {
            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            await using var cmd = _sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"
SELECT
    (SELECT COUNT_BIG(1) FROM [dbo].[Batch]) AS TotalBatches,
    (SELECT COUNT_BIG(1) FROM [dbo].[Batch] WHERE [IndexStatus] <> 0) AS IndexedBatches;";

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) return new IndexingStatus(0, 0);

            return new IndexingStatus(
                checked((int)reader.GetInt64(0)),
                checked((int)reader.GetInt64(1)));
        }

        public async Task<StatisticsSnapshot> GetAsync(CancellationToken cancellationToken = default)
        {
            await using var cmd = _sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;

            cmd.CommandText = @"
SELECT
    (SELECT COUNT_BIG(1) FROM [Batch]) AS BatchCount,
    (SELECT COUNT_BIG(1) FROM [File]) AS FileCount,
    (SELECT COUNT_BIG(1) FROM [BatchAttribute]) AS BatchAttributeCount,
    (SELECT COUNT_BIG(1) FROM [FileAttribute]) AS FileAttributeCount,
    (SELECT COUNT_BIG(1) FROM [BatchReadUser]) AS BatchReadUserCount,
    (SELECT COUNT_BIG(1) FROM [BatchReadGroup]) AS BatchReadGroupCount;";

            await EnsureConnectionOpenAsync(cancellationToken).ConfigureAwait(false);

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return new StatisticsSnapshot(0, 0, 0, 0, 0, 0, Labels, EmptyMetadata);

            var batchCount = checked((int)reader.GetInt64(0));
            var fileCount = checked((int)reader.GetInt64(1));
            var batchAttributeCount = checked((int)reader.GetInt64(2));
            var fileAttributeCount = checked((int)reader.GetInt64(3));
            var batchReadUserCount = checked((int)reader.GetInt64(4));
            var batchReadGroupCount = checked((int)reader.GetInt64(5));

            await reader.DisposeAsync().ConfigureAwait(false);

            var localMetadata = await GetLocalMetadataAsync(_sqlConnection, cancellationToken).ConfigureAwait(false);

            return new StatisticsSnapshot(
                batchCount,
                fileCount,
                batchAttributeCount,
                fileAttributeCount,
                batchReadUserCount,
                batchReadGroupCount,
                Labels,
                localMetadata);
        }

        private async Task EnsureConnectionOpenAsync(CancellationToken cancellationToken)
        {
            if (_sqlConnection.State != ConnectionState.Open)
                await _sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<IReadOnlyDictionary<string, string?>> GetLocalMetadataAsync(SqlConnection connection,
            CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"
SELECT [Name], [Value]
FROM [dbo].[LocalMetadata]
ORDER BY [Name];";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var name = reader.GetString(0);
                var value = reader.IsDBNull(1) ? null : reader.GetString(1);
                result[HumanizeLabel(name)] = value;
            }

            return result;
        }

        private static string HumanizeLabel(string value)
        {
            return value.Humanize(LetterCasing.Title);
        }

        public sealed record IndexingStatus(int TotalBatches, int IndexedBatches);
    }
}