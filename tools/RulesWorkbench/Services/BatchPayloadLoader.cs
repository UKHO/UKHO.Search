using System.Data;
using FileShareEmulator.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RulesWorkbench.Contracts;
using UKHO.Search.Configuration;

namespace RulesWorkbench.Services
{
    public sealed class BatchPayloadLoader
    {
        private readonly SqlConnection _sqlConnection;
        private readonly ILogger<BatchPayloadLoader> _logger;

        public BatchPayloadLoader(SqlConnection sqlConnection, ILogger<BatchPayloadLoader> logger)
        {
            _sqlConnection = sqlConnection;
            _logger = logger;
        }

        public async Task<BatchPayloadLoadResult> TryLoadAsync(string batchId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(batchId))
            {
                return BatchPayloadLoadResult.Failed("Batch id is required.");
            }

            if (!Guid.TryParse(batchId, out var batchGuid))
            {
                return BatchPayloadLoadResult.NotFound(batchId);
            }

            try
            {
                await using var connection = new SqlConnection(_sqlConnection.ConnectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                if (!await BatchExistsAsync(connection, batchGuid, cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogWarning("Batch {BatchId} not found.", batchGuid);
                    return BatchPayloadLoadResult.NotFound(batchGuid.ToString("D"));
                }

                var createdOn = await GetBatchCreatedOnAsync(connection, batchGuid, cancellationToken).ConfigureAwait(false);
                var attributes = await GetBatchAttributesAsync(connection, batchGuid, cancellationToken).ConfigureAwait(false);
                var files = await GetBatchFilesAsync(connection, batchGuid, cancellationToken).ConfigureAwait(false);
                var businessUnitName = await GetActiveBusinessUnitNameAsync(connection, batchGuid, cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(businessUnitName))
                {
                    _logger.LogWarning("No active business unit found for batch {BatchId}; omitting business-unit security token.", batchGuid);
                }

                var securityTokens = SecurityTokenPolicy.CreateTokens(businessUnitName);

                _logger.LogDebug(
                    "Calculated {SecurityTokenCount} security tokens for batch {BatchId}.",
                    securityTokens.Length,
                    batchGuid);

                var properties = attributes.Select(a => new EvaluationPayloadPropertyDto
                {
                    Name = a.AttributeKey,
                    Type = "String",
                    Value = a.AttributeValue
                }).ToList();

                properties.Add(new EvaluationPayloadPropertyDto
                {
                    Name = "BusinessUnitName",
                    Type = "String",
                    Value = businessUnitName ?? string.Empty
                });

                var payload = new EvaluationPayloadDto
                {
                    Id = batchGuid.ToString("D"),
                    Timestamp = createdOn,
                    SecurityTokens = securityTokens.ToList(),
                    Properties = properties,
                    Files = files.Select(f => new EvaluationPayloadFileDto
                    {
                        Filename = f.Filename,
                        Size = f.Size,
                        Timestamp = f.Timestamp,
                        MimeType = f.MimeType
                    }).ToList()
                };

                return BatchPayloadLoadResult.Success(payload);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "DB error while loading batch {BatchId}", batchId);
                return BatchPayloadLoadResult.Failed("Database error while loading batch.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error while loading batch {BatchId}", batchId);
                return BatchPayloadLoadResult.Failed(ex.Message);
            }
        }

        private static async Task<bool> BatchExistsAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT 1 FROM [Batch] WHERE [Id] = @batchId;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var value = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return value is not null && value != DBNull.Value;
        }

        private static async Task<DateTimeOffset> GetBatchCreatedOnAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT [CreatedOn] FROM [Batch] WHERE [Id] = @batchId;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var value = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
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

        private static async Task<List<(string AttributeKey, string AttributeValue)>> GetBatchAttributesAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT [AttributeKey], [AttributeValue] FROM [BatchAttribute] WHERE [BatchId] = @batchId;";
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

        private static async Task<List<(string Filename, long Size, DateTimeOffset Timestamp, string MimeType)>> GetBatchFilesAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT [FileName], [FileByteSize], [CreatedOn], [MIMEType] FROM [File] WHERE [BatchId] = @batchId;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var results = new List<(string, long, DateTimeOffset, string)>();

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
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
                results.Add((filename, size, timestamp, mimeType));
            }

            return results;
        }

        private static async Task<string?> GetActiveBusinessUnitNameAsync(SqlConnection connection, Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT bu.[Name]
FROM [Batch] b
INNER JOIN [BusinessUnit] bu ON bu.[Id] = b.[BusinessUnitId]
WHERE b.[Id] = @batchId AND bu.[IsActive] = 1;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is DBNull or null ? null : (string)result;
        }
    }
}
