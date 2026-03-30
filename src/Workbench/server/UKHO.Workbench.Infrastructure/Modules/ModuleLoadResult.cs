namespace UKHO.Workbench.Infrastructure.Modules
{
    /// <summary>
    /// Aggregates the outcome of a Workbench module loading run.
    /// </summary>
    public class ModuleLoadResult
    {
        private readonly List<ModuleLoadFailure> _failures = [];
        private readonly List<LoadedWorkbenchModule> _loadedModules = [];

        /// <summary>
        /// Gets the modules that were loaded successfully.
        /// </summary>
        public IReadOnlyList<LoadedWorkbenchModule> LoadedModules => _loadedModules;

        /// <summary>
        /// Gets the failures that occurred while loading modules.
        /// </summary>
        public IReadOnlyList<ModuleLoadFailure> Failures => _failures;

        /// <summary>
        /// Records a successfully loaded module.
        /// </summary>
        /// <param name="loadedModule">The loaded module that should be retained in the result.</param>
        public void AddLoadedModule(LoadedWorkbenchModule loadedModule)
        {
            // The result object centralizes success tracking so tests and host diagnostics can reason over one outcome model.
            ArgumentNullException.ThrowIfNull(loadedModule);
            _loadedModules.Add(loadedModule);
        }

        /// <summary>
        /// Records a module loading failure.
        /// </summary>
        /// <param name="failure">The failure that should be retained in the result.</param>
        public void AddFailure(ModuleLoadFailure failure)
        {
            // Failures are accumulated rather than thrown so one bad module does not block other valid modules from loading.
            ArgumentNullException.ThrowIfNull(failure);
            _failures.Add(failure);
        }
    }
}
