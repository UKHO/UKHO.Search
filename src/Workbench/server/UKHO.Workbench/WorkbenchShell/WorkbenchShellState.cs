using UKHO.Workbench.Tools;

namespace UKHO.Workbench.WorkbenchShell
{
    /// <summary>
    /// Stores the lightweight bootstrap shell state for explorers and hosted tools.
    /// </summary>
    public class WorkbenchShellState
    {
        private static readonly IReadOnlyList<WorkbenchShellRegion> BootstrapVisibleRegions =
        [
            WorkbenchShellRegion.MenuBar,
            WorkbenchShellRegion.ActivityRail,
            WorkbenchShellRegion.Explorer,
            WorkbenchShellRegion.ToolSurface,
            WorkbenchShellRegion.ActiveToolToolbar,
            WorkbenchShellRegion.StatusBar
        ];

        private readonly Dictionary<string, ToolInstance> _singletonInstancesByToolId = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ToolInstance> _instancesByInstanceId = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchShellState"/> class.
        /// </summary>
        public WorkbenchShellState()
        {
            // The bootstrap shell starts empty and gains active explorer/tool state during host initialization.
            VisibleRegions = BootstrapVisibleRegions;
        }

        /// <summary>
        /// Gets the fixed bootstrap shell regions that are currently visible.
        /// </summary>
        public IReadOnlyList<WorkbenchShellRegion> VisibleRegions { get; }

        /// <summary>
        /// Gets the active explorer identifier selected by the shell.
        /// </summary>
        public string? ActiveExplorerId { get; private set; }

        /// <summary>
        /// Gets the currently active tool instance hosted by the shell.
        /// </summary>
        public ToolInstance? ActiveTool { get; private set; }

        /// <summary>
        /// Gets the singleton tool instances currently tracked by the shell.
        /// </summary>
        public IReadOnlyCollection<ToolInstance> ToolInstances => _singletonInstancesByToolId.Values;

        /// <summary>
        /// Updates the active explorer tracked by the shell.
        /// </summary>
        /// <param name="explorerId">The identifier of the explorer that should become active.</param>
        public void SetActiveExplorer(string explorerId)
        {
            // The first slice only tracks one active explorer identifier because the left rail selects a single explorer at a time.
            ArgumentException.ThrowIfNullOrWhiteSpace(explorerId);

            ActiveExplorerId = explorerId;
        }

        /// <summary>
        /// Determines whether a shell region is currently visible.
        /// </summary>
        /// <param name="region">The region to check.</param>
        /// <returns><see langword="true"/> when the region is part of the fixed bootstrap shell; otherwise, <see langword="false"/>.</returns>
        public bool IsRegionVisible(WorkbenchShellRegion region)
        {
            // The bootstrap shell keeps a fixed chrome layout so region visibility is a simple membership check.
            return VisibleRegions.Contains(region);
        }

        /// <summary>
        /// Opens or focuses a tool according to the singleton hosting policy of the bootstrap shell.
        /// </summary>
        /// <param name="definition">The static tool definition being activated.</param>
        /// <param name="activationTarget">The shell target that should host the tool.</param>
        /// <returns>The active runtime tool instance after the activation request completes.</returns>
        public ToolInstance ActivateTool(
            ToolDefinition definition,
            ActivationTarget activationTarget,
            Func<string, ToolContext> toolContextFactory)
        {
            // Activation requires both the static tool registration and the resolved shell target.
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(activationTarget);
            ArgumentNullException.ThrowIfNull(toolContextFactory);

            // The bootstrap slice only allows hosted tool content inside the central working region.
            if (activationTarget.Region != WorkbenchShellRegion.ToolSurface || !IsRegionVisible(activationTarget.Region))
            {
                throw new InvalidOperationException($"The bootstrap Workbench shell can only host tools in the {WorkbenchShellRegion.ToolSurface} region.");
            }

            // Singleton tools are reused so reopening them only changes focus rather than duplicating the hosted UI.
            if (definition.IsSingleton && _singletonInstancesByToolId.TryGetValue(definition.Id, out var existingInstance))
            {
                ActiveTool = existingInstance;
                return existingInstance;
            }

            // Non-singleton behavior is intentionally out of scope for this slice, so every registered tool is tracked as a singleton.
            var instanceId = Guid.NewGuid().ToString("N");
            var newInstance = new ToolInstance(
                instanceId,
                definition,
                definition.DisplayName,
                definition.Icon,
                null,
                activationTarget.Region,
                DateTimeOffset.UtcNow,
                toolContextFactory(instanceId));

            _singletonInstancesByToolId[definition.Id] = newInstance;
            _instancesByInstanceId[instanceId] = newInstance;
            ActiveTool = newInstance;
            return newInstance;
        }

        /// <summary>
        /// Attempts to resolve a tracked tool instance by its runtime instance identifier.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance identifier to resolve.</param>
        /// <param name="toolInstance">The resolved tool instance when the lookup succeeds; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the tool instance exists; otherwise, <see langword="false"/>.</returns>
        public bool TryGetToolInstance(string toolInstanceId, out ToolInstance? toolInstance)
        {
            // Tool-context updates route through runtime instance identifiers so commands can target the focused singleton accurately.
            ArgumentException.ThrowIfNullOrWhiteSpace(toolInstanceId);

            var exists = _instancesByInstanceId.TryGetValue(toolInstanceId, out var resolvedToolInstance);
            toolInstance = resolvedToolInstance;
            return exists;
        }
    }
}
