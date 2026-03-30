namespace UKHO.Workbench.Modules
{
    /// <summary>
    /// Defines the bounded startup entry point that a dynamically discovered Workbench module must expose.
    /// </summary>
    public interface IWorkbenchModule
    {
        /// <summary>
        /// Gets the metadata that identifies the current module for configuration, diagnostics, and registration summaries.
        /// </summary>
        ModuleMetadata Metadata { get; }

        /// <summary>
        /// Registers module services and static Workbench contributions with the host before DI container finalization.
        /// </summary>
        /// <param name="context">The bounded registration context supplied by the Workbench host during startup.</param>
        void Register(ModuleRegistrationContext context);
    }
}
