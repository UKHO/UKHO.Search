import * as React from '@theia/core/shared/react';
import { Button } from 'primereact/button';
import { Checkbox, CheckboxChangeEvent } from 'primereact/checkbox';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { Divider } from 'primereact/divider';
import { InputText } from 'primereact/inputtext';
import { InputTextarea } from 'primereact/inputtextarea';
import { Panel } from 'primereact/panel';
import { SelectButton, SelectButtonChangeEvent } from 'primereact/selectbutton';
import { Splitter, SplitterPanel } from 'primereact/splitter';
import { Tag } from 'primereact/tag';
import { Tree } from 'primereact/tree';
import {
    createDataTableDemoRecords,
    createTreeDemoNodes,
    SearchStudioPrimeReactDemoStatus,
    SearchStudioPrimeReactDemoTableRecord
} from '../data/search-studio-primereact-demo-data';
import {
    countSelectedTreeKeys,
    createExpandedTreeKeys,
    createScenarioSnapshot,
    SearchStudioPrimeReactDemoExpandedKeys,
    SearchStudioPrimeReactDemoScenario,
    SearchStudioPrimeReactDemoTreeSelectionKeys
} from '../data/search-studio-primereact-demo-state';
import { SearchStudioPrimeReactDemoPageProps } from '../search-studio-primereact-demo-page-props';

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
 * Returns the short theme label shown in the temporary combined showcase header.
 *
 * @param activeThemeVariant Identifies the current Theia-aligned PrimeReact theme variant.
 * @returns The short theme label displayed to reviewers.
 */
function getThemeLabel(activeThemeVariant: SearchStudioPrimeReactDemoPageProps['activeThemeVariant']): string {
    // Surface the current light or dark mapping explicitly so reviewers can confirm Theia theme following while the combined showcase is open.
    return activeThemeVariant === 'light' ? 'Theia light -> Lara Light Blue' : 'Theia dark -> Lara Dark Blue';
}

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
 * Renders the temporary PrimeReact combined showcase evaluation page.
 *
 * @param props Supplies the current Theia-aligned theme mapping for the styled PrimeReact demo page.
 * @returns The React node tree for the temporary combined showcase evaluation surface.
 */
export function SearchStudioPrimeReactShowcaseDemoPage(props: SearchStudioPrimeReactDemoPageProps): React.ReactNode {
    const initialRecords = React.useMemo(() => createDataTableDemoRecords(48), []);
    const treeNodes = React.useMemo(() => createTreeDemoNodes(4, 4, 4), []);
    const [records, setRecords] = React.useState<SearchStudioPrimeReactDemoTableRecord[]>(() => Array.from(initialRecords));
    const [scenario, setScenario] = React.useState<SearchStudioPrimeReactDemoScenario>('ready');
    const [tableFilter, setTableFilter] = React.useState('');
    const [selectedRecords, setSelectedRecords] = React.useState<SearchStudioPrimeReactDemoTableRecord[]>([]);
    const [treeSelectionKeys, setTreeSelectionKeys] = React.useState<SearchStudioPrimeReactDemoTreeSelectionKeys | undefined>();
    const [expandedKeys, setExpandedKeys] = React.useState<SearchStudioPrimeReactDemoExpandedKeys>(() => createExpandedTreeKeys(treeNodes));
    const [detailFormState, setDetailFormState] = React.useState<SearchStudioPrimeReactShowcaseDetailFormState>(() => createDetailFormState(undefined));
    const [lastActionSummary, setLastActionSummary] = React.useState('Review the tree, grid, and detail form together to judge how cohesive PrimeReact feels inside one Theia-hosted content surface.');

    const filteredRecords = React.useMemo(
        () => getFilteredRecords(records, tableFilter),
        [records, tableFilter]
    );
    const scenarioSnapshot = React.useMemo(
        () => createScenarioSnapshot(filteredRecords, scenario),
        [filteredRecords, scenario]
    );
    const selectedTreeCount = React.useMemo(
        () => countSelectedTreeKeys(treeSelectionKeys),
        [treeSelectionKeys]
    );
    const activeDetailRecord = selectedRecords[0];

    React.useEffect(() => {
        // Synchronize the mock detail form whenever the lead selected row changes so the edit panel always reflects the active table selection.
        setDetailFormState(currentDetailFormState => ({
            ...createDetailFormState(activeDetailRecord),
            reviewLane: currentDetailFormState.reviewLane,
            requestPublish: currentDetailFormState.requestPublish && Boolean(activeDetailRecord)
        }));
    }, [activeDetailRecord]);

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
        setLastActionSummary(`Scenario switched to ${String(event.value)} for the combined showcase.`);
+        console.info('Switched temporary PrimeReact showcase scenario.', { scenario: event.value });
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
         setLastActionSummary(event.value.length > 0
             ? `Loaded ${event.value.length} selected grid row(s) into the combined review surface.`
             : 'Cleared the current combined grid selection.');
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
         setLastActionSummary('Expanded every hierarchy branch in the combined showcase.');
     }
 
     /**
      * Collapses the combined showcase hierarchy back to its root level.
      */
     function handleCollapseAll(): void {
         // Clear the expansion dictionary so PrimeReact collapses every hierarchy branch in one consistent update.
         setExpandedKeys({});
         setLastActionSummary('Collapsed the combined showcase hierarchy to its top level.');
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
             setLastActionSummary('Select a grid row before saving the combined detail panel.');
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
             setLastActionSummary(`Saved the mock detail changes for ${activeDetailRecord.title}.`);
             console.info('Saved temporary PrimeReact showcase detail changes.', {
                 recordId: activeDetailRecord.id,
                 reviewLane: detailFormState.reviewLane,
                 requestPublish: detailFormState.requestPublish
             });
         } catch (error) {
             // Log the failure so temporary combined-page save issues remain diagnosable during stakeholder review.
             console.error('Failed to save temporary PrimeReact showcase detail changes.', error);
             setLastActionSummary('Failed to save the mock detail panel changes. Check the console for diagnostics.');
         }
     }
 
     /**
      * Restores the combined detail panel to the currently selected row values.
      */
     function handleResetDetailChanges(): void {
         // Rebuild the detail form from the lead selected row so reviewers can return to the current grid state quickly.
         setDetailFormState(createDetailFormState(activeDetailRecord));
         setLastActionSummary(activeDetailRecord
             ? `Reset the detail panel back to ${activeDetailRecord.title}.`
             : 'Reset the detail panel back to its empty placeholder state.');
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
 
     // Render the broadest temporary PrimeReact review surface using tree, grid, and form controls together inside one Theia-hosted document.
     return (
         <div className="search-studio-primereact-demo-page search-studio-primereact-demo-page--styled">
             <header className="search-studio-primereact-demo-page__hero">
                 <div className="search-studio-primereact-demo-page__hero-copy">
                     <div className="search-studio-primereact-demo-page__hero-heading-row">
                         <h1 className="search-studio-primereact-demo-page__title">PrimeReact showcase demo</h1>
                         <Tag value="Tree + grid + detail form" severity="success" rounded className="search-studio-primereact-demo-page__mode-tag" />
                     </div>
                     <p className="search-studio-primereact-demo-page__summary">
                         This temporary page combines hierarchy browsing, dense grid content, and a mock edit/detail panel so reviewers can judge how
                         broad PrimeReact page composition feels inside the Studio Theia shell.
                     </p>
                     <div className="search-studio-primereact-demo-page__theme-row">
                         <span className="search-studio-primereact-demo-page__theme-label">Styled theme sync</span>
                         <strong className="search-studio-primereact-demo-page__theme-value">{getThemeLabel(props.activeThemeVariant)}</strong>
                     </div>
                 </div>
                 <div className="search-studio-primereact-demo-page__mode-card">
                     <p className="search-studio-primereact-demo-page__toggle-help">
                         All actions remain mock-only and local. The purpose is to review component breadth, density, and cohesiveness rather than to
                         model a production workflow.
                     </p>
                     <Tag value={`${records.length} combined records`} severity="info" rounded />
                 </div>
             </header>
 
             <section className="search-studio-primereact-demo-page__metrics-grid" aria-label="PrimeReact showcase metrics">
                 <article className="search-studio-primereact-demo-page__metric-card">
                     <span className="search-studio-primereact-demo-page__metric-label">Selected grid rows</span>
                     <strong className="search-studio-primereact-demo-page__metric-value">{selectedRecords.length}</strong>
                 </article>
                 <article className="search-studio-primereact-demo-page__metric-card">
                     <span className="search-studio-primereact-demo-page__metric-label">Selected tree nodes</span>
                     <strong className="search-studio-primereact-demo-page__metric-value">{selectedTreeCount}</strong>
                 </article>
                 <article className="search-studio-primereact-demo-page__metric-card">
                     <span className="search-studio-primereact-demo-page__metric-label">Scenario</span>
                     <strong className="search-studio-primereact-demo-page__metric-value">{scenario}</strong>
                 </article>
             </section>
 
             <section className="search-studio-primereact-demo-page__surface search-studio-primereact-demo-page__surface--flush">
                 <div className="search-studio-primereact-demo-page__section-heading-row">
                     <h2 className="search-studio-primereact-demo-page__section-title">Combined showcase workspace</h2>
                     <Tag value="Stakeholder review" severity="warning" rounded className="search-studio-primereact-demo-page__section-tag" />
                 </div>
                 <p className="search-studio-primereact-demo-page__section-summary">
                     Switch the scenario, filter the grid, expand the hierarchy, and edit the detail panel to review several PrimeReact surfaces on one
                     page without leaving the Theia shell.
                 </p>
                 <div className="search-studio-primereact-demo-page__toolbar">
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
                 <div className="search-studio-primereact-demo-page__callout-row">
                     <Tag value={`${selectedRecords.length} rows`} severity="info" rounded />
                     <Tag value={`${selectedTreeCount} tree nodes`} severity="success" rounded />
                     <span className="search-studio-primereact-demo-page__section-summary">{lastActionSummary}</span>
                 </div>
                 <Splitter className="search-studio-primereact-demo-page__layout-splitter search-studio-primereact-demo-page__layout-splitter--showcase">
                     <SplitterPanel size={28} minSize={20} className="search-studio-primereact-demo-page__layout-pane">
                         <Panel header="Hierarchy lane">
                             <div className="search-studio-primereact-demo-page__action-row search-studio-primereact-demo-page__action-row--start">
                                 <Button label="Expand all" icon="pi pi-plus" onClick={handleExpandAll} disabled={scenario !== 'ready'} />
                                 <Button label="Collapse all" icon="pi pi-minus" severity="secondary" onClick={handleCollapseAll} disabled={scenario !== 'ready'} />
                             </div>
                             <Divider />
                             <Tree
                                 value={Array.from(createScenarioSnapshot(treeNodes, scenario).items) as never[]}
                                 selectionMode="checkbox"
                                 selectionKeys={treeSelectionKeys as never}
                                 onSelectionChange={event => handleTreeSelectionChanged(event as SearchStudioPrimeReactShowcaseTreeSelectionChangedEvent)}
                                 expandedKeys={expandedKeys as never}
                                 onToggle={event => handleExpansionChanged(event as SearchStudioPrimeReactShowcaseTreeToggleEvent)}
                                 loading={scenario === 'loading'}
                                 filter
                                 filterPlaceholder="Filter showcase hierarchy"
                                 className="search-studio-primereact-demo-page__tree"
                             />
                         </Panel>
                     </SplitterPanel>
                     <SplitterPanel size={72} minSize={35} className="search-studio-primereact-demo-page__layout-pane">
                         <Splitter layout="vertical" className="search-studio-primereact-demo-page__layout-splitter search-studio-primereact-demo-page__layout-splitter--inner">
                             <SplitterPanel size={56} minSize={35} className="search-studio-primereact-demo-page__layout-pane">
                                 <Panel header="Combined data grid">
                                     <DataTable
                                         value={Array.from(scenarioSnapshot.items)}
                                         selection={selectedRecords}
                                         onSelectionChange={event => handleSelectionChanged(event as SearchStudioPrimeReactShowcaseSelectionChangedEvent)}
                                         dataKey="id"
                                         selectionMode="multiple"
                                         paginator
                                         rows={8}
                                         rowsPerPageOptions={[8, 12, 16]}
                                         scrollable
                                         scrollHeight="18rem"
                                         loading={scenarioSnapshot.isLoading}
                                         emptyMessage="No combined showcase rows match the current scenario and filter."
                                         className="search-studio-primereact-demo-page__datatable">
                                         <Column selectionMode="multiple" headerStyle={{ width: '3.5rem' }} />
                                         <Column field="title" header="Title" body={renderTitleBody} sortable style={{ minWidth: '22rem' }} />
                                         <Column field="provider" header="Provider" sortable style={{ minWidth: '10rem' }} />
                                         <Column field="status" header="Status" body={renderStatusBody} sortable style={{ minWidth: '10rem' }} />
                                         <Column field="owner" header="Owner" sortable style={{ minWidth: '11rem' }} />
                                         <Column field="lastReviewedOn" header="Last reviewed" sortable style={{ minWidth: '11rem' }} />
                                     </DataTable>
                                 </Panel>
                             </SplitterPanel>
                             <SplitterPanel size={44} minSize={25} className="search-studio-primereact-demo-page__layout-pane">
                                 <Panel header="Edit/detail panel">
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
                                         <Divider />
                                         <div className="search-studio-primereact-demo-page__action-row search-studio-primereact-demo-page__action-row--start">
                                             <Button label="Save mock changes" icon="pi pi-save" onClick={handleSaveDetailChanges} disabled={!activeDetailRecord} />
                                             <Button label="Reset detail" icon="pi pi-refresh" text severity="secondary" onClick={handleResetDetailChanges} />
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
 }
