using UKHO.Workbench.Commands;
using UKHO.Workbench.Tools;

namespace UKHO.Workbench.Services.Commands
{
    /// <summary>
    /// Stores declarative Workbench commands and executes them through a single routing entry point.
    /// </summary>
    public class CommandManager
    {
        private readonly Dictionary<string, CommandContribution> _commands = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the registered Workbench commands in display order.
        /// </summary>
        public IReadOnlyList<CommandContribution> Commands => _commands.Values
            .OrderBy(commandContribution => commandContribution.DisplayName, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Registers a declarative Workbench command.
        /// </summary>
        /// <param name="commandContribution">The command contribution that should be available for routing.</param>
        public void RegisterCommand(CommandContribution commandContribution)
        {
            // Command identifiers must stay unique so shell routing always resolves to one deterministic action.
            ArgumentNullException.ThrowIfNull(commandContribution);

            if (_commands.TryGetValue(commandContribution.Id, out var existingCommand))
            {
                // Idempotent re-registration is allowed when the command metadata is unchanged so repeated startup paths remain stable in tests.
                if (existingCommand.DisplayName == commandContribution.DisplayName
                    && existingCommand.Scope == commandContribution.Scope
                    && existingCommand.Icon == commandContribution.Icon
                    && existingCommand.Description == commandContribution.Description
                    && existingCommand.OwnerToolId == commandContribution.OwnerToolId
                    && existingCommand.ActivationTarget?.ToolId == commandContribution.ActivationTarget?.ToolId
                    && existingCommand.ActivationTarget?.Region == commandContribution.ActivationTarget?.Region
                    && existingCommand.ExecutionHandler == commandContribution.ExecutionHandler)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench command with id '{commandContribution.Id}' has already been registered.");
            }

            _commands.Add(commandContribution.Id, commandContribution);
        }

        /// <summary>
        /// Executes a registered Workbench command.
        /// </summary>
        /// <param name="commandId">The command identifier that should be executed.</param>
        /// <param name="activeToolContext">The currently active tool context, or <see langword="null"/> when no tool is active.</param>
        /// <param name="activateToolAsync">The bounded activation callback used when the command declares an activation target.</param>
        /// <param name="cancellationToken">The cancellation token that can stop command execution before it completes.</param>
        /// <returns>A task that completes when the command has finished executing.</returns>
        public async Task ExecuteAsync(
            string commandId,
            ToolContext? activeToolContext,
            Func<ActivationTarget, Task> activateToolAsync,
            CancellationToken cancellationToken = default)
        {
            // Command execution always starts with an identifier lookup so every surface routes consistently.
            ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
            ArgumentNullException.ThrowIfNull(activateToolAsync);

            if (!_commands.TryGetValue(commandId, out var commandContribution))
            {
                throw new InvalidOperationException($"The Workbench command '{commandId}' is not registered.");
            }

            // Declarative activation targets execute first so host-navigation commands remain simple to register.
            if (commandContribution.ActivationTarget is not null)
            {
                await activateToolAsync(commandContribution.ActivationTarget).ConfigureAwait(false);
            }

            // Imperative handlers then run when the command needs custom runtime behavior beyond pure activation.
            if (commandContribution.ExecutionHandler is not null)
            {
                await commandContribution.ExecutionHandler(activeToolContext, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
