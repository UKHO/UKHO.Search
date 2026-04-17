using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Query.Rules
{
    /// <summary>
    /// Monitors configuration refresh signals and reloads the cached query-rule snapshot when the backing configuration changes.
    /// </summary>
    internal sealed class AppConfigQueryRulesRefreshService : BackgroundService
    {
        private readonly IConfigurationRefresherProvider? _refresherProvider;
        private readonly IConfiguration _configuration;
        private readonly QueryRulesCatalog _catalog;
        private readonly ILogger<AppConfigQueryRulesRefreshService> _logger;
        private readonly TimeSpan _pollInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfigQueryRulesRefreshService"/> class.
        /// </summary>
        /// <param name="refresherProviders">The optional Azure App Configuration refresher providers supplied by the host. The query host may not register this service, so the refresh loop must tolerate an empty sequence.</param>
        /// <param name="catalog">The query-rule catalog that should be reloaded after configuration refresh.</param>
        /// <param name="logger">The logger that records refresh diagnostics.</param>
        /// <param name="configuration">The live configuration root used to detect reload-token changes when refreshers are unavailable.</param>
        public AppConfigQueryRulesRefreshService(
            IEnumerable<IConfigurationRefresherProvider> refresherProviders,
            QueryRulesCatalog catalog,
            ILogger<AppConfigQueryRulesRefreshService> logger,
            IConfiguration configuration)
        {
            // Resolve the optional refresher provider from an enumerable so DI can still activate the service when Azure App Configuration refresh is not registered.
            _refresherProvider = refresherProviders?.FirstOrDefault();
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            var refreshIntervalSeconds = configuration.GetValue<int?>("configuration:refreshIntervalSeconds") ?? 30;
            if (refreshIntervalSeconds <= 0)
            {
                refreshIntervalSeconds = 30;
            }

            // Reuse the shared refresh cadence so query rules and other configuration-backed runtime features poll predictably.
            _pollInterval = TimeSpan.FromSeconds(refreshIntervalSeconds);
        }

        /// <summary>
        /// Runs the background refresh loop until host shutdown.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token that stops the background loop during host shutdown.</param>
        /// <returns>A task that completes when the background refresh loop exits.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_refresherProvider is null || !_refresherProvider.Refreshers.Any())
            {
                // Fall back to change-token monitoring when Azure App Configuration refreshers are unavailable.
                if (_refresherProvider is null)
                {
                    _logger.LogInformation("Azure App Configuration refreshers are unavailable; using configuration reload-token monitoring for query rules.");
                }
                else
                {
                    _logger.LogInformation("Azure App Configuration refresh service is enabled but no refreshers were registered; using configuration reload-token monitoring for query rules.");
                }

                await RunChangeTokenLoopAsync(stoppingToken).ConfigureAwait(false);
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
                        _logger.LogInformation("Azure App Configuration refreshed; reloading query rules.");
                        _catalog.Reload();
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Log refresh failures without stopping the host so the previous validated snapshot remains active.
                    _logger.LogError(ex, "Azure App Configuration refresh failed for query rules.");
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

        /// <summary>
        /// Monitors the configuration reload token and reloads the catalog when it changes.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token that stops the monitoring loop during host shutdown.</param>
        /// <returns>A task that completes when monitoring stops.</returns>
        private async Task RunChangeTokenLoopAsync(CancellationToken stoppingToken)
        {
            // Start from the current token and poll for replacement tokens that indicate a reload has already happened.
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
                    _logger.LogInformation("Configuration reload token changed; reloading query rules.");
                    _catalog.Reload();
                }
            }
        }
    }
}
