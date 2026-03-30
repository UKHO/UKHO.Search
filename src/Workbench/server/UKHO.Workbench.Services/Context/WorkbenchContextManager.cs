using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Services.Context
{
    /// <summary>
    /// Produces the fixed context-key snapshot exposed by the current Workbench shell state.
    /// </summary>
    public class WorkbenchContextManager
    {
        /// <summary>
        /// Returns the current fixed Workbench context values for the supplied shell state.
        /// </summary>
        /// <param name="shellState">The shell state that should be projected into context values.</param>
        /// <returns>The current fixed Workbench context values keyed by <see cref="WorkbenchContextKeys"/>.</returns>
        public IReadOnlyDictionary<string, string> GetContextValues(WorkbenchShellState shellState)
        {
            // The first context model remains intentionally small and fixed, so the manager simply projects the current shell state.
            ArgumentNullException.ThrowIfNull(shellState);

            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [WorkbenchContextKeys.ActiveExplorer] = shellState.ActiveExplorerId ?? string.Empty,
                [WorkbenchContextKeys.ActiveTool] = shellState.ActiveTool?.Definition.Id ?? string.Empty,
                [WorkbenchContextKeys.ActiveRegion] = shellState.ActiveTool?.HostedRegion.ToString() ?? string.Empty,
                [WorkbenchContextKeys.SelectionType] = shellState.ActiveTool?.SelectionType ?? string.Empty,
                [WorkbenchContextKeys.SelectionCount] = (shellState.ActiveTool?.SelectionCount ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
                [WorkbenchContextKeys.ToolSurfaceReady] = shellState.IsRegionVisible(WorkbenchShellRegion.ToolSurface).ToString()
            };
        }
    }
}
