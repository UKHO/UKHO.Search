using System.Data;
using System.Linq;
using Humanizer;
using Microsoft.Data.SqlClient;

namespace FileShareEmulator.Services
{
    public sealed class StatisticsService
    {
        private static readonly IReadOnlyDictionary<string, string?> _emptyMetadata = new Dictionary<string, string?>(0, StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyDictionary<string, string> _labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(StatisticsSnapshot.BatchCount)] = HumanizeLabel(nameof(StatisticsSnapshot.BatchCount)),
            [nameof(StatisticsSnapshot.FileCount)] = HumanizeLabel(nameof(StatisticsSnapshot.FileCount)),
            [nameof(StatisticsSnapshot.BatchAttributeCount)] = HumanizeLabel(nameof(StatisticsSnapshot.BatchAttributeCount)),
            [nameof(StatisticsSnapshot.FileAttributeCount)] = HumanizeLabel(nameof(StatisticsSnapshot.FileAttributeCount)),
            [nameof(StatisticsSnapshot.BatchReadUserCount)] = HumanizeLabel(nameof(StatisticsSnapshot.BatchReadUserCount)),
            [nameof(StatisticsSnapshot.BatchReadGroupCount)] = HumanizeLabel(nameof(StatisticsSnapshot.BatchReadGroupCount))
        };

        private readonly string _connectionString;

        public StatisticsService(SqlConnection sqlConnection)
        {
            _connectionString = sqlConnection.ConnectionString;
        }

        public async Task<IndexingStatus> GetIndexingStatusAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"
SELECT
    (SELECT COUNT_BIG(1) FROM [dbo].[Batch]) AS TotalBatches,
    (SELECT COUNT_BIG(1) FROM [dbo].[Batch] WHERE [IndexStatus] <> 0) AS IndexedBatches;";

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
                                              .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken)
                             .ConfigureAwait(false))
            {
                return new IndexingStatus(0, 0);
            }

            return new IndexingStatus(checked((int)reader.GetInt64(0)), checked((int)reader.GetInt64(1)));
        }

        public async Task<StatisticsSnapshot> GetAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            await using var cmd = connection.CreateCommand();
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

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
                                              .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken)
                             .ConfigureAwait(false))
            {
                return new StatisticsSnapshot(0, 0, 0, 0, 0, 0, _labels, _emptyMetadata);
            }

            var batchCount = checked((int)reader.GetInt64(0));
            var fileCount = checked((int)reader.GetInt64(1));
            var batchAttributeCount = checked((int)reader.GetInt64(2));
            var fileAttributeCount = checked((int)reader.GetInt64(3));
            var batchReadUserCount = checked((int)reader.GetInt64(4));
            var batchReadGroupCount = checked((int)reader.GetInt64(5));

            await reader.DisposeAsync()
                        .ConfigureAwait(false);

            var localMetadata = await GetLocalMetadataAsync(connection, cancellationToken)
                .ConfigureAwait(false);

            return new StatisticsSnapshot(batchCount, fileCount, batchAttributeCount, fileAttributeCount, batchReadUserCount, batchReadGroupCount, _labels, localMetadata);
        }

        public async Task<IReadOnlyList<BusinessUnitStatistics>> GetBusinessUnitStatisticsAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            var results = new Dictionary<string, Builder>(StringComparer.OrdinalIgnoreCase);

            await ReadBatchAttributeCountsAsync(connection, results, cancellationToken)
                .ConfigureAwait(false);

            await ReadMimeTypeCountsAsync(connection, results, cancellationToken)
                .ConfigureAwait(false);

            return results.Values
                          .OrderBy(x => x.BusinessUnitName, StringComparer.OrdinalIgnoreCase)
                          .Select(x => x.Build())
                          .ToArray();
        }

        private static async Task<IReadOnlyDictionary<string, string?>> GetLocalMetadataAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"
SELECT [Name], [Value]
FROM [dbo].[LocalMetadata]
ORDER BY [Name];";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
                                              .ConfigureAwait(false);
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            while (await reader.ReadAsync(cancellationToken)
                               .ConfigureAwait(false))
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

        private static async Task ReadBatchAttributeCountsAsync(SqlConnection connection, Dictionary<string, Builder> results, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"
SELECT bu.[Name] AS BusinessUnitName,
       ba.[AttributeKey] AS AttributeName,
       COUNT(DISTINCT b.[Id]) AS BatchCount
FROM [BusinessUnit] bu
INNER JOIN [Batch] b ON b.[BusinessUnitId] = bu.[Id]
INNER JOIN [BatchAttribute] ba ON ba.[BatchId] = b.[Id]
WHERE ba.[AttributeKey] IS NOT NULL
  AND LTRIM(RTRIM(ba.[AttributeKey])) <> ''
GROUP BY bu.[Name], ba.[AttributeKey]
ORDER BY bu.[Name] ASC, BatchCount DESC, ba.[AttributeKey] ASC;";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
                                              .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
                {
                    continue;
                }

                var businessUnitName = reader.GetString(0);
                var attributeName = reader.GetString(1);
                var countValue = Convert.ToInt64(reader.GetValue(2));
                var count = checked((int)countValue);

                if (!results.TryGetValue(businessUnitName, out var builder))
                {
                    builder = new Builder(businessUnitName);
                    results[businessUnitName] = builder;
                }

                builder.BatchAttributeNames.Add(new NamedCount(attributeName, count));
            }
        }

        private static async Task ReadMimeTypeCountsAsync(SqlConnection connection, Dictionary<string, Builder> results, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"
SELECT bu.[Name] AS BusinessUnitName,
       f.[MIMEType] AS MimeType,
       COUNT_BIG(1) AS FileCount
FROM [BusinessUnit] bu
INNER JOIN [Batch] b ON b.[BusinessUnitId] = bu.[Id]
INNER JOIN [File] f ON f.[BatchId] = b.[Id]
WHERE f.[MIMEType] IS NOT NULL
  AND LTRIM(RTRIM(f.[MIMEType])) <> ''
GROUP BY bu.[Name], f.[MIMEType]
ORDER BY bu.[Name] ASC, FileCount DESC, f.[MIMEType] ASC;";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
                                              .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
                {
                    continue;
                }

                var businessUnitName = reader.GetString(0);
                var mimeType = reader.GetString(1);
                var countValue = Convert.ToInt64(reader.GetValue(2));
                var count = checked((int)countValue);

                if (!results.TryGetValue(businessUnitName, out var builder))
                {
                    builder = new Builder(businessUnitName);
                    results[businessUnitName] = builder;
                }

                builder.MimeTypes.Add(new NamedCount(mimeType, count));
            }
        }

        private sealed class Builder
        {
            public Builder(string businessUnitName)
            {
                BusinessUnitName = businessUnitName;
            }

            public string BusinessUnitName { get; }

            public List<NamedCount> BatchAttributeNames { get; } = [];

            public List<NamedCount> MimeTypes { get; } = [];

            public BusinessUnitStatistics Build()
            {
                var batchAttributes = BatchAttributeNames
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var mimeTypes = MimeTypes
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new BusinessUnitStatistics(BusinessUnitName, batchAttributes, mimeTypes);
            }
        }

        public sealed record IndexingStatus(int TotalBatches, int IndexedBatches);
    }
}