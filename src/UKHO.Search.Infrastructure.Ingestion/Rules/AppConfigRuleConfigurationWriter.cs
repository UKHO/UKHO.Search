using System.Net.Mime;

using Azure.Data.AppConfiguration;
using Azure.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class AppConfigRuleConfigurationWriter : IRuleConfigurationWriter
    {
        private const string DefaultLabel = "adds";

        private readonly IConfiguration _configuration;
        private readonly ILogger<AppConfigRuleConfigurationWriter> _logger;
        private readonly AppConfigEndpointResolver _endpointResolver;

        public AppConfigRuleConfigurationWriter(
            IConfiguration configuration,
            AppConfigEndpointResolver endpointResolver,
            ILogger<AppConfigRuleConfigurationWriter> logger)
        {
            _configuration = configuration;
            _endpointResolver = endpointResolver;
            _logger = logger;
        }

        public async Task SetRuleAsync(string provider, string ruleId, string json, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(json);

            var client = CreateClient();

            var label = GetLabel();
            var key = $"rules:{provider}:{ruleId}";

            _logger.LogInformation("Saving rule to App Configuration. Key={Key} Label={Label}", key, label);

            var setting = new Azure.Data.AppConfiguration.ConfigurationSetting(key, json)
            {
                Label = label,
                ContentType = MediaTypeNames.Application.Json
            };

            await client.SetConfigurationSettingAsync(setting, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task TouchSentinelAsync(CancellationToken cancellationToken)
        {
            var client = CreateClient();

            var label = GetLabel();
            var key = "auto.reload.sentinel";

            var value = DateTimeOffset.UtcNow.ToString("O");

            _logger.LogInformation("Touching App Configuration refresh sentinel. Key={Key} Label={Label}", key, label);

            var setting = new Azure.Data.AppConfiguration.ConfigurationSetting(key, value)
            {
                Label = label,
                ContentType = MediaTypeNames.Text.Plain
            };

            await client.SetConfigurationSettingAsync(setting, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private Azure.Data.AppConfiguration.ConfigurationClient CreateClient()
        {
            var endpoint = _endpointResolver.TryResolveEndpoint();
            if (endpoint is null)
            {
                throw new InvalidOperationException("Azure App Configuration endpoint could not be resolved from configuration.");
            }

            var environment = _configuration["adds-environment"];
            if (string.Equals(environment, "local", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = _configuration["configuration:connectionString"];
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    // Local emulator default.
                    // Matches `UKHO.Aspire.Configuration.ConfigurationExtensions`.
                    connectionString = $"Endpoint={endpoint.AbsoluteUri};Id=aac-credential;Secret=c2VjcmV0;";
                }

                return new Azure.Data.AppConfiguration.ConfigurationClient(connectionString);
            }

            var managedIdentityClientId = _configuration["configuration:managedIdentityClientId"];
            var credential = string.IsNullOrWhiteSpace(managedIdentityClientId)
                ? new DefaultAzureCredential()
                : new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = managedIdentityClientId
                });

            return new Azure.Data.AppConfiguration.ConfigurationClient(endpoint, credential);
        }

        private string GetLabel()
        {
            return (_configuration["configuration:label"] ?? DefaultLabel).ToLowerInvariant();
        }
    }
}
