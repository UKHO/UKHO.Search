using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Tools
{
    /// <summary>
    /// Describes the shell target used when a tool activation request is issued.
    /// </summary>
    public class ActivationTarget
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivationTarget"/> class.
        /// </summary>
        /// <param name="toolId">The identifier of the tool that should be opened or focused.</param>
        /// <param name="region">The shell region that should host the tool.</param>
        public ActivationTarget(string toolId, WorkbenchShellRegion region)
        {
            // The bootstrap shell routes every activation through a tool identifier so the host can focus existing singleton instances.
            ArgumentException.ThrowIfNullOrWhiteSpace(toolId);

            ToolId = toolId;
            Region = region;
        }

        /// <summary>
        /// Gets the identifier of the tool that should be opened or focused.
        /// </summary>
        public string ToolId { get; }

        /// <summary>
        /// Gets the shell region that should host the tool.
        /// </summary>
        public WorkbenchShellRegion Region { get; }

        /// <summary>
        /// Creates an activation target that hosts the tool inside the central tool surface.
        /// </summary>
        /// <param name="toolId">The identifier of the tool that should be opened or focused.</param>
        /// <returns>A tool-surface activation target for the supplied tool identifier.</returns>
        public static ActivationTarget CreateToolSurfaceTarget(string toolId)
        {
            // The bootstrap slice only supports tool hosting in the center working surface.
            return new ActivationTarget(toolId, WorkbenchShellRegion.ToolSurface);
        }
    }
}
