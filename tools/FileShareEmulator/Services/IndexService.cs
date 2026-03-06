using System.Data;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Data.SqlClient;
using UKHO.Search.Ingestion.Requests;

namespace FileShareEmulator.Services
{
    public sealed class IndexService
    {
        private const string QueueName = "file-share-queue";
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly QueueServiceClient _queueServiceClient;

        private readonly SqlConnection _sqlConnection;

        public IndexService(SqlConnection sqlConnection, QueueServiceClient queueServiceClient)
        {
            _sqlConnection = sqlConnection;
            _queueServiceClient = queueServiceClient;

            _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public Task<int> IndexAllPendingAsync(CancellationToken cancellationToken = default)
        {
            return IndexNextPendingAsync(null, cancellationToken);
        }

        public async Task<int> IndexNextPendingAsync(int n, CancellationToken cancellationToken = default)
        {
            if (n <= 0) return 0;

            return await IndexNextPendingAsync(count: n, cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> ResetAllToPendingAsync(CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);

            await using var cmd = _sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"UPDATE [Batch] SET [IndexStatus] = 0;";

            return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<int> IndexNextPendingAsync(int? count, CancellationToken cancellationToken)
        {
            await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);

            var batchIds = await GetPendingBatchIdsAsync(count, cancellationToken).ConfigureAwait(false);
            if (batchIds.Count == 0) return 0;

            var queueClient = _queueServiceClient.GetQueueClient(QueueName);
            await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            var indexed = 0;

            foreach (var batchId in batchIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var request = await CreateRequestAsync(batchId, cancellationToken).ConfigureAwait(false);
                var json = JsonSerializer.Serialize(request, _jsonOptions);

                await queueClient.SendMessageAsync(json, cancellationToken).ConfigureAwait(false);
                await MarkBatchIndexedAsync(batchId, cancellationToken).ConfigureAwait(false);

                indexed++;
            }

            return indexed;
        }

        private async Task<IngestionRequest> CreateRequestAsync(Guid batchId, CancellationToken cancellationToken)
        {
            var attributes = await GetBatchAttributesAsync(batchId, cancellationToken).ConfigureAwait(false);

            var properties = new List<IngestionProperty>(attributes.Count + 1);

            foreach (var (key, value) in attributes)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;

                properties.Add(new IngestionProperty
                {
                    Name = key,
                    Type = IngestionPropertyType.String,
                    Value = value
                });
            }

            properties.Add(new IngestionProperty
            {
                Name = "ID",
                Type = IngestionPropertyType.Id,
                Value = batchId.ToString("D")
            });

            return new IngestionRequest
            {
                Properties = properties
            };
        }

        private async Task<List<Guid>> GetPendingBatchIdsAsync(int? count, CancellationToken cancellationToken)
        {
            await using var cmd = _sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;

            cmd.CommandText = count is null
                ? @"SELECT [Id] FROM [Batch] WHERE [IndexStatus] = 0 ORDER BY [CreatedOn] ASC, [Id] ASC;"
                : @"SELECT TOP (@n) [Id] FROM [Batch] WHERE [IndexStatus] = 0 ORDER BY [CreatedOn] ASC, [Id] ASC;";

            if (count is not null) cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.Int) { Value = count.Value });

            var results = new List<Guid>(count ?? 128);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) results.Add(reader.GetGuid(0));

            return results;
        }

        private async Task<List<(string AttributeKey, string AttributeValue)>> GetBatchAttributesAsync(Guid batchId,
            CancellationToken cancellationToken)
        {
            await using var cmd = _sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText =
                @"SELECT [AttributeKey], [AttributeValue] FROM [BatchAttribute] WHERE [BatchId] = @batchId;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var results = new List<(string, string)>();

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var key = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                var value = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                results.Add((key, value));
            }

            return results;
        }

        private async Task MarkBatchIndexedAsync(Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = _sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"UPDATE [Batch] SET [IndexStatus] = 1 WHERE [Id] = @id;";
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = batchId });

            _ = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task EnsureOpenAsync(CancellationToken cancellationToken)
        {
            if (_sqlConnection.State == ConnectionState.Open) return;

            await _sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}