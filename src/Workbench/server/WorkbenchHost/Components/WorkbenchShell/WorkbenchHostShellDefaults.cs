namespace WorkbenchHost.Components.WorkbenchShell
{
    /// <summary>
    /// Centralizes the identifiers and labels used by the bootstrap Workbench host shell.
    /// </summary>
    internal static class WorkbenchHostShellDefaults
    {
        /// <summary>
        /// Gets the identifier of the fallback explorer rendered when no module explorers are available.
        /// </summary>
        internal const string FallbackExplorerId = "explorer.host.overview";

        /// <summary>
        /// Gets the display label of the fallback explorer rendered when no module explorers are available.
        /// </summary>
        internal const string FallbackExplorerDisplayName = "Workbench";

        /// <summary>
        /// Gets the identifier of the host-owned explorer section that groups the overview tool.
        /// </summary>
        internal const string HostToolsSectionId = "explorer.section.host.tools";

        /// <summary>
        /// Gets the identifier of the first host-owned exemplar tool.
        /// </summary>
        internal const string OverviewToolId = "tool.bootstrap.overview";

        /// <summary>
        /// Gets the identifier of the host-owned overview command.
        /// </summary>
        internal const string OverviewCommandId = "command.host.open-overview";

        /// <summary>
        /// Gets the identifier of the host-owned menu contribution that opens the overview tool.
        /// </summary>
        internal const string OverviewMenuId = "menu.host.overview";

        /// <summary>
        /// Gets the identifier of the host-owned toolbar contribution that opens the overview tool.
        /// </summary>
        internal const string OverviewToolbarId = "toolbar.host.overview";

        /// <summary>
        /// Gets the identifier of the host-owned static status-bar contribution.
        /// </summary>
        internal const string HostReadyStatusId = "status.host.ready";
    }
}
