using FileShareEmulator.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace FileShareEmulator.Tests
{
    public sealed class BusinessUnitLookupServiceTests
    {
        [Fact]
        public async Task GetBusinessUnitsAsync_WhenConnectionFails_ReturnsFailure()
        {
            var service = CreateService(string.Empty);

            var result = await service.GetBusinessUnitsAsync(CancellationToken.None);

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
            result.BusinessUnits.ShouldBeEmpty();
        }

        private static BusinessUnitLookupService CreateService(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            return new BusinessUnitLookupService(connection, NullLogger<BusinessUnitLookupService>.Instance);
        }
    }
}
