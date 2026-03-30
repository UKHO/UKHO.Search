namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Identifies the fixed shell regions rendered by the bootstrap Workbench layout.
    /// </summary>
    public enum WorkbenchShellRegion
    {
        /// <summary>
        /// Represents the full-width top menu bar.
        /// </summary>
        MenuBar,

        /// <summary>
        /// Represents the narrow activity rail or explorer selector strip.
        /// </summary>
        ActivityRail,

        /// <summary>
        /// Represents the left-hand explorer pane.
        /// </summary>
        Explorer,

        /// <summary>
        /// Represents the central hosted tool surface.
        /// </summary>
        ToolSurface,

        /// <summary>
        /// Represents the active-tool toolbar shown above the working surface.
        /// </summary>
        ActiveToolToolbar,

        /// <summary>
        /// Represents the full-width status bar shown at the bottom of the shell.
        /// </summary>
        StatusBar
    }
}
