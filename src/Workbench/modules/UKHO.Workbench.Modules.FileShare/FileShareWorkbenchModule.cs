using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Modules;
using UKHO.Workbench.Modules.FileShare.Tools;
using UKHO.Workbench.Tools;

namespace UKHO.Workbench.Modules.FileShare
{
    /// <summary>
    /// Registers the File Share Workbench module and its initial dummy tool contribution.
    /// </summary>
    public class FileShareWorkbenchModule : IWorkbenchModule
    {
        private const string FileShareExplorerId = "explorer.module.fileshare.workspace";
        private const string FileShareSectionId = "explorer.section.fileshare.tools";
        private const string FileShareWorkspaceToolId = "tool.module.fileshare.workspace";
        private const string OpenFileShareWorkspaceCommandId = "command.module.fileshare.open-workspace";

        /// <summary>
        /// Gets the metadata that identifies the File Share Workbench module.
        /// </summary>
        public ModuleMetadata Metadata { get; } = new(
            "UKHO.Workbench.Modules.FileShare",
            "File Share module",
            "Provides the initial File Share dummy tool used to validate multi-module Workbench composition.");

        /// <summary>
        /// Registers the File Share module services and static Workbench contributions.
        /// </summary>
        /// <param name="context">The bounded registration context supplied by the Workbench host during startup.</param>
        public void Register(ModuleRegistrationContext context)
        {
            // The File Share module contributes a single dummy tool so the first repository-specific module map can validate multi-module discovery.
            ArgumentNullException.ThrowIfNull(context);

            context.AddExplorer(new ExplorerContribution(FileShareExplorerId, "File Share workspace", "folder_shared", 300));

            context.AddTool(
                new ToolDefinition(
                    FileShareWorkspaceToolId,
                    "File Share workspace",
                    typeof(FileShareWorkspaceTool),
                    FileShareExplorerId,
                    "folder_shared",
                    "Dummy File Share tool used to validate multi-module shell composition."));

            context.AddExplorerSection(new ExplorerSectionContribution(FileShareSectionId, FileShareExplorerId, "File Share module", 100));
            context.AddCommand(
                new CommandContribution(
                    OpenFileShareWorkspaceCommandId,
                    "Open File Share workspace",
                    CommandScope.Host,
                    icon: "folder_shared",
                    description: "Opens the File Share workspace tool in the shared Workbench shell.",
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget(FileShareWorkspaceToolId)));
            context.AddExplorerItem(
                new ExplorerItem(
                    "explorer.item.fileshare.workspace",
                    FileShareExplorerId,
                    FileShareSectionId,
                    "File Share workspace",
                    OpenFileShareWorkspaceCommandId,
                    ActivationTarget.CreateToolSurfaceTarget(FileShareWorkspaceToolId),
                    "folder_shared",
                    "Dummy File Share workspace surface for the initial Workbench module map.",
                    100));
        }
    }
}
