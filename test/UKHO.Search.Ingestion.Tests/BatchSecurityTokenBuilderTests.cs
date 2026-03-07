using FileShareEmulator.Services;
using Shouldly;
using Xunit;

namespace UKHO.Search.Ingestion.Tests
{
    public sealed class BatchSecurityTokenBuilderTests
    {
        [Fact]
        public void BuildTokens_AlwaysIncludesBatchCreate()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens([], [], null);

            tokens.ShouldBe(["batchcreate"]);
        }

        [Fact]
        public void BuildTokens_WhenBusinessUnitIsProvided_AddsBuToken()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens([], [], "Sales");

            tokens.ShouldBe(["batchcreate", "batchcreate_sales"]);
        }

        [Fact]
        public void BuildTokens_NormalisesToLowerCase_Trims_AndFiltersBlanks()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens([" GroupA ", "  ", "GROUPB"], [" User1 ", "USER2"], "  FiShErIeS  ");

            tokens.ShouldBe(["batchcreate", "batchcreate_fisheries", "groupa", "groupb", "user1", "user2"]);
        }

        [Fact]
        public void BuildTokens_DeduplicatesAcrossStandardGroupAndUserTokens()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens(["BatchCreate", "GroupA", "groupa"], ["GROUPA", "User1"], "Sales");

            tokens.ShouldBe(["batchcreate", "batchcreate_sales", "groupa", "user1"]);
        }

        [Fact]
        public void BuildTokens_SortsGroupsAndUsersIndependently()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens(["z", "a", "m"], ["u2", "u1"], null);

            tokens.ShouldBe(["batchcreate", "a", "m", "z", "u1", "u2"]);
        }

        [Fact]
        public void BuildTokens_WhenBusinessUnitIsBlank_DoesNotAddBuToken()
        {
            var tokens = BatchSecurityTokenBuilder.BuildTokens([], [], "  ");

            tokens.ShouldBe(["batchcreate"]);
        }
    }
}