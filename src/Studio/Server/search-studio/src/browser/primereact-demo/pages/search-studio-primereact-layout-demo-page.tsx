import * as React from '@theia/core/shared/react';
import { Button } from 'primereact/button';
import { Divider } from 'primereact/divider';
import { Panel } from 'primereact/panel';
import { Splitter, SplitterPanel } from 'primereact/splitter';
import { TabPanel, TabView } from 'primereact/tabview';
import { Tag } from 'primereact/tag';
import { SearchStudioPrimeReactDemoPageProps } from '../search-studio-primereact-demo-page-props';

/**
 * Describes the minimal event payload needed from PrimeReact `TabView` tab change callbacks.
 */
interface SearchStudioPrimeReactDemoTabChangeEvent {
    /**
     * Stores the zero-based tab index that should become active.
     */
    readonly index: number;
}

/**
 * Describes the minimal event payload needed from PrimeReact splitter resize callbacks.
 */
interface SearchStudioPrimeReactDemoSplitterResizeEvent {
    /**
     * Stores the relative pane sizes reported by the splitter after a drag interaction completes.
     */
    readonly sizes: number[];
}

const layoutTabLabels = ['Workspace', 'Inspector', 'Checklist'] as const;

/**
 * Returns the short theme label shown in the temporary layout demo header.
 *
 * @param activeThemeVariant Identifies the current Theia-aligned PrimeReact theme variant.
 * @returns The short theme label displayed to reviewers.
 */
function getThemeLabel(activeThemeVariant: SearchStudioPrimeReactDemoPageProps['activeThemeVariant']): string {
    // Surface the current light or dark mapping explicitly so reviewers can confirm Theia theme following while the layout page is open.
    return activeThemeVariant === 'light' ? 'Theia light -> Lara Light Blue' : 'Theia dark -> Lara Dark Blue';
}

/**
 * Formats the splitter resize summary shown after reviewers drag a splitter handle.
 *
 * @param sizes Supplies the relative pane sizes emitted by PrimeReact.
 * @returns The short summary string shown in the layout demo banner.
 */
function formatResizeSummary(sizes: number[]): string {
    // Join the reported percentages into a readable summary so the page surfaces resizing feedback without any custom persistence layer.
    return `Splitter resized to ${sizes.map(size => `${size}%`).join(' / ')}.`;
}

/**
 * Renders the temporary PrimeReact layout and container evaluation page.
 *
 * @param props Supplies the current Theia-aligned theme mapping for the styled PrimeReact demo page.
 * @returns The React node tree for the temporary layout and resizing evaluation surface.
 */
export function SearchStudioPrimeReactLayoutDemoPage(props: SearchStudioPrimeReactDemoPageProps): React.ReactNode {
    const [activeTabIndex, setActiveTabIndex] = React.useState(0);
    const [splitterVersion, setSplitterVersion] = React.useState(0);
    const [lastResizeSummary, setLastResizeSummary] = React.useState('Drag either splitter handle to review how PrimeReact resizing behaves inside the Theia shell.');
    const [lastActionSummary, setLastActionSummary] = React.useState('Use the navigation buttons, tabs, and splitter handles to inspect layout composition rather than any production workflow.');

    /**
     * Updates the currently active top-level layout tab.
     *
     * @param event Supplies the PrimeReact tab change event.
     */
    function handleTabChanged(event: SearchStudioPrimeReactDemoTabChangeEvent): void {
        // Store the active tab index directly so the metrics and summary banner can stay aligned with the visible content.
        setActiveTabIndex(event.index);
        setLastActionSummary(`Switched to the ${layoutTabLabels[event.index]} tab.`);
    }

    /**
     * Records the most recent outer splitter resize interaction.
     *
     * @param event Supplies the splitter resize summary emitted by PrimeReact.
     */
    function handleOuterResizeEnd(event: SearchStudioPrimeReactDemoSplitterResizeEvent): void {
        // Surface the outer splitter sizes so reviewers can confirm the page reacts cleanly after horizontal resizing.
        setLastResizeSummary(formatResizeSummary(event.sizes));
    }

    /**
     * Records the most recent inner splitter resize interaction.
     *
     * @param event Supplies the splitter resize summary emitted by PrimeReact.
     */
    function handleInnerResizeEnd(event: SearchStudioPrimeReactDemoSplitterResizeEvent): void {
        // Surface the inner splitter sizes separately so nested layout adjustments remain visible in the review banner.
        setLastResizeSummary(`Nested ${formatResizeSummary(event.sizes).toLowerCase()}`);
    }

    /**
     * Resets the splitters by remounting the temporary layout surface.
     */
    function handleResetLayout(): void {
        // Remount the splitters so reviewers can return to the original pane sizes without introducing custom resize persistence.
        setSplitterVersion(currentVersion => currentVersion + 1);
        setLastResizeSummary('Reset both splitters to their original proportions.');
        setLastActionSummary('Restored the temporary layout demo to its default pane arrangement.');
    }

    /**
     * Updates the review banner to reflect a lightweight navigation or action selection.
     *
     * @param summary Supplies the short summary text that should be surfaced to reviewers.
     */
    function handleFocusAction(summary: string): void {
        // Keep the action mock-only by updating the banner text instead of invoking any backend or workbench navigation behavior.
        setLastActionSummary(summary);
    }

    // Render the layout evaluation surface using full styled PrimeReact so tabs, panels, dividers, and splitters can be judged in-context.
    return (
        <div className="search-studio-primereact-demo-page search-studio-primereact-demo-page--styled">
            <header className="search-studio-primereact-demo-page__hero">
                <div className="search-studio-primereact-demo-page__hero-copy">
                    <div className="search-studio-primereact-demo-page__hero-heading-row">
                        <h1 className="search-studio-primereact-demo-page__title">PrimeReact layout and container demo</h1>
                        <Tag value="Tabs + splitter + panels" severity="success" rounded className="search-studio-primereact-demo-page__mode-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__summary">
                        This temporary page evaluates page-level composition inside the Studio Theia shell, using PrimeReact splitters, tabs, panels,
                        and dividers to show roomy layouts plus draggable resizing interactions.
                    </p>
                    <div className="search-studio-primereact-demo-page__theme-row">
                        <span className="search-studio-primereact-demo-page__theme-label">Styled theme sync</span>
                        <strong className="search-studio-primereact-demo-page__theme-value">{getThemeLabel(props.activeThemeVariant)}</strong>
                    </div>
                </div>
                <div className="search-studio-primereact-demo-page__mode-card">
                    <p className="search-studio-primereact-demo-page__toggle-help">
                        The content remains disposable and mock-only. The emphasis is on how roomy composition, container chrome, and resize handles
                        feel inside the shell rather than on any business workflow.
                    </p>
                    <Tag value="Drag the splitters" severity="info" rounded />
                </div>
            </header>

            <section className="search-studio-primereact-demo-page__metrics-grid" aria-label="PrimeReact layout metrics">
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Active tab</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{layoutTabLabels[activeTabIndex]}</strong>
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Resize feedback</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">Live</strong>
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Reset actions</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{splitterVersion}</strong>
                </article>
            </section>

            <section className="search-studio-primereact-demo-page__surface search-studio-primereact-demo-page__surface--flush">
                <div className="search-studio-primereact-demo-page__section-heading-row">
                    <h2 className="search-studio-primereact-demo-page__section-title">Layout workspace</h2>
                    <Tag value="Resizable" severity="warning" rounded className="search-studio-primereact-demo-page__section-tag" />
                </div>
                <p className="search-studio-primereact-demo-page__section-summary">
                    Resize the panes, switch tabs, and inspect the supporting panels to compare whether PrimeReact provides the right composition tools
                    for broader Studio page layouts.
                </p>
                <div className="search-studio-primereact-demo-page__callout-row">
                    <Tag value="Spacious composition" severity="info" rounded />
                    <Tag value="Nested splitter" severity="success" rounded />
                    <span className="search-studio-primereact-demo-page__section-summary">{lastResizeSummary}</span>
                </div>
                <div className="search-studio-primereact-demo-page__action-row search-studio-primereact-demo-page__action-row--start">
                    <Button label="Reset layout" icon="pi pi-refresh" onClick={handleResetLayout} />
                    <Button label="Inspect spacing" icon="pi pi-arrows-alt" severity="secondary" onClick={() => handleFocusAction('Focused on panel spacing, tab chrome, and divider rhythm for layout review.')} />
                </div>
                <Splitter key={`outer-${splitterVersion}`} className="search-studio-primereact-demo-page__layout-splitter" onResizeEnd={event => handleOuterResizeEnd(event as SearchStudioPrimeReactDemoSplitterResizeEvent)}>
                    <SplitterPanel size={28} minSize={20} className="search-studio-primereact-demo-page__layout-pane">
                        <div className="search-studio-primereact-demo-page__detail-stack">
                            <Panel header="Navigation lane" toggleable>
                                <div className="search-studio-primereact-demo-page__detail-stack search-studio-primereact-demo-page__detail-stack--tight">
                                    <Button label="Workspace" icon="pi pi-home" severity="secondary" text onClick={() => handleFocusAction('Navigation lane set the workspace view as the current review context.')} />
                                    <Button label="Inspector" icon="pi pi-search" severity="secondary" text onClick={() => {
                                        setActiveTabIndex(1);
                                        handleFocusAction('Navigation lane opened the inspector tab for layout review.');
                                    }} />
                                    <Button label="Checklist" icon="pi pi-list-check" severity="secondary" text onClick={() => {
                                        setActiveTabIndex(2);
                                        handleFocusAction('Navigation lane opened the checklist tab for layout review.');
                                    }} />
                                </div>
                            </Panel>
                            <Panel header="Pinned notes" toggleable collapsed>
                                <p className="search-studio-primereact-demo-page__section-summary">
                                    Keep this panel collapsed or expanded to inspect how optional chrome reads inside the overall layout.
                                </p>
                            </Panel>
                        </div>
                    </SplitterPanel>
                    <SplitterPanel size={72} minSize={35} className="search-studio-primereact-demo-page__layout-pane">
                        <TabView activeIndex={activeTabIndex} onTabChange={event => handleTabChanged(event as SearchStudioPrimeReactDemoTabChangeEvent)}>
                            <TabPanel header="Workspace">
                                <Splitter key={`inner-${splitterVersion}`} layout="vertical" className="search-studio-primereact-demo-page__layout-splitter search-studio-primereact-demo-page__layout-splitter--inner" onResizeEnd={event => handleInnerResizeEnd(event as SearchStudioPrimeReactDemoSplitterResizeEvent)}>
                                    <SplitterPanel size={58} minSize={35} className="search-studio-primereact-demo-page__layout-pane">
                                        <Panel header="Summary canvas">
                                            <div className="search-studio-primereact-demo-page__detail-stack search-studio-primereact-demo-page__detail-stack--tight">
                                                <p className="search-studio-primereact-demo-page__section-summary">
                                                    This upper pane stays intentionally spacious so reviewers can judge whether PrimeReact can support
                                                    broader page composition without feeling overly boxed or admin-heavy.
                                                </p>
                                                <Divider />
                                                <div className="search-studio-primereact-demo-page__mini-metrics-grid">
                                                    <div className="search-studio-primereact-demo-page__metric-chip">
                                                        <span className="search-studio-primereact-demo-page__helper-text">Canvas mode</span>
                                                        <strong>Review ready</strong>
                                                    </div>
                                                    <div className="search-studio-primereact-demo-page__metric-chip">
                                                        <span className="search-studio-primereact-demo-page__helper-text">Visible panels</span>
                                                        <strong>3</strong>
                                                    </div>
                                                    <div className="search-studio-primereact-demo-page__metric-chip">
                                                        <span className="search-studio-primereact-demo-page__helper-text">Focus</span>
                                                        <strong>Layout rhythm</strong>
                                                    </div>
                                                </div>
                                            </div>
                                        </Panel>
                                    </SplitterPanel>
                                    <SplitterPanel size={42} minSize={25} className="search-studio-primereact-demo-page__layout-pane">
                                        <Panel header="Detail lane">
                                            <div className="search-studio-primereact-demo-page__detail-stack search-studio-primereact-demo-page__detail-stack--tight">
                                                <p className="search-studio-primereact-demo-page__section-summary">{lastActionSummary}</p>
                                                <Divider />
                                                <div className="search-studio-primereact-demo-page__action-row search-studio-primereact-demo-page__action-row--start">
                                                    <Button label="Open inspector tab" icon="pi pi-search" onClick={() => {
                                                        setActiveTabIndex(1);
                                                        handleFocusAction('Detail lane promoted the inspector tab to the foreground.');
                                                    }} />
                                                    <Button label="Open checklist tab" icon="pi pi-list-check" severity="secondary" onClick={() => {
                                                        setActiveTabIndex(2);
                                                        handleFocusAction('Detail lane promoted the checklist tab to the foreground.');
                                                    }} />
                                                </div>
                                            </div>
                                        </Panel>
                                    </SplitterPanel>
                                </Splitter>
                            </TabPanel>
                            <TabPanel header="Inspector">
                                <Panel header="Inspector surface">
                                    <div className="search-studio-primereact-demo-page__detail-stack search-studio-primereact-demo-page__detail-stack--tight">
                                        <p className="search-studio-primereact-demo-page__section-summary">
                                            This tab keeps the chrome lighter than a dense admin screen while still showing how tabs and panels can
                                            separate contextual detail from the main content area.
                                        </p>
                                        <Divider />
                                        <div className="search-studio-primereact-demo-page__callout-row search-studio-primereact-demo-page__callout-row--inline">
                                            <Tag value="Inspector" severity="info" rounded />
                                            <Tag value="Tab container" severity="success" rounded />
                                            <Tag value="Panel body" severity="contrast" rounded />
                                        </div>
                                    </div>
                                </Panel>
                            </TabPanel>
                            <TabPanel header="Checklist">
                                <Panel header="Reviewer checklist">
                                    <ul className="search-studio-primereact-demo-page__activity-list search-studio-primereact-demo-page__activity-list--tight">
                                        <li className="search-studio-primereact-demo-page__activity-item">Drag both splitter handles and inspect the resize affordance.</li>
                                        <li className="search-studio-primereact-demo-page__activity-item">Switch between tabs to compare container chrome and spacing.</li>
                                        <li className="search-studio-primereact-demo-page__activity-item">Expand or collapse the navigation panels to review optional surface density.</li>
                                    </ul>
                                </Panel>
                            </TabPanel>
                        </TabView>
                    </SplitterPanel>
                </Splitter>
            </section>
        </div>
    );
}
