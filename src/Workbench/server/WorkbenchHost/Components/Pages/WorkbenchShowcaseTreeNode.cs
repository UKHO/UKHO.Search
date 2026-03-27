using System;
using System.Collections.Generic;

namespace WorkbenchHost.Components.Pages
{
    /// <summary>
    /// Represents a single static node in the temporary Workbench Radzen tree showcase.
    /// </summary>
    internal sealed class WorkbenchShowcaseTreeNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkbenchShowcaseTreeNode"/> class.
        /// </summary>
        /// <param name="text">The visible label rendered by the tree item.</param>
        /// <param name="icon">The Material icon name used to visually distinguish the node.</param>
        /// <param name="isExpanded">A value indicating whether the node should appear expanded on first render.</param>
        /// <param name="children">The child nodes rendered beneath this node in the static hierarchy.</param>
        public WorkbenchShowcaseTreeNode(string text, string icon, bool isExpanded, IReadOnlyList<WorkbenchShowcaseTreeNode>? children = null)
        {
            // Store the fixed tree presentation details so the page can render a deterministic hierarchy for theme review.
            Text = text;
            Icon = icon;
            IsExpanded = isExpanded;
            Children = children ?? Array.Empty<WorkbenchShowcaseTreeNode>();
        }

        /// <summary>
        /// Gets the visible label rendered for the node.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the Material icon name rendered beside the node label.
        /// </summary>
        public string Icon { get; }

        /// <summary>
        /// Gets a value indicating whether the node should appear expanded when the showcase first loads.
        /// </summary>
        public bool IsExpanded { get; }

        /// <summary>
        /// Gets the child nodes rendered beneath this node.
        /// </summary>
        public IReadOnlyList<WorkbenchShowcaseTreeNode> Children { get; }
    }
}
