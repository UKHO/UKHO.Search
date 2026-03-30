namespace UKHO.Workbench.Infrastructure.Modules
{
    /// <summary>
    /// Represents the host-owned module discovery configuration loaded from <c>modules.json</c>.
    /// </summary>
    public class ModulesOptions
    {
        /// <summary>
        /// Gets or sets the probe roots that the host is allowed to scan for module assemblies.
        /// </summary>
        public IReadOnlyList<string> ProbeRoots { get; set; } = [];

        /// <summary>
        /// Gets or sets the per-module enablement settings used to allow or suppress discovered assemblies.
        /// </summary>
        public IReadOnlyList<ModuleOptions> Modules { get; set; } = [];

        /// <summary>
        /// Determines whether a module should be considered enabled for startup discovery.
        /// </summary>
        /// <param name="moduleId">The module identifier to evaluate.</param>
        /// <returns><see langword="true"/> when the module is enabled or has no explicit override; otherwise, <see langword="false"/>.</returns>
        public bool IsModuleEnabled(string moduleId)
        {
            // Module enablement defaults to true so new modules can be discovered unless the host explicitly disables them.
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);

            var matchingModule = Modules.FirstOrDefault(
                module => string.Equals(module.Id, moduleId, StringComparison.Ordinal));

            return matchingModule?.Enabled ?? true;
        }
    }
}
