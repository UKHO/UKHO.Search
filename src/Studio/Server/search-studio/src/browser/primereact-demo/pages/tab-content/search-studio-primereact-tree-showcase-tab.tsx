import * as React from '@theia/core/shared/react';
import { Button } from 'primereact/button';
import { SelectButton, SelectButtonChangeEvent } from 'primereact/selectbutton';
import { Tag } from 'primereact/tag';
import { Tree } from 'primereact/tree';
import {
    createTreeDemoNodes,
    SearchStudioPrimeReactDemoStatus,
    SearchStudioPrimeReactDemoTreeNode
} from '../../data/search-studio-primereact-demo-data';
import {
    countSelectedTreeKeys,
    createExpandedTreeKeys,
    createScenarioSnapshot,
    SearchStudioPrimeReactDemoExpandedKeys,
    SearchStudioPrimeReactDemoScenario,
    SearchStudioPrimeReactDemoTreeSelectionKeys
} from '../../data/search-studio-primereact-demo-state';
import { getSearchStudioPrimeReactDemoPageClassName } from '../../search-studio-primereact-demo-page-layout';
import { SearchStudioPrimeReactDemoPageProps } from '../../search-studio-primereact-demo-page-props';

const scenarioOptions = [
    { label: 'Ready', value: 'ready' },
    { label: 'Loading', value: 'loading' },
    { label: 'Empty', value: 'empty' }
];

/**
 * Describes the minimal event payload needed from PrimeReact tree selection callbacks.
 */
interface SearchStudioPrimeReactDemoTreeSelectionChangedEvent {
    /**
     * Stores the current checkbox-tree selection dictionary.
     */
    readonly value: SearchStudioPrimeReactDemoTreeSelectionKeys | undefined;
}

/**
 * Describes the minimal event payload needed from PrimeReact tree toggle callbacks.
 */
interface SearchStudioPrimeReactDemoTreeToggleEvent {
    /**
     * Stores the current expansion-key dictionary.
     */
    readonly value: SearchStudioPrimeReactDemoExpandedKeys;
}

/**
 * Returns the short theme label shown in the temporary `Tree` demo header.
 *
 * @param activeThemeVariant Identifies the current Theia-aligned PrimeReact theme variant.
 * @returns The short theme label displayed to reviewers.
 */
function getThemeLabel(activeThemeVariant: SearchStudioPrimeReactDemoPageProps['activeThemeVariant']): string {
    // Surface the current light or dark mapping explicitly so reviewers can confirm Theia theme following while the hierarchy page is open.
    return activeThemeVariant === 'light' ? 'Theia light -> UKHO/Theia light' : 'Theia dark -> UKHO/Theia dark';
}

/**
 * Maps a mock lifecycle state to the PrimeReact tag severity used across the temporary `Tree` page.
 *
 * @param status Identifies the mock lifecycle state that should be rendered.
 * @returns The PrimeReact severity token that best matches the supplied state.
 */
function getStatusSeverity(status: SearchStudioPrimeReactDemoStatus): 'success' | 'info' | 'warning' | 'danger' {
    // Keep the status-to-severity mapping aligned with the other data-heavy pages so cross-page comparison stays straightforward.
    switch (status) {
        case 'Ready':
            return 'success';
        case 'Review':
            return 'info';
        case 'Attention':
            return 'warning';
        default:
            return 'danger';
    }
}

/**
 * Renders the temporary PrimeReact `Tree` evaluation page.
 *
 * @param props Supplies the current Theia-aligned theme mapping for the styled PrimeReact demo page.
 * @returns The React node tree for the temporary `Tree` evaluation surface.
 */
export function SearchStudioPrimeReactTreeDemoPage(props: SearchStudioPrimeReactDemoPageProps): React.ReactNode {
    const nodes = React.useMemo(() => createTreeDemoNodes(), []);
    const [scenario, setScenario] = React.useState<SearchStudioPrimeReactDemoScenario>('ready');
    const [selectionKeys, setSelectionKeys] = React.useState<SearchStudioPrimeReactDemoTreeSelectionKeys | undefined>();
    const [expandedKeys, setExpandedKeys] = React.useState<SearchStudioPrimeReactDemoExpandedKeys>(() => createExpandedTreeKeys(nodes));
    const [lastActionSummary, setLastActionSummary] = React.useState('Toolbar actions are mock only and update this banner for review.');

    const scenarioSnapshot = React.useMemo(
        () => createScenarioSnapshot(nodes, scenario),
        [nodes, scenario]
    );
    const selectedCount = React.useMemo(
        () => countSelectedTreeKeys(selectionKeys),
        [selectionKeys]
    );
    const pageClassName = getSearchStudioPrimeReactDemoPageClassName({
        hostDisplayMode: props.hostDisplayMode,
        usesDataHeavyLayout: true
    });

    /**
     * Updates the current high-level page scenario shown by the hierarchy surface.
     *
     * @param event Supplies the currently selected scenario value emitted by PrimeReact.
     */
    function handleScenarioChanged(event: SelectButtonChangeEvent): void {
        if (!event.value) {
            // Ignore empty events so the page always stays in one explicit visual state.
            return;
        }

        // Reset selection when the page switches scenarios so loading and empty-state review stays uncluttered.
        setScenario(event.value as SearchStudioPrimeReactDemoScenario);
        setSelectionKeys(undefined);
        setLastActionSummary(`Scenario switched to ${String(event.value)}.`);
    }

    /**
     * Updates the current checkbox-tree selection dictionary.
     *
     * @param event Supplies the latest selection dictionary emitted by PrimeReact.
     */
    function handleSelectionChanged(event: SearchStudioPrimeReactDemoTreeSelectionChangedEvent): void {
        // Store the current selection so toolbar actions can enable, disable, and summarise node counts immediately.
        setSelectionKeys(event.value);
    }

    /**
     * Updates the current expansion-key dictionary.
     *
     * @param event Supplies the latest expansion dictionary emitted by PrimeReact.
     */
    function handleExpansionChanged(event: SearchStudioPrimeReactDemoTreeToggleEvent): void {
        // Store the current expansion state so reviewers can mix manual toggling with the toolbar shortcuts.
        setExpandedKeys(event.value);
    }

    /**
     * Expands all parent nodes in the generated hierarchy.
     */
    function handleExpandAll(): void {
        // Recompute the full expansion dictionary so the action keeps working even after future hierarchy changes.
        setExpandedKeys(createExpandedTreeKeys(nodes));
        setLastActionSummary('Expanded all generated hierarchy nodes.');
    }

    /**
     * Collapses the generated hierarchy back to its root level.
     */
    function handleCollapseAll(): void {
        // Clear the expansion dictionary so PrimeReact collapses every branch in one consistent update.
        setExpandedKeys({});
        setLastActionSummary('Collapsed the generated hierarchy to its top level.');
    }

    /**
     * Emits a mock summary for the currently selected nodes.
     */
    function handleInspectSelection(): void {
        // Keep the action mock-only by updating the summary banner instead of triggering any navigation or backend behavior.
        setLastActionSummary(`Marked ${selectedCount} selected nodes for mock follow-up.`);
    }

    /**
     * Clears the current node selection.
     */
    function handleClearSelection(): void {
        // Reset the selection dictionary explicitly so disabled toolbar actions and summary tags update together.
        setSelectionKeys(undefined);
        setLastActionSummary('Cleared the current hierarchy selection.');
    }

    /**
     * Renders a custom node template that surfaces useful supporting metadata beside the label.
     *
     * @param node Supplies the current generated hierarchy node.
     * @returns The React node shown for the current tree row.
     */
    function renderNodeTemplate(node: SearchStudioPrimeReactDemoTreeNode): React.ReactNode {
        // Combine the label, metadata, and status tag so the page reads like a realistic review tree rather than a skeletal example.
        return (
            <div className="search-studio-primereact-demo-page__tree-node">
                <div className="search-studio-primereact-demo-page__tree-node-copy">
                    <strong>{node.label}</strong>
                    <span className="search-studio-primereact-demo-page__tree-node-summary">
                        {node.data.type} · {node.data.owner} · {node.data.itemCount} items · {node.data.lastUpdatedOn}
                    </span>
                </div>
                <Tag value={node.data.status} severity={getStatusSeverity(node.data.status)} rounded />
            </div>
        );
    }

    // Render the hierarchy evaluation surface using full styled PrimeReact so expand/collapse and selection can be reviewed in-context.
    return (
        <div className={pageClassName}>
            <header className="search-studio-primereact-demo-page__hero">
                <div className="search-studio-primereact-demo-page__hero-copy">
                    <div className="search-studio-primereact-demo-page__hero-heading-row">
                        <h1 className="search-studio-primereact-demo-page__title">PrimeReact `Tree` demo</h1>
                        <Tag value="Hierarchy + toolbar" severity="success" rounded className="search-studio-primereact-demo-page__mode-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__summary">
                        This temporary page evaluates PrimeReact&apos;s hierarchical navigation surface inside the Studio Theia shell, including
                        expand/collapse, checkbox selection, loading and empty states, and simple toolbar-style actions.
                    </p>
                    <div className="search-studio-primereact-demo-page__theme-row">
                        <span className="search-studio-primereact-demo-page__theme-label">Styled theme sync</span>
                        <strong className="search-studio-primereact-demo-page__theme-value">{getThemeLabel(props.activeThemeVariant)}</strong>
                    </div>
                </div>
                <div className="search-studio-primereact-demo-page__mode-card">
                    <p className="search-studio-primereact-demo-page__toggle-help">
                        The hierarchy is generated fully in-memory. Toolbar actions update only local page state so reviewers can focus on how the
                        component behaves inside the shell.
                    </p>
                    <Tag value={`${nodes.length} root nodes`} severity="info" rounded />
                </div>
            </header>

            <section className="search-studio-primereact-demo-page__metrics-grid" aria-label="PrimeReact Tree metrics">
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Selected nodes</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{selectedCount}</strong>
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Expanded groups</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{Object.keys(expandedKeys).length}</strong>
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Scenario</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{scenario}</strong>
                </article>
            </section>

            <section className="search-studio-primereact-demo-page__content-grid search-studio-primereact-demo-page__content-grid--stacked">
                <article className="search-studio-primereact-demo-page__surface">
                    <div className="search-studio-primereact-demo-page__section-heading-row">
                        <h2 className="search-studio-primereact-demo-page__section-title">Hierarchy controls and actions</h2>
                        <Tag value="Mock only" severity="warning" rounded className="search-studio-primereact-demo-page__section-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        Switch between ready, loading, and empty states, then expand, collapse, and act on the selected hierarchy nodes without any
                        backend dependency.
                    </p>
                    <div className="search-studio-primereact-demo-page__toolbar">
                        <div className="search-studio-primereact-demo-page__toolbar-group">
                            <span className="search-studio-primereact-demo-page__field-label">Scenario</span>
                            <SelectButton value={scenario} options={scenarioOptions} optionLabel="label" optionValue="value" onChange={handleScenarioChanged} />
                        </div>
                        <div className="search-studio-primereact-demo-page__toolbar-group search-studio-primereact-demo-page__toolbar-group--actions">
                            <Button label="Expand all" icon="pi pi-plus" onClick={handleExpandAll} disabled={scenario !== 'ready'} />
                            <Button label="Collapse all" icon="pi pi-minus" severity="secondary" onClick={handleCollapseAll} disabled={scenario !== 'ready'} />
                            <Button label="Inspect selection" icon="pi pi-search" onClick={handleInspectSelection} disabled={selectedCount === 0 || scenario !== 'ready'} />
                            <Button label="Clear selection" icon="pi pi-times" text severity="secondary" onClick={handleClearSelection} disabled={selectedCount === 0} />
                        </div>
                    </div>
                    <div className="search-studio-primereact-demo-page__callout-row">
                        <Tag value={`${selectedCount} selected`} severity="info" rounded />
                        <Tag value={`${Object.keys(expandedKeys).length} expanded`} severity="success" rounded />
                        <span className="search-studio-primereact-demo-page__section-summary">{lastActionSummary}</span>
                    </div>
                </article>

                <article className="search-studio-primereact-demo-page__surface search-studio-primereact-demo-page__surface--flush search-studio-primereact-demo-page__surface--contained">
                    <div className="search-studio-primereact-demo-page__section-heading-row">
                        <h2 className="search-studio-primereact-demo-page__section-title">Dense review hierarchy</h2>
                        <Tag value={scenarioSnapshot.isLoading ? 'Loading overlay' : scenarioSnapshot.isEmpty ? 'Empty state' : 'Ready'} severity="info" rounded className="search-studio-primereact-demo-page__section-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        The tree starts fully expanded to show density immediately, while checkbox selection and toolbar shortcuts make it easy to
                        inspect deeper interaction states.
                    </p>
                    <div className="search-studio-primereact-demo-page__pane-scroll-host search-studio-primereact-demo-page__pane-scroll-host--tree">
                        <Tree
                            value={Array.from(scenarioSnapshot.items) as never[]}
                            selectionMode="checkbox"
                            selectionKeys={selectionKeys as never}
                            onSelectionChange={event => handleSelectionChanged(event as SearchStudioPrimeReactDemoTreeSelectionChangedEvent)}
                            expandedKeys={expandedKeys as never}
                            onToggle={event => handleExpansionChanged(event as SearchStudioPrimeReactDemoTreeToggleEvent)}
                            loading={scenarioSnapshot.isLoading}
                            filter
                            filterPlaceholder="Filter generated nodes"
                            nodeTemplate={renderNodeTemplate as never}
                            className="search-studio-primereact-demo-page__tree"
                        />
                    </div>
                </article>
            </section>
        </div>
    );
}
