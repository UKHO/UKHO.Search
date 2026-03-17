using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class AppConfigEndpointResolver
    {
        private const string ConfigurationServiceName = "adds-configuration";

        private readonly IConfiguration _configuration;
        private readonly ILogger<AppConfigEndpointResolver> _logger;

        public AppConfigEndpointResolver(IConfiguration configuration, ILogger<AppConfigEndpointResolver> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Uri? TryResolveEndpoint()
        {
            // Aspire provides referenced service endpoints via configuration.
            // In local emulation this will be injected as:
            // services__adds-configuration__http__0
            const string serviceEnvironmentKey = $"services__{ConfigurationServiceName}__http__0";

            var url = _configuration[serviceEnvironmentKey];
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogWarning("Azure App Configuration endpoint not found in configuration. Key={Key}", serviceEnvironmentKey);
                return null;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                _logger.LogWarning("Azure App Configuration endpoint is invalid. Url={Url}", url);
                return null;
            }

            return uri;
        }
    }
}
