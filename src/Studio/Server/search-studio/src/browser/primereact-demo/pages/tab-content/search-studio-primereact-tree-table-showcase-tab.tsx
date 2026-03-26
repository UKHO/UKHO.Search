import * as React from '@theia/core/shared/react';
import { Button } from 'primereact/button';
import { Column } from 'primereact/column';
import { SelectButton, SelectButtonChangeEvent } from 'primereact/selectbutton';
import { Tag } from 'primereact/tag';
import { TreeTable } from 'primereact/treetable';
import {
    createTreeTableDemoNodes,
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
import {
    getSearchStudioPrimeReactDemoDataScrollHeight,
    getSearchStudioPrimeReactDemoPageClassName
} from '../../search-studio-primereact-demo-page-layout';
import { SearchStudioPrimeReactDemoPageProps } from '../../search-studio-primereact-demo-page-props';

const scenarioOptions = [
    { label: 'Ready', value: 'ready' },
    { label: 'Loading', value: 'loading' },
    { label: 'Empty', value: 'empty' }
];

/**
 * Describes the minimal event payload needed from PrimeReact tree-table selection callbacks.
 */
interface SearchStudioPrimeReactDemoTreeTableSelectionChangedEvent {
    /**
     * Stores the current checkbox tree-table selection dictionary.
     */
    readonly value: SearchStudioPrimeReactDemoTreeSelectionKeys | undefined;
}

/**
 * Describes the minimal event payload needed from PrimeReact tree-table toggle callbacks.
 */
interface SearchStudioPrimeReactDemoTreeTableToggleEvent {
    /**
     * Stores the current expansion-key dictionary.
     */
    readonly value: SearchStudioPrimeReactDemoExpandedKeys;
}

/**
 * Returns the short theme label shown in the temporary `TreeTable` demo header.
 *
 * @param activeThemeVariant Identifies the current Theia-aligned PrimeReact theme variant.
 * @returns The short theme label displayed to reviewers.
 */
function getThemeLabel(activeThemeVariant: SearchStudioPrimeReactDemoPageProps['activeThemeVariant']): string {
    // Surface the current light or dark mapping explicitly so reviewers can confirm Theia theme following while the hierarchical grid is open.
    return activeThemeVariant === 'light' ? 'Theia light -> UKHO/Theia light' : 'Theia dark -> UKHO/Theia dark';
}

/**
 * Maps a mock lifecycle state to the PrimeReact tag severity used across the temporary `TreeTable` page.
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
 * Renders the temporary PrimeReact `TreeTable` evaluation page.
 *
 * @param props Supplies the current Theia-aligned theme mapping for the styled PrimeReact demo page.
 * @returns The React node tree for the temporary `TreeTable` evaluation surface.
 */
export function SearchStudioPrimeReactTreeTableDemoPage(props: SearchStudioPrimeReactDemoPageProps): React.ReactNode {
    const nodes = React.useMemo(() => createTreeTableDemoNodes(), []);
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
    const treeTableScrollHeight = getSearchStudioPrimeReactDemoDataScrollHeight(props.hostDisplayMode);

    /**
     * Updates the current high-level page scenario shown by the tree-table surface.
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
     * Updates the current checkbox tree-table selection dictionary.
     *
     * @param event Supplies the latest selection dictionary emitted by PrimeReact.
     */
    function handleSelectionChanged(event: SearchStudioPrimeReactDemoTreeTableSelectionChangedEvent): void {
        // Store the current selection so toolbar actions can enable, disable, and summarise node counts immediately.
        setSelectionKeys(event.value);
    }

    /**
     * Updates the current expansion-key dictionary.
     *
     * @param event Supplies the latest expansion dictionary emitted by PrimeReact.
     */
    function handleExpansionChanged(event: SearchStudioPrimeReactDemoTreeTableToggleEvent): void {
        // Store the current expansion state so reviewers can mix manual toggling with the toolbar shortcuts.
        setExpandedKeys(event.value);
    }

    /**
     * Expands all parent rows in the generated hierarchy.
     */
    function handleExpandAll(): void {
        // Recompute the full expansion dictionary so the action keeps working even after future hierarchy changes.
        setExpandedKeys(createExpandedTreeKeys(nodes));
        setLastActionSummary('Expanded all generated tree-table groups.');
    }

    /**
     * Collapses the generated hierarchy back to its root level.
     */
    function handleCollapseAll(): void {
        // Clear the expansion dictionary so PrimeReact collapses every branch in one consistent update.
        setExpandedKeys({});
        setLastActionSummary('Collapsed the generated tree-table groups to their top level.');
    }

    /**
     * Emits a mock summary for the currently selected rows.
     */
    function handleInspectSelection(): void {
        // Keep the action mock-only by updating the summary banner instead of triggering any navigation or backend behavior.
        setLastActionSummary(`Inspected ${selectedCount} selected hierarchical rows.`);
    }

    /**
     * Clears the current hierarchical row selection.
     */
    function handleClearSelection(): void {
        // Reset the selection dictionary explicitly so disabled toolbar actions and summary tags update together.
        setSelectionKeys(undefined);
        setLastActionSummary('Cleared the current tree-table selection.');
    }

    /**
     * Renders the status tag shown in the status column.
     *
     * @param node Supplies the current generated hierarchy node.
     * @returns The React node shown in the status column.
     */
    function renderStatusBody(node: SearchStudioPrimeReactDemoTreeNode): React.ReactNode {
        // Render a PrimeReact tag so the hierarchical grid still communicates state compactly in a dense layout.
        return <Tag value={node.data.status} severity={getStatusSeverity(node.data.status)} rounded />;
    }

    // Render the hierarchical grid evaluation surface using full styled PrimeReact so nested rows and columns can be reviewed in-context.
    return (
        <div className={pageClassName}>
            <header className="search-studio-primereact-demo-page__hero">
                <div className="search-studio-primereact-demo-page__hero-copy">
                    <div className="search-studio-primereact-demo-page__hero-heading-row">
                        <h1 className="search-studio-primereact-demo-page__title">PrimeReact `TreeTable` demo</h1>
                        <Tag value="Hierarchy + columns" severity="success" rounded className="search-studio-primereact-demo-page__mode-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__summary">
                        This temporary page evaluates PrimeReact&apos;s hierarchical grid surface inside the Studio Theia shell, including nested rows,
                        business-style columns, checkbox selection, loading and empty states, and toolbar-driven expansion.
                    </p>
                    <div className="search-studio-primereact-demo-page__theme-row">
                        <span className="search-studio-primereact-demo-page__theme-label">Styled theme sync</span>
                        <strong className="search-studio-primereact-demo-page__theme-value">{getThemeLabel(props.activeThemeVariant)}</strong>
                    </div>
                </div>
                <div className="search-studio-primereact-demo-page__mode-card">
                    <p className="search-studio-primereact-demo-page__toggle-help">
                        The hierarchy is generated fully in-memory. Toolbar actions update only local page state so reviewers can focus on column
                        density, nested row behavior, and selection affordances.
                    </p>
                    <Tag value={`${nodes.length} portfolios`} severity="info" rounded />
                </div>
            </header>

            <section className="search-studio-primereact-demo-page__metrics-grid" aria-label="PrimeReact TreeTable metrics">
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Selected rows</span>
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
                        <h2 className="search-studio-primereact-demo-page__section-title">Hierarchical grid controls and actions</h2>
                        <Tag value="Mock only" severity="warning" rounded className="search-studio-primereact-demo-page__section-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        Switch between ready, loading, and empty states, then expand, collapse, and act on selected hierarchical rows without any
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
                        <h2 className="search-studio-primereact-demo-page__section-title">Dense hierarchical grid</h2>
                        <Tag value={scenarioSnapshot.isLoading ? 'Loading overlay' : scenarioSnapshot.isEmpty ? 'Empty state' : 'Ready'} severity="info" rounded className="search-studio-primereact-demo-page__section-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        The tree-table starts fully expanded to show column density immediately, while checkbox selection and toolbar shortcuts make
                        it easy to inspect nested row behavior.
                    </p>
                    <div className="search-studio-primereact-demo-page__pane-scroll-host search-studio-primereact-demo-page__pane-scroll-host--grid">
                        <TreeTable
                            value={Array.from(scenarioSnapshot.items) as never[]}
                            selectionMode="checkbox"
                            selectionKeys={selectionKeys as never}
                            onSelectionChange={event => handleSelectionChanged(event as SearchStudioPrimeReactDemoTreeTableSelectionChangedEvent)}
                            expandedKeys={expandedKeys as never}
                            onToggle={event => handleExpansionChanged(event as SearchStudioPrimeReactDemoTreeTableToggleEvent)}
                            loading={scenarioSnapshot.isLoading}
                            scrollable
                            scrollHeight={treeTableScrollHeight}
                            tableStyle={{ minWidth: '76rem' }}
                            className="search-studio-primereact-demo-page__treetable">
                            <Column field="name" header="Name" expander style={{ minWidth: '20rem' }} sortable />
                            <Column field="type" header="Type" style={{ minWidth: '10rem' }} sortable />
                            <Column field="owner" header="Owner" style={{ minWidth: '11rem' }} sortable />
                            <Column field="status" header="Status" body={renderStatusBody} style={{ minWidth: '10rem' }} sortable />
                            <Column field="itemCount" header="Items" style={{ minWidth: '8rem' }} sortable />
                            <Column field="lastUpdatedOn" header="Last updated" style={{ minWidth: '11rem' }} sortable />
                        </TreeTable>
                    </div>
                </article>
            </section>
        </div>
    );
}
