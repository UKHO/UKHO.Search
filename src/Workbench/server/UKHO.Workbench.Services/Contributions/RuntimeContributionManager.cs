using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Services.Contributions
{
    /// <summary>
    /// Composes static and active-tool runtime contributions for menu, toolbar, and status-bar surfaces.
    /// </summary>
    public class RuntimeContributionManager
    {
        private readonly Dictionary<string, MenuContribution> _menuContributions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ToolbarContribution> _toolbarContributions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, StatusBarContribution> _statusBarContributions = new(StringComparer.Ordinal);

        /// <summary>
        /// Registers a static menu contribution.
        /// </summary>
        /// <param name="menuContribution">The menu contribution that should be available to the shell menu bar.</param>
        public void RegisterMenu(MenuContribution menuContribution)
        {
            // Menu contributions remain unique so shell composition stays deterministic across host and module startup.
            ArgumentNullException.ThrowIfNull(menuContribution);
            _menuContributions[menuContribution.Id] = menuContribution;
        }

        /// <summary>
        /// Registers a static toolbar contribution.
        /// </summary>
        /// <param name="toolbarContribution">The toolbar contribution that should be available to the active-view toolbar.</param>
        public void RegisterToolbar(ToolbarContribution toolbarContribution)
        {
            // Toolbar contributions remain unique so repeated startup runs stay idempotent.
            ArgumentNullException.ThrowIfNull(toolbarContribution);
            _toolbarContributions[toolbarContribution.Id] = toolbarContribution;
        }

        /// <summary>
        /// Registers a static status-bar contribution.
        /// </summary>
        /// <param name="statusBarContribution">The status-bar contribution that should be available to the shell status bar.</param>
        public void RegisterStatusBar(StatusBarContribution statusBarContribution)
        {
            // Status-bar contribution registration mirrors the other static shell surfaces.
            ArgumentNullException.ThrowIfNull(statusBarContribution);
            _statusBarContributions[statusBarContribution.Id] = statusBarContribution;
        }

        /// <summary>
        /// Returns the menu contributions visible for the supplied active tool.
        /// </summary>
        /// <param name="activeTool">The currently active tool instance, or <see langword="null"/> when no tool is active.</param>
        /// <returns>The static menu contributions plus any runtime menu contributions from the active tool.</returns>
        public IReadOnlyList<MenuContribution> GetMenuContributions(ToolInstance? activeTool)
        {
            // Only the active tool participates in runtime menu composition for this slice.
            return _menuContributions.Values
                .Concat(activeTool?.RuntimeMenuContributions ?? Array.Empty<MenuContribution>())
                .OrderBy(menuContribution => menuContribution.Order)
                .ThenBy(menuContribution => menuContribution.DisplayName, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Returns the toolbar contributions visible for the supplied active tool.
        /// </summary>
        /// <param name="activeTool">The currently active tool instance, or <see langword="null"/> when no tool is active.</param>
        /// <returns>The static toolbar contributions plus any runtime toolbar contributions from the active tool.</returns>
        public IReadOnlyList<ToolbarContribution> GetToolbarContributions(ToolInstance? activeTool)
        {
            // Runtime toolbar participation is intentionally limited to the active-view toolbar in this first slice.
            return _toolbarContributions.Values
                .Concat(activeTool?.RuntimeToolbarContributions ?? Array.Empty<ToolbarContribution>())
                .OrderBy(toolbarContribution => toolbarContribution.Order)
                .ThenBy(toolbarContribution => toolbarContribution.DisplayName, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Returns the status-bar contributions visible for the supplied active tool.
        /// </summary>
        /// <param name="activeTool">The currently active tool instance, or <see langword="null"/> when no tool is active.</param>
        /// <returns>The static status-bar contributions plus any runtime status-bar contributions from the active tool.</returns>
        public IReadOnlyList<StatusBarContribution> GetStatusBarContributions(ToolInstance? activeTool)
        {
            // Runtime status-bar contributions follow the active tool only, so they disappear automatically when focus moves away.
            return _statusBarContributions.Values
                .Concat(activeTool?.RuntimeStatusBarContributions ?? Array.Empty<StatusBarContribution>())
                .OrderBy(statusBarContribution => statusBarContribution.Order)
                .ThenBy(statusBarContribution => statusBarContribution.Text, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
