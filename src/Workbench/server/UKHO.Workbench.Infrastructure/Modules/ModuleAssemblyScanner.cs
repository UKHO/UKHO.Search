using Microsoft.Extensions.Logging;

namespace UKHO.Workbench.Infrastructure.Modules
{
    /// <summary>
    /// Scans approved probe roots for Workbench module assemblies that match the configured naming convention.
    /// </summary>
    public class ModuleAssemblyScanner
    {
        private const string ModuleAssemblyPrefix = "UKHO.Workbench.Modules.";
        private readonly ILogger<ModuleAssemblyScanner> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleAssemblyScanner"/> class.
        /// </summary>
        /// <param name="logger">The logger used to record probe-root traversal and enablement decisions.</param>
        public ModuleAssemblyScanner(ILogger<ModuleAssemblyScanner> logger)
        {
            // The scanner owns the probe-root and file-system rules so the host does not duplicate discovery logic.
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Scans the configured probe roots and returns the enabled module assemblies that are safe to load.
        /// </summary>
        /// <param name="options">The host-owned module discovery options.</param>
        /// <param name="configurationDirectoryPath">The directory that contains <c>modules.json</c> and anchors relative probe roots.</param>
        /// <returns>The discovered module assemblies that passed naming and enablement checks.</returns>
        public IReadOnlyList<DiscoveredModuleAssembly> Scan(ModulesOptions options, string configurationDirectoryPath)
        {
            // Discovery is controlled entirely by the host configuration, so both the options and configuration directory must be valid.
            ArgumentNullException.ThrowIfNull(options);
            ArgumentException.ThrowIfNullOrWhiteSpace(configurationDirectoryPath);

            var discoveredAssemblies = new List<DiscoveredModuleAssembly>();
            var seenModuleIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var configuredProbeRoot in options.ProbeRoots)
            {
                // Relative probe roots are resolved against the host-owned configuration location so the file can travel with the host project.
                var resolvedProbeRoot = ResolveProbeRoot(configurationDirectoryPath, configuredProbeRoot);
                _logger.LogInformation("Scanning Workbench probe root {ProbeRoot}.", resolvedProbeRoot);

                if (!Directory.Exists(resolvedProbeRoot))
                {
                    _logger.LogWarning("Skipping missing Workbench probe root {ProbeRoot}.", resolvedProbeRoot);
                    continue;
                }

                foreach (var assemblyPath in Directory.EnumerateFiles(resolvedProbeRoot, $"{ModuleAssemblyPrefix}*.dll", SearchOption.TopDirectoryOnly))
                {
                    // Only assemblies with the approved naming convention are considered module candidates.
                    var moduleId = Path.GetFileNameWithoutExtension(assemblyPath);
                    if (!moduleId.StartsWith(ModuleAssemblyPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var isEnabled = options.IsModuleEnabled(moduleId);
                    _logger.LogInformation(
                        "Workbench module candidate {ModuleId} discovered at {AssemblyPath}. Enabled: {IsEnabled}.",
                        moduleId,
                        assemblyPath,
                        isEnabled);

                    if (!isEnabled)
                    {
                        continue;
                    }

                    // The host keeps one discovered assembly per module identifier to avoid duplicate registrations from overlapping probe roots.
                    if (!seenModuleIds.Add(moduleId))
                    {
                        _logger.LogInformation(
                            "Skipping duplicate Workbench module candidate {ModuleId} discovered at {AssemblyPath}.",
                            moduleId,
                            assemblyPath);
                        continue;
                    }

                    discoveredAssemblies.Add(new DiscoveredModuleAssembly(moduleId, Path.GetFullPath(assemblyPath), resolvedProbeRoot));
                }
            }

            return discoveredAssemblies;
        }

        /// <summary>
        /// Resolves a configured probe root into an absolute directory path.
        /// </summary>
        /// <param name="configurationDirectoryPath">The directory that contains the host-owned module configuration file.</param>
        /// <param name="configuredProbeRoot">The configured probe root, which may be relative or absolute.</param>
        /// <returns>The absolute path to the probe root that should be scanned.</returns>
        private static string ResolveProbeRoot(string configurationDirectoryPath, string configuredProbeRoot)
        {
            // Probe roots are host-owned configuration values, so empty entries are rejected before any file-system work occurs.
            ArgumentException.ThrowIfNullOrWhiteSpace(configurationDirectoryPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(configuredProbeRoot);

            return Path.GetFullPath(
                Path.IsPathRooted(configuredProbeRoot)
                    ? configuredProbeRoot
                    : Path.Combine(configurationDirectoryPath, configuredProbeRoot));
        }
    }
}
