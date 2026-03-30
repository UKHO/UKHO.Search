using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Services.Tools
{
    /// <summary>
    /// Stores registered tool definitions and applies the singleton activation rules for the Workbench shell.
    /// </summary>
    public class ToolActivationManager
    {
        private readonly IToolContextBridge _toolContextBridge;
        private readonly Dictionary<string, ToolDefinition> _toolDefinitions = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolActivationManager"/> class.
        /// </summary>
        /// <param name="toolContextBridge">The bounded bridge used to create tool contexts for new runtime instances.</param>
        public ToolActivationManager(IToolContextBridge toolContextBridge)
        {
            // New runtime tool instances need a bounded tool context immediately, so the activation manager keeps the bridge required to create one.
            _toolContextBridge = toolContextBridge ?? throw new ArgumentNullException(nameof(toolContextBridge));
            State = new WorkbenchShellState();
        }

        /// <summary>
        /// Gets the current Workbench shell state.
        /// </summary>
        public WorkbenchShellState State { get; }

        /// <summary>
        /// Gets the registered tool definitions in display order.
        /// </summary>
        public IReadOnlyList<ToolDefinition> ToolDefinitions => _toolDefinitions.Values
            .OrderBy(toolDefinition => toolDefinition.DisplayName, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Registers a static Workbench tool definition.
        /// </summary>
        /// <param name="toolDefinition">The tool definition that should become available for activation.</param>
        public void RegisterTool(ToolDefinition toolDefinition)
        {
            // Tool identifiers must stay unique so activation requests always resolve to one hosted component definition.
            ArgumentNullException.ThrowIfNull(toolDefinition);

            if (_toolDefinitions.TryGetValue(toolDefinition.Id, out var existingToolDefinition))
            {
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

                throw new InvalidOperationException($"A Workbench tool with id '{toolDefinition.Id}' has already been registered.");
            }

            _toolDefinitions.Add(toolDefinition.Id, toolDefinition);
        }

        /// <summary>
        /// Opens or focuses a tool using the singleton activation rules of the Workbench shell.
        /// </summary>
        /// <param name="activationTarget">The shell target that identifies which tool should be opened or focused.</param>
        /// <returns>The active runtime tool instance after the activation request completes.</returns>
        public ToolInstance ActivateTool(ActivationTarget activationTarget)
        {
            // Activation requests must resolve through the registered tool catalog so the shell controls which components may be hosted.
            ArgumentNullException.ThrowIfNull(activationTarget);

            if (!_toolDefinitions.TryGetValue(activationTarget.ToolId, out var toolDefinition))
            {
                throw new InvalidOperationException($"The Workbench tool '{activationTarget.ToolId}' is not registered.");
            }

            // The shell state owns singleton reuse while the activation manager supplies tool contexts for new instances only.
            return State.ActivateTool(
                toolDefinition,
                activationTarget,
                toolInstanceId => new ToolContext(toolInstanceId, _toolContextBridge));
        }

        /// <summary>
        /// Attempts to resolve a tracked tool instance by its runtime instance identifier.
        /// </summary>
        /// <param name="toolInstanceId">The runtime tool instance identifier to resolve.</param>
        /// <param name="toolInstance">The resolved tool instance when the lookup succeeds; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the tool instance exists; otherwise, <see langword="false"/>.</returns>
        public bool TryGetToolInstance(string toolInstanceId, out ToolInstance? toolInstance)
        {
            // Tool-context callbacks route through runtime instance identifiers, so the activation manager forwards the lookup to shell state.
            return State.TryGetToolInstance(toolInstanceId, out toolInstance);
        }
    }
}
