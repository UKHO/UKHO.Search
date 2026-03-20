using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RulesWorkbench.Contracts;

namespace RulesWorkbench.Services
{
    public sealed class BatchScanService
    {
        private readonly SqlConnection _sqlConnection;
        private readonly ILogger<BatchScanService> _logger;

        public BatchScanService(SqlConnection sqlConnection, ILogger<BatchScanService> logger)
        {
            _sqlConnection = sqlConnection;
            _logger = logger;
        }

        public async Task<BatchScanResultDto> GetBatchesForBusinessUnitAsync(int businessUnitId, int maxRows, CancellationToken cancellationToken)
        {
            if (businessUnitId <= 0)
            {
                return BatchScanResultDto.Failure("Business unit id is required.");
            }

            if (maxRows <= 0)
            {
                return BatchScanResultDto.Failure("Max rows must be greater than zero.");
            }

            return await GetBatchesAsync(
                businessUnitId,
                cancellationToken,
                BatchScanQueries.GetBoundedQuery(),
                cmd => cmd.Parameters.Add(new SqlParameter("@maxRows", SqlDbType.Int) { Value = maxRows }),
                isUnboundedScan: false,
                maxRows)
                .ConfigureAwait(false);
        }

        public async Task<BatchScanResultDto> GetAllBatchesForBusinessUnitAsync(int businessUnitId, CancellationToken cancellationToken)
        {
            if (businessUnitId <= 0)
            {
                return BatchScanResultDto.Failure("Business unit id is required.");
            }

            return await GetBatchesAsync(
                businessUnitId,
                cancellationToken,
                BatchScanQueries.GetUnboundedQuery(),
                parameterizeCommand: null,
                isUnboundedScan: true,
                maxRows: null)
                .ConfigureAwait(false);
        }

        private async Task<BatchScanResultDto> GetBatchesAsync(
            int businessUnitId,
            CancellationToken cancellationToken,
            string commandText,
            Action<SqlCommand>? parameterizeCommand,
            bool isUnboundedScan,
            int? maxRows)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(commandText);

            try
            {
                await using var connection = new SqlConnection(_sqlConnection.ConnectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                await using var cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 30;
                cmd.CommandText = commandText;
                parameterizeCommand?.Invoke(cmd);
                cmd.Parameters.Add(new SqlParameter("@businessUnitId", SqlDbType.Int) { Value = businessUnitId });

                var batches = new List<BatchScanBatchDto>();
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (reader.IsDBNull(0) || reader.IsDBNull(1))
                    {
                        continue;
                    }

                    var createdOnValue = reader.GetValue(1);
                    var createdOn = createdOnValue switch
                    {
                        DateTimeOffset dto => dto,
                        DateTime dt => new DateTimeOffset(dt),
                        var value => throw new InvalidOperationException($"Batch.CreatedOn has unexpected type '{value.GetType().FullName}'.")
                    };

                    batches.Add(new BatchScanBatchDto
                    {
                        BatchId = reader.GetGuid(0),
                        CreatedOn = createdOn,
                    });
                }

                if (isUnboundedScan)
                {
                    _logger.LogInformation(
                        "Loaded {BatchCount} batches for unbounded checker scan. BusinessUnitId={BusinessUnitId}",
                        batches.Count,
                        businessUnitId);
                }
                else
                {
                    _logger.LogInformation(
                        "Loaded {BatchCount} batches for bounded checker scan. BusinessUnitId={BusinessUnitId} MaxRows={MaxRows}",
                        batches.Count,
                        businessUnitId,
                        maxRows);
                }

                return BatchScanResultDto.Success(batches);
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "DB error while loading batches for {ScanMode} checker scan. BusinessUnitId={BusinessUnitId}",
                    isUnboundedScan ? "unbounded" : "bounded",
                    businessUnitId);
                return BatchScanResultDto.Failure("Database error while loading batches for scan.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while loading batches for {ScanMode} checker scan. BusinessUnitId={BusinessUnitId}",
                    isUnboundedScan ? "unbounded" : "bounded",
                    businessUnitId);
                return BatchScanResultDto.Failure(ex.Message);
            }
        }
    }
}
