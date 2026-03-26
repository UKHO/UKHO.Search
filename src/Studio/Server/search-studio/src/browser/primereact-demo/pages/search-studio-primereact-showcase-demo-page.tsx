import * as React from '@theia/core/shared/react';
import { Button } from 'primereact/button';
import { Checkbox, CheckboxChangeEvent } from 'primereact/checkbox';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { InputText } from 'primereact/inputtext';
import { InputTextarea } from 'primereact/inputtextarea';
import { Panel } from 'primereact/panel';
import { SelectButton, SelectButtonChangeEvent } from 'primereact/selectbutton';
import { Splitter, SplitterPanel } from 'primereact/splitter';
import { TabPanel, TabView } from 'primereact/tabview';
import { Tag } from 'primereact/tag';
import { Tree } from 'primereact/tree';
import {
    createDataTableDemoRecords,
    createTreeDemoNodes,
    SearchStudioPrimeReactDemoStatus,
    SearchStudioPrimeReactDemoTableRecord,
    SearchStudioPrimeReactDemoTreeNode
} from '../data/search-studio-primereact-demo-data';
import {
    countSelectedTreeKeys,
    createExpandedTreeKeys,
    createScenarioSnapshot,
    SearchStudioPrimeReactDemoExpandedKeys,
    SearchStudioPrimeReactDemoScenario,
    SearchStudioPrimeReactDemoScenarioSnapshot,
    SearchStudioPrimeReactDemoTreeSelectionKeys
} from '../data/search-studio-primereact-demo-state';
import { SearchStudioPrimeReactDemoTabContent } from '../search-studio-primereact-demo-page-layout';
import { SearchStudioPrimeReactDemoPageProps } from '../search-studio-primereact-demo-page-props';
import { SearchStudioPrimeReactDataTableDemoPage } from './tab-content/search-studio-primereact-data-table-showcase-tab';
import { SearchStudioPrimeReactDataViewDemoPage } from './tab-content/search-studio-primereact-data-view-showcase-tab';
import { SearchStudioPrimeReactFormsDemoPage } from './tab-content/search-studio-primereact-forms-showcase-tab';
import { SearchStudioPrimeReactTreeDemoPage } from './tab-content/search-studio-primereact-tree-showcase-tab';
import { SearchStudioPrimeReactTreeTableDemoPage } from './tab-content/search-studio-primereact-tree-table-showcase-tab';

/**
 * Describes the minimal event payload needed from PrimeReact row-selection callbacks.
 */
interface SearchStudioPrimeReactShowcaseSelectionChangedEvent {
    /**
     * Stores the rows currently selected by the reviewer.
     */
    readonly value: SearchStudioPrimeReactDemoTableRecord[];
}

/**
 * Describes the minimal event payload needed from PrimeReact tree selection callbacks.
 */
interface SearchStudioPrimeReactShowcaseTreeSelectionChangedEvent {
    /**
     * Stores the current checkbox-tree selection dictionary.
     */
    readonly value: SearchStudioPrimeReactDemoTreeSelectionKeys | undefined;
}

/**
 * Describes the minimal event payload needed from PrimeReact tree toggle callbacks.
 */
interface SearchStudioPrimeReactShowcaseTreeToggleEvent {
    /**
     * Stores the current expansion-key dictionary.
     */
    readonly value: SearchStudioPrimeReactDemoExpandedKeys;
}

/**
 * Describes the minimal event payload needed from the PrimeReact root tab view.
 */
interface SearchStudioPrimeReactShowcaseTabChangedEvent {
    /**
     * Stores the zero-based tab index selected by the reviewer.
     */
    readonly index: number;
}

/**
 * Describes the controlled edit/detail form state rendered in the combined showcase page.
 */
interface SearchStudioPrimeReactShowcaseDetailFormState {
    /**
     * Stores the editable mock title shown in the detail panel.
     */
    readonly draftTitle: string;

    /**
     * Stores the editable mock note shown in the multiline field.
     */
    readonly draftReviewNotes: string;

    /**
     * Stores the selected review lane shown by the segmented button group.
     */
    readonly reviewLane: 'review' | 'compare' | 'publish';

    /**
     * Stores whether the mock detail panel should highlight the publish route.
     */
    readonly requestPublish: boolean;
}

/**
 * Identifies the supported root tabs shown by the consolidated PrimeReact showcase page.
 */
export type SearchStudioPrimeReactShowcaseTabId = 'showcase' | 'forms' | 'dataview' | 'datatable' | 'tree' | 'treetable';

/**
 * Describes the metadata required to render one root tab inside the consolidated showcase shell.
 */
export interface SearchStudioPrimeReactShowcaseTabDefinition {
    /**
     * Stores the stable tab identifier used by the root tab shell.
     */
    readonly id: SearchStudioPrimeReactShowcaseTabId;

    /**
     * Stores the visible tab label shown to reviewers.
     */
    readonly label: string;

    /**
     * Stores the decorative PrimeIcons class shown beside the tab label.
     */
    readonly iconClassName: string;
}

/**
 * Describes which root tabs have already been rendered at least once.
 */
export type SearchStudioPrimeReactShowcaseTabRenderState = Record<SearchStudioPrimeReactShowcaseTabId, boolean>;

/**
 * Describes the stable grid configuration used by the compact Showcase review surface.
 */
export interface SearchStudioPrimeReactShowcaseGridConfiguration {
    /**
     * Stores the default page size used by the compact Showcase grid.
     */
    readonly rows: number;

    /**
     * Stores the paginator page-size options shown by the compact Showcase grid.
     */
    readonly rowsPerPageOptions: ReadonlyArray<number>;

    /**
     * Stores the grid scroll-height contract so the inner grid owns vertical overflow.
     */
    readonly scrollHeight: '100%';

    /**
     * Stores the PrimeReact density token used to reduce row height in the Showcase grid.
     */
    readonly size: 'small';

    /**
     * Stores the CSS class list used to scope grid-specific overflow and density refinements.
     */
    readonly className: string;
}

/**
 * Describes the stable hierarchy configuration used by the compact Showcase review surface.
 */
export interface SearchStudioPrimeReactShowcaseHierarchyConfiguration {
    /**
     * Stores the hierarchy filter placeholder so the compact tree stays aligned with the page-level reviewer guidance.
     */
    readonly filterPlaceholder: string;

    /**
     * Stores the CSS class list used to scope tree-density refinements to the Showcase hierarchy only.
     */
    readonly className: string;
}

/**
 * Stores the fixed root-tab order required by the consolidated showcase shell.
 */
export const SearchStudioPrimeReactShowcaseTabOrder: ReadonlyArray<SearchStudioPrimeReactShowcaseTabId> = [
    'showcase',
    'forms',
    'dataview',
    'datatable',
    'tree',
    'treetable'
];

const searchStudioPrimeReactShowcaseTabDefinitions: ReadonlyArray<SearchStudioPrimeReactShowcaseTabDefinition> = [
    {
        id: 'showcase',
        label: 'Showcase',
        iconClassName: 'pi pi-desktop'
    },
    {
        id: 'forms',
        label: 'Forms',
        iconClassName: 'pi pi-pencil'
    },
    {
        id: 'dataview',
        label: 'Data View',
        iconClassName: 'pi pi-th-large'
    },
    {
        id: 'datatable',
        label: 'Data Table',
        iconClassName: 'pi pi-table'
    },
    {
        id: 'tree',
        label: 'Tree',
        iconClassName: 'pi pi-sitemap'
    },
    {
        id: 'treetable',
        label: 'Tree Table',
        iconClassName: 'pi pi-list'
    }
];

const searchStudioPrimeReactShowcaseGridConfiguration: SearchStudioPrimeReactShowcaseGridConfiguration = {
    rows: 8,
    rowsPerPageOptions: [8, 12, 16],
    scrollHeight: '100%',
    size: 'small',
    className: 'search-studio-primereact-demo-page__datatable search-studio-primereact-demo-page__datatable--showcase-grid'
};

const searchStudioPrimeReactShowcaseHierarchyConfiguration: SearchStudioPrimeReactShowcaseHierarchyConfiguration = {
    filterPlaceholder: 'Filter showcase hierarchy',
    className: 'search-studio-primereact-demo-page__tree search-studio-primereact-demo-page__tree--showcase-compact'
};

/**
 * Gets the ordered root-tab metadata used by the consolidated showcase shell.
 *
 * @returns The immutable ordered tab-definition list used by the root tab view.
 */
export function getSearchStudioPrimeReactShowcaseTabDefinitions(): ReadonlyArray<SearchStudioPrimeReactShowcaseTabDefinition> {
    // Keep the tab metadata centralized so the runtime shell and focused tests share one stable tab order.
    return searchStudioPrimeReactShowcaseTabDefinitions;
}

/**
 * Gets the stable grid configuration used by the compact Showcase review surface.
 *
 * @returns The immutable grid-configuration contract shared by the runtime page and focused regression tests.
 */
export function getSearchStudioPrimeReactShowcaseGridConfiguration(): SearchStudioPrimeReactShowcaseGridConfiguration {
    // Keep the scroll-height and density contract centralized so runtime rendering and tests stay aligned when the grid layout is refined.
    return searchStudioPrimeReactShowcaseGridConfiguration;
}

/**
 * Gets the stable hierarchy configuration used by the compact Showcase review surface.
 *
 * @returns The immutable hierarchy-configuration contract shared by the runtime page and focused regression tests.
 */
export function getSearchStudioPrimeReactShowcaseHierarchyConfiguration(): SearchStudioPrimeReactShowcaseHierarchyConfiguration {
    // Keep the hierarchy filter copy and scoped class list centralized so tree-density refinements remain easy to review and test.
    return searchStudioPrimeReactShowcaseHierarchyConfiguration;
}

/**
 * Creates the initial lazy-render state used by the consolidated showcase shell.
 *
 * @returns The render-state dictionary with only the default `Showcase` tab marked as rendered.
 */
export function createSearchStudioPrimeReactShowcaseInitialRenderState(): SearchStudioPrimeReactShowcaseTabRenderState {
    // Render only the default Showcase tab on first open so the consolidated page stays lightweight until reviewers activate other tabs.
    return {
        showcase: true,
        forms: false,
        dataview: false,
        datatable: false,
        tree: false,
        treetable: false
    };
}

/**
 * Marks one tab as rendered while preserving all tabs already activated previously.
 *
 * @param currentRenderState Supplies the current per-tab render state.
 * @param tabId Identifies the tab that was just activated.
 * @returns The next render-state dictionary with the requested tab marked as rendered.
 */
export function activateSearchStudioPrimeReactShowcaseRenderState(
    currentRenderState: SearchStudioPrimeReactShowcaseTabRenderState,
    tabId: SearchStudioPrimeReactShowcaseTabId
): SearchStudioPrimeReactShowcaseTabRenderState {
    if (currentRenderState[tabId]) {
        // Reuse the current dictionary when the tab has already been rendered so mounted tab content keeps its local state.
        return currentRenderState;
    }

    // Mark the selected tab as rendered while preserving all previously activated tabs.
    return {
        ...currentRenderState,
        [tabId]: true
    };
}

/**
 * Resolves the zero-based tab index for a supplied tab identifier.
 *
 * @param tabId Identifies the root tab whose index should be resolved.
 * @returns The zero-based tab index used by the PrimeReact root tab view.
 */
export function getSearchStudioPrimeReactShowcaseTabIndex(tabId: SearchStudioPrimeReactShowcaseTabId): number {
    // Resolve the tab index from the centralized ordered metadata so the default tab and tab-change logging stay aligned.
    return SearchStudioPrimeReactShowcaseTabOrder.indexOf(tabId);
}

/**
 * Resolves the stable DOM identifier used by the tab-focus transfer helper for one retained tab.
 *
 * @param tabId Identifies the retained tab whose focus target should be resolved.
 * @returns The stable DOM identifier for the requested tab content wrapper.
 */
export function getSearchStudioPrimeReactShowcaseTabFocusTargetId(tabId: SearchStudioPrimeReactShowcaseTabId): string {
    // Keep the DOM identifiers deterministic so the runtime focus helper and node-based tests share the same content targets.
    return `search-studio-primereact-showcase-tab-focus-target-${tabId}`;
}

/**
 * Moves browser keyboard focus into the active retained-tab content when the target element exists.
 *
 * @param tabId Identifies the retained tab whose content should receive focus.
 * @returns `true` when focus moved successfully; otherwise, `false`.
 */
export function focusSearchStudioPrimeReactShowcaseTabContent(tabId: SearchStudioPrimeReactShowcaseTabId): boolean {
    if (typeof document === 'undefined' || typeof document.getElementById !== 'function') {
        // Skip focus transfer during server rendering or non-browser test contexts that do not expose document lookups.
        return false;
    }

    const focusTarget = document.getElementById(getSearchStudioPrimeReactShowcaseTabFocusTargetId(tabId));

    if (!focusTarget || typeof focusTarget.focus !== 'function') {
        // Skip missing or non-focusable targets so tab activation never throws while content is still being rendered.
        return false;
    }

    // Move focus into the tab content wrapper so keyboard review continues inside the newly displayed surface rather than staying on the tab header.
    focusTarget.focus();
    return document.activeElement === focusTarget;
}

const scenarioOptions = [
    { label: 'Ready', value: 'ready' },
    { label: 'Loading', value: 'loading' },
    { label: 'Empty', value: 'empty' }
];

const reviewLaneOptions = [
    { label: 'Review', value: 'review' },
    { label: 'Compare', value: 'compare' },
    { label: 'Publish', value: 'publish' }
];

/**
 * Maps a mock lifecycle state to the PrimeReact tag severity used across the temporary combined showcase page.
 *
 * @param status Identifies the mock lifecycle state that should be rendered.
 * @returns The PrimeReact severity token that best matches the supplied state.
 */
function getStatusSeverity(status: SearchStudioPrimeReactDemoStatus): 'success' | 'info' | 'warning' | 'danger' {
    // Keep the status-to-severity mapping aligned with the other PrimeReact demo pages so cross-page comparison stays straightforward.
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
 * Creates the default controlled detail-panel state for a supplied row.
 *
 * @param record Supplies the currently selected table row when one exists.
 * @returns The controlled detail-panel state that should be shown to reviewers.
 */
function createDetailFormState(record: SearchStudioPrimeReactDemoTableRecord | undefined): SearchStudioPrimeReactShowcaseDetailFormState {
    // Derive the detail-panel fields from the selected record so the edit panel always starts from visible grid data.
    return {
        draftTitle: record?.title ?? 'Select a row to edit',
        draftReviewNotes: record?.reviewNotes ?? 'Use the table selection to load a mock detail record into this panel.',
        reviewLane: 'review',
        requestPublish: false
    };
}

/**
 * Filters the generated grid records using the current lightweight free-text query.
 *
 * @param records Supplies the full generated dataset.
 * @param tableFilter Supplies the current free-text query shown above the grid.
 * @returns The filtered records that should be rendered in the combined grid area.
 */
function getFilteredRecords(
    records: ReadonlyArray<SearchStudioPrimeReactDemoTableRecord>,
    tableFilter: string
): SearchStudioPrimeReactDemoTableRecord[] {
    const normalizedTableFilter = tableFilter.trim().toLowerCase();

    if (!normalizedTableFilter) {
        // Return the full generated set when no filter is active so the combined showcase starts with maximum breadth on screen.
        return Array.from(records);
    }

    // Search a compact set of key fields so reviewers can narrow the combined grid quickly without introducing the heavier PrimeReact filter model.
    return records.filter(record => [
        record.title,
        record.workspace,
        record.provider,
        record.region,
        record.owner,
        record.reviewNotes,
        record.status
    ].join(' ').toLowerCase().includes(normalizedTableFilter));
}

/**
 * Resolves the tree dataset rendered by the compact `Showcase` hierarchy.
 *
 * @param scenarioSnapshot Supplies the current tree snapshot for the active showcase scenario.
 * @returns The exact tree array that should be passed to PrimeReact so expansion state is not destabilized by unnecessary root-array cloning.
 */
export function getSearchStudioPrimeReactShowcaseTreeValue(
    scenarioSnapshot: SearchStudioPrimeReactDemoScenarioSnapshot<SearchStudioPrimeReactDemoTreeNode>
): ReadonlyArray<SearchStudioPrimeReactDemoTreeNode> {
    // Preserve the original root-array identity because PrimeReact's controlled tree expansion can collapse flickerily when the showcase recreates the top-level value array on every render.
    return scenarioSnapshot.items;
}

/**
 * Renders the temporary PrimeReact combined showcase evaluation page.
 *
 * @param props Supplies the current Theia-aligned theme mapping for the styled PrimeReact demo page.
 * @returns The React node tree for the temporary combined showcase evaluation surface.
 */
export function SearchStudioPrimeReactShowcaseDemoPage(props: SearchStudioPrimeReactDemoPageProps): React.ReactNode {
    const tabDefinitions = React.useMemo(() => getSearchStudioPrimeReactShowcaseTabDefinitions(), []);
    const hierarchyConfiguration = React.useMemo(() => getSearchStudioPrimeReactShowcaseHierarchyConfiguration(), []);
    const gridConfiguration = React.useMemo(() => getSearchStudioPrimeReactShowcaseGridConfiguration(), []);
    const initialRecords = React.useMemo(() => createDataTableDemoRecords(48), []);
    const treeNodes = React.useMemo(() => createTreeDemoNodes(4, 4, 4), []);
    const [activeTabId, setActiveTabId] = React.useState<SearchStudioPrimeReactShowcaseTabId>('showcase');
    const [renderedTabs, setRenderedTabs] = React.useState<SearchStudioPrimeReactShowcaseTabRenderState>(() => createSearchStudioPrimeReactShowcaseInitialRenderState());
    const [records, setRecords] = React.useState<SearchStudioPrimeReactDemoTableRecord[]>(() => Array.from(initialRecords));
    const [scenario, setScenario] = React.useState<SearchStudioPrimeReactDemoScenario>('ready');
    const [tableFilter, setTableFilter] = React.useState('');
    const [selectedRecords, setSelectedRecords] = React.useState<SearchStudioPrimeReactDemoTableRecord[]>([]);
    const [treeSelectionKeys, setTreeSelectionKeys] = React.useState<SearchStudioPrimeReactDemoTreeSelectionKeys | undefined>();
    const [expandedKeys, setExpandedKeys] = React.useState<SearchStudioPrimeReactDemoExpandedKeys>(() => createExpandedTreeKeys(treeNodes));
    const [detailFormState, setDetailFormState] = React.useState<SearchStudioPrimeReactShowcaseDetailFormState>(() => createDetailFormState(undefined));

    const filteredRecords = React.useMemo(
        () => getFilteredRecords(records, tableFilter),
        [records, tableFilter]
    );
    const scenarioSnapshot = React.useMemo(
        () => createScenarioSnapshot(filteredRecords, scenario),
        [filteredRecords, scenario]
    );
    const treeScenarioSnapshot = React.useMemo(
        () => createScenarioSnapshot(treeNodes, scenario),
        [treeNodes, scenario]
    );
    const selectedTreeCount = React.useMemo(
        () => countSelectedTreeKeys(treeSelectionKeys),
        [treeSelectionKeys]
    );
    const activeTabIndex = React.useMemo(
        () => getSearchStudioPrimeReactShowcaseTabIndex(activeTabId),
        [activeTabId]
    );
    const activeDetailRecord = selectedRecords[0];
    const shouldTransferFocusRef = React.useRef(false);

    React.useEffect(() => {
        // Synchronize the mock detail form whenever the lead selected row changes so the edit panel always reflects the active table selection.
        setDetailFormState(currentDetailFormState => ({
            ...createDetailFormState(activeDetailRecord),
            reviewLane: currentDetailFormState.reviewLane,
            requestPublish: currentDetailFormState.requestPublish && Boolean(activeDetailRecord)
        }));
    }, [activeDetailRecord]);

    React.useEffect(() => {
        if (!shouldTransferFocusRef.current) {
            // Skip the initial page render so the consolidated showcase opens normally without pulling focus unexpectedly.
            return;
        }

        shouldTransferFocusRef.current = false;

        // Defer the focus handoff until after React has committed the newly activated tab content to the DOM.
        if (typeof window !== 'undefined' && typeof window.requestAnimationFrame === 'function') {
            const animationFrameHandle = window.requestAnimationFrame(() => {
                focusSearchStudioPrimeReactShowcaseTabContent(activeTabId);
            });

            return () => {
                if (typeof window.cancelAnimationFrame === 'function') {
                    // Cancel the deferred focus work if React replaces the active tab again before the frame executes.
                    window.cancelAnimationFrame(animationFrameHandle);
                }
            };
        }

        focusSearchStudioPrimeReactShowcaseTabContent(activeTabId);
        return undefined;
    }, [activeTabId, renderedTabs]);

    /**
     * Updates the current high-level page scenario shown by the combined showcase.
     *
     * @param event Supplies the currently selected scenario value emitted by PrimeReact.
     */
    function handleScenarioChanged(event: SelectButtonChangeEvent): void {
        if (!event.value) {
            // Ignore empty events so the combined page always stays in one explicit visual state.
            return;
        }

        // Reset transient selections when the scenario changes so the loading and empty states remain easy to review.
        setScenario(event.value as SearchStudioPrimeReactDemoScenario);
        setSelectedRecords([]);
        setTreeSelectionKeys(undefined);
        console.info('Switched temporary PrimeReact showcase scenario.', { scenario: event.value });
    }

    /**
     * Updates the lightweight free-text filter used by the grid area.
     *
     * @param event Supplies the current text input change event.
     */
    function handleTableFilterChanged(event: React.ChangeEvent<HTMLInputElement>): void {
        // Store the raw filter text directly so the combined showcase can narrow the grid without any extra filter chrome.
        setTableFilter(event.target.value);
    }

    /**
     * Updates the current set of selected grid rows.
     *
     * @param event Supplies the rows selected through the PrimeReact table selection callbacks.
     */
    function handleSelectionChanged(event: SearchStudioPrimeReactShowcaseSelectionChangedEvent): void {
        // Keep the selected rows in page state so the detail panel can react immediately to the lead selected record.
        setSelectedRecords(event.value);
    }

    /**
     * Updates the current checkbox-tree selection dictionary.
     *
     * @param event Supplies the latest selection dictionary emitted by PrimeReact.
     */
    function handleTreeSelectionChanged(event: SearchStudioPrimeReactShowcaseTreeSelectionChangedEvent): void {
        // Store the tree selection so the page can surface hierarchy counts beside the grid and form controls.
        setTreeSelectionKeys(event.value);
    }

    /**
     * Updates the current expansion-key dictionary used by the combined showcase tree.
     *
     * @param event Supplies the latest expansion dictionary emitted by PrimeReact.
     */
    function handleExpansionChanged(event: SearchStudioPrimeReactShowcaseTreeToggleEvent): void {
        // Store the current tree expansion state so reviewers can mix toolbar actions with direct node interaction.
        setExpandedKeys(event.value);
    }

    /**
     * Expands every parent node in the combined showcase hierarchy.
     */
    function handleExpandAll(): void {
        // Recompute the expansion dictionary so the action remains aligned with the generated tree even if the sample shape changes later.
        setExpandedKeys(createExpandedTreeKeys(treeNodes));
    }

    /**
     * Collapses the combined showcase hierarchy back to its root level.
     */
    function handleCollapseAll(): void {
        // Clear the expansion dictionary so PrimeReact collapses every hierarchy branch in one consistent update.
        setExpandedKeys({});
    }

    /**
     * Updates a single controlled field inside the combined detail form.
     *
     * @param field Identifies which controlled detail-form property should be replaced.
     * @param value Supplies the next field value.
     */
    function updateDetailFormValue<TKey extends keyof SearchStudioPrimeReactShowcaseDetailFormState>(
        field: TKey,
        value: SearchStudioPrimeReactShowcaseDetailFormState[TKey]
    ): void {
        // Replace only the targeted detail field so the edit panel stays fully controlled while preserving the rest of the mock state.
        setDetailFormState(currentDetailFormState => ({
            ...currentDetailFormState,
            [field]: value
        }));
    }

    /**
     * Updates the editable draft title shown by the detail form.
     *
     * @param event Supplies the current text input change event.
     */
    function handleDraftTitleChanged(event: React.ChangeEvent<HTMLInputElement>): void {
        // Store the raw input value directly so the detail form behaves like a normal controlled edit surface.
        updateDetailFormValue('draftTitle', event.target.value);
    }

    /**
     * Updates the editable draft notes shown by the detail form.
     *
     * @param event Supplies the current textarea change event.
     */
    function handleDraftReviewNotesChanged(event: React.ChangeEvent<HTMLTextAreaElement>): void {
        // Preserve the rest of the controlled detail state while replacing only the editable notes value.
        updateDetailFormValue('draftReviewNotes', event.target.value);
    }

    /**
     * Updates the review lane shown by the detail form segmented button group.
     *
     * @param event Supplies the PrimeReact segmented-button change event.
     */
    function handleReviewLaneChanged(event: SelectButtonChangeEvent): void {
        if (!event.value) {
            // Ignore empty events so the detail panel always keeps one explicit mock review lane active.
            return;
        }

        // Store the selected lane directly because the option values already use the page-owned review-lane union.
        updateDetailFormValue('reviewLane', event.value as SearchStudioPrimeReactShowcaseDetailFormState['reviewLane']);
    }

    /**
     * Updates the publish-route checkbox shown by the combined detail panel.
     *
     * @param event Supplies the PrimeReact checkbox change event.
     */
    function handleRequestPublishChanged(event: CheckboxChangeEvent): void {
        // Store the current boolean state directly so the edit/detail panel can demonstrate a representative secondary toggle.
        updateDetailFormValue('requestPublish', Boolean(event.checked));
    }
 
    /**
     * Commits the mock edit/detail panel state back to the in-memory combined grid records.
     */
    function handleSaveDetailChanges(): void {
        if (!activeDetailRecord) {
            // Stop early when no lead record is selected because the combined edit panel is intentionally selection-driven.
            return;
        }

        try {
            // Update only the lead selected row so the grid and detail panel stay aligned without simulating any backend persistence layer.
            setRecords(currentRecords => currentRecords.map(record => {
                if (record.id !== activeDetailRecord.id) {
                    return record;
                }

                return {
                    ...record,
                    title: detailFormState.draftTitle.trim() || record.title,
                    reviewNotes: detailFormState.draftReviewNotes.trim() || record.reviewNotes
                };
            }));
            console.info('Saved temporary PrimeReact showcase detail changes.', {
                recordId: activeDetailRecord.id,
                reviewLane: detailFormState.reviewLane,
                requestPublish: detailFormState.requestPublish
            });
        } catch (error) {
            // Log the failure so temporary combined-page save issues remain diagnosable during stakeholder review.
            console.error('Failed to save temporary PrimeReact showcase detail changes.', error);
        }
    }

    /**
     * Restores the combined detail panel to the currently selected row values.
     */
    function handleResetDetailChanges(): void {
        // Rebuild the detail form from the lead selected row so reviewers can return to the current grid state quickly.
        setDetailFormState(createDetailFormState(activeDetailRecord));
    }

    /**
     * Updates the active root tab and marks the selected tab as rendered for future visits.
     *
     * @param event Supplies the zero-based tab index emitted by the PrimeReact root tab view.
     */
    function handleRootTabChanged(event: SearchStudioPrimeReactShowcaseTabChangedEvent): void {
        const nextTabDefinition = tabDefinitions[event.index];

        if (!nextTabDefinition) {
            // Ignore out-of-range tab indices so unexpected PrimeReact event payloads cannot destabilize the showcase page.
            return;
        }

        if (nextTabDefinition.id === activeTabId) {
            // Ignore redundant activations so the currently visible tab does not steal focus back from content that the reviewer is already using.
            return;
        }

        // Mark the selected tab as rendered before activating it so the first visit loads content and later visits keep that content mounted.
        shouldTransferFocusRef.current = true;
        setRenderedTabs(currentRenderState => activateSearchStudioPrimeReactShowcaseRenderState(currentRenderState, nextTabDefinition.id));
        setActiveTabId(nextTabDefinition.id);
        console.info('Switched consolidated PrimeReact showcase root tab.', { tabId: nextTabDefinition.id });
    }

    /**
     * Renders the primary dataset column with denser supporting metadata.
     *
     * @param record Supplies the current table row.
     * @returns The React node shown in the first business column.
     */
    function renderTitleBody(record: SearchStudioPrimeReactDemoTableRecord): React.ReactNode {
        // Stack the title and workspace text so the combined grid still reads like business content rather than an isolated component sample.
        return (
            <div className="search-studio-primereact-demo-page__table-copy">
                <strong>{record.title}</strong>
                <span className="search-studio-primereact-demo-page__table-secondary">{record.workspace} · {record.region}</span>
            </div>
        );
    }

    /**
     * Renders the mock status tag shown in the combined grid.
     *
     * @param record Supplies the current table row.
     * @returns The React node shown in the status column.
     */
    function renderStatusBody(record: SearchStudioPrimeReactDemoTableRecord): React.ReactNode {
        // Render a PrimeReact tag so the combined showcase still communicates status compactly inside the denser right-hand grid.
        return <Tag value={record.status} severity={getStatusSeverity(record.status)} rounded />;
    }

    const showcaseTabContent = (
        <div className="search-studio-primereact-demo-page search-studio-primereact-demo-page--styled search-studio-primereact-demo-page--showcase-compact">
            <section className="search-studio-primereact-demo-page__showcase-workspace">
                <div className="search-studio-primereact-demo-page__workspace-bar">
                    <div className="search-studio-primereact-demo-page__workspace-title-block">
                        <h2 className="search-studio-primereact-demo-page__section-title">Compact review workspace</h2>
                        <p className="search-studio-primereact-demo-page__section-summary">
                            Switch the scenario, filter the grid, expand the hierarchy, and edit the selected record without leaving the page.
                        </p>
                    </div>
                    <Tag value={`${records.length} mock records`} severity="info" rounded className="search-studio-primereact-demo-page__section-tag" />
                </div>
                <div className="search-studio-primereact-demo-page__toolbar search-studio-primereact-demo-page__toolbar--showcase">
                    <div className="search-studio-primereact-demo-page__toolbar-group">
                        <span className="search-studio-primereact-demo-page__field-label">Scenario</span>
                        <SelectButton value={scenario} options={scenarioOptions} optionLabel="label" optionValue="value" onChange={handleScenarioChanged} />
                    </div>
                    <div className="search-studio-primereact-demo-page__toolbar-group search-studio-primereact-demo-page__toolbar-group--wide">
                        <label htmlFor="search-studio-primereact-showcase-filter" className="search-studio-primereact-demo-page__field-label">
                            Grid filter
                        </label>
                        <InputText
                            id="search-studio-primereact-showcase-filter"
                            value={tableFilter}
                            onChange={handleTableFilterChanged}
                            placeholder="Filter records by title, workspace, provider, or status"
                            className="search-studio-primereact-demo-page__input"
                        />
                    </div>
                </div>
                <div className="search-studio-primereact-demo-page__callout-row search-studio-primereact-demo-page__callout-row--compact">
                    <Tag value={`${selectedRecords.length} rows`} severity="info" rounded />
                    <Tag value={`${selectedTreeCount} tree nodes`} severity="success" rounded />
                    <span className="search-studio-primereact-demo-page__helper-text">
                        All actions stay mock-only so the page can focus on density, alignment, and combined pane usability.
                    </span>
                </div>
                <Splitter className="search-studio-primereact-demo-page__layout-splitter search-studio-primereact-demo-page__layout-splitter--showcase">
                    <SplitterPanel size={28} minSize={20} className="search-studio-primereact-demo-page__layout-pane">
                        <Panel header="Hierarchy" className="search-studio-primereact-demo-page__pane-panel">
                            <div className="search-studio-primereact-demo-page__pane-shell search-studio-primereact-demo-page__pane-shell--tree">
                                <div className="search-studio-primereact-demo-page__action-row search-studio-primereact-demo-page__action-row--start">
                                    <Button label="Expand all" icon="pi pi-plus" size="small" onClick={handleExpandAll} disabled={scenario !== 'ready'} />
                                    <Button label="Collapse all" icon="pi pi-minus" severity="secondary" size="small" onClick={handleCollapseAll} disabled={scenario !== 'ready'} />
                                </div>
                                <div className="search-studio-primereact-demo-page__pane-scroll-host search-studio-primereact-demo-page__pane-scroll-host--tree">
                                    <Tree
                                        value={getSearchStudioPrimeReactShowcaseTreeValue(treeScenarioSnapshot) as never[]}
                                        selectionMode="checkbox"
                                        selectionKeys={treeSelectionKeys as never}
                                        onSelectionChange={event => handleTreeSelectionChanged(event as SearchStudioPrimeReactShowcaseTreeSelectionChangedEvent)}
                                        expandedKeys={expandedKeys as never}
                                        onToggle={event => handleExpansionChanged(event as SearchStudioPrimeReactShowcaseTreeToggleEvent)}
                                        loading={scenario === 'loading'}
                                        filter
                                        filterPlaceholder={hierarchyConfiguration.filterPlaceholder}
                                        className={hierarchyConfiguration.className}
                                    />
                                </div>
                            </div>
                        </Panel>
                    </SplitterPanel>
                    <SplitterPanel size={72} minSize={35} className="search-studio-primereact-demo-page__layout-pane">
                        <Splitter layout="vertical" className="search-studio-primereact-demo-page__layout-splitter search-studio-primereact-demo-page__layout-splitter--inner">
                            <SplitterPanel size={56} minSize={35} className="search-studio-primereact-demo-page__layout-pane">
                                <Panel header="Grid" className="search-studio-primereact-demo-page__pane-panel">
                                    <div className="search-studio-primereact-demo-page__pane-shell search-studio-primereact-demo-page__pane-shell--grid">
                                        <div className="search-studio-primereact-demo-page__pane-scroll-host search-studio-primereact-demo-page__pane-scroll-host--grid">
                                            <DataTable
                                                value={Array.from(scenarioSnapshot.items)}
                                                selection={selectedRecords}
                                                onSelectionChange={event => handleSelectionChanged(event as SearchStudioPrimeReactShowcaseSelectionChangedEvent)}
                                                dataKey="id"
                                                selectionMode="multiple"
                                                paginator
                                                rows={gridConfiguration.rows}
                                                rowsPerPageOptions={Array.from(gridConfiguration.rowsPerPageOptions)}
                                                scrollable
                                                scrollHeight={gridConfiguration.scrollHeight}
                                                size={gridConfiguration.size}
                                                loading={scenarioSnapshot.isLoading}
                                                emptyMessage="No combined showcase rows match the current scenario and filter."
                                                className={gridConfiguration.className}>
                                                <Column selectionMode="multiple" headerStyle={{ width: '3.5rem' }} />
                                                <Column field="title" header="Title" body={renderTitleBody} sortable style={{ minWidth: '22rem' }} />
                                                <Column field="provider" header="Provider" sortable style={{ minWidth: '10rem' }} />
                                                <Column field="status" header="Status" body={renderStatusBody} sortable style={{ minWidth: '10rem' }} />
                                                <Column field="owner" header="Owner" sortable style={{ minWidth: '11rem' }} />
                                                <Column field="lastReviewedOn" header="Last reviewed" sortable style={{ minWidth: '11rem' }} />
                                            </DataTable>
                                        </div>
                                    </div>
                                </Panel>
                            </SplitterPanel>
                            <SplitterPanel size={44} minSize={25} className="search-studio-primereact-demo-page__layout-pane">
                                <Panel header="Detail" className="search-studio-primereact-demo-page__pane-panel">
                                    <div className="search-studio-primereact-demo-page__pane-shell search-studio-primereact-demo-page__pane-shell--detail">
                                        <div className="search-studio-primereact-demo-page__detail-stack">
                                            <div className="search-studio-primereact-demo-page__section-heading-row">
                                                <strong>{activeDetailRecord ? activeDetailRecord.title : 'No row selected'}</strong>
                                                <Tag
                                                    value={activeDetailRecord ? activeDetailRecord.status : 'Select a row'}
                                                    severity={activeDetailRecord ? getStatusSeverity(activeDetailRecord.status) : 'contrast'}
                                                    rounded
                                                />
                                            </div>
                                            <div className="search-studio-primereact-demo-page__field-grid search-studio-primereact-demo-page__field-grid--wide">
                                                <div className="search-studio-primereact-demo-page__field-group search-studio-primereact-demo-page__field-group--full-width">
                                                    <label htmlFor="search-studio-primereact-showcase-title" className="search-studio-primereact-demo-page__field-label">
                                                        Draft title
                                                    </label>
                                                    <InputText
                                                        id="search-studio-primereact-showcase-title"
                                                        value={detailFormState.draftTitle}
                                                        onChange={handleDraftTitleChanged}
                                                        disabled={!activeDetailRecord}
                                                        className="search-studio-primereact-demo-page__input"
                                                    />
                                                </div>
                                                <div className="search-studio-primereact-demo-page__field-group search-studio-primereact-demo-page__field-group--full-width">
                                                    <label htmlFor="search-studio-primereact-showcase-notes" className="search-studio-primereact-demo-page__field-label">
                                                        Draft review notes
                                                    </label>
                                                    <InputTextarea
                                                        id="search-studio-primereact-showcase-notes"
                                                        value={detailFormState.draftReviewNotes}
                                                        onChange={handleDraftReviewNotesChanged}
                                                        autoResize
                                                        rows={4}
                                                        disabled={!activeDetailRecord}
                                                        className="search-studio-primereact-demo-page__input search-studio-primereact-demo-page__textarea"
                                                    />
                                                </div>
                                                <div className="search-studio-primereact-demo-page__field-group search-studio-primereact-demo-page__field-group--full-width">
                                                    <span className="search-studio-primereact-demo-page__field-label">Review lane</span>
                                                    <SelectButton
                                                        value={detailFormState.reviewLane}
                                                        options={reviewLaneOptions}
                                                        optionLabel="label"
                                                        optionValue="value"
                                                        onChange={handleReviewLaneChanged}
                                                        disabled={!activeDetailRecord}
                                                        className="search-studio-primereact-demo-page__select-button"
                                                    />
                                                </div>
                                                <label className={`search-studio-primereact-demo-page__checkbox-card ${!activeDetailRecord ? 'search-studio-primereact-demo-page__checkbox-card--disabled' : ''}`}>
                                                    <Checkbox
                                                        inputId="search-studio-primereact-showcase-request-publish"
                                                        value="publish"
                                                        onChange={handleRequestPublishChanged}
                                                        checked={detailFormState.requestPublish}
                                                        disabled={!activeDetailRecord}
                                                    />
                                                    <div className="search-studio-primereact-demo-page__checkbox-copy">
                                                        <span>Request publish follow-up</span>
                                                        <small className="search-studio-primereact-demo-page__helper-text">
                                                            Keep this mock-only checkbox enabled to assess how secondary toggles read beside denser edit fields.
                                                        </small>
                                                    </div>
                                                </label>
                                            </div>
                                        </div>
                                        <div className="search-studio-primereact-demo-page__action-row search-studio-primereact-demo-page__action-row--start search-studio-primereact-demo-page__action-row--section-top">
                                            <Button label="Save mock changes" icon="pi pi-save" size="small" onClick={handleSaveDetailChanges} disabled={!activeDetailRecord} />
                                            <Button label="Reset detail" icon="pi pi-refresh" text severity="secondary" size="small" onClick={handleResetDetailChanges} />
                                        </div>
                                    </div>
                                </Panel>
                            </SplitterPanel>
                        </Splitter>
                    </SplitterPanel>
                </Splitter>
            </section>
        </div>
    );

    // Render the consolidated root tab shell first so the entire research surface now opens through one compact tabbed page.
    return (
        <div className="search-studio-primereact-demo-page search-studio-primereact-demo-page--styled search-studio-primereact-demo-page--tabbed-shell">
            <TabView
                activeIndex={activeTabIndex}
                onTabChange={event => handleRootTabChanged(event as SearchStudioPrimeReactShowcaseTabChangedEvent)}
                scrollable
                className="search-studio-primereact-demo-page__root-tabview">
                <TabPanel header="Showcase" leftIcon="pi pi-desktop mr-2" className="search-studio-primereact-demo-page__root-tab-panel">
                    {renderedTabs.showcase ? (
                        <SearchStudioPrimeReactDemoTabContent
                            focusTargetId={getSearchStudioPrimeReactShowcaseTabFocusTargetId('showcase')}
                            ariaLabel="PrimeReact Showcase tab content"
                            overflowMode="contained">
                            {showcaseTabContent}
                        </SearchStudioPrimeReactDemoTabContent>
                    ) : null}
                </TabPanel>
                <TabPanel header="Forms" leftIcon="pi pi-pencil mr-2" className="search-studio-primereact-demo-page__root-tab-panel">
                    {renderedTabs.forms ? (
                        <SearchStudioPrimeReactDemoTabContent
                            focusTargetId={getSearchStudioPrimeReactShowcaseTabFocusTargetId('forms')}
                            ariaLabel="PrimeReact Forms tab content">
                            <SearchStudioPrimeReactFormsDemoPage {...props} hostDisplayMode="tabbed" />
                        </SearchStudioPrimeReactDemoTabContent>
                    ) : null}
                </TabPanel>
                <TabPanel header="Data View" leftIcon="pi pi-th-large mr-2" className="search-studio-primereact-demo-page__root-tab-panel">
                    {renderedTabs.dataview ? (
                        <SearchStudioPrimeReactDemoTabContent
                            focusTargetId={getSearchStudioPrimeReactShowcaseTabFocusTargetId('dataview')}
                            ariaLabel="PrimeReact Data View tab content">
                            <SearchStudioPrimeReactDataViewDemoPage {...props} hostDisplayMode="tabbed" />
                        </SearchStudioPrimeReactDemoTabContent>
                    ) : null}
                </TabPanel>
                <TabPanel header="Data Table" leftIcon="pi pi-table mr-2" className="search-studio-primereact-demo-page__root-tab-panel">
                    {renderedTabs.datatable ? (
                        <SearchStudioPrimeReactDemoTabContent
                            focusTargetId={getSearchStudioPrimeReactShowcaseTabFocusTargetId('datatable')}
                            ariaLabel="PrimeReact Data Table tab content"
                            overflowMode="contained">
                            <SearchStudioPrimeReactDataTableDemoPage {...props} hostDisplayMode="tabbed" />
                        </SearchStudioPrimeReactDemoTabContent>
                    ) : null}
                </TabPanel>
                <TabPanel header="Tree" leftIcon="pi pi-sitemap mr-2" className="search-studio-primereact-demo-page__root-tab-panel">
                    {renderedTabs.tree ? (
                        <SearchStudioPrimeReactDemoTabContent
                            focusTargetId={getSearchStudioPrimeReactShowcaseTabFocusTargetId('tree')}
                            ariaLabel="PrimeReact Tree tab content"
                            overflowMode="contained">
                            <SearchStudioPrimeReactTreeDemoPage {...props} hostDisplayMode="tabbed" />
                        </SearchStudioPrimeReactDemoTabContent>
                    ) : null}
                </TabPanel>
                <TabPanel header="Tree Table" leftIcon="pi pi-list mr-2" className="search-studio-primereact-demo-page__root-tab-panel">
                    {renderedTabs.treetable ? (
                        <SearchStudioPrimeReactDemoTabContent
                            focusTargetId={getSearchStudioPrimeReactShowcaseTabFocusTargetId('treetable')}
                            ariaLabel="PrimeReact Tree Table tab content"
                            overflowMode="contained">
                            <SearchStudioPrimeReactTreeTableDemoPage {...props} hostDisplayMode="tabbed" />
                        </SearchStudioPrimeReactDemoTabContent>
                    ) : null}
                </TabPanel>
            </TabView>
        </div>
    );
}
