using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class AppConfigIngestionRulesRefreshService : BackgroundService
    {
        private readonly IConfigurationRefresherProvider? _refresherProvider;
        private readonly IConfiguration _configuration;
        private readonly IngestionRulesCatalog _catalog;
        private readonly ILogger<AppConfigIngestionRulesRefreshService> _logger;
        private readonly TimeSpan _pollInterval;

        public AppConfigIngestionRulesRefreshService(
            IConfigurationRefresherProvider? refresherProvider,
            IngestionRulesCatalog catalog,
            ILogger<AppConfigIngestionRulesRefreshService> logger,
            IConfiguration configuration)
        {
            _refresherProvider = refresherProvider;
            _catalog = catalog;
            _logger = logger;
            _configuration = configuration;

            var refreshIntervalSeconds = configuration.GetValue<int?>("configuration:refreshIntervalSeconds") ?? 30;
            if (refreshIntervalSeconds <= 0)
            {
                refreshIntervalSeconds = 30;
            }

            _pollInterval = TimeSpan.FromSeconds(refreshIntervalSeconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_refresherProvider is null)
            {
                await RunChangeTokenLoop(stoppingToken).ConfigureAwait(false);
                return;
            }

            if (!_refresherProvider.Refreshers.Any())
            {
                _logger.LogInformation("Azure App Configuration refresh service is enabled but no refreshers were registered; falling back to change token reload monitoring.");
                await RunChangeTokenLoop(stoppingToken).ConfigureAwait(false);
                return;
            }

            var refresher = _refresherProvider.Refreshers.First();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var refreshed = await refresher.TryRefreshAsync(stoppingToken).ConfigureAwait(false);
                    if (refreshed)
                    {
                        _logger.LogInformation("Azure App Configuration refreshed; reloading ingestion rules.");
                        _catalog.Reload();
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Azure App Configuration refresh failed.");
                }

                try
                {
                    await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }

        }

        private async Task RunChangeTokenLoop(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting configuration reload monitor loop for ingestion rules.");

            var lastReloadToken = _configuration.GetReloadToken();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_pollInterval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                var currentToken = _configuration.GetReloadToken();
                if (!ReferenceEquals(currentToken, lastReloadToken))
                {
                    lastReloadToken = currentToken;
                    _logger.LogInformation("Configuration reload token changed; reloading ingestion rules.");
                    _catalog.Reload();
                }
            }
        }
    }
}
