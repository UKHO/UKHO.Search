import * as React from '@theia/core/shared/react';
import { Button } from 'primereact/button';
import { DataView } from 'primereact/dataview';
import { Divider } from 'primereact/divider';
import { SelectButton, SelectButtonChangeEvent } from 'primereact/selectbutton';
import { Tag } from 'primereact/tag';
import {
    createDataViewDemoRecords,
    SearchStudioPrimeReactDemoDataViewRecord,
    SearchStudioPrimeReactDemoStatus
} from '../data/search-studio-primereact-demo-data';
import {
    createScenarioSnapshot,
    SearchStudioPrimeReactDemoScenario
} from '../data/search-studio-primereact-demo-state';
import { SearchStudioPrimeReactDemoPageProps } from '../search-studio-primereact-demo-page-props';

/**
 * Identifies the supported card/list density modes shown by the temporary `DataView` page.
 */
type SearchStudioPrimeReactDataViewDensity = 'relaxed' | 'compact';

/**
 * Identifies the supported PrimeReact `DataView` layout modes used by the temporary page.
 */
type SearchStudioPrimeReactDataViewLayout = 'grid' | 'list';

const scenarioOptions = [
    { label: 'Ready', value: 'ready' },
    { label: 'Loading', value: 'loading' },
    { label: 'Empty', value: 'empty' }
];

const layoutOptions = [
    { label: 'Grid cards', value: 'grid' },
    { label: 'List rows', value: 'list' }
];

const densityOptions = [
    { label: 'Relaxed', value: 'relaxed' },
    { label: 'Compact', value: 'compact' }
];

/**
 * Returns the short theme label shown in the temporary `DataView` demo header.
 *
 * @param activeThemeVariant Identifies the current Theia-aligned PrimeReact theme variant.
 * @returns The short theme label displayed to reviewers.
 */
function getThemeLabel(activeThemeVariant: SearchStudioPrimeReactDemoPageProps['activeThemeVariant']): string {
    // Surface the current light or dark mapping explicitly so reviewers can confirm Theia theme following while the card-list page is open.
    return activeThemeVariant === 'light' ? 'Theia light -> Lara Light Blue' : 'Theia dark -> Lara Dark Blue';
}

/**
 * Maps a mock lifecycle state to the PrimeReact tag severity used across the temporary `DataView` page.
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
 * Renders the temporary PrimeReact `DataView` evaluation page.
 *
 * @param props Supplies the current Theia-aligned theme mapping for the styled PrimeReact demo page.
 * @returns The React node tree for the temporary card-list evaluation surface.
 */
export function SearchStudioPrimeReactDataViewDemoPage(props: SearchStudioPrimeReactDemoPageProps): React.ReactNode {
    const records = React.useMemo(() => createDataViewDemoRecords(), []);
    const [scenario, setScenario] = React.useState<SearchStudioPrimeReactDemoScenario>('ready');
    const [layout, setLayout] = React.useState<SearchStudioPrimeReactDataViewLayout>('grid');
    const [density, setDensity] = React.useState<SearchStudioPrimeReactDataViewDensity>('relaxed');
    const [selectedRecordId, setSelectedRecordId] = React.useState<string | undefined>(records[0]?.id);
    const [lastActionSummary, setLastActionSummary] = React.useState('Select cards and switch between list and grid layouts to compare non-tabular presentation against the data-grid demos.');

    const scenarioSnapshot = React.useMemo(
        () => createScenarioSnapshot(records, scenario),
        [records, scenario]
    );
    const selectedRecord = React.useMemo(
        () => records.find(record => record.id === selectedRecordId),
        [records, selectedRecordId]
    );

    /**
     * Updates the current high-level page scenario shown by the `DataView` surface.
     *
     * @param event Supplies the currently selected scenario value emitted by PrimeReact.
     */
    function handleScenarioChanged(event: SelectButtonChangeEvent): void {
        if (!event.value) {
            // Ignore empty events so the page always stays in one explicit visual state.
            return;
        }

        // Reset the selected card when the page switches scenarios so the loading and empty states stay easy to read.
        setScenario(event.value as SearchStudioPrimeReactDemoScenario);
        setSelectedRecordId(undefined);
        setLastActionSummary(`Scenario switched to ${String(event.value)}.`);
    }

    /**
     * Updates the current layout mode shown by PrimeReact `DataView`.
     *
     * @param event Supplies the currently selected layout value emitted by PrimeReact.
     */
    function handleLayoutChanged(event: SelectButtonChangeEvent): void {
        if (!event.value) {
            // Ignore empty events so the page always keeps one explicit layout mode active.
            return;
        }

        // Store the selected layout directly because the option values already use the page-owned layout union.
        setLayout(event.value as SearchStudioPrimeReactDataViewLayout);
        setLastActionSummary(`Layout switched to ${String(event.value)} for presentation comparison.`);
    }

    /**
     * Updates the current card density mode shown by the page.
     *
     * @param event Supplies the currently selected density value emitted by PrimeReact.
     */
    function handleDensityChanged(event: SelectButtonChangeEvent): void {
        if (!event.value) {
            // Ignore empty events so the card and list surfaces always keep one explicit density mode active.
            return;
        }

        // Store the density mode directly so the item template can switch spacing classes without additional mapping.
        setDensity(event.value as SearchStudioPrimeReactDataViewDensity);
        setLastActionSummary(`Density switched to ${String(event.value)} to compare card spacing.`);
    }

    /**
     * Marks a generated record as the currently selected card or list row.
     *
     * @param record Supplies the generated card-list record that should become active.
     */
    function handleSelectRecord(record: SearchStudioPrimeReactDemoDataViewRecord): void {
        // Persist only the record identifier so the page can keep the selected summary stable across pagination and layout changes.
        setSelectedRecordId(record.id);
        setLastActionSummary(`Selected ${record.title} for detail comparison.`);
    }

    /**
     * Clears the current card or list selection.
     */
    function handleClearSelection(): void {
        // Clear the selected identifier explicitly so the detail summary falls back to its empty-state wording.
        setSelectedRecordId(undefined);
        setLastActionSummary('Cleared the current card-list selection.');
    }

    /**
     * Renders one generated record as either a grid card or a list row.
     *
     * @param record Supplies the generated card-list record.
     * @param currentLayout Supplies the active PrimeReact layout mode.
     * @returns The React node shown for the current `DataView` item.
     */
    function renderItemTemplate(
        record: SearchStudioPrimeReactDemoDataViewRecord,
        currentLayout?: SearchStudioPrimeReactDataViewLayout
    ): React.ReactNode {
        const isSelected = record.id === selectedRecordId;
        const currentDensity = density === 'compact' ? 'search-studio-primereact-demo-page__dataview-item--compact' : '';

        if (currentLayout === 'list') {
            // Render a single-row list item so reviewers can compare flatter presentation against the card layout using the same mock data.
            return (
                <article className={`search-studio-primereact-demo-page__dataview-item search-studio-primereact-demo-page__dataview-item--list ${currentDensity} ${isSelected ? 'search-studio-primereact-demo-page__dataview-item--selected' : ''}`}>
                    <div className="search-studio-primereact-demo-page__dataview-copy">
                        <div className="search-studio-primereact-demo-page__section-heading-row">
                            <strong>{record.title}</strong>
                            <Tag value={record.status} severity={getStatusSeverity(record.status)} rounded />
                        </div>
                        <p className="search-studio-primereact-demo-page__section-summary">{record.summary}</p>
                        <div className="search-studio-primereact-demo-page__callout-row search-studio-primereact-demo-page__callout-row--inline">
                            <Tag value={record.provider} severity="info" rounded />
                            <Tag value={record.emphasis} severity="contrast" rounded />
                            <span className="search-studio-primereact-demo-page__helper-text">
                                {record.owner} · {record.region} · {record.itemCount} items · {record.lastUpdatedOn}
                            </span>
                        </div>
                    </div>
                    <div className="search-studio-primereact-demo-page__dataview-actions">
                        <Button label="Select" icon="pi pi-check" onClick={() => handleSelectRecord(record)} />
                    </div>
                </article>
            );
        }

        // Render a richer card so reviewers can compare spacious non-tabular presentation against the denser data-grid pages.
        return (
            <article className={`search-studio-primereact-demo-page__dataview-item search-studio-primereact-demo-page__dataview-item--grid ${currentDensity} ${isSelected ? 'search-studio-primereact-demo-page__dataview-item--selected' : ''}`}>
                <div className="search-studio-primereact-demo-page__section-heading-row">
                    <Tag value={record.emphasis} severity="contrast" rounded className="search-studio-primereact-demo-page__section-tag" />
                    <Tag value={record.status} severity={getStatusSeverity(record.status)} rounded />
                </div>
                <div className="search-studio-primereact-demo-page__dataview-copy">
                    <strong>{record.title}</strong>
                    <p className="search-studio-primereact-demo-page__section-summary">{record.summary}</p>
                </div>
                <Divider />
                <div className="search-studio-primereact-demo-page__mini-metrics-grid">
                    <div className="search-studio-primereact-demo-page__metric-chip">
                        <span className="search-studio-primereact-demo-page__helper-text">Provider</span>
                        <strong>{record.provider}</strong>
                    </div>
                    <div className="search-studio-primereact-demo-page__metric-chip">
                        <span className="search-studio-primereact-demo-page__helper-text">Owner</span>
                        <strong>{record.owner}</strong>
                    </div>
                    <div className="search-studio-primereact-demo-page__metric-chip">
                        <span className="search-studio-primereact-demo-page__helper-text">Region</span>
                        <strong>{record.region}</strong>
                    </div>
                    <div className="search-studio-primereact-demo-page__metric-chip">
                        <span className="search-studio-primereact-demo-page__helper-text">Items</span>
                        <strong>{record.itemCount}</strong>
                    </div>
                </div>
                <div className="search-studio-primereact-demo-page__action-row search-studio-primereact-demo-page__action-row--start">
                    <Button label="Select card" icon="pi pi-check" onClick={() => handleSelectRecord(record)} />
                    <Button label="Compare to grid" icon="pi pi-table" text severity="secondary" onClick={() => setLastActionSummary(`Compared ${record.title} with the DataTable page using the same mock review context.`)} />
                </div>
            </article>
        );
    }

    // Render the card-list evaluation surface using full styled PrimeReact so non-tabular layouts can be judged in-context inside the Theia shell.
    return (
        <div className="search-studio-primereact-demo-page search-studio-primereact-demo-page--styled">
            <header className="search-studio-primereact-demo-page__hero">
                <div className="search-studio-primereact-demo-page__hero-copy">
                    <div className="search-studio-primereact-demo-page__hero-heading-row">
                        <h1 className="search-studio-primereact-demo-page__title">PrimeReact `DataView` demo</h1>
                        <Tag value="Cards + list rows" severity="success" rounded className="search-studio-primereact-demo-page__mode-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__summary">
                        This temporary page evaluates how PrimeReact presents the same mock data in card and list layouts, with light density controls
                        and selection state that can be compared directly against the grid-heavy pages.
                    </p>
                    <div className="search-studio-primereact-demo-page__theme-row">
                        <span className="search-studio-primereact-demo-page__theme-label">Styled theme sync</span>
                        <strong className="search-studio-primereact-demo-page__theme-value">{getThemeLabel(props.activeThemeVariant)}</strong>
                    </div>
                </div>
                <div className="search-studio-primereact-demo-page__mode-card">
                    <p className="search-studio-primereact-demo-page__toggle-help">
                        The dataset stays in-memory only. Layout switches, density changes, and selection updates operate locally so reviewers can focus
                        on presentation quality rather than workflow behavior.
                    </p>
                    <Tag value={`${records.length} generated items`} severity="info" rounded />
                </div>
            </header>

            <section className="search-studio-primereact-demo-page__metrics-grid" aria-label="PrimeReact DataView metrics">
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Visible items</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{scenarioSnapshot.items.length}</strong>
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Layout</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{layout}</strong>
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Selected item</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{selectedRecord ? selectedRecord.title : 'None'}</strong>
                </article>
            </section>

            <section className="search-studio-primereact-demo-page__content-grid">
                <article className="search-studio-primereact-demo-page__surface search-studio-primereact-demo-page__surface--flush">
                    <div className="search-studio-primereact-demo-page__section-heading-row">
                        <h2 className="search-studio-primereact-demo-page__section-title">Card and list controls</h2>
                        <Tag value="Presentation review" severity="warning" rounded className="search-studio-primereact-demo-page__section-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        Switch between grid cards and flatter list rows, then adjust density and page scenario to compare how much chrome the content
                        needs outside a traditional table.
                    </p>
                    <div className="search-studio-primereact-demo-page__toolbar">
                        <div className="search-studio-primereact-demo-page__toolbar-group">
                            <span className="search-studio-primereact-demo-page__field-label">Scenario</span>
                            <SelectButton value={scenario} options={scenarioOptions} optionLabel="label" optionValue="value" onChange={handleScenarioChanged} />
                        </div>
                        <div className="search-studio-primereact-demo-page__toolbar-group">
                            <span className="search-studio-primereact-demo-page__field-label">Layout</span>
                            <SelectButton value={layout} options={layoutOptions} optionLabel="label" optionValue="value" onChange={handleLayoutChanged} />
                        </div>
                        <div className="search-studio-primereact-demo-page__toolbar-group">
                            <span className="search-studio-primereact-demo-page__field-label">Density</span>
                            <SelectButton value={density} options={densityOptions} optionLabel="label" optionValue="value" onChange={handleDensityChanged} />
                        </div>
                    </div>
                    <div className="search-studio-primereact-demo-page__callout-row">
                        <Tag value={scenario} severity="info" rounded />
                        <Tag value={density} severity="success" rounded />
                        <span className="search-studio-primereact-demo-page__section-summary">{lastActionSummary}</span>
                    </div>
                    <DataView
                        value={Array.from(scenarioSnapshot.items)}
                        layout={layout}
                        itemTemplate={renderItemTemplate}
                        paginator
                        rows={6}
                        className={`search-studio-primereact-demo-page__dataview search-studio-primereact-demo-page__dataview--${layout}`}
                    />
                    {scenarioSnapshot.isEmpty ? (
                        <p className="search-studio-primereact-demo-page__section-summary">
                            No generated review packs are shown for the current scenario, which lets reviewers inspect the non-tabular empty state.
                        </p>
                    ) : null}
                </article>

                <article className="search-studio-primereact-demo-page__surface">
                    <div className="search-studio-primereact-demo-page__section-heading-row">
                        <h2 className="search-studio-primereact-demo-page__section-title">Selected item summary</h2>
                        <Tag value={selectedRecord ? 'Selected' : 'No selection'} severity={selectedRecord ? 'success' : 'contrast'} rounded className="search-studio-primereact-demo-page__section-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        The detail panel keeps the current selection visible so reviewers can compare card/list reading flow against the data-table demos.
                    </p>
                    {selectedRecord ? (
                        <div className="search-studio-primereact-demo-page__detail-stack">
                            <strong>{selectedRecord.title}</strong>
                            <p className="search-studio-primereact-demo-page__section-summary">{selectedRecord.summary}</p>
                            <div className="search-studio-primereact-demo-page__callout-row search-studio-primereact-demo-page__callout-row--inline">
                                <Tag value={selectedRecord.provider} severity="info" rounded />
                                <Tag value={selectedRecord.status} severity={getStatusSeverity(selectedRecord.status)} rounded />
                                <Tag value={selectedRecord.emphasis} severity="contrast" rounded />
                            </div>
                            <div className="search-studio-primereact-demo-page__detail-stack search-studio-primereact-demo-page__detail-stack--tight">
                                <span className="search-studio-primereact-demo-page__helper-text">Owner: {selectedRecord.owner}</span>
                                <span className="search-studio-primereact-demo-page__helper-text">Region: {selectedRecord.region}</span>
                                <span className="search-studio-primereact-demo-page__helper-text">Items: {selectedRecord.itemCount}</span>
                                <span className="search-studio-primereact-demo-page__helper-text">Updated: {selectedRecord.lastUpdatedOn}</span>
                            </div>
                            <div className="search-studio-primereact-demo-page__action-row search-studio-primereact-demo-page__action-row--start">
                                <Button label="Compare with table" icon="pi pi-table" onClick={() => setLastActionSummary(`Used ${selectedRecord.title} as the current comparison point against the DataTable demo.`)} />
                                <Button label="Clear selection" icon="pi pi-times" text severity="secondary" onClick={handleClearSelection} />
                            </div>
                        </div>
                    ) : (
                        <p className="search-studio-primereact-demo-page__section-summary">
                            Select any card or list row to populate this summary pane and compare how the same mock data reads outside a grid surface.
                        </p>
                    )}
                </article>
            </section>
        </div>
    );
}
