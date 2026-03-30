using Microsoft.Extensions.DependencyInjection;
using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Modules
{
    /// <summary>
    /// Supplies the bounded registration surface that a module can use while the host is composing startup services.
    /// </summary>
    public class ModuleRegistrationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleRegistrationContext"/> class.
        /// </summary>
        /// <param name="module">The metadata that identifies the module currently registering with the host.</param>
        /// <param name="services">The service collection that still accepts module service registrations before container finalization.</param>
        /// <param name="contributionRegistry">The bounded contribution registry used to collect static Workbench definitions.</param>
        public ModuleRegistrationContext(ModuleMetadata module, IServiceCollection services, IWorkbenchContributionRegistry contributionRegistry)
        {
            // The host creates one registration context per module so all startup changes remain attributable and bounded.
            Module = module ?? throw new ArgumentNullException(nameof(module));
            Services = services ?? throw new ArgumentNullException(nameof(services));
            ContributionRegistry = contributionRegistry ?? throw new ArgumentNullException(nameof(contributionRegistry));
        }

        /// <summary>
        /// Gets the metadata that identifies the module currently registering with the host.
        /// </summary>
        public ModuleMetadata Module { get; }

        /// <summary>
        /// Gets the service collection that still accepts module service registrations before container finalization.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// Gets the bounded contribution registry used to collect static Workbench definitions.
        /// </summary>
        public IWorkbenchContributionRegistry ContributionRegistry { get; }

        /// <summary>
        /// Adds a tool definition to the shared Workbench contribution registry.
        /// </summary>
        /// <param name="toolDefinition">The tool definition that should become available to the shell after startup completes.</param>
        public void AddTool(ToolDefinition toolDefinition)
        {
            // The helper keeps module code focused on bounded contributions rather than the registry implementation details.
            ContributionRegistry.AddTool(toolDefinition);
        }

        /// <summary>
        /// Adds a command contribution to the shared Workbench contribution registry.
        /// </summary>
        /// <param name="commandContribution">The command contribution that should become available to shell surfaces.</param>
        public void AddCommand(CommandContribution commandContribution)
        {
            // The helper keeps module registration aligned to the bounded contract rather than the underlying registry implementation.
            ContributionRegistry.AddCommand(commandContribution);
        }

        /// <summary>
        /// Adds an explorer contribution to the shared Workbench contribution registry.
        /// </summary>
        /// <param name="explorerContribution">The explorer contribution that should become available to the shell activity rail.</param>
        public void AddExplorer(ExplorerContribution explorerContribution)
        {
            // Modules register explorer surfaces declaratively so the host still owns composition and rendering.
            ContributionRegistry.AddExplorer(explorerContribution);
        }

        /// <summary>
        /// Adds an explorer-section contribution to the shared Workbench contribution registry.
        /// </summary>
        /// <param name="explorerSectionContribution">The explorer-section contribution that should become available to the explorer pane.</param>
        public void AddExplorerSection(ExplorerSectionContribution explorerSectionContribution)
        {
            // Section registration remains bounded to the shared contribution catalog.
            ContributionRegistry.AddExplorerSection(explorerSectionContribution);
        }

        /// <summary>
        /// Adds an explorer-item contribution to the shared Workbench contribution registry.
        /// </summary>
        /// <param name="explorerItem">The explorer item that should become available to the explorer pane.</param>
        public void AddExplorerItem(ExplorerItem explorerItem)
        {
            // Explorer items stay declarative so the host can keep routing logic centralized.
            ContributionRegistry.AddExplorerItem(explorerItem);
        }

        /// <summary>
        /// Adds a static menu contribution to the shared Workbench contribution registry.
        /// </summary>
        /// <param name="menuContribution">The menu contribution that should become available to the shell menu bar.</param>
        public void AddMenu(MenuContribution menuContribution)
        {
            // Static menu contributions are collected during startup so the shell can compose them after DI finalization.
            ContributionRegistry.AddMenu(menuContribution);
        }

        /// <summary>
        /// Adds a static toolbar contribution to the shared Workbench contribution registry.
        /// </summary>
        /// <param name="toolbarContribution">The toolbar contribution that should become available to the active-view toolbar.</param>
        public void AddToolbar(ToolbarContribution toolbarContribution)
        {
            // Static toolbar contributions follow the same bounded startup registration path as other shell surfaces.
            ContributionRegistry.AddToolbar(toolbarContribution);
        }

        /// <summary>
        /// Adds a static status-bar contribution to the shared Workbench contribution registry.
        /// </summary>
        /// <param name="statusBarContribution">The status-bar contribution that should become available to the shell status bar.</param>
        public void AddStatusBar(StatusBarContribution statusBarContribution)
        {
            // Static status items remain host-composed even when they originate from modules.
            ContributionRegistry.AddStatusBar(statusBarContribution);
        }
    }
}
