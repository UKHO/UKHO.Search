using UKHO.Search.Configuration;
using Xunit;

namespace UKHO.Search.Tests.Configuration
{
    public class IngestionModeOptionsTests
    {
        [Fact]
        public void Mode_is_exposed_as_expected()
        {
            var options = new IngestionModeOptions(IngestionMode.BestEffort);

            Assert.Equal(IngestionMode.BestEffort, options.Mode);
        }
    }
}
