using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using UKHO.Search.Ingestion.Contracts;

namespace UKHO.Search.Studio.Providers.FileShare
{
    public sealed class FileShareStudioBatchPayloadStore : IFileShareStudioBatchPayloadStore
    {
        private readonly ILogger<FileShareStudioBatchPayloadStore> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public FileShareStudioBatchPayloadStore(IServiceScopeFactory serviceScopeFactory, ILogger<FileShareStudioBatchPayloadStore> logger)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FileShareStudioBatchPayloadSource?> TryGetPayloadSourceAsync(Guid batchId, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sqlConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();

            await using var connection = new SqlConnection(sqlConnection.ConnectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            if (!await BatchExistsAsync(connection, batchId, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogWarning("No batch payload source was found for batch {BatchId}.", batchId);
                return null;
            }

            return new FileShareStudioBatchPayloadSource
            {
                BatchId = batchId,
                CreatedOn = await GetBatchCreatedOnAsync(connection, batchId, cancellationToken).ConfigureAwait(false),
                ActiveBusinessUnitName = await GetActiveBusinessUnitNameAsync(connection, batchId, cancellationToken).ConfigureAwait(false),
                Attributes = await GetBatchAttributesAsync(connection, batchId, cancellationToken).ConfigureAwait(false),
                Files = await GetBatchFilesAsync(connection, batchId, cancellationToken).ConfigureAwait(false)
            };
        }

        public async Task<IReadOnlyList<Guid>> GetPendingBatchIdsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sqlConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();

            await using var connection = new SqlConnection(sqlConnection.ConnectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"SELECT [Id] FROM [Batch] WHERE [IndexStatus] = 0 ORDER BY [CreatedOn] ASC, [Id] ASC;";

            var results = new List<Guid>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                                                  .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                results.Add(reader.GetGuid(0));
            }

            return results;
        }

        public async Task<IReadOnlyList<FileShareStudioBusinessUnit>> GetBusinessUnitsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sqlConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();

            await using var connection = new SqlConnection(sqlConnection.ConnectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"SELECT [Id], [Name]
FROM [BusinessUnit]
ORDER BY [Name] ASC, [Id] ASC;";

            var results = new List<FileShareStudioBusinessUnit>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                                                  .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1))
                {
                    continue;
                }

                results.Add(new FileShareStudioBusinessUnit
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }

            return results;
        }

        public async Task<IReadOnlyList<Guid>> GetPendingBatchIdsForBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sqlConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();

            await using var connection = new SqlConnection(sqlConnection.ConnectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"SELECT [Id]
FROM [Batch]
WHERE [BusinessUnitId] = @businessUnitId
AND [IndexStatus] = 0
ORDER BY [CreatedOn] ASC, [Id] ASC;";
            command.Parameters.Add(new SqlParameter("@businessUnitId", SqlDbType.Int) { Value = businessUnitId });

            var results = new List<Guid>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                                                  .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                results.Add(reader.GetGuid(0));
            }

            return results;
        }

        public async Task MarkBatchIndexedAsync(Guid batchId, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sqlConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();

            await using var connection = new SqlConnection(sqlConnection.ConnectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"UPDATE [Batch] SET [IndexStatus] = 1 WHERE [Id] = @batchId;";
            command.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            _ = await command.ExecuteNonQueryAsync(cancellationToken)
                             .ConfigureAwait(false);
        }

        public async Task<int> ResetAllIndexingStatusAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sqlConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();

            await using var connection = new SqlConnection(sqlConnection.ConnectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"UPDATE [Batch] SET [IndexStatus] = 0;";

            return await command.ExecuteNonQueryAsync(cancellationToken)
                                .ConfigureAwait(false);
        }

        public async Task<int> ResetIndexingStatusForBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var sqlConnection = scope.ServiceProvider.GetRequiredService<SqlConnection>();

            await using var connection = new SqlConnection(sqlConnection.ConnectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"UPDATE [Batch]
SET [IndexStatus] = 0
WHERE [BusinessUnitId] = @businessUnitId;";
            command.Parameters.Add(new SqlParameter("@businessUnitId", SqlDbType.Int) { Value = businessUnitId });

            return await command.ExecuteNonQueryAsync(cancellationToken)
                                .ConfigureAwait(false);
        }

        private static async Task<bool> BatchExistsAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"SELECT 1 FROM [Batch] WHERE [Id] = @batchId;";
            command.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var value = await command.ExecuteScalarAsync(cancellationToken)
                                     .ConfigureAwait(false);

            return value is not null && value != DBNull.Value;
        }

        private static async Task<DateTimeOffset> GetBatchCreatedOnAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"SELECT [CreatedOn] FROM [Batch] WHERE [Id] = @batchId;";
            command.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var value = await command.ExecuteScalarAsync(cancellationToken)
                                     .ConfigureAwait(false);
            if (value is null || value == DBNull.Value)
            {
                throw new InvalidOperationException($"Batch {batchId:D} does not have a CreatedOn value.");
            }

            return value switch
            {
                DateTimeOffset createdOn => createdOn,
                DateTime createdOn => new DateTimeOffset(createdOn),
                var createdOn => throw new InvalidOperationException($"Batch {batchId:D} CreatedOn has unexpected type '{createdOn.GetType().FullName}'.")
            };
        }

        private static async Task<IReadOnlyList<KeyValuePair<string, string>>> GetBatchAttributesAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"SELECT [AttributeKey], [AttributeValue] FROM [BatchAttribute] WHERE [BatchId] = @batchId;";
            command.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var results = new List<KeyValuePair<string, string>>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                                                  .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var key = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                var value = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                results.Add(new KeyValuePair<string, string>(key, value));
            }

            return results;
        }

        private static async Task<IngestionFileList> GetBatchFilesAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"SELECT [FileName], [FileByteSize], [CreatedOn], [MIMEType] FROM [File] WHERE [BatchId] = @batchId;";
            command.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var results = new IngestionFileList();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                                                  .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3))
                {
                    throw new InvalidOperationException($"File rows for batch {batchId:D} contained null values in required columns.");
                }

                var fileName = reader.GetString(0);
                var size = Convert.ToInt64(reader.GetValue(1));
                var createdOnValue = reader.GetValue(2);
                var createdOn = createdOnValue switch
                {
                    DateTimeOffset created => created,
                    DateTime created => new DateTimeOffset(created),
                    var created => throw new InvalidOperationException($"File.CreatedOn for batch {batchId:D} has unexpected type '{created.GetType().FullName}'.")
                };
                var mimeType = reader.GetString(3);

                results.Add(new IngestionFile(fileName, size, createdOn, mimeType));
            }

            return results;
        }

        private static async Task<string?> GetActiveBusinessUnitNameAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;
            command.CommandText = @"SELECT bu.[Name]
FROM [Batch] b
INNER JOIN [BusinessUnit] bu ON bu.[Id] = b.[BusinessUnitId]
WHERE b.[Id] = @batchId
AND bu.[IsActive] = 1;";
            command.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var result = await command.ExecuteScalarAsync(cancellationToken)
                                      .ConfigureAwait(false);
            return result is DBNull or null ? null : (string)result;
        }
    }
}
