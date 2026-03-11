using System.Net.Mime;
using Azure;
using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Logging;
using UKHO.ADDS.Infrastructure.Serialization.Json;
using UKHO.Aspire.Configuration.Seeder.Json;

namespace UKHO.Aspire.Configuration.Seeder.Services
{
    internal class ConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;

        private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(15);
        private const int MaxAttempts = 8;

        public ConfigurationService(ILogger<ConfigurationService> logger)
        {
            _logger = logger;
        }

        public async Task SeedConfigurationAsync(AddsEnvironment environment, ConfigurationClient configurationClient, string serviceName, string configFilePath, string servicesFilePath, CancellationToken cancellationToken)
        {
            var label = serviceName.ToLowerInvariant();

            _logger.LogDebug(
                "SeedConfigurationAsync starting. Environment={Environment}, ServiceName={ServiceName}, Label={Label}, ConfigFilePath={ConfigFilePath}, ServicesFilePath={ServicesFilePath}",
                environment,
                serviceName,
                label,
                configFilePath,
                servicesFilePath);

            var configurationSetting = new ConfigurationSetting(WellKnownConfigurationName.ReloadSentinelKey, "change this value to reload all", label) { ContentType = MediaTypeNames.Text.Plain };
            _logger.LogDebug("Setting reload sentinel key {Key} (Label={Label}).", WellKnownConfigurationName.ReloadSentinelKey, label);
            await SetConfigurationSettingWithRetryAsync(configurationClient, configurationSetting, cancellationToken);

            _logger.LogDebug("Reading configuration json from {ConfigFilePath}.", configFilePath);
            var configJson = await File.ReadAllTextAsync(configFilePath, cancellationToken);
            var configJsonCleaned = JsonStripper.StripJsonComments(configJson);

            var flattenedConfig = JsonFlattener.Flatten(environment, configJsonCleaned, label);

            _logger.LogInformation("Writing {ConfigurationCount} configuration settings to App Configuration (Label={Label}).", flattenedConfig.Count, label);

            foreach (var value in flattenedConfig)
            {
                await SetConfigurationSettingWithRetryAsync(configurationClient, value.Value, cancellationToken);
            }

            _logger.LogDebug("Reading external services json from {ServicesFilePath}.", servicesFilePath);
            var externalServiceJson = await File.ReadAllTextAsync(servicesFilePath, cancellationToken);
            var externalServiceJsonCleaned = JsonStripper.StripJsonComments(externalServiceJson);

            _logger.LogDebug("Parsing and resolving external service definitions.");
            var externalServices = await ExternalServiceDefinitionParser.ParseAndResolveAsync(environment, externalServiceJsonCleaned);

            _logger.LogInformation("Writing {ExternalServiceCount} external service definitions (Label={Label}).", externalServices.Count, label);

            foreach (var externalService in externalServices)
            {
                var json = JsonCodec.Encode(externalService);
                var key = $"{WellKnownConfigurationName.ExternalServiceKeyPrefix}:{externalService.Service}";

                configurationSetting = new ConfigurationSetting(key, json, label) { ContentType = MediaTypeNames.Text.Plain };
                await SetConfigurationSettingWithRetryAsync(configurationClient, configurationSetting, cancellationToken);
            }

            _logger.LogInformation("SeedConfigurationAsync completed.");
        }

        private async Task SetConfigurationSettingWithRetryAsync(ConfigurationClient configurationClient, ConfigurationSetting configurationSetting, CancellationToken cancellationToken)
        {
            var attemptDelay = TimeSpan.FromSeconds(1);

            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                attemptCts.CancelAfter(OperationTimeout);

                try
                {
                    await configurationClient.SetConfigurationSettingAsync(configurationSetting, false, attemptCts.Token);
                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts && IsTransient(ex, attemptCts))
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to set configuration setting {Key} (Label={Label}) on attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}.",
                        configurationSetting.Key,
                        configurationSetting.Label,
                        attempt,
                        MaxAttempts,
                        attemptDelay);

                    await Task.Delay(attemptDelay, cancellationToken);
                    attemptDelay = TimeSpan.FromSeconds(Math.Min(attemptDelay.TotalSeconds * 2, 10));
                }
            }
        }

        private static bool IsTransient(Exception ex, CancellationTokenSource attemptCts)
        {
            if (ex is TaskCanceledException && attemptCts.IsCancellationRequested)
            {
                return true;
            }

            if (ex is RequestFailedException requestFailed)
            {
                // Treat common retryable statuses as transient.
                return requestFailed.Status is 0 or 408 or 429 or 500 or 502 or 503 or 504;
            }

            return ex is HttpRequestException;
        }
    }
}