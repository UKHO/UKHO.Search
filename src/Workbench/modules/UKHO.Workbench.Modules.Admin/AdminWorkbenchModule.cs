using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Modules;
using UKHO.Workbench.Modules.Admin.Tools;
using UKHO.Workbench.Tools;

namespace UKHO.Workbench.Modules.Admin
{
    /// <summary>
    /// Registers the Admin Workbench module and its initial dummy tool contribution.
    /// </summary>
    public class AdminWorkbenchModule : IWorkbenchModule
    {
        private const string BootstrapExplorerId = "explorer.bootstrap";
        private const string AdminSectionId = "explorer.section.admin.tools";
        private const string AdminConsoleToolId = "tool.module.admin.console";
        private const string OpenAdminConsoleCommandId = "command.module.admin.open-console";

        /// <summary>
        /// Gets the metadata that identifies the Admin Workbench module.
        /// </summary>
        public ModuleMetadata Metadata { get; } = new(
            "UKHO.Workbench.Modules.Admin",
            "Admin module",
            "Provides the initial admin dummy tool used to validate multi-module Workbench composition.");

        /// <summary>
        /// Registers the Admin module services and static Workbench contributions.
        /// </summary>
        /// <param name="context">The bounded registration context supplied by the Workbench host during startup.</param>
        public void Register(ModuleRegistrationContext context)
        {
            // The Admin module contributes a single dummy tool so the first repository-specific module map can validate multi-module discovery.
            ArgumentNullException.ThrowIfNull(context);

            context.AddTool(
                new ToolDefinition(
                    AdminConsoleToolId,
                    "Administration",
                    typeof(AdminConsoleTool),
                    BootstrapExplorerId,
                    "admin_panel_settings",
                    "Dummy admin tool used to validate multi-module shell composition."));

            context.AddExplorerSection(new ExplorerSectionContribution(AdminSectionId, BootstrapExplorerId, "Administration", 500));
            context.AddCommand(
                new CommandContribution(
                    OpenAdminConsoleCommandId,
                    "Open Administration",
                    CommandScope.Host,
                    icon: "admin_panel_settings",
                    description: "Opens the Administration tool in the shared Workbench shell.",
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget(AdminConsoleToolId)));
            context.AddExplorerItem(
                new ExplorerItem(
                    "explorer.item.admin.console",
                    BootstrapExplorerId,
                    AdminSectionId,
                    "Administration",
                    OpenAdminConsoleCommandId,
                    ActivationTarget.CreateToolSurfaceTarget(AdminConsoleToolId),
                    "admin_panel_settings",
                    "Dummy Administration surface for the initial Workbench module map.",
                    100));
        }
    }
}
