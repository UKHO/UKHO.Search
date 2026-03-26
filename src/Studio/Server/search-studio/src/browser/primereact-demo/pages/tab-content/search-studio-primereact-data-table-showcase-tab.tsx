import * as React from '@theia/core/shared/react';
import { Button } from 'primereact/button';
import { Column } from 'primereact/column';
import { DataTable } from 'primereact/datatable';
import { Dropdown, DropdownChangeEvent } from 'primereact/dropdown';
import { InputText } from 'primereact/inputtext';
import { SelectButton, SelectButtonChangeEvent } from 'primereact/selectbutton';
import { Tag } from 'primereact/tag';
import {
    createDataTableDemoRecords,
    getSearchStudioPrimeReactDemoStatuses,
    SearchStudioPrimeReactDemoStatus,
    SearchStudioPrimeReactDemoTableRecord
} from '../../data/search-studio-primereact-demo-data';
import {
    createScenarioSnapshot,
    SearchStudioPrimeReactDemoScenario
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
] ;
const statusFilterOptions = [{ label: 'All states', value: 'all' }, ...getSearchStudioPrimeReactDemoStatuses().map(status => ({
    label: status,
    value: status
}))];

/**
 * Describes the supported status filter values used by the temporary `DataTable` page.
 */
type SearchStudioPrimeReactDemoStatusFilter = 'all' | SearchStudioPrimeReactDemoStatus;

/**
 * Describes the supported editable field names on the temporary `DataTable` page.
 */
type SearchStudioPrimeReactDemoEditableField = 'status' | 'reviewNotes';

/**
 * Describes the minimal event payload needed from PrimeReact row-selection callbacks.
 */
interface SearchStudioPrimeReactDemoSelectionChangedEvent {
    /**
     * Stores the rows currently selected by the reviewer.
     */
    readonly value: SearchStudioPrimeReactDemoTableRecord[];
}

/**
 * Describes the minimal event payload needed from PrimeReact cell-edit completion callbacks.
 */
interface SearchStudioPrimeReactDemoCellEditEvent {
    /**
     * Stores the row being edited.
     */
    readonly rowData: SearchStudioPrimeReactDemoTableRecord;

    /**
     * Stores the field being edited.
     */
    readonly field: SearchStudioPrimeReactDemoEditableField;

    /**
     * Stores the value committed by the inline editor.
     */
    readonly newValue: string;
}

/**
 * Describes the minimal editor options needed by the temporary inline-edit controls.
 */
interface SearchStudioPrimeReactDemoColumnEditorOptions<TValue> {
    /**
     * Stores the row being edited.
     */
    readonly rowData: SearchStudioPrimeReactDemoTableRecord;

    /**
     * Stores the current field value.
     */
    readonly value: TValue;

    /**
     * Stores the callback that writes the edited value back to PrimeReact.
     */
    readonly editorCallback?: (value: TValue) => void;
}

/**
 * Returns the short theme label shown in the temporary `DataTable` demo header.
 *
 * @param activeThemeVariant Identifies the current Theia-aligned PrimeReact theme variant.
 * @returns The short theme label displayed to reviewers.
 */
function getThemeLabel(activeThemeVariant: SearchStudioPrimeReactDemoPageProps['activeThemeVariant']): string {
    // Surface the current light or dark mapping explicitly so reviewers can confirm Theia theme following while the data-heavy page is open.
    return activeThemeVariant === 'light' ? 'Theia light -> UKHO/Theia light' : 'Theia dark -> UKHO/Theia dark';
}

/**
 * Maps a mock lifecycle state to the PrimeReact tag severity used across the temporary `DataTable` page.
 *
 * @param status Identifies the mock lifecycle state that should be rendered.
 * @returns The PrimeReact severity token that best matches the supplied state.
 */
function getStatusSeverity(status: SearchStudioPrimeReactDemoStatus): 'success' | 'info' | 'warning' | 'danger' {
    // Keep the status-to-severity mapping centralized so table cells and summary tags stay visually consistent.
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
 * Filters the generated dataset using lightweight page-owned controls rather than the heavier PrimeReact filter model.
 *
 * @param records Supplies the full generated dataset.
 * @param globalFilter Supplies the current free-text search query.
 * @param statusFilter Supplies the current mock status filter.
 * @returns The filtered records that should be rendered in the `DataTable`.
 */
function getFilteredRecords(
    records: ReadonlyArray<SearchStudioPrimeReactDemoTableRecord>,
    globalFilter: string,
    statusFilter: SearchStudioPrimeReactDemoStatusFilter
): SearchStudioPrimeReactDemoTableRecord[] {
    const normalizedGlobalFilter = globalFilter.trim().toLowerCase();

    // Evaluate the simple text and status filters together so the page highlights the resulting row count clearly for reviewers.
    return records.filter(record => {
        if (statusFilter !== 'all' && record.status !== statusFilter) {
            return false;
        }

        if (!normalizedGlobalFilter) {
            return true;
        }

        const searchableText = [
            record.title,
            record.workspace,
            record.provider,
            record.region,
            record.owner,
            record.reviewNotes,
            record.status,
            record.priority,
            record.lastReviewedOn
        ].join(' ').toLowerCase();

        return searchableText.includes(normalizedGlobalFilter);
    });
}

/**
 * Renders the temporary PrimeReact `DataTable` evaluation page.
 *
 * @param props Supplies the current Theia-aligned theme mapping for the styled PrimeReact demo page.
 * @returns The React node tree for the temporary `DataTable` evaluation surface.
 */
export function SearchStudioPrimeReactDataTableDemoPage(props: SearchStudioPrimeReactDemoPageProps): React.ReactNode {
    const initialRecords = React.useMemo(() => createDataTableDemoRecords(), []);
    const [records, setRecords] = React.useState<SearchStudioPrimeReactDemoTableRecord[]>(() => Array.from(initialRecords));
    const [scenario, setScenario] = React.useState<SearchStudioPrimeReactDemoScenario>('ready');
    const [globalFilter, setGlobalFilter] = React.useState('');
    const [statusFilter, setStatusFilter] = React.useState<SearchStudioPrimeReactDemoStatusFilter>('all');
    const [selectedRecords, setSelectedRecords] = React.useState<SearchStudioPrimeReactDemoTableRecord[]>([]);
    const [lastActionSummary, setLastActionSummary] = React.useState('Selection-aware actions are mock only and update this banner for review.');

    const filteredRecords = React.useMemo(
        () => getFilteredRecords(records, globalFilter, statusFilter),
        [records, globalFilter, statusFilter]
    );
    const scenarioSnapshot = React.useMemo(
        () => createScenarioSnapshot(filteredRecords, scenario),
        [filteredRecords, scenario]
    );
    const pageClassName = getSearchStudioPrimeReactDemoPageClassName({
        hostDisplayMode: props.hostDisplayMode,
        usesDataHeavyLayout: true
    });
    const dataTableScrollHeight = getSearchStudioPrimeReactDemoDataScrollHeight(props.hostDisplayMode);

    /**
     * Updates the current high-level page scenario shown by the `DataTable` surface.
     *
     * @param event Supplies the currently selected scenario value emitted by PrimeReact.
     */
    function handleScenarioChanged(event: SelectButtonChangeEvent): void {
        if (!event.value) {
            // Ignore empty events so the page always stays in one explicit visual state.
            return;
        }

        // Reset selection when the page switches scenarios so the loading and empty states stay easy to read.
        setScenario(event.value as SearchStudioPrimeReactDemoScenario);
        setSelectedRecords([]);
        setLastActionSummary(`Scenario switched to ${String(event.value)}.`);
    }

    /**
     * Updates the free-text filter used to narrow the generated dataset.
     *
     * @param event Supplies the current text input change event.
     */
    function handleGlobalFilterChanged(event: React.ChangeEvent<HTMLInputElement>): void {
        // Store the raw filter text so reviewers can test partial matches across multiple columns.
        setGlobalFilter(event.target.value);
    }

    /**
     * Updates the mock status filter used to narrow the generated dataset.
     *
     * @param event Supplies the selected status value emitted by PrimeReact.
     */
    function handleStatusFilterChanged(event: DropdownChangeEvent): void {
        // Store the selected status filter directly because the option values already use the page-owned filter union.
        setStatusFilter(event.value as SearchStudioPrimeReactDemoStatusFilter);
    }

    /**
     * Updates the currently selected rows shown by the temporary `DataTable` page.
     *
     * @param event Supplies the rows selected through the PrimeReact checkbox column.
     */
    function handleSelectionChanged(event: SearchStudioPrimeReactDemoSelectionChangedEvent): void {
        // Keep the selected rows in page state so mock toolbar actions can enable, disable, and report counts immediately.
        setSelectedRecords(event.value);
    }

    /**
     * Selects the first few attention rows so reviewers can inspect the table's selection styling quickly.
     */
    function handleSelectAttentionRows(): void {
        const attentionRows = filteredRecords.filter(record => record.status === 'Attention').slice(0, 4);

        // Reuse a deterministic attention subset so the selection demo remains predictable across repeated openings.
        setSelectedRecords(attentionRows);
        setLastActionSummary(`Selected ${attentionRows.length} attention rows for mock follow-up.`);
    }

    /**
     * Clears the current row selection.
     */
    function handleClearSelection(): void {
        // Reset the selected rows explicitly so disabled toolbar actions and selection tags update together.
        setSelectedRecords([]);
        setLastActionSummary('Cleared the current row selection.');
    }

    /**
     * Emits a mock summary for the currently selected rows.
     */
    function handleInspectSelection(): void {
        // Keep the action mock-only by updating the summary banner instead of triggering any navigation or backend behavior.
        setLastActionSummary(`Inspected ${selectedRecords.length} selected rows in the disposable review surface.`);
    }

    /**
     * Restores the original generated data and clears any transient page-level edits.
     */
    function handleResetEdits(): void {
        // Reset every page-owned state value so reviewers can return to the original dense sample quickly.
        setRecords(Array.from(initialRecords));
        setSelectedRecords([]);
        setGlobalFilter('');
        setStatusFilter('all');
        setScenario('ready');
        setLastActionSummary('Reset filters, selection, and inline edits to the original generated state.');
    }

    /**
     * Commits inline cell edits back to the generated dataset.
     *
     * @param event Supplies the edited row, field, and committed value emitted by PrimeReact.
     */
    function handleCellEditComplete(event: SearchStudioPrimeReactDemoCellEditEvent): void {
        const normalizedValue = event.newValue.trim();

        if (!event.rowData.isEditable) {
            // Ignore edits on locked rows so the page still demonstrates disabled editing states without extra validators.
            setLastActionSummary(`Row ${event.rowData.id} is locked to show a disabled edit state.`);
            return;
        }

        if (event.field === 'status' && !normalizedValue) {
            // Ignore blank status values because the mock rows should always surface a visible lifecycle state.
            return;
        }

        // Update only the edited row and field so PrimeReact can rerender the changed cell without rebuilding the full dataset.
        setRecords(currentRecords => currentRecords.map(record => {
            if (record.id !== event.rowData.id) {
                return record;
            }

            return {
                ...record,
                [event.field]: normalizedValue
            };
        }));
        setLastActionSummary(`Updated ${event.field} for ${event.rowData.title}.`);
    }

    /**
     * Renders the primary dataset column with denser supporting metadata.
     *
     * @param record Supplies the current table row.
     * @returns The React node shown in the first business column.
     */
    function renderTitleBody(record: SearchStudioPrimeReactDemoTableRecord): React.ReactNode {
        // Stack the title and supporting workspace text so the page reads like a business-style grid rather than an isolated component sample.
        return (
            <div className="search-studio-primereact-demo-page__table-copy">
                <strong>{record.title}</strong>
                <span className="search-studio-primereact-demo-page__table-secondary">{record.workspace}</span>
            </div>
        );
    }

    /**
     * Renders the mock status tag shown in both rows and summary cards.
     *
     * @param record Supplies the current table row.
     * @returns The React node shown in the status column.
     */
    function renderStatusBody(record: SearchStudioPrimeReactDemoTableRecord): React.ReactNode {
        // Render a PrimeReact tag so reviewers can inspect how compact status chips read in a dense grid.
        return <Tag value={record.status} severity={getStatusSeverity(record.status)} rounded />;
    }

    /**
     * Renders the inline-edit state badge shown for each row.
     *
     * @param record Supplies the current table row.
     * @returns The React node shown in the edit-state column.
     */
    function renderEditStateBody(record: SearchStudioPrimeReactDemoTableRecord): React.ReactNode {
        // Surface whether each row is editable so the grid demonstrates both active and disabled inline editing behavior.
        return record.isEditable
            ? <Tag value="Inline edit" severity="success" rounded />
            : <Tag value="Locked" severity="warning" rounded />;
    }

    /**
     * Renders the inline status editor shown when a status cell enters edit mode.
     *
     * @param options Supplies the current cell editor options from PrimeReact.
     * @returns The React node used as the inline status editor.
     */
    function renderStatusEditor(options: SearchStudioPrimeReactDemoColumnEditorOptions<SearchStudioPrimeReactDemoStatus>): React.ReactNode {
        // Keep the editor mock-only by using the shared status list directly and disabling the control for locked rows.
        return (
            <Dropdown
                value={options.value}
                options={Array.from(getSearchStudioPrimeReactDemoStatuses())}
                onChange={event => options.editorCallback?.(event.value as SearchStudioPrimeReactDemoStatus)}
                disabled={!options.rowData.isEditable}
                className="search-studio-primereact-demo-page__cell-editor"
            />
        );
    }

    /**
     * Renders the inline notes editor shown when a notes cell enters edit mode.
     *
     * @param options Supplies the current cell editor options from PrimeReact.
     * @returns The React node used as the inline notes editor.
     */
    function renderReviewNotesEditor(options: SearchStudioPrimeReactDemoColumnEditorOptions<string>): React.ReactNode {
        // Use a simple text editor so reviewers can inspect baseline inline editing without introducing heavier overlay components.
        return (
            <InputText
                value={options.value}
                onChange={event => options.editorCallback?.(event.target.value)}
                disabled={!options.rowData.isEditable}
                className="search-studio-primereact-demo-page__cell-editor"
            />
        );
    }

    // Render the data-heavy evaluation surface using full styled PrimeReact so the grid can be judged in-context inside the Theia shell.
    return (
        <div className={pageClassName}>
            <header className="search-studio-primereact-demo-page__hero">
                <div className="search-studio-primereact-demo-page__hero-copy">
                    <div className="search-studio-primereact-demo-page__hero-heading-row">
                        <h1 className="search-studio-primereact-demo-page__title">PrimeReact `DataTable` demo</h1>
                        <Tag value="Scrollable + editable" severity="success" rounded className="search-studio-primereact-demo-page__mode-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__summary">
                        This temporary page evaluates PrimeReact&apos;s dense grid capabilities inside the Studio Theia shell, including sorting,
                        filtering, pagination, selection, loading, empty states, and lightweight inline editing.
                    </p>
                    <div className="search-studio-primereact-demo-page__theme-row">
                        <span className="search-studio-primereact-demo-page__theme-label">Styled theme sync</span>
                        <strong className="search-studio-primereact-demo-page__theme-value">{getThemeLabel(props.activeThemeVariant)}</strong>
                    </div>
                </div>
                <div className="search-studio-primereact-demo-page__mode-card">
                    <p className="search-studio-primereact-demo-page__toggle-help">
                        The dataset is generated in-memory only. Toolbar actions and inline edits update local page state so reviewers can focus on
                        component fit and feel rather than backend workflows.
                    </p>
                    <Tag value={`${records.length} generated rows`} severity="info" rounded />
                </div>
            </header>

            <section className="search-studio-primereact-demo-page__metrics-grid" aria-label="PrimeReact DataTable metrics">
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Visible rows</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{scenarioSnapshot.items.length}</strong>
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Selected rows</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{selectedRecords.length}</strong>
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Scenario</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{scenario}</strong>
                </article>
            </section>

            <section className="search-studio-primereact-demo-page__content-grid search-studio-primereact-demo-page__content-grid--stacked">
                <article className="search-studio-primereact-demo-page__surface">
                    <div className="search-studio-primereact-demo-page__section-heading-row">
                        <h2 className="search-studio-primereact-demo-page__section-title">Grid controls and scenarios</h2>
                        <Tag value="Mock only" severity="warning" rounded className="search-studio-primereact-demo-page__section-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        Reviewers can drive the main grid through ready, loading, and empty states, apply lightweight filters, and trigger mock
                        selection-aware actions without leaving the page.
                    </p>
                    <div className="search-studio-primereact-demo-page__toolbar">
                        <div className="search-studio-primereact-demo-page__toolbar-group">
                            <span className="search-studio-primereact-demo-page__field-label">Scenario</span>
                            <SelectButton value={scenario} options={scenarioOptions} optionLabel="label" optionValue="value" onChange={handleScenarioChanged} />
                        </div>
                        <div className="search-studio-primereact-demo-page__toolbar-group search-studio-primereact-demo-page__toolbar-group--actions">
                            <Button label="Select attention" icon="pi pi-filter-fill" onClick={handleSelectAttentionRows} disabled={scenario !== 'ready'} />
                            <Button label="Inspect selection" icon="pi pi-search" onClick={handleInspectSelection} disabled={selectedRecords.length === 0 || scenario !== 'ready'} />
                            <Button label="Clear selection" icon="pi pi-times" severity="secondary" onClick={handleClearSelection} disabled={selectedRecords.length === 0} />
                            <Button label="Reset" icon="pi pi-refresh" text severity="secondary" onClick={handleResetEdits} />
                        </div>
                    </div>
                    <div className="search-studio-primereact-demo-page__field-grid search-studio-primereact-demo-page__field-grid--filters">
                        <div className="search-studio-primereact-demo-page__field-group">
                            <label htmlFor="search-studio-primereact-datatable-filter" className="search-studio-primereact-demo-page__field-label">
                                Search rows
                            </label>
                            <InputText
                                id="search-studio-primereact-datatable-filter"
                                value={globalFilter}
                                onChange={handleGlobalFilterChanged}
                                placeholder="Dataset, workspace, owner, status..."
                                className="search-studio-primereact-demo-page__input"
                            />
                        </div>
                        <div className="search-studio-primereact-demo-page__field-group">
                            <label htmlFor="search-studio-primereact-datatable-status" className="search-studio-primereact-demo-page__field-label">
                                Status filter
                            </label>
                            <Dropdown
                                id="search-studio-primereact-datatable-status"
                                value={statusFilter}
                                options={statusFilterOptions}
                                optionLabel="label"
                                optionValue="value"
                                onChange={handleStatusFilterChanged}
                                className="search-studio-primereact-demo-page__input"
                            />
                        </div>
                    </div>
                    <div className="search-studio-primereact-demo-page__callout-row">
                        <Tag value={`${filteredRecords.length} filtered rows`} severity="info" rounded />
                        <Tag value={`${records.filter(record => record.isEditable).length} editable rows`} severity="success" rounded />
                        <span className="search-studio-primereact-demo-page__section-summary">{lastActionSummary}</span>
                    </div>
                </article>

                <article className="search-studio-primereact-demo-page__surface search-studio-primereact-demo-page__surface--flush search-studio-primereact-demo-page__surface--contained">
                    <div className="search-studio-primereact-demo-page__section-heading-row">
                        <h2 className="search-studio-primereact-demo-page__section-title">Dense business-style grid</h2>
                        <Tag value={scenarioSnapshot.isLoading ? 'Loading overlay' : scenarioSnapshot.isEmpty ? 'Empty state' : 'Ready'} severity="info" rounded className="search-studio-primereact-demo-page__section-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        Sort any business column, page through the generated dataset, edit the status and notes columns inline, and inspect the
                        disabled edit state on locked rows.
                    </p>
                    <div className="search-studio-primereact-demo-page__pane-scroll-host search-studio-primereact-demo-page__pane-scroll-host--grid">
                        <DataTable
                            value={Array.from(scenarioSnapshot.items)}
                            dataKey="id"
                            paginator
                            rows={12}
                            rowsPerPageOptions={[12, 24, 48]}
                            scrollable
                            scrollHeight={dataTableScrollHeight}
                            sortMode="multiple"
                            loading={scenarioSnapshot.isLoading}
                            selection={selectedRecords}
                            onSelectionChange={event => handleSelectionChanged(event as SearchStudioPrimeReactDemoSelectionChangedEvent)}
                            selectionMode="multiple"
                            editMode="cell"
                            emptyMessage="No rows match the current demo scenario or filter combination."
                            className="search-studio-primereact-demo-page__datatable">
                            <Column selectionMode="multiple" headerStyle={{ width: '4rem' }} />
                            <Column field="title" header="Dataset" sortable body={renderTitleBody} style={{ minWidth: '18rem' }} />
                            <Column field="provider" header="Provider" sortable style={{ minWidth: '10rem' }} />
                            <Column field="region" header="Region" sortable style={{ minWidth: '10rem' }} />
                            <Column field="owner" header="Owner" sortable style={{ minWidth: '11rem' }} />
                            <Column field="status" header="Status" sortable body={renderStatusBody} editor={options => renderStatusEditor(options as SearchStudioPrimeReactDemoColumnEditorOptions<SearchStudioPrimeReactDemoStatus>)} onCellEditComplete={event => handleCellEditComplete(event as SearchStudioPrimeReactDemoCellEditEvent)} style={{ minWidth: '10rem' }} />
                            <Column field="priority" header="Priority" sortable style={{ minWidth: '8rem' }} />
                            <Column field="itemCount" header="Items" sortable style={{ minWidth: '8rem' }} />
                            <Column field="lastReviewedOn" header="Last reviewed" sortable style={{ minWidth: '11rem' }} />
                            <Column field="reviewNotes" header="Reviewer notes" editor={options => renderReviewNotesEditor(options as SearchStudioPrimeReactDemoColumnEditorOptions<string>)} onCellEditComplete={event => handleCellEditComplete(event as SearchStudioPrimeReactDemoCellEditEvent)} style={{ minWidth: '16rem' }} />
                            <Column field="isEditable" header="Edit state" body={renderEditStateBody} style={{ minWidth: '10rem' }} />
                        </DataTable>
                    </div>
                </article>
            </section>
        </div>
    );
}
