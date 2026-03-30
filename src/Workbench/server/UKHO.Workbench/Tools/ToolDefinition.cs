using Microsoft.AspNetCore.Components;
using UKHO.Workbench.WorkbenchShell;

namespace UKHO.Workbench.Tools
{
    /// <summary>
    /// Describes a statically registered Workbench tool that can be opened by the shell.
    /// </summary>
    public class ToolDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToolDefinition"/> class.
        /// </summary>
        /// <param name="id">The unique identifier used to register and activate the tool.</param>
        /// <param name="displayName">The human-readable label shown in shell surfaces such as the explorer and toolbar.</param>
        /// <param name="componentType">The Razor component type rendered when the tool is active.</param>
        /// <param name="explorerId">The explorer that should list the tool in the bootstrap shell.</param>
        /// <param name="icon">The icon key used by the shell when representing the tool.</param>
        /// <param name="description">The optional explanatory text shown in shell surfaces.</param>
        /// <param name="isSingleton">Indicates whether reopening the tool should focus the existing instance rather than creating another one.</param>
        /// <param name="defaultRegion">The default shell region that hosts the tool.</param>
        public ToolDefinition(
            string id,
            string displayName,
            Type componentType,
            string explorerId,
            string icon,
            string? description = null,
            bool isSingleton = true,
            WorkbenchShellRegion defaultRegion = WorkbenchShellRegion.ToolSurface)
        {
            // The bootstrap slice uses explicit identifiers so later command and explorer work can activate tools without direct component coupling.
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
            ArgumentNullException.ThrowIfNull(componentType);
            ArgumentException.ThrowIfNullOrWhiteSpace(explorerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(icon);

            // Tools must point at a Razor component so the shell can host them through DynamicComponent safely.
            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"The component type '{componentType.FullName}' must implement {nameof(IComponent)}.", nameof(componentType));
            }

            Id = id;
            DisplayName = displayName;
            ComponentType = componentType;
            ExplorerId = explorerId;
            Icon = icon;
            Description = description;
            IsSingleton = isSingleton;
            DefaultRegion = defaultRegion;
        }

        /// <summary>
        /// Gets the unique identifier used to register and activate the tool.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the human-readable label shown in shell surfaces such as the explorer and toolbar.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the Razor component type rendered when the tool is active.
        /// </summary>
        public Type ComponentType { get; }

        /// <summary>
        /// Gets the explorer that should list the tool in the bootstrap shell.
        /// </summary>
        public string ExplorerId { get; }

        /// <summary>
        /// Gets the icon key used by the shell when representing the tool.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// Gets the optional explanatory text shown in shell surfaces.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Gets a value indicating whether reopening the tool should focus the existing instance rather than creating another one.
        /// </summary>
        public bool IsSingleton { get; }

        /// <summary>
        /// Gets the default shell region that hosts the tool.
        /// </summary>
        public WorkbenchShellRegion DefaultRegion { get; }
    }
}
