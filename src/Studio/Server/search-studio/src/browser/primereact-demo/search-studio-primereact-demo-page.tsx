import * as React from '@theia/core/shared/react';
import { Button } from 'primereact/button';
import { InputText } from 'primereact/inputtext';
import { ProgressBar } from 'primereact/progressbar';
import { SelectButton, SelectButtonChangeEvent } from 'primereact/selectbutton';
import { Tag } from 'primereact/tag';
import { SearchStudioPrimeReactDemoPageProps } from './search-studio-primereact-demo-page-props';
import { SearchStudioPrimeReactDemoThemeVariant } from './search-studio-primereact-demo-presentation-state';

/**
 * Describes the controlled form state rendered on the temporary PrimeReact demo page.
 */
interface SearchStudioPrimeReactDemoFormState {
    /**
     * Stores the current mock workspace name entered by the reviewer.
     */
    readonly workspaceName: string;

    /**
     * Stores the current selected mock review lane.
     */
    readonly reviewLane: string;
}

/**
 * Describes a single review lane option shown by the temporary PrimeReact demo page.
 */
interface SearchStudioPrimeReactDemoLaneOption {
    /**
     * Stores the visible lane label shown to the reviewer.
     */
    readonly label: string;

    /**
     * Stores the stable lane value used by the controlled PrimeReact selection component.
     */
    readonly value: string;
}

/**
 * Describes a single lightweight activity row shown on the temporary PrimeReact demo page.
 */
interface SearchStudioPrimeReactDemoActivityItem {
    /**
     * Stores the activity title shown in the review list.
     */
    readonly title: string;

    /**
     * Stores the short explanatory text shown below the activity title.
     */
    readonly summary: string;

    /**
     * Stores the PrimeReact severity token used for the activity status tag.
     */
    readonly severity: 'success' | 'info' | 'warning';

    /**
     * Stores the short activity status label rendered inside the PrimeReact tag.
     */
    readonly status: string;
}

const reviewLaneOptions: SearchStudioPrimeReactDemoLaneOption[] = [
    { label: 'Explore', value: 'explore' },
    { label: 'Review', value: 'review' },
    { label: 'Publish', value: 'publish' }
];

const activityItems: ReadonlyArray<SearchStudioPrimeReactDemoActivityItem> = [
    {
        title: 'Dataset snapshot loaded',
        summary: 'A temporary mock summary shows how status-oriented list content reads beside lightweight form controls.',
        severity: 'success',
        status: 'Ready'
    },
    {
        title: 'Tree and table follow-up pages planned',
        summary: 'The first page stays intentionally small while proving that the Theia shell can host fully styled PrimeReact content cleanly.',
        severity: 'info',
        status: 'Next'
    },
    {
        title: 'Overlay-heavy components excluded',
        summary: 'This baseline avoids dialogs and popup-driven controls so the initial look-and-feel review stays aligned to the work-package scope.',
        severity: 'warning',
        status: 'Scoped'
    }
];

/**
 * Returns the short theme label shown in the temporary PrimeReact demo header.
 *
 * @param activeThemeVariant Identifies the current Theia-aligned PrimeReact theme variant.
 * @returns The short theme label displayed to reviewers.
 */
function getThemeLabel(activeThemeVariant: SearchStudioPrimeReactDemoThemeVariant): string {
    // Surface the current light or dark mapping explicitly so reviewers can confirm Theia theme following while styled mode is active.
    return activeThemeVariant === 'light' ? 'Theia light -> Lara Light Blue' : 'Theia dark -> Lara Dark Blue';
}

/**
 * Renders the first temporary PrimeReact evaluation page using the stock styled PrimeReact theme.
 *
 * @param props Supplies the current Theia-aligned theme mapping for the styled PrimeReact demo page.
 * @returns The React node tree for the temporary PrimeReact evaluation surface.
 */
export function SearchStudioPrimeReactDemoPage(props: SearchStudioPrimeReactDemoPageProps): React.ReactNode {
    const [formState, setFormState] = React.useState<SearchStudioPrimeReactDemoFormState>({
        workspaceName: 'North Sea survey review',
        reviewLane: 'review'
    });

    /**
     * Updates the controlled workspace-name field shown on the temporary demo page.
     *
     * @param event Supplies the current input value emitted by the PrimeReact text input.
     */
    function handleWorkspaceNameChanged(event: React.ChangeEvent<HTMLInputElement>): void {
        // Preserve the rest of the controlled form state while replacing only the edited text field value.
        setFormState(currentState => ({
            ...currentState,
            workspaceName: event.target.value
        }));
    }

    /**
     * Updates the controlled mock review-lane selection shown on the temporary demo page.
     *
     * @param event Supplies the PrimeReact selection value emitted by the lane toggle group.
     */
    function handleReviewLaneChanged(event: SelectButtonChangeEvent): void {
        // Ignore empty selection events so the lightweight demo keeps a stable lane value on screen.
        if (!event.value) {
            return;
        }

        // Preserve the rest of the controlled form state while replacing only the selected review lane.
        setFormState(currentState => ({
            ...currentState,
            reviewLane: event.value as string
        }));
    }

    // Render the page using the stock PrimeReact theme only because the current research direction now prefers styled mode exclusively.
    return (
        <div className="search-studio-primereact-demo-page search-studio-primereact-demo-page--styled">
            <header className="search-studio-primereact-demo-page__hero">
                <div className="search-studio-primereact-demo-page__hero-copy">
                    <div className="search-studio-primereact-demo-page__hero-heading-row">
                        <h1 className="search-studio-primereact-demo-page__title">PrimeReact research demo</h1>
                        <Tag
                            value="Styled theme"
                            severity="info"
                            rounded
                            className="search-studio-primereact-demo-page__mode-tag"
                        />
                    </div>
                    <p className="search-studio-primereact-demo-page__summary">
                        This first temporary page proves that PrimeReact can render inside the Studio Theia shell, open from the View menu,
                        and follow Theia light and dark theme changes while remaining fully styled with the stock PrimeReact theme set.
                    </p>
                    <div className="search-studio-primereact-demo-page__theme-row">
                        <span className="search-studio-primereact-demo-page__theme-label">Styled theme sync</span>
                        <strong className="search-studio-primereact-demo-page__theme-value">{getThemeLabel(props.activeThemeVariant)}</strong>
                    </div>
                </div>
                <div className="search-studio-primereact-demo-page__mode-card">
                    <p className="search-studio-primereact-demo-page__toggle-help">
                        This temporary review page now assumes full styled PrimeReact only, using the current Theia-aligned Lara theme automatically.
                    </p>
                </div>
            </header>

            <section className="search-studio-primereact-demo-page__metrics-grid" aria-label="PrimeReact bootstrap metrics">
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Shell host</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">Theia document tab</strong>
                    <ProgressBar value={20} showValue={false} className="search-studio-primereact-demo-page__progress" />
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">PrimeReact mode</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">Always styled</strong>
                    <ProgressBar value={100} showValue={false} className="search-studio-primereact-demo-page__progress" />
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Theme following</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{props.activeThemeVariant}</strong>
                    <ProgressBar value={props.activeThemeVariant === 'light' ? 55 : 85} showValue={false} className="search-studio-primereact-demo-page__progress" />
                </article>
            </section>

            <section className="search-studio-primereact-demo-page__content-grid">
                <article className="search-studio-primereact-demo-page__surface">
                    <div className="search-studio-primereact-demo-page__section-heading-row">
                        <h2 className="search-studio-primereact-demo-page__section-title">Controlled form sample</h2>
                        <Tag
                            value="Mock only"
                            severity="warning"
                            rounded
                            className="search-studio-primereact-demo-page__section-tag"
                        />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        The page starts with a small set of controlled inputs so reviewers can judge spacing, labels, and simple action styling.
                    </p>
                    <div className="search-studio-primereact-demo-page__field-grid">
                        <div className="search-studio-primereact-demo-page__field-group">
                            <label htmlFor="search-studio-primereact-workspace-name" className="search-studio-primereact-demo-page__field-label">
                                Workspace name
                            </label>
                            <InputText
                                id="search-studio-primereact-workspace-name"
                                value={formState.workspaceName}
                                onChange={handleWorkspaceNameChanged}
                                className="search-studio-primereact-demo-page__input"
                            />
                        </div>
                        <div className="search-studio-primereact-demo-page__field-group">
                            <span className="search-studio-primereact-demo-page__field-label">Review lane</span>
                            <SelectButton
                                value={formState.reviewLane}
                                options={reviewLaneOptions}
                                optionLabel="label"
                                optionValue="value"
                                onChange={handleReviewLaneChanged}
                                className="search-studio-primereact-demo-page__select-button"
                            />
                        </div>
                    </div>
                    <div className="search-studio-primereact-demo-page__action-row">
                        <Button label="Run temporary review" icon="pi pi-play" className="search-studio-primereact-demo-page__primary-button" />
                        <Button label="Reset" severity="secondary" text className="search-studio-primereact-demo-page__secondary-button" />
                    </div>
                </article>

                <article className="search-studio-primereact-demo-page__surface">
                    <div className="search-studio-primereact-demo-page__section-heading-row">
                        <h2 className="search-studio-primereact-demo-page__section-title">Initial review feed</h2>
                        <Tag
                            value="Representative"
                            severity="info"
                            rounded
                            className="search-studio-primereact-demo-page__section-tag"
                        />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        A compact status-oriented list sits alongside the form sample so the first page shows more than isolated controls.
                    </p>
                    <ul className="search-studio-primereact-demo-page__activity-list">
                        {activityItems.map(activityItem => (
                            <li key={activityItem.title} className="search-studio-primereact-demo-page__activity-item">
                                <div className="search-studio-primereact-demo-page__activity-copy">
                                    <strong className="search-studio-primereact-demo-page__activity-title">{activityItem.title}</strong>
                                    <p className="search-studio-primereact-demo-page__activity-summary">{activityItem.summary}</p>
                                </div>
                                <Tag
                                    value={activityItem.status}
                                    severity={activityItem.severity}
                                    rounded
                                    className="search-studio-primereact-demo-page__activity-tag"
                                />
                            </li>
                        ))}
                    </ul>
                </article>
            </section>
        </div>
    );
}
