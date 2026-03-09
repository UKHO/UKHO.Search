using System.Data;
using Microsoft.Data.SqlClient;

namespace FileShareEmulator.Services
{
    public sealed class BatchSecurityTokenService
    {
        private readonly ILogger<BatchSecurityTokenService> _logger;
        private readonly SqlConnection _sqlConnection;

        public BatchSecurityTokenService(SqlConnection sqlConnection, ILogger<BatchSecurityTokenService> logger)
        {
            _sqlConnection = sqlConnection;
            _logger = logger;
        }

        public async Task<(string[] SecurityTokens, string? BusinessUnitName)> GetSecurityTokensAsync(Guid batchId, CancellationToken cancellationToken = default)
        {
            await EnsureOpenAsync(cancellationToken)
                .ConfigureAwait(false);

            var groupIdentifiers = await GetGroupIdentifiersAsync(batchId, cancellationToken)
                .ConfigureAwait(false);
            var userIdentifiers = await GetUserIdentifiersAsync(batchId, cancellationToken)
                .ConfigureAwait(false);
            var businessUnitName = await GetActiveBusinessUnitNameAsync(batchId, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(businessUnitName))
            {
                _logger.LogWarning("No active business unit found for batch {BatchId}; omitting business-unit security token.", batchId);
            }

            var tokens = BatchSecurityTokenBuilder.BuildTokens(groupIdentifiers, userIdentifiers, businessUnitName);

            _logger.LogDebug("Calculated {SecurityTokenCount} security tokens for batch {BatchId} (groups={GroupCount}, users={UserCount}).", tokens.Length, batchId, groupIdentifiers.Count, userIdentifiers.Count);

            return (tokens, businessUnitName);
        }

        private async Task<List<string>> GetGroupIdentifiersAsync(Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = _sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT [GroupIdentifier] FROM [BatchReadGroup] WHERE [BatchId] = @batchId;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var results = new List<string>();

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
                                              .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                results.Add(reader.GetString(0));
            }

            return results;
        }

        private async Task<List<string>> GetUserIdentifiersAsync(Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = _sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT [UserIdentifier] FROM [BatchReadUser] WHERE [BatchId] = @batchId;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var results = new List<string>();

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
                                              .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken)
                               .ConfigureAwait(false))
            {
                if (reader.IsDBNull(0))
                {
                    continue;
                }

                results.Add(reader.GetString(0));
            }

            return results;
        }

        private async Task<string?> GetActiveBusinessUnitNameAsync(Guid batchId, CancellationToken cancellationToken)
        {
            await using var cmd = _sqlConnection.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            cmd.CommandText = @"SELECT bu.[Name]
FROM [Batch] b
INNER JOIN [BusinessUnit] bu ON bu.[Id] = b.[BusinessUnitId]
WHERE b.[Id] = @batchId AND bu.[IsActive] = 1;";
            cmd.Parameters.Add(new SqlParameter("@batchId", SqlDbType.UniqueIdentifier) { Value = batchId });

            var result = await cmd.ExecuteScalarAsync(cancellationToken)
                                  .ConfigureAwait(false);
            return result is DBNull or null ? null : (string)result;
        }

        private async Task EnsureOpenAsync(CancellationToken cancellationToken)
        {
            if (_sqlConnection.State == ConnectionState.Open)
            {
                return;
            }

            await _sqlConnection.OpenAsync(cancellationToken)
                                .ConfigureAwait(false);
        }
    }
}