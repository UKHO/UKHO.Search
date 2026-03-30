using System.Diagnostics.CodeAnalysis;

namespace UKHO.Workbench.Modules
{
    /// <summary>
    /// Describes a Workbench module that can contribute services and tools during host startup.
    /// </summary>
    public class ModuleMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleMetadata"/> class.
        /// </summary>
        /// <param name="id">The stable module identifier used by host configuration and diagnostics.</param>
        /// <param name="displayName">The human-readable module name shown in diagnostics and documentation.</param>
        /// <param name="description">The optional explanatory description for the module.</param>
        public ModuleMetadata(string id, string displayName, string? description = null)
        {
            // Module metadata is part of the host-controlled registration contract, so invalid identifiers must fail fast.
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

            Id = id;
            DisplayName = displayName;
            Description = description;
        }

        /// <summary>
        /// Gets the stable module identifier used by host configuration and diagnostics.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the human-readable module name shown in diagnostics and documentation.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the optional explanatory description for the module.
        /// </summary>
        public string? Description { get; }

        /// <summary>
        /// Returns the module display name for logging and debugging output.
        /// </summary>
        /// <returns>The display name when available; otherwise, the stable identifier.</returns>
        public override string ToString()
        {
            // Logging commonly needs a concise module label, so the metadata returns a readable display string.
            return string.IsNullOrWhiteSpace(DisplayName) ? Id : DisplayName;
        }
    }
}
