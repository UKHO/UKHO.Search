using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Modules
{
    /// <summary>
    /// Provides the bounded registration surface that modules use to contribute Workbench definitions during startup.
    /// </summary>
    public interface IWorkbenchContributionRegistry
    {
        /// <summary>
        /// Gets the tool definitions contributed through the current registration session.
        /// </summary>
        IReadOnlyList<ToolDefinition> ToolDefinitions { get; }

        /// <summary>
        /// Gets the command contributions contributed through the current registration session.
        /// </summary>
        IReadOnlyList<CommandContribution> CommandContributions { get; }

        /// <summary>
        /// Gets the explorer contributions contributed through the current registration session.
        /// </summary>
        IReadOnlyList<ExplorerContribution> ExplorerContributions { get; }

        /// <summary>
        /// Gets the explorer-section contributions contributed through the current registration session.
        /// </summary>
        IReadOnlyList<ExplorerSectionContribution> ExplorerSectionContributions { get; }

        /// <summary>
        /// Gets the explorer-item contributions contributed through the current registration session.
        /// </summary>
        IReadOnlyList<ExplorerItem> ExplorerItems { get; }

        /// <summary>
        /// Gets the static menu contributions contributed through the current registration session.
        /// </summary>
        IReadOnlyList<MenuContribution> MenuContributions { get; }

        /// <summary>
        /// Gets the static toolbar contributions contributed through the current registration session.
        /// </summary>
        IReadOnlyList<ToolbarContribution> ToolbarContributions { get; }

        /// <summary>
        /// Gets the static status-bar contributions contributed through the current registration session.
        /// </summary>
        IReadOnlyList<StatusBarContribution> StatusBarContributions { get; }

        /// <summary>
        /// Adds a tool definition to the Workbench contribution catalog.
        /// </summary>
        /// <param name="toolDefinition">The tool definition that should become available to the Workbench shell.</param>
        void AddTool(ToolDefinition toolDefinition);

        /// <summary>
        /// Adds a command contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="commandContribution">The command contribution that should become available to Workbench shell surfaces.</param>
        void AddCommand(CommandContribution commandContribution);

        /// <summary>
        /// Adds an explorer contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="explorerContribution">The explorer contribution that should become available to the activity rail.</param>
        void AddExplorer(ExplorerContribution explorerContribution);

        /// <summary>
        /// Adds an explorer-section contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="explorerSectionContribution">The explorer-section contribution that should become available to the explorer pane.</param>
        void AddExplorerSection(ExplorerSectionContribution explorerSectionContribution);

        /// <summary>
        /// Adds an explorer-item contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="explorerItem">The explorer item that should become available to the explorer pane.</param>
        void AddExplorerItem(ExplorerItem explorerItem);

        /// <summary>
        /// Adds a static menu contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="menuContribution">The menu contribution that should become available to the shell menu bar.</param>
        void AddMenu(MenuContribution menuContribution);

        /// <summary>
        /// Adds a static toolbar contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="toolbarContribution">The toolbar contribution that should become available to the active-view toolbar.</param>
        void AddToolbar(ToolbarContribution toolbarContribution);

        /// <summary>
        /// Adds a static status-bar contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="statusBarContribution">The status-bar contribution that should become available to the shell status bar.</param>
        void AddStatusBar(StatusBarContribution statusBarContribution);
    }
}
