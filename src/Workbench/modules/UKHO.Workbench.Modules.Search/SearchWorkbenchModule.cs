using Microsoft.Extensions.DependencyInjection;
using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Modules;
using UKHO.Workbench.Modules.Search.Tools;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Modules.Search
{
    /// <summary>
    /// Registers the Search Workbench module and its initial exemplar tool set.
    /// </summary>
    public class SearchWorkbenchModule : IWorkbenchModule
    {
        private const string QueryExplorerId = "explorer.module.search.query";
        private const string IngestionExplorerId = "explorer.module.search.ingestion";
        private const string RuleEditorExplorerId = "explorer.module.search.rule-editor";
        private const string QuerySectionId = "explorer.section.search.query";
        private const string IngestionSectionId = "explorer.section.search.ingestion";
        private const string RuleEditorSectionId = "explorer.section.search.rule-editor";
        private const string SearchIngestionToolId = "tool.module.search.ingestion";
        private const string SearchQueryToolId = "tool.module.search.query";
        private const string IngestionRuleEditorToolId = "tool.module.search.rule-editor";
        private const string OpenSearchIngestionCommandId = "command.module.search.open-ingestion";
        private const string OpenSearchQueryCommandId = "command.module.search.open-query";
        private const string OpenIngestionRuleEditorCommandId = "command.module.search.open-rule-editor";
        private const string RunSampleSearchQueryCommandId = "command.module.search.query.run-sample";
        private const string ResetSearchQueryCommandId = "command.module.search.query.reset";

        /// <summary>
        /// Gets the metadata that identifies the Search Workbench module.
        /// </summary>
        public ModuleMetadata Metadata { get; } = new(
            "UKHO.Workbench.Modules.Search",
            "Search module",
            "Provides the initial Search exemplar tools used to validate multi-tool module composition.");

        /// <summary>
        /// Registers the Search module services and static Workbench contributions.
        /// </summary>
        /// <param name="context">The bounded registration context supplied by the Workbench host during startup.</param>
        public void Register(ModuleRegistrationContext context)
        {
            // The module keeps registration intentionally lightweight for this slice while still proving multi-tool composition, command routing, and tool-context behavior.
            ArgumentNullException.ThrowIfNull(context);

            // A marker service proves the module can participate in the host service collection before DI finalization.
            context.Services.AddSingleton<SearchModuleRegistrationMarker>();

            RegisterExplorers(context);

            RegisterSearchIngestionTool(context);
            RegisterSearchQueryTool(context);
            RegisterIngestionRuleEditorTool(context);
        }

        /// <summary>
        /// Registers the activity-rail explorers used to keep the Search module's three exemplar tools separate.
        /// </summary>
        /// <param name="context">The bounded module registration context supplied by the host.</param>
        private static void RegisterExplorers(ModuleRegistrationContext context)
        {
            // Each Search capability gets its own explorer so the rail mirrors the requested composition rather than grouping everything under one shared node.
            context.AddExplorer(new ExplorerContribution(QueryExplorerId, "Query", "manage_search", 100));
            context.AddExplorer(new ExplorerContribution(IngestionExplorerId, "Ingestion", "publish", 110));
            context.AddExplorer(new ExplorerContribution(RuleEditorExplorerId, "Rule Editor", "rule", 120));

            context.AddExplorerSection(new ExplorerSectionContribution(QuerySectionId, QueryExplorerId, "Search module", 100));
            context.AddExplorerSection(new ExplorerSectionContribution(IngestionSectionId, IngestionExplorerId, "Search module", 100));
            context.AddExplorerSection(new ExplorerSectionContribution(RuleEditorSectionId, RuleEditorExplorerId, "Search module", 100));
        }

        /// <summary>
        /// Registers the Search ingestion exemplar tool.
        /// </summary>
        /// <param name="context">The bounded module registration context supplied by the host.</param>
        private static void RegisterSearchIngestionTool(ModuleRegistrationContext context)
        {
            // The ingestion exemplar proves one module can contribute multiple singleton tools to the shared shell.
            context.AddTool(
                new ToolDefinition(
                    SearchIngestionToolId,
                    "Search ingestion",
                    typeof(SearchIngestionTool),
                    IngestionExplorerId,
                    "publish",
                    "Dummy Search ingestion tool used to validate multi-tool module composition."));

            context.AddCommand(
                new CommandContribution(
                    OpenSearchIngestionCommandId,
                    "Open Search ingestion",
                    CommandScope.Host,
                    icon: "publish",
                    description: "Opens the Search ingestion tool in the shared Workbench shell.",
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget(SearchIngestionToolId)));

            context.AddExplorerItem(
                new ExplorerItem(
                    "explorer.item.search.ingestion",
                    IngestionExplorerId,
                    IngestionSectionId,
                    "Search ingestion",
                    OpenSearchIngestionCommandId,
                    ActivationTarget.CreateToolSurfaceTarget(SearchIngestionToolId),
                    "publish",
                    "Dummy Search ingestion surface for the initial Workbench module map.",
                    100));
        }

        /// <summary>
        /// Registers the Search query exemplar tool and its runtime commands.
        /// </summary>
        /// <param name="context">The bounded module registration context supplied by the host.</param>
        private static void RegisterSearchQueryTool(ModuleRegistrationContext context)
        {
            // The query exemplar keeps the earlier runtime menu, toolbar, and status contribution demonstration alive within the broader Search module map.
            context.AddTool(
                new ToolDefinition(
                    SearchQueryToolId,
                    "Search query",
                    typeof(SearchQueryTool),
                    QueryExplorerId,
                    "manage_search",
                    "Dummy Search query tool used to verify runtime shell participation."));

            context.AddCommand(
                new CommandContribution(
                    OpenSearchQueryCommandId,
                    "Open Search query",
                    CommandScope.Host,
                    icon: "manage_search",
                    description: "Opens the Search query tool in the shared Workbench shell.",
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget(SearchQueryToolId)));

            context.AddCommand(
                new CommandContribution(
                    RunSampleSearchQueryCommandId,
                    "Run sample Search query",
                    CommandScope.Tool,
                    icon: "play_circle",
                    description: "Updates the active Search query tool with a sample query state.",
                    ownerToolId: SearchQueryToolId,
                    executionHandler: static async (toolContext, cancellationToken) =>
                    {
                        if (toolContext is null)
                        {
                            return;
                        }

                        // The sample command demonstrates title, badge, selection, status, and notification updates through the bounded context.
                        toolContext.SetTitle("Search query • sample query");
                        toolContext.SetIcon("manage_search");
                        toolContext.SetBadge("1");
                        toolContext.SetSelection("search.query", 1);
                        toolContext.SetRuntimeStatusBarContributions(
                        [
                            new StatusBarContribution("status.runtime.search.query.last-action", "Sample query executed", ownerToolId: SearchQueryToolId, order: 200),
                            new StatusBarContribution("status.runtime.search.query.selection", "Selection: 1 query", ownerToolId: SearchQueryToolId, order: 201)
                        ]);

                        await toolContext.NotifyAsync(
                            "success",
                            "Search query executed",
                            "The sample Search query updated the active tool state.",
                            cancellationToken).ConfigureAwait(false);
                    }));

            context.AddCommand(
                new CommandContribution(
                    ResetSearchQueryCommandId,
                    "Reset Search query",
                    CommandScope.Tool,
                    icon: "restart_alt",
                    description: "Restores the Search query dummy tool to its default state.",
                    ownerToolId: SearchQueryToolId,
                    executionHandler: static async (toolContext, cancellationToken) =>
                    {
                        if (toolContext is null)
                        {
                            return;
                        }

                        // Resetting restores the original title and clears the selection and badge so the runtime shell state visibly recomposes.
                        toolContext.SetTitle("Search query");
                        toolContext.SetIcon("manage_search");
                        toolContext.SetBadge(null);
                        toolContext.SetSelection(null, 0);
                        toolContext.SetRuntimeStatusBarContributions(
                        [
                            new StatusBarContribution("status.runtime.search.query.ready", "Search query ready", ownerToolId: SearchQueryToolId, order: 200)
                        ]);

                        await toolContext.NotifyAsync(
                            "info",
                            "Search query reset",
                            "The Search query tool returned to its default runtime state.",
                            cancellationToken).ConfigureAwait(false);
                    }));

            context.AddExplorerItem(
                new ExplorerItem(
                    "explorer.item.search.query",
                    QueryExplorerId,
                    QuerySectionId,
                    "Search query",
                    OpenSearchQueryCommandId,
                    ActivationTarget.CreateToolSurfaceTarget(SearchQueryToolId),
                    "manage_search",
                    "Dummy Search query tool used to verify runtime menu, toolbar, and status contributions.",
                    110));
        }

        /// <summary>
        /// Registers the ingestion rule editor exemplar tool.
        /// </summary>
        /// <param name="context">The bounded module registration context supplied by the host.</param>
        private static void RegisterIngestionRuleEditorTool(ModuleRegistrationContext context)
        {
            // The rule-editor exemplar completes the three-tool Search module map required by the work package.
            context.AddTool(
                new ToolDefinition(
                    IngestionRuleEditorToolId,
                    "Ingestion rule editor",
                    typeof(IngestionRuleEditorTool),
                    RuleEditorExplorerId,
                    "rule",
                    "Dummy ingestion rule editor used to validate multi-tool Search module discovery."));

            context.AddCommand(
                new CommandContribution(
                    OpenIngestionRuleEditorCommandId,
                    "Open ingestion rule editor",
                    CommandScope.Host,
                    icon: "rule",
                    description: "Opens the ingestion rule editor tool in the shared Workbench shell.",
                    activationTarget: ActivationTarget.CreateToolSurfaceTarget(IngestionRuleEditorToolId)));

            context.AddExplorerItem(
                new ExplorerItem(
                    "explorer.item.search.rule-editor",
                    RuleEditorExplorerId,
                    RuleEditorSectionId,
                    "Ingestion rule editor",
                    OpenIngestionRuleEditorCommandId,
                    ActivationTarget.CreateToolSurfaceTarget(IngestionRuleEditorToolId),
                    "rule",
                    "Dummy ingestion rule editor surface for the initial Workbench module map.",
                    120));
        }
    }
}
