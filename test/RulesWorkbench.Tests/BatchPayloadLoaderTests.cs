using FileShareEmulator.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using RulesWorkbench.Services;
using Shouldly;
using Xunit;

namespace RulesWorkbench.Tests
{
    public sealed class BatchPayloadLoaderTests
    {
        [Fact]
        public async Task TryLoadAsync_WhenBatchIdEmpty_ReturnsFailed()
        {
            var loader = CreateLoader("Server=(local);Database=doesnotmatter;Trusted_Connection=True;");

            var result = await loader.TryLoadAsync("", CancellationToken.None);

            result.Found.ShouldBeFalse();
            result.Error.ShouldNotBeNull();
        }

        [Fact]
        public async Task TryLoadAsync_WhenBatchIdNotGuid_ReturnsNotFound()
        {
            var loader = CreateLoader("Server=(local);Database=doesnotmatter;Trusted_Connection=True;");

            var result = await loader.TryLoadAsync("not-a-guid", CancellationToken.None);

            result.Found.ShouldBeFalse();
            result.Error!.ToLowerInvariant().ShouldContain("not found");
        }

        [Fact]
        public void BuildTokens_AlwaysIncludesBatchCreate()
        {
            var tokens = SecurityTokenPolicy.CreateTokens(null);
            tokens.ShouldBe(["batchcreate", "public"]);
        }

        [Fact]
        public void BuildTokens_WhenBusinessUnitIsProvided_AddsBuToken()
        {
            var tokens = SecurityTokenPolicy.CreateTokens("Sales");
            tokens.ShouldBe(["batchcreate", "batchcreate_sales", "public"]);
        }

        [Fact]
        public void BuildTokens_WhenBusinessUnitHasWhitespace_TrimsAndLowercases()
        {
            var tokens = SecurityTokenPolicy.CreateTokens("  FiShErIeS  ");
            tokens.ShouldBe(["batchcreate", "batchcreate_fisheries", "public"]);
        }

        private static BatchPayloadLoader CreateLoader(string connectionString)
        {
            var cnn = new SqlConnection(connectionString);
            return new BatchPayloadLoader(cnn, NullLogger<BatchPayloadLoader>.Instance);
        }
    }
}
