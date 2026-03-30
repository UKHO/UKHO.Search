namespace UKHO.Workbench.Infrastructure.Modules
{
    /// <summary>
    /// Describes an approved module assembly discovered under a configured probe root.
    /// </summary>
    public class DiscoveredModuleAssembly
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveredModuleAssembly"/> class.
        /// </summary>
        /// <param name="moduleId">The module identifier derived from the assembly simple name.</param>
        /// <param name="assemblyPath">The absolute path to the discovered module assembly.</param>
        /// <param name="probeRoot">The absolute probe root that produced the discovered assembly.</param>
        public DiscoveredModuleAssembly(string moduleId, string assemblyPath, string probeRoot)
        {
            // Discovery results are later logged and loaded, so all identifying values must be present and stable.
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(probeRoot);

            ModuleId = moduleId;
            AssemblyPath = assemblyPath;
            ProbeRoot = probeRoot;
        }

        /// <summary>
        /// Gets the module identifier derived from the assembly simple name.
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// Gets the absolute path to the discovered module assembly.
        /// </summary>
        public string AssemblyPath { get; }

        /// <summary>
        /// Gets the absolute probe root that produced the discovered assembly.
        /// </summary>
        public string ProbeRoot { get; }
    }
}
