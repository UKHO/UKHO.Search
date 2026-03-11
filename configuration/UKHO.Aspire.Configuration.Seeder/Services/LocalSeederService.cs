using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UKHO.Aspire.Configuration.Seeder.Services
{
    internal class LocalSeederService : IHostedService
    {
        private readonly string _configFilePath;
        private readonly ConfigurationService _configService;
        private readonly ConfigurationClient _configurationClient;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<LocalSeederService> _logger;
        private readonly string _serviceName;
        private readonly string _servicesFilePath;

        public LocalSeederService(IHostApplicationLifetime hostApplicationLifetime, ConfigurationService configService, string serviceName, ConfigurationClient configurationClient, string configFilePath, string servicesFilePath, ILogger<LocalSeederService> logger)
        {
            _hostApplicationLifetime = hostApplicationLifetime;
            _configService = configService;
            _serviceName = serviceName;
            _configurationClient = configurationClient;
            _configFilePath = configFilePath;
            _servicesFilePath = servicesFilePath;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug(
                "LocalSeederService starting. ServiceName={ServiceName}, ConfigurationFilePath={ConfigurationFilePath}, ServicesFilePath={ServicesFilePath}",
                _serviceName,
                _configFilePath,
                _servicesFilePath);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMinutes(2));

            try
            {
                await _configService.SeedConfigurationAsync(AddsEnvironment.Local, _configurationClient, _serviceName, _configFilePath, _servicesFilePath, timeoutCts.Token);
                _logger.LogInformation("Local seeding completed; stopping host.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Local seeding failed; stopping host.");
                throw;
            }
            finally
            {
                _hostApplicationLifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}