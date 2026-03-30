namespace UKHO.Workbench.Infrastructure.Modules
{
    /// <summary>
    /// Represents the host-owned enablement settings for a single Workbench module.
    /// </summary>
    public class ModuleOptions
    {
        /// <summary>
        /// Gets or sets the stable module identifier that matches the assembly simple name.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the module is enabled for discovery and registration.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
