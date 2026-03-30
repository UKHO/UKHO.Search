using UKHO.Workbench.Modules;

namespace UKHO.Workbench.Infrastructure.Modules
{
    /// <summary>
    /// Describes a module that was successfully loaded and allowed to register with the host.
    /// </summary>
    public class LoadedWorkbenchModule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadedWorkbenchModule"/> class.
        /// </summary>
        /// <param name="metadata">The metadata exposed by the successfully loaded module.</param>
        /// <param name="assemblyPath">The absolute path to the assembly that supplied the module.</param>
        /// <param name="probeRoot">The absolute probe root that produced the loaded assembly.</param>
        public LoadedWorkbenchModule(ModuleMetadata metadata, string assemblyPath, string probeRoot)
        {
            // Successful load results retain both metadata and assembly path so later diagnostics can describe exactly what happened.
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(probeRoot);

            AssemblyPath = assemblyPath;
            ProbeRoot = probeRoot;
        }

        /// <summary>
        /// Gets the metadata exposed by the successfully loaded module.
        /// </summary>
        public ModuleMetadata Metadata { get; }

        /// <summary>
        /// Gets the absolute path to the assembly that supplied the module.
        /// </summary>
        public string AssemblyPath { get; }

        /// <summary>
        /// Gets the absolute probe root that produced the loaded assembly.
        /// </summary>
        public string ProbeRoot { get; }
    }
}
