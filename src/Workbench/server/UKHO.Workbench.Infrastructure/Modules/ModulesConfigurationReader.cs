using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace UKHO.Workbench.Infrastructure.Modules
{
    /// <summary>
    /// Reads the host-owned <c>modules.json</c> configuration file that controls Workbench module discovery.
    /// </summary>
    public class ModulesConfigurationReader
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly ILogger<ModulesConfigurationReader> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModulesConfigurationReader"/> class.
        /// </summary>
        /// <param name="logger">The logger used to record configuration loading progress and failures.</param>
        public ModulesConfigurationReader(ILogger<ModulesConfigurationReader> logger)
        {
            // The reader keeps configuration concerns isolated so the host bootstrap remains focused on orchestration.
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Reads and deserializes the module discovery configuration from disk.
        /// </summary>
        /// <param name="configurationPath">The absolute path to the host-owned <c>modules.json</c> file.</param>
        /// <returns>The deserialized module discovery options.</returns>
        public ModulesOptions Read(string configurationPath)
        {
            // Startup must fail with a descriptive message when the host-owned configuration path is invalid.
            ArgumentException.ThrowIfNullOrWhiteSpace(configurationPath);

            var fullConfigurationPath = Path.GetFullPath(configurationPath);
            _logger.LogInformation("Reading Workbench module configuration from {ConfigurationPath}.", fullConfigurationPath);

            if (!File.Exists(fullConfigurationPath))
            {
                throw new FileNotFoundException("The Workbench module configuration file was not found.", fullConfigurationPath);
            }

            // The first dynamic-loading slice reads the full file eagerly because the configuration is small and host-owned.
            var json = File.ReadAllText(fullConfigurationPath);
            var options = JsonSerializer.Deserialize<ModulesOptions>(json, SerializerOptions);

            if (options is null)
            {
                throw new InvalidOperationException($"The Workbench module configuration file '{fullConfigurationPath}' could not be deserialized.");
            }

            _logger.LogInformation(
                "Loaded Workbench module configuration with {ProbeRootCount} probe roots and {ModuleCount} module entries.",
                options.ProbeRoots.Count,
                options.Modules.Count);

            return options;
        }
    }
}
