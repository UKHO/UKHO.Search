using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace FileShareEmulator.Services
{
    public sealed class BusinessUnitLookupService
    {
        private readonly SqlConnection _sqlConnection;
        private readonly ILogger<BusinessUnitLookupService> _logger;

        public BusinessUnitLookupService(SqlConnection sqlConnection, ILogger<BusinessUnitLookupService> logger)
        {
            _sqlConnection = sqlConnection;
            _logger = logger;
        }

        public async Task<BusinessUnitLookupResultDto> GetBusinessUnitsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await using var connection = new SqlConnection(_sqlConnection.ConnectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                await using var cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 30;
                cmd.CommandText = @"SELECT [Id], [Name]
FROM [BusinessUnit]
ORDER BY [Name] ASC, [Id] ASC;";

                var businessUnits = new List<BusinessUnitOptionDto>();
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (reader.IsDBNull(0) || reader.IsDBNull(1))
                    {
                        continue;
                    }

                    businessUnits.Add(new BusinessUnitOptionDto
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                    });
                }

                _logger.LogInformation("Loaded {BusinessUnitCount} business units for indexing selector.", businessUnits.Count);
                return BusinessUnitLookupResultDto.Success(businessUnits);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "DB error while loading business units for indexing selector.");
                return BusinessUnitLookupResultDto.Failure("Database error while loading business units.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Unexpected error while loading business units for indexing selector.");
                return BusinessUnitLookupResultDto.Failure(ex.Message);
            }
        }
    }
}
