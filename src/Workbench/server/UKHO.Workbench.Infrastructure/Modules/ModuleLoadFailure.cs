namespace UKHO.Workbench.Infrastructure.Modules
{
    /// <summary>
    /// Captures a diagnosable module loading failure without aborting the entire Workbench startup sequence.
    /// </summary>
    public class ModuleLoadFailure
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleLoadFailure"/> class.
        /// </summary>
        /// <param name="moduleId">The module identifier that was being processed when the failure occurred.</param>
        /// <param name="assemblyPath">The absolute assembly path involved in the failure.</param>
        /// <param name="probeRoot">The absolute probe root that produced the assembly involved in the failure.</param>
        /// <param name="failureStage">The startup stage where the failure occurred.</param>
        /// <param name="message">The safe diagnostic summary for the failure.</param>
        public ModuleLoadFailure(string moduleId, string assemblyPath, string probeRoot, string failureStage, string message)
        {
            // Failure records are intentionally plain data so they can be logged, surfaced to the user, or asserted in tests.
            ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
            ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
            ArgumentException.ThrowIfNullOrWhiteSpace(probeRoot);
            ArgumentException.ThrowIfNullOrWhiteSpace(failureStage);
            ArgumentException.ThrowIfNullOrWhiteSpace(message);

            ModuleId = moduleId;
            AssemblyPath = assemblyPath;
            ProbeRoot = probeRoot;
            FailureStage = failureStage;
            Message = message;
        }

        /// <summary>
        /// Gets the module identifier that was being processed when the failure occurred.
        /// </summary>
        public string ModuleId { get; }

        /// <summary>
        /// Gets the absolute assembly path involved in the failure.
        /// </summary>
        public string AssemblyPath { get; }

        /// <summary>
        /// Gets the absolute probe root that produced the assembly involved in the failure.
        /// </summary>
        public string ProbeRoot { get; }

        /// <summary>
        /// Gets the startup stage where the failure occurred.
        /// </summary>
        public string FailureStage { get; }

        /// <summary>
        /// Gets the safe diagnostic summary for the failure.
        /// </summary>
        public string Message { get; }
    }
}
