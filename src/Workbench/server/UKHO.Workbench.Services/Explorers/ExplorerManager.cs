using UKHO.Workbench.Explorers;

namespace UKHO.Workbench.Services.Explorers
{
    /// <summary>
    /// Stores explorer metadata so the shell can render explorers, sections, and items from contributed definitions.
    /// </summary>
    public class ExplorerManager
    {
        private readonly Dictionary<string, ExplorerContribution> _explorers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ExplorerSectionContribution> _sections = new(StringComparer.Ordinal);
        private readonly Dictionary<string, ExplorerItem> _items = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the registered explorers in display order.
        /// </summary>
        public IReadOnlyList<ExplorerContribution> Explorers => _explorers.Values
            .OrderBy(explorerContribution => explorerContribution.Order)
            .ThenBy(explorerContribution => explorerContribution.DisplayName, StringComparer.Ordinal)
            .ToArray();

        /// <summary>
        /// Registers an explorer contribution.
        /// </summary>
        /// <param name="explorerContribution">The explorer contribution that should become available to the shell activity rail.</param>
        public void RegisterExplorer(ExplorerContribution explorerContribution)
        {
            // Explorer identifiers must stay unique so shell state can safely track the active explorer.
            ArgumentNullException.ThrowIfNull(explorerContribution);

            if (_explorers.TryGetValue(explorerContribution.Id, out var existingExplorer))
            {
                if (existingExplorer.DisplayName == explorerContribution.DisplayName
                    && existingExplorer.Icon == explorerContribution.Icon
                    && existingExplorer.Order == explorerContribution.Order)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench explorer with id '{explorerContribution.Id}' has already been registered.");
            }

            _explorers.Add(explorerContribution.Id, explorerContribution);
        }

        /// <summary>
        /// Registers an explorer-section contribution.
        /// </summary>
        /// <param name="explorerSectionContribution">The explorer-section contribution that should become available to the explorer pane.</param>
        public void RegisterExplorerSection(ExplorerSectionContribution explorerSectionContribution)
        {
            // Section identifiers must stay unique so explorer items can bind to sections deterministically.
            ArgumentNullException.ThrowIfNull(explorerSectionContribution);

            if (_sections.TryGetValue(explorerSectionContribution.Id, out var existingSection))
            {
                if (existingSection.ExplorerId == explorerSectionContribution.ExplorerId
                    && existingSection.DisplayName == explorerSectionContribution.DisplayName
                    && existingSection.Order == explorerSectionContribution.Order)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench explorer section with id '{explorerSectionContribution.Id}' has already been registered.");
            }

            _sections.Add(explorerSectionContribution.Id, explorerSectionContribution);
        }

        /// <summary>
        /// Registers an explorer-item contribution.
        /// </summary>
        /// <param name="explorerItem">The explorer item that should become available to the explorer pane.</param>
        public void RegisterExplorerItem(ExplorerItem explorerItem)
        {
            // Explorer items remain declarative routing entries, so their identifiers must also remain unique.
            ArgumentNullException.ThrowIfNull(explorerItem);

            if (_items.TryGetValue(explorerItem.Id, out var existingItem))
            {
                if (existingItem.ExplorerId == explorerItem.ExplorerId
                    && existingItem.SectionId == explorerItem.SectionId
                    && existingItem.DisplayName == explorerItem.DisplayName
                    && existingItem.CommandId == explorerItem.CommandId
                    && existingItem.ActivationTarget.ToolId == explorerItem.ActivationTarget.ToolId
                    && existingItem.ActivationTarget.Region == explorerItem.ActivationTarget.Region
                    && existingItem.Icon == explorerItem.Icon
                    && existingItem.Description == explorerItem.Description
                    && existingItem.Order == explorerItem.Order)
                {
                    return;
                }

                throw new InvalidOperationException($"A Workbench explorer item with id '{explorerItem.Id}' has already been registered.");
            }

            _items.Add(explorerItem.Id, explorerItem);
        }

        /// <summary>
        /// Returns the explorer metadata for the supplied identifier.
        /// </summary>
        /// <param name="explorerId">The explorer identifier that should be resolved.</param>
        /// <returns>The matching explorer contribution when one exists; otherwise, <see langword="null"/>.</returns>
        public ExplorerContribution? GetExplorer(string explorerId)
        {
            // Lookup helpers keep shell UI code concise while preserving the manager as the source of explorer truth.
            ArgumentException.ThrowIfNullOrWhiteSpace(explorerId);
            _explorers.TryGetValue(explorerId, out var explorerContribution);
            return explorerContribution;
        }

        /// <summary>
        /// Returns the explorer sections that belong to the supplied explorer.
        /// </summary>
        /// <param name="explorerId">The explorer identifier whose sections should be returned.</param>
        /// <returns>The explorer sections belonging to the supplied explorer.</returns>
        public IReadOnlyList<ExplorerSectionContribution> GetSections(string explorerId)
        {
            // Section filtering stays centralized so the UI only renders already-composed explorer data.
            ArgumentException.ThrowIfNullOrWhiteSpace(explorerId);

            return _sections.Values
                .Where(section => string.Equals(section.ExplorerId, explorerId, StringComparison.Ordinal))
                .OrderBy(section => section.Order)
                .ThenBy(section => section.DisplayName, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Returns the explorer items that belong to the supplied explorer section.
        /// </summary>
        /// <param name="explorerId">The explorer identifier that owns the section.</param>
        /// <param name="sectionId">The section identifier whose items should be returned.</param>
        /// <returns>The explorer items belonging to the supplied section.</returns>
        public IReadOnlyList<ExplorerItem> GetItems(string explorerId, string sectionId)
        {
            // Item filtering stays centralized so modules can contribute items without host-specific markup rules.
            ArgumentException.ThrowIfNullOrWhiteSpace(explorerId);
            ArgumentException.ThrowIfNullOrWhiteSpace(sectionId);

            return _items.Values
                .Where(item => string.Equals(item.ExplorerId, explorerId, StringComparison.Ordinal)
                    && string.Equals(item.SectionId, sectionId, StringComparison.Ordinal))
                .OrderBy(item => item.Order)
                .ThenBy(item => item.DisplayName, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
