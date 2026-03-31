using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Modules;
using UKHO.Workbench.Modules.PKS.Tools;
using UKHO.Workbench.Tools;

namespace UKHO.Workbench.Modules.PKS
{
    /// <summary>
    /// Registers the PKS Workbench module and its initial dummy tool contribution.
    /// </summary>
    public class PksWorkbenchModule : IWorkbenchModule
    {
        private const string PksExplorerId = "explorer.module.pks.operations";
        private const string PksSectionId = "explorer.section.pks.tools";
        private const string PksOperationsToolId = "tool.module.pks.operations";
        private const string OpenPksOperationsCommandId = "command.module.pks.open-operations";

        /// <summary>
        /// Gets the metadata that identifies the PKS Workbench module.
        /// </summary>
        public ModuleMetadata Metadata { get; } = new(
            "UKHO.Workbench.Modules.PKS",
            "PKS module",
            "Provides the initial PKS dummy tool used to validate multi-module Workbench composition.");

        /// <summary>
        /// Registers the PKS module services and static Workbench contributions.
        /// </summary>
        /// <param name="context">The bounded registration context supplied by the Workbench host during startup.</param>
        public void Register(ModuleRegistrationContext context)
        {
            // The PKS module contributes a single dummy tool so the first repository-specific module map can validate multi-module discovery.
            ArgumentNullException.ThrowIfNull(context);

            context.AddExplorer(new ExplorerContribution(PksExplorerId, "PKS operations", "hub", 200));

            context.AddTool(
                new ToolDefinition(
                    PksOperationsToolId,
                    "PKS operations",
                    typeof(PksOperationsTool),
                    PksExplorerId,
                    "hub",
                    "Dummy PKS tool used to validate multi-module shell composition."));

            context.AddExplorerSection(new ExplorerSectionContribution(PksSectionId, PksExplorerId, "PKS module", 100));
            context.AddCommand(
                new CommandContribution(
                    OpenPksOperationsCommandId,
                    "Open PKS operations",
                    CommandScope.Host,
                    icon: "hub",
                    description: "Opens the PKS operations tool in the shared Workbench shell.",
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget(PksOperationsToolId)));
            context.AddExplorerItem(
                new ExplorerItem(
                    "explorer.item.pks.operations",
                    PksExplorerId,
                    PksSectionId,
                    "PKS operations",
                    OpenPksOperationsCommandId,
                    ActivationTarget.CreateToolSurfaceTarget(PksOperationsToolId),
                    "hub",
                    "Dummy PKS operations surface for the initial Workbench module map.",
                    100));
        }
    }
}
