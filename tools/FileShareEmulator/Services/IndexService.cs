using System.Data;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Data.SqlClient;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Requests.Serialization;

namespace FileShareEmulator.Services
{
    public sealed class IndexService
    {
        private const string QueueName = "file-share-queue";

        private readonly BatchSecurityTokenService _batchSecurityTokenService;

        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<IndexService> _logger;
        private readonly QueueServiceClient _queueServiceClient;

        public IndexService(SqlConnection sqlConnection, QueueServiceClient queueServiceClient, BatchSecurityTokenService batchSecurityTokenService, ILogger<IndexService> logger)
        {
            _connectionString = sqlConnection.ConnectionString;
            _queueServiceClient = queueServiceClient;
            _batchSecurityTokenService = batchSecurityTokenService;
            _logger = logger;

            _jsonOptions = IngestionJsonSerializerOptions.Create();
        }

        public Task<int> IndexAllPendingAsync(CancellationToken cancellationToken = default)
        {
            return IndexNextPendingAsync(null, cancellationToken);
        }

        public async Task<int> IndexNextPendingAsync(int n, CancellationToken cancellationToken = default)
        {
            if (n <= 0)
            {
                return 0;
            }

            return await IndexNextPendingAsync(count: n, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<int> ResetAllToPendingAsync(CancellationToken cancellationToken = default)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"UPDATE [Batch] SET [IndexStatus] = 0;";

            return await cmd.ExecuteNonQueryAsync(cancellationToken)
                            .ConfigureAwait(false);
        }

        private async Task<int> IndexNextPendingAsync(int? count, CancellationToken cancellationToken)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken)
                            .ConfigureAwait(false);

            var batchIds = await GetPendingBatchIdsAsync(connection, count, cancellationToken)
                .ConfigureAwait(false);
            if (batchIds.Count == 0)
            {
                return 0;
            }

            var queueClient = _queueServiceClient.GetQueueClient(QueueName);
            await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken)
                             .ConfigureAwait(false);

            var indexed = 0;

            foreach (var batchId in batchIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var request = await CreateRequestAsync(connection, batchId, cancellationToken)
                    .ConfigureAwait(false);
                var json = JsonSerializer.Serialize(request, _jsonOptions);

                await queueClient.SendMessageAsync(json, cancellationToken)
                                 .ConfigureAwait(false);
                await MarkBatchIndexedAsync(connection, batchId, cancellationToken)
                    .ConfigureAwait(false);

                indexed++;
            }

            return indexed;
        }

        private async Task<IngestionRequest> CreateRequestAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            var attributes = await GetBatchAttributesAsync(connection, batchId, cancellationToken)
                .ConfigureAwait(false);

            var batchCreatedOn = await GetBatchCreatedOnAsync(connection, batchId, cancellationToken)
                .ConfigureAwait(false);

            var files = await GetBatchFilesAsync(connection, batchId, cancellationToken)
                .ConfigureAwait(false);

            var securityTokenResult = await _batchSecurityTokenService.GetSecurityTokensAsync(batchId, cancellationToken)
                                                                      .ConfigureAwait(false);

            var properties = new List<IngestionProperty>(attributes.Count + 2);

            foreach (var (key, value) in attributes)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                properties.Add(new IngestionProperty
                {
                    Name = key,
                    Type = IngestionPropertyType.String,
                    Value = value
                });
            }

            properties.Add(new IngestionProperty
            {
                Name = "BusinessUnitName",
                Type = IngestionPropertyType.String,
                Value = securityTokenResult.BusinessUnitName ?? string.Empty
            });

            var addItem = new AddItemRequest(batchId.ToString("D"), properties, securityTokenResult.SecurityTokens, batchCreatedOn, files);

            _logger.LogDebug("Created ingestion request for batch {BatchId} with {SecurityTokenCount} security tokens and {FileCount} files.", batchId, securityTokenResult.SecurityTokens.Length, files.Count);

            return new IngestionRequest
            {
                RequestType = IngestionRequestType.AddItem,
                AddItem = addItem
            };
        }

        private static async Task<List<Guid>> GetPendingBatchIdsAsync(SqlConnection connection, int? count, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;

            cmd.CommandText = count is null ? @"SELECT [Id] FROM [Batch] WHERE [IndexStatus] = 0 ORDER BY [CreatedOn] ASC, [Id] ASC;" : @"SELECT TOP (@n) [Id] FROM [Batch] WHERE [IndexStatus] = 0 ORDER BY [CreatedOn] ASC, [Id] ASC;";

            if (count is not null)
            {
                cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.Int) { Value = count.Value });
            }

            var results = new List<Guid>(count ?? 128);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
                                              .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                results.Add(reader.GetGuid(0));
            }

            return results;
        }

        private static async Task<DateTimeOffset> GetBatchCreatedOnAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT [CreatedOn] FROM [Batch] WHERE [Id] = @batchId;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var value = await cmd.ExecuteScalarAsync(cancellationToken)
                                 .ConfigureAwait(false);

            if (value is null || value == DBNull.Value)
            {
                throw new InvalidOperationException($"Batch {batchId:D} does not have a CreatedOn value.");
            }

            return value switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(dt),
                var v => throw new InvalidOperationException($"Batch {batchId:D} CreatedOn has unexpected type '{v.GetType().FullName}'.")
            };
        }

        private static async Task<IngestionFileList> GetBatchFilesAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT [FileName], [FileByteSize], [CreatedOn], [MIMEType] FROM [File] WHERE [BatchId] = @batchId;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var results = new IngestionFileList();

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
                                              .ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3))
                {
                    throw new InvalidOperationException($"File rows for batch {batchId:D} contained null values in required columns.");
                }

                var filename = reader.GetString(0);
                var size = Convert.ToInt64(reader.GetValue(1));

                var createdOnValue = reader.GetValue(2);
                var timestamp = createdOnValue switch
                {
                    DateTimeOffset dto => dto,
                    DateTime dt => new DateTimeOffset(dt),
                    var v => throw new InvalidOperationException($"File.CreatedOn for batch {batchId:D} has unexpected type '{v.GetType().FullName}'.")
                };

                var mimeType = reader.GetString(3);

                try
                {
                    results.Add(new IngestionFile(filename, size, timestamp, mimeType));
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException($"Invalid file metadata encountered for batch {batchId:D}.", ex);
                }
            }

            return results;
        }

        private static async Task<List<(string AttributeKey, string AttributeValue)>> GetBatchAttributesAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT [AttributeKey], [AttributeValue] FROM [BatchAttribute] WHERE [BatchId] = @batchId;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var results = new List<(string, string)>();

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
                                              .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                var key = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                var value = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                results.Add((key, value));
            }

            return results;
        }

        private static async Task MarkBatchIndexedAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"UPDATE [Batch] SET [IndexStatus] = 1 WHERE [Id] = @id;";
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = batchId });

            _ = await cmd.ExecuteNonQueryAsync(cancellationToken)
                         .ConfigureAwait(false);
        }
    }
}