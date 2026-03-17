using Microsoft.Extensions.DependencyInjection;
using Xunit;
using UKHO.Search.Configuration;

namespace UKHO.Search.Tests.Hosts
{
    public class IngestionServiceHostIngestionModeRegistrationTests
    {
        [Fact]
        public void Is_sane_to_register_IngestionMode_in_DI()
        {
            var services = new ServiceCollection();

            services.AddSingleton(new IngestionModeOptions(IngestionMode.BestEffort));

            var sp = services.BuildServiceProvider();
            var options = sp.GetRequiredService<IngestionModeOptions>();

            Assert.Equal(IngestionMode.BestEffort, options.Mode);
        }
    }
}
