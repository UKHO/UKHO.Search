using System.Net.Mime;

using Azure.Data.AppConfiguration;
using Azure.Identity;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    /// <summary>
    /// Persists ingestion rule documents back to Azure App Configuration using the namespace-aware ingestion rule key contract.
    /// </summary>
    internal sealed class AppConfigRuleConfigurationWriter : IRuleConfigurationWriter
    {
        private const string DefaultLabel = "adds";

        private readonly IConfiguration _configuration;
        private readonly ILogger<AppConfigRuleConfigurationWriter> _logger;
        private readonly AppConfigEndpointResolver _endpointResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfigRuleConfigurationWriter"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration used to resolve label and authentication settings.</param>
        /// <param name="endpointResolver">The resolver that locates the active Azure App Configuration endpoint.</param>
        /// <param name="logger">The logger that records save-back activity.</param>
        public AppConfigRuleConfigurationWriter(
            IConfiguration configuration,
            AppConfigEndpointResolver endpointResolver,
            ILogger<AppConfigRuleConfigurationWriter> logger)
        {
            // Retain configuration because writer behavior depends on runtime environment, labels, and credential settings.
            _configuration = configuration;

            // Retain the endpoint resolver so save-back targets the same configuration store the application reads from.
            _endpointResolver = endpointResolver;

            // Retain the logger so save-back operations surface the effective namespace-aware key being written.
            _logger = logger;
        }

        /// <summary>
        /// Saves a rule document to Azure App Configuration.
        /// </summary>
        /// <param name="provider">The logical provider that owns the rule.</param>
        /// <param name="ruleId">The provider-relative rule identifier to persist.</param>
        /// <param name="json">The rule JSON payload to store.</param>
        /// <param name="cancellationToken">The cancellation token that can stop the save operation.</param>
        public async Task SetRuleAsync(string provider, string ruleId, string json, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(json);

            // Create the client only after validating the inputs so avoidable configuration work is skipped for invalid requests.
            var client = CreateClient();

            // Resolve the label once so the write and the diagnostic log both describe the same effective target.
            var label = GetLabel();
            var setting = CreateRuleSetting(provider, ruleId, json, label);

            _logger.LogInformation("Saving rule to App Configuration. Key={Key} Label={Label}", setting.Key, label);

            await client.SetConfigurationSettingAsync(setting, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the sentinel entry that triggers downstream configuration refresh.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token that can stop the sentinel update.</param>
        public async Task TouchSentinelAsync(CancellationToken cancellationToken)
        {
            // Resolve the client and label using the same path as rule save-back so sentinel updates target the active store.
            var client = CreateClient();

            var label = GetLabel();
            var key = "auto.reload.sentinel";

            // Use an ISO-8601 UTC timestamp so the sentinel value changes monotonically and remains human-readable.
            var value = DateTimeOffset.UtcNow.ToString("O");

            _logger.LogInformation("Touching App Configuration refresh sentinel. Key={Key} Label={Label}", key, label);

            var setting = new Azure.Data.AppConfiguration.ConfigurationSetting(key, value)
            {
                Label = label,
                ContentType = MediaTypeNames.Text.Plain
            };

            await client.SetConfigurationSettingAsync(setting, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the App Configuration setting that represents one ingestion rule document.
        /// </summary>
        /// <param name="provider">The logical provider that owns the rule.</param>
        /// <param name="ruleId">The provider-relative rule identifier to persist.</param>
        /// <param name="json">The rule JSON payload to store.</param>
        /// <param name="label">The normalized label to attach to the configuration setting.</param>
        /// <returns>The configuration setting that will be written to App Configuration.</returns>
        internal static ConfigurationSetting CreateRuleSetting(string provider, string ruleId, string json, string label)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(provider);
            ArgumentException.ThrowIfNullOrWhiteSpace(ruleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(json);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);

            // Compose the key through the shared path helper so writer and reader always target the same namespace-aware contract.
            return new ConfigurationSetting(IngestionRuleConfigurationPath.BuildRuleKey(provider, ruleId), json)
            {
                Label = label,
                ContentType = MediaTypeNames.Application.Json
            };
        }

        /// <summary>
        /// Creates a configuration client for the active environment.
        /// </summary>
        /// <returns>A configuration client pointed at the resolved Azure App Configuration endpoint.</returns>
        private Azure.Data.AppConfiguration.ConfigurationClient CreateClient()
        {
            // Resolve the endpoint first because both local-emulator and managed-identity paths depend on the same service URL.
            var endpoint = _endpointResolver.TryResolveEndpoint();
            if (endpoint is null)
            {
                throw new InvalidOperationException("Azure App Configuration endpoint could not be resolved from configuration.");
            }

            // Local development uses the emulator connection string contract instead of Azure identity.
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

            // Non-local environments use DefaultAzureCredential, optionally narrowed to a configured managed identity.
            var managedIdentityClientId = _configuration["configuration:managedIdentityClientId"];
            var credential = string.IsNullOrWhiteSpace(managedIdentityClientId)
                ? new DefaultAzureCredential()
                : new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = managedIdentityClientId
                });

            return new Azure.Data.AppConfiguration.ConfigurationClient(endpoint, credential);
        }

        /// <summary>
        /// Resolves the App Configuration label used for rule and sentinel writes.
        /// </summary>
        /// <returns>The normalized label used for App Configuration writes.</returns>
        private string GetLabel()
        {
            // Normalize the configured label once so the saved settings use predictable lowercase labels across environments.
            return NormalizeLabel(_configuration["configuration:label"]);
        }

        /// <summary>
        /// Normalizes a configured App Configuration label for save-back operations.
        /// </summary>
        /// <param name="configuredLabel">The optionally configured label value.</param>
        /// <returns>The effective lowercase label, or the default label when configuration is missing.</returns>
        internal static string NormalizeLabel(string? configuredLabel)
        {
            // Fall back to the repository-standard label and normalize to lowercase so environment-specific casing does not create duplicates.
            return (configuredLabel ?? DefaultLabel).ToLowerInvariant();
        }
    }
}
