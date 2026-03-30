using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Modules
{
    /// <summary>
    /// Stores the static Workbench contributions collected during host startup before the shell is initialized.
    /// </summary>
    public class WorkbenchContributionRegistry : IWorkbenchContributionRegistry
    {
        private readonly Dictionary<string, ToolDefinition> _toolDefinitions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CommandContribution> _commandContributions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ExplorerContribution> _explorerContributions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ExplorerSectionContribution> _explorerSectionContributions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ExplorerItem> _explorerItems = new(StringComparer.Ordinal);
        private readonly Dictionary<string, MenuContribution> _menuContributions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ToolbarContribution> _toolbarContributions = new(StringComparer.Ordinal);
        private readonly Dictionary<string, StatusBarContribution> _statusBarContributions = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the tool definitions contributed through the current registration session.
        /// </summary>
        public IReadOnlyList<ToolDefinition> ToolDefinitions => _toolDefinitions.Values
            .OrderBy(toolDefinition => toolDefinition.DisplayName, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Gets the command contributions contributed through the current registration session.
        /// </summary>
        public IReadOnlyList<CommandContribution> CommandContributions => _commandContributions.Values
            .OrderBy(commandContribution => commandContribution.DisplayName, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Gets the explorer contributions contributed through the current registration session.
        /// </summary>
        public IReadOnlyList<ExplorerContribution> ExplorerContributions => _explorerContributions.Values
            .OrderBy(explorerContribution => explorerContribution.Order)
            .ThenBy(explorerContribution => explorerContribution.DisplayName, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Gets the explorer-section contributions contributed through the current registration session.
        /// </summary>
        public IReadOnlyList<ExplorerSectionContribution> ExplorerSectionContributions => _explorerSectionContributions.Values
            .OrderBy(explorerSectionContribution => explorerSectionContribution.Order)
            .ThenBy(explorerSectionContribution => explorerSectionContribution.DisplayName, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Gets the explorer-item contributions contributed through the current registration session.
        /// </summary>
        public IReadOnlyList<ExplorerItem> ExplorerItems => _explorerItems.Values
            .OrderBy(explorerItem => explorerItem.Order)
            .ThenBy(explorerItem => explorerItem.DisplayName, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Gets the static menu contributions contributed through the current registration session.
        /// </summary>
        public IReadOnlyList<MenuContribution> MenuContributions => _menuContributions.Values
            .OrderBy(menuContribution => menuContribution.Order)
            .ThenBy(menuContribution => menuContribution.DisplayName, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Gets the static toolbar contributions contributed through the current registration session.
        /// </summary>
        public IReadOnlyList<ToolbarContribution> ToolbarContributions => _toolbarContributions.Values
            .OrderBy(toolbarContribution => toolbarContribution.Order)
            .ThenBy(toolbarContribution => toolbarContribution.DisplayName, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Gets the static status-bar contributions contributed through the current registration session.
        /// </summary>
        public IReadOnlyList<StatusBarContribution> StatusBarContributions => _statusBarContributions.Values
            .OrderBy(statusBarContribution => statusBarContribution.Order)
            .ThenBy(statusBarContribution => statusBarContribution.Text, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Adds a tool definition to the Workbench contribution catalog.
        /// </summary>
        /// <param name="toolDefinition">The tool definition that should become available to the Workbench shell.</param>
        public void AddTool(ToolDefinition toolDefinition)
        {
            // Modules must contribute through stable identifiers so the shell can activate tools deterministically later.
            ArgumentNullException.ThrowIfNull(toolDefinition);

            if (_toolDefinitions.TryGetValue(toolDefinition.Id, out var existingToolDefinition))
            {
                // Re-registering the same tool definition is tolerated so repeated startup paths remain idempotent in tests.
                if (existingToolDefinition.ComponentType == toolDefinition.ComponentType
                    && string.Equals(existingToolDefinition.ExplorerId, toolDefinition.ExplorerId, StringComparison.Ordinal)
                    && string.Equals(existingToolDefinition.Icon, toolDefinition.Icon, StringComparison.Ordinal)
                    && string.Equals(existingToolDefinition.DisplayName, toolDefinition.DisplayName, StringComparison.Ordinal)
                    && string.Equals(existingToolDefinition.Description, toolDefinition.Description, StringComparison.Ordinal)
                    && existingToolDefinition.IsSingleton == toolDefinition.IsSingleton
                    && existingToolDefinition.DefaultRegion == toolDefinition.DefaultRegion)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench tool with id '{toolDefinition.Id}' has already been contributed.");
            }

            _toolDefinitions.Add(toolDefinition.Id, toolDefinition);
        }

        /// <summary>
        /// Adds a command contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="commandContribution">The command contribution that should become available to Workbench shell surfaces.</param>
        public void AddCommand(CommandContribution commandContribution)
        {
            // Command identifiers must remain unique so every UI surface routes to one deterministic action.
            ArgumentNullException.ThrowIfNull(commandContribution);

            if (_commandContributions.TryGetValue(commandContribution.Id, out var existingContribution))
            {
                if (existingContribution.DisplayName == commandContribution.DisplayName
                    && existingContribution.Scope == commandContribution.Scope
                    && existingContribution.Icon == commandContribution.Icon
                    && existingContribution.Description == commandContribution.Description
                    && existingContribution.OwnerToolId == commandContribution.OwnerToolId
                    && existingContribution.ActivationTarget?.ToolId == commandContribution.ActivationTarget?.ToolId
                    && existingContribution.ActivationTarget?.Region == commandContribution.ActivationTarget?.Region
                    && existingContribution.ExecutionHandler == commandContribution.ExecutionHandler)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench command with id '{commandContribution.Id}' has already been contributed.");
            }

            _commandContributions.Add(commandContribution.Id, commandContribution);
        }

        /// <summary>
        /// Adds an explorer contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="explorerContribution">The explorer contribution that should become available to the activity rail.</param>
        public void AddExplorer(ExplorerContribution explorerContribution)
        {
            // Explorer identifiers must stay unique so shell state can safely track the active explorer.
            ArgumentNullException.ThrowIfNull(explorerContribution);

            if (_explorerContributions.TryGetValue(explorerContribution.Id, out var existingContribution))
            {
                if (existingContribution.DisplayName == explorerContribution.DisplayName
                    && existingContribution.Icon == explorerContribution.Icon
                    && existingContribution.Order == explorerContribution.Order)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench explorer with id '{explorerContribution.Id}' has already been contributed.");
            }

            _explorerContributions.Add(explorerContribution.Id, explorerContribution);
        }

        /// <summary>
        /// Adds an explorer-section contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="explorerSectionContribution">The explorer-section contribution that should become available to the explorer pane.</param>
        public void AddExplorerSection(ExplorerSectionContribution explorerSectionContribution)
        {
            // Section identifiers must stay unique so explorer items can bind to sections deterministically.
            ArgumentNullException.ThrowIfNull(explorerSectionContribution);

            if (_explorerSectionContributions.TryGetValue(explorerSectionContribution.Id, out var existingContribution))
            {
                if (existingContribution.ExplorerId == explorerSectionContribution.ExplorerId
                    && existingContribution.DisplayName == explorerSectionContribution.DisplayName
                    && existingContribution.Order == explorerSectionContribution.Order)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench explorer section with id '{explorerSectionContribution.Id}' has already been contributed.");
            }

            _explorerSectionContributions.Add(explorerSectionContribution.Id, explorerSectionContribution);
        }

        /// <summary>
        /// Adds an explorer-item contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="explorerItem">The explorer item that should become available to the explorer pane.</param>
        public void AddExplorerItem(ExplorerItem explorerItem)
        {
            // Explorer item identifiers must stay unique so rendering keys and diagnostics remain stable.
            ArgumentNullException.ThrowIfNull(explorerItem);

            if (_explorerItems.TryGetValue(explorerItem.Id, out var existingContribution))
            {
                if (existingContribution.ExplorerId == explorerItem.ExplorerId
                    && existingContribution.SectionId == explorerItem.SectionId
                    && existingContribution.DisplayName == explorerItem.DisplayName
                    && existingContribution.CommandId == explorerItem.CommandId
                    && existingContribution.ActivationTarget.ToolId == explorerItem.ActivationTarget.ToolId
                    && existingContribution.ActivationTarget.Region == explorerItem.ActivationTarget.Region
                    && existingContribution.Icon == explorerItem.Icon
                    && existingContribution.Description == explorerItem.Description
                    && existingContribution.Order == explorerItem.Order)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench explorer item with id '{explorerItem.Id}' has already been contributed.");
            }

            _explorerItems.Add(explorerItem.Id, explorerItem);
        }

        /// <summary>
        /// Adds a static menu contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="menuContribution">The menu contribution that should become available to the shell menu bar.</param>
        public void AddMenu(MenuContribution menuContribution)
        {
            // Menu contribution identifiers remain unique so menu composition stays deterministic across host and module registrations.
            ArgumentNullException.ThrowIfNull(menuContribution);

            if (_menuContributions.TryGetValue(menuContribution.Id, out var existingContribution))
            {
                if (existingContribution.DisplayName == menuContribution.DisplayName
                    && existingContribution.CommandId == menuContribution.CommandId
                    && existingContribution.Icon == menuContribution.Icon
                    && existingContribution.OwnerToolId == menuContribution.OwnerToolId
                    && existingContribution.Order == menuContribution.Order)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench menu contribution with id '{menuContribution.Id}' has already been contributed.");
            }

            _menuContributions.Add(menuContribution.Id, menuContribution);
        }

        /// <summary>
        /// Adds a static toolbar contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="toolbarContribution">The toolbar contribution that should become available to the active-view toolbar.</param>
        public void AddToolbar(ToolbarContribution toolbarContribution)
        {
            // Toolbar contribution identifiers remain unique so command surfaces do not duplicate unexpectedly.
            ArgumentNullException.ThrowIfNull(toolbarContribution);

            if (_toolbarContributions.TryGetValue(toolbarContribution.Id, out var existingContribution))
            {
                if (existingContribution.DisplayName == toolbarContribution.DisplayName
                    && existingContribution.CommandId == toolbarContribution.CommandId
                    && existingContribution.Icon == toolbarContribution.Icon
                    && existingContribution.OwnerToolId == toolbarContribution.OwnerToolId
                    && existingContribution.Order == toolbarContribution.Order)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench toolbar contribution with id '{toolbarContribution.Id}' has already been contributed.");
            }

            _toolbarContributions.Add(toolbarContribution.Id, toolbarContribution);
        }

        /// <summary>
        /// Adds a static status-bar contribution to the Workbench contribution catalog.
        /// </summary>
        /// <param name="statusBarContribution">The status-bar contribution that should become available to the shell status bar.</param>
        public void AddStatusBar(StatusBarContribution statusBarContribution)
        {
            // Status contribution identifiers remain unique so the shell status bar stays stable and diagnosable.
            ArgumentNullException.ThrowIfNull(statusBarContribution);

            if (_statusBarContributions.TryGetValue(statusBarContribution.Id, out var existingContribution))
            {
                if (existingContribution.Text == statusBarContribution.Text
                    && existingContribution.CommandId == statusBarContribution.CommandId
                    && existingContribution.Icon == statusBarContribution.Icon
                    && existingContribution.OwnerToolId == statusBarContribution.OwnerToolId
                    && existingContribution.Order == statusBarContribution.Order)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench status-bar contribution with id '{statusBarContribution.Id}' has already been contributed.");
            }

            _statusBarContributions.Add(statusBarContribution.Id, statusBarContribution);
        }
    }
}
