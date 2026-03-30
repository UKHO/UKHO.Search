namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Defines the fixed context keys exposed by the initial Workbench shell slice.
    /// </summary>
    public static class WorkbenchContextKeys
    {
        /// <summary>
        /// Identifies the current active explorer within the shell.
        /// </summary>
        public const string ActiveExplorer = "workbench.activeExplorer";

        /// <summary>
        /// Identifies the current active tool within the shell.
        /// </summary>
        public const string ActiveTool = "workbench.activeTool";

        /// <summary>
        /// Identifies the active shell region currently hosting the focused tool.
        /// </summary>
        public const string ActiveRegion = "workbench.activeRegion";

        /// <summary>
        /// Identifies the logical selection type published by the active tool.
        /// </summary>
        public const string SelectionType = "workbench.selectionType";

        /// <summary>
        /// Identifies the current selection count published by the active tool.
        /// </summary>
        public const string SelectionCount = "workbench.selectionCount";

        /// <summary>
        /// Identifies whether the shell tool surface is ready to host a tool.
        /// </summary>
        public const string ToolSurfaceReady = "workbench.toolSurface.ready";
    }
}
