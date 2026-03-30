using UKHO.Workbench.Tools;

namespace UKHO.Workbench.Commands
{
    /// <summary>
    /// Describes a declarative Workbench command that can be surfaced from explorers, menus, toolbars, or tool UIs.
    /// </summary>
    public class CommandContribution
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandContribution"/> class.
        /// </summary>
        /// <param name="id">The stable command identifier used for routing and diagnostics.</param>
        /// <param name="displayName">The label shown by shell surfaces that render the command.</param>
        /// <param name="scope">The ownership boundary that classifies the command as host-owned or tool-owned.</param>
        /// <param name="icon">The optional icon key shown when the command is rendered by an icon-capable surface.</param>
        /// <param name="description">The optional explanatory text used by tooltips or diagnostics.</param>
        /// <param name="ownerToolId">The optional tool identifier that owns the command when the command scope is <see cref="CommandScope.Tool"/>.</param>
        /// <param name="activationTarget">The optional declarative tool activation target executed when the command is invoked.</param>
        /// <param name="executionHandler">The optional imperative handler used when the command needs custom runtime behavior.</param>
        public CommandContribution(
            string id,
            string displayName,
            CommandScope scope,
            string? icon = null,
            string? description = null,
            string? ownerToolId = null,
            ActivationTarget? activationTarget = null,
            Func<ToolContext?, CancellationToken, Task>? executionHandler = null)
        {
            // Command identifiers and labels must stay stable because every shell surface routes through them.
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            // Tool-scoped commands remain attributable to a specific tool definition for diagnostics and future capability checks.
            if (scope == CommandScope.Tool)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(ownerToolId);
            }

            // Commands must resolve to either a declarative activation target or executable logic so invocation never becomes a silent no-op.
            if (activationTarget is null && executionHandler is null)
            {
                throw new ArgumentException("A command contribution must define either an activation target or an execution handler.", nameof(activationTarget));
            }

            Id = id;
            DisplayName = displayName;
            Scope = scope;
            Icon = icon;
            Description = description;
            OwnerToolId = ownerToolId;
            ActivationTarget = activationTarget;
            ExecutionHandler = executionHandler;
        }

        /// <summary>
        /// Gets the stable command identifier used for routing and diagnostics.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the label shown by shell surfaces that render the command.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the ownership boundary that classifies the command as host-owned or tool-owned.
        /// </summary>
        public CommandScope Scope { get; }

        /// <summary>
        /// Gets the optional icon key shown when the command is rendered by an icon-capable surface.
        /// </summary>
        public string? Icon { get; }

        /// <summary>
        /// Gets the optional explanatory text used by tooltips or diagnostics.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets the optional tool identifier that owns the command when the command is tool-scoped.
        /// </summary>
        public string? OwnerToolId { get; }

        /// <summary>
        /// Gets the optional declarative tool activation target executed when the command is invoked.
        /// </summary>
        public ActivationTarget? ActivationTarget { get; }

        /// <summary>
        /// Gets the optional imperative handler used when the command needs custom runtime behavior.
        /// </summary>
        public Func<ToolContext?, CancellationToken, Task>? ExecutionHandler { get; }
    }
}
