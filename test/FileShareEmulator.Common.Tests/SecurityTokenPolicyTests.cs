using FileShareEmulator.Common;
using Shouldly;
using Xunit;

namespace FileShareEmulator.Common.Tests
{
    public sealed class SecurityTokenPolicyTests
    {
        [Fact]
        public void CreateTokens_WithoutBusinessUnit_ReturnsExactlyBatchCreateAndPublic()
        {
            var tokens = SecurityTokenPolicy.CreateTokens(null);

            tokens.ShouldBe(["batchcreate", "public"]);
        }

        [Fact]
        public void CreateTokens_WithBusinessUnit_ReturnsExactlyBatchCreateBuAndPublic()
        {
            var tokens = SecurityTokenPolicy.CreateTokens("Sales");

            tokens.ShouldBe(["batchcreate", "batchcreate_sales", "public"]);
        }

        [Fact]
        public void CreateTokens_NormalizesBusinessUnit()
        {
            var tokens = SecurityTokenPolicy.CreateTokens("  FiShErIeS  ");

            tokens.ShouldBe(["batchcreate", "batchcreate_fisheries", "public"]);
        }

        [Fact]
        public void CreateTokens_WhenBusinessUnitIsWhitespace_DoesNotAddBuToken()
        {
            var tokens = SecurityTokenPolicy.CreateTokens("   ");

            tokens.ShouldBe(["batchcreate", "public"]);
        }
    }
}
