import * as React from '@theia/core/shared/react';
import { Button } from 'primereact/button';
import { Checkbox, CheckboxChangeEvent } from 'primereact/checkbox';
import { Divider } from 'primereact/divider';
import { InputNumber, InputNumberValueChangeEvent } from 'primereact/inputnumber';
import { InputSwitch, InputSwitchChangeEvent } from 'primereact/inputswitch';
import { InputText } from 'primereact/inputtext';
import { InputTextarea } from 'primereact/inputtextarea';
import { Password } from 'primereact/password';
import { RadioButton, RadioButtonChangeEvent } from 'primereact/radiobutton';
import { SelectButton, SelectButtonChangeEvent } from 'primereact/selectbutton';
import { Slider, SliderChangeEvent } from 'primereact/slider';
import { Tag } from 'primereact/tag';
import {
    createSearchStudioPrimeReactFormsDemoInitialValues,
    SearchStudioPrimeReactFormsDemoValidation,
    SearchStudioPrimeReactFormsDemoValues,
    validateSearchStudioPrimeReactFormsDemoValues
} from '../data/search-studio-primereact-demo-state';
import { SearchStudioPrimeReactDemoPageProps } from '../search-studio-primereact-demo-page-props';

/**
 * Describes a single selection option shown by the forms demo segmented button group.
 */
interface SearchStudioPrimeReactFormsDemoOption {
    /**
     * Stores the visible label shown to reviewers.
     */
    readonly label: string;

    /**
     * Stores the stable value written into the controlled form state.
     */
    readonly value: string;
}

/**
 * Describes the grouped checkbox metadata shown by the forms demo page.
 */
interface SearchStudioPrimeReactFormsDemoCapabilityOption {
    /**
     * Stores the stable capability key written into the controlled form state.
     */
    readonly value: string;

    /**
     * Stores the visible capability label shown beside the checkbox.
     */
    readonly label: string;

    /**
     * Stores the short supporting description shown beneath the capability label.
     */
    readonly summary: string;
}

const reviewModeOptions: SearchStudioPrimeReactFormsDemoOption[] = [
    { label: 'Guided', value: 'guided' },
    { label: 'Audit', value: 'audit' },
    { label: 'Expedite', value: 'expedite' }
];

const sourceProfileOptions: ReadonlyArray<SearchStudioPrimeReactFormsDemoOption> = [
    { label: 'Provider feed', value: 'provider' },
    { label: 'Catalogue bundle', value: 'catalogue' },
    { label: 'Operations handoff', value: 'operations' }
];

const capabilityOptions: ReadonlyArray<SearchStudioPrimeReactFormsDemoCapabilityOption> = [
    {
        value: 'quality',
        label: 'Quality checks',
        summary: 'Demonstrates grouped checkbox selection plus inline validation.'
    },
    {
        value: 'selection',
        label: 'Selection preview',
        summary: 'Highlights how grouped options read beside larger controlled inputs.'
    },
    {
        value: 'notifications',
        label: 'Notification routing',
        summary: 'Shows how helper text and disabled state can sit beside boolean controls.'
    }
];

/**
 * Returns the short theme label shown in the temporary forms demo header.
 *
 * @param activeThemeVariant Identifies the current Theia-aligned PrimeReact theme variant.
 * @returns The short theme label displayed to reviewers.
 */
function getThemeLabel(activeThemeVariant: SearchStudioPrimeReactDemoPageProps['activeThemeVariant']): string {
    // Surface the current light or dark mapping explicitly so reviewers can confirm Theia theme following while the forms page is open.
    return activeThemeVariant === 'light' ? 'Theia light -> Lara Light Blue' : 'Theia dark -> Lara Dark Blue';
}

/**
 * Counts how many validation messages are currently present on the forms demo page.
 *
 * @param validation Supplies the current inline validation result.
 * @returns The number of non-empty validation messages.
 */
function countValidationMessages(validation: SearchStudioPrimeReactFormsDemoValidation): number {
    // Count only defined messages so the metrics summary can report how much inline guidance the current form state exposes.
    return Object.values(validation).filter(message => Boolean(message)).length;
}

/**
 * Renders the temporary PrimeReact forms evaluation page.
 *
 * @param props Supplies the current Theia-aligned theme mapping for the styled PrimeReact demo page.
 * @returns The React node tree for the temporary forms evaluation surface.
 */
export function SearchStudioPrimeReactFormsDemoPage(props: SearchStudioPrimeReactDemoPageProps): React.ReactNode {
    const initialValues = React.useMemo(() => createSearchStudioPrimeReactFormsDemoInitialValues(), []);
    const [formValues, setFormValues] = React.useState<SearchStudioPrimeReactFormsDemoValues>(initialValues);
    const [hasAttemptedSubmit, setHasAttemptedSubmit] = React.useState(false);
    const [submissionState, setSubmissionState] = React.useState<'idle' | 'saving' | 'saved'>('idle');
    const [lastActionSummary, setLastActionSummary] = React.useState('Change the controlled inputs to review PrimeReact form spacing, inline help, and disabled-state behavior.');
    const saveTimerRef = React.useRef<number | undefined>(undefined);

    const validation = React.useMemo(
        () => validateSearchStudioPrimeReactFormsDemoValues(formValues),
        [formValues]
    );
    const validationCount = React.useMemo(
        () => countValidationMessages(validation),
        [validation]
    );
    const visibleValidation = hasAttemptedSubmit ? validation : {};
    const canRequestPublish = formValues.reviewMode === 'expedite';

    React.useEffect(() => () => {
        if (!saveTimerRef.current) {
            // Skip cleanup when the page never started its mock save timer.
            return;
        }

        // Cancel the pending mock save callback so it cannot update state after the reviewer navigates away.
        window.clearTimeout(saveTimerRef.current);
    }, []);

    /**
     * Updates a single scalar field inside the controlled form state.
     *
     * @param field Identifies which controlled property should be replaced.
     * @param value Supplies the next field value.
     */
    function updateFormValue<TKey extends keyof SearchStudioPrimeReactFormsDemoValues>(
        field: TKey,
        value: SearchStudioPrimeReactFormsDemoValues[TKey]
    ): void {
        // Replace only the targeted field so the page stays fully controlled while preserving the rest of the mock review state.
        setFormValues(currentValues => ({
            ...currentValues,
            [field]: value
        }));
    }

    /**
     * Updates the primary workspace text field shown at the top of the forms page.
     *
     * @param event Supplies the current text input change event.
     */
    function handleWorkspaceNameChanged(event: React.ChangeEvent<HTMLInputElement>): void {
        // Store the raw text value directly so reviewers can test how inline validation reacts to partial edits.
        updateFormValue('workspaceName', event.target.value);
    }

    /**
     * Updates the multiline notes field shown in the broader form section.
     *
     * @param event Supplies the current textarea change event.
     */
    function handleReleaseNotesChanged(event: React.ChangeEvent<HTMLTextAreaElement>): void {
        // Preserve the rest of the controlled form state while replacing only the explanatory notes value.
        updateFormValue('releaseNotes', event.target.value);
    }

    /**
     * Updates the segmented review-lane selection shown by the forms page.
     *
     * @param event Supplies the PrimeReact selection change event.
     */
    function handleReviewModeChanged(event: SelectButtonChangeEvent): void {
        if (!event.value) {
            // Ignore empty events so the segmented control always keeps one explicit selection.
            return;
        }

        // Switch the current lane while also clearing the publish request when the expedite-only path becomes unavailable.
        setFormValues(currentValues => ({
            ...currentValues,
            reviewMode: event.value as SearchStudioPrimeReactFormsDemoValues['reviewMode'],
            requestPublish: event.value === 'expedite' ? currentValues.requestPublish : false
        }));
        setLastActionSummary(`Review mode switched to ${String(event.value)}.`);
    }

    /**
     * Updates the current radio-button source selection.
     *
     * @param event Supplies the PrimeReact radio-button change event.
     */
    function handleSourceProfileChanged(event: RadioButtonChangeEvent): void {
        // Store the selected radio value directly because the option values already match the page-owned source union.
        updateFormValue('sourceProfile', event.value as SearchStudioPrimeReactFormsDemoValues['sourceProfile']);
    }

    /**
     * Toggles one grouped capability checkbox inside the controlled form state.
     *
     * @param event Supplies the PrimeReact checkbox change event.
     */
    function handleCapabilityChanged(event: CheckboxChangeEvent): void {
        const capabilityValue = String(event.value);

        // Add or remove the selected capability so the page demonstrates grouped multi-selection without any backend dependency.
        setFormValues(currentValues => {
            const selectedCapabilities = event.checked
                ? [...currentValues.selectedCapabilities, capabilityValue]
                : currentValues.selectedCapabilities.filter(value => value !== capabilityValue);

            return {
                ...currentValues,
                selectedCapabilities
            };
        });
    }

    /**
     * Updates the numeric batch-size control.
     *
     * @param event Supplies the PrimeReact numeric input change event.
     */
    function handleBatchSizeChanged(event: InputNumberValueChangeEvent): void {
        // Fall back to zero when PrimeReact clears the numeric input so validation can surface the issue explicitly.
        updateFormValue('batchSize', event.value ?? 0);
    }

    /**
     * Updates the slider-backed quality threshold.
     *
     * @param event Supplies the PrimeReact slider change event.
     */
    function handleQualityThresholdChanged(event: SliderChangeEvent): void {
        // Use only single numeric slider values because the demo focuses on one review threshold rather than a range control.
        updateFormValue('qualityThreshold', Number(event.value));
    }

    /**
     * Updates the notification switch shown in the workflow options section.
     *
     * @param event Supplies the PrimeReact switch change event.
     */
    function handleSendNotificationsChanged(event: InputSwitchChangeEvent): void {
        // Store the current boolean value directly so the page can demonstrate cross-field validation in the expedite path.
        updateFormValue('sendNotifications', Boolean(event.value));
    }

    /**
     * Updates the expedite-only publish request checkbox.
     *
     * @param event Supplies the PrimeReact checkbox change event.
     */
    function handleRequestPublishChanged(event: CheckboxChangeEvent): void {
        // Store the publish request only when the expedite lane is active so the page shows a clear disabled state elsewhere.
        updateFormValue('requestPublish', Boolean(event.checked));
    }

    /**
     * Starts the mock save flow or surfaces inline validation feedback when the form is incomplete.
     */
    function handleMockSave(): void {
        setHasAttemptedSubmit(true);

        if (validationCount > 0) {
            // Stop before the mock save begins so the page can focus attention on the inline guidance messages.
            setSubmissionState('idle');
            setLastActionSummary(`Blocked the mock save because ${validationCount} inline validation message(s) are visible.`);
            return;
        }

        if (saveTimerRef.current) {
            // Clear any previous timer so repeated saves keep only one pending completion callback.
            window.clearTimeout(saveTimerRef.current);
        }

        // Simulate a short async save so the page can demonstrate loading-state buttons without depending on a backend API.
        setSubmissionState('saving');
        setLastActionSummary('Running a mock save so reviewers can inspect PrimeReact loading-button behavior.');
        saveTimerRef.current = window.setTimeout(() => {
            setSubmissionState('saved');
            setLastActionSummary('Completed the mock save. All actions on this page remain local and disposable.');
        }, 900);
    }

    /**
     * Forces the inline validation messages to become visible without changing the underlying data.
     */
    function handlePreviewValidation(): void {
        // Reveal the current validation messages explicitly so reviewers can inspect inline help without starting the mock save flow.
        setHasAttemptedSubmit(true);
        setLastActionSummary(`Surfaced ${validationCount} inline validation message(s) for review.`);
    }

    /**
     * Restores the original generated values and clears transient form state.
     */
    function handleResetForm(): void {
        if (saveTimerRef.current) {
            // Cancel any pending save callback so the reset returns the page to a clean deterministic state.
            window.clearTimeout(saveTimerRef.current);
            saveTimerRef.current = undefined;
        }

        // Restore the deterministic initial values so reviewers can compare the form from a consistent starting point.
        setFormValues(initialValues);
        setHasAttemptedSubmit(false);
        setSubmissionState('idle');
        setLastActionSummary('Reset the forms demo to its original mock values.');
    }

    // Render the broader forms surface using full styled PrimeReact so reviewers can compare controlled inputs, validation, and boolean state in-context.
    return (
        <div className="search-studio-primereact-demo-page search-studio-primereact-demo-page--styled">
            <header className="search-studio-primereact-demo-page__hero">
                <div className="search-studio-primereact-demo-page__hero-copy">
                    <div className="search-studio-primereact-demo-page__hero-heading-row">
                        <h1 className="search-studio-primereact-demo-page__title">PrimeReact forms demo</h1>
                        <Tag value="Controlled + validated" severity="success" rounded className="search-studio-primereact-demo-page__mode-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__summary">
                        This temporary page evaluates a broader set of PrimeReact form controls inside the Studio Theia shell, including controlled
                        inputs, inline validation, grouped selection controls, disabled states, and a lightweight mock save flow.
                    </p>
                    <div className="search-studio-primereact-demo-page__theme-row">
                        <span className="search-studio-primereact-demo-page__theme-label">Styled theme sync</span>
                        <strong className="search-studio-primereact-demo-page__theme-value">{getThemeLabel(props.activeThemeVariant)}</strong>
                    </div>
                </div>
                <div className="search-studio-primereact-demo-page__mode-card">
                    <p className="search-studio-primereact-demo-page__toggle-help">
                        All business behavior remains mock-only. The page exists to compare input spacing, inline feedback, and state transitions
                        rather than to model a production workflow.
                    </p>
                    <Tag value={`${formValues.selectedCapabilities.length} grouped selections`} severity="info" rounded />
                </div>
            </header>

            <section className="search-studio-primereact-demo-page__metrics-grid" aria-label="PrimeReact forms metrics">
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Validation messages</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{validationCount}</strong>
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Review mode</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{formValues.reviewMode}</strong>
                </article>
                <article className="search-studio-primereact-demo-page__metric-card">
                    <span className="search-studio-primereact-demo-page__metric-label">Mock save state</span>
                    <strong className="search-studio-primereact-demo-page__metric-value">{submissionState}</strong>
                </article>
            </section>

            <section className="search-studio-primereact-demo-page__content-grid">
                <article className="search-studio-primereact-demo-page__surface">
                    <div className="search-studio-primereact-demo-page__section-heading-row">
                        <h2 className="search-studio-primereact-demo-page__section-title">Controlled form inputs</h2>
                        <Tag value="Mock only" severity="warning" rounded className="search-studio-primereact-demo-page__section-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        Review how broad PrimeReact form controls align on one spacious page, including text, numeric, grouped selection, toggle,
                        slider, and disabled-state combinations.
                    </p>
                    <div className="search-studio-primereact-demo-page__field-grid search-studio-primereact-demo-page__field-grid--wide">
                        <div className="search-studio-primereact-demo-page__field-group">
                            <label htmlFor="search-studio-primereact-forms-workspace-name" className="search-studio-primereact-demo-page__field-label">
                                Workspace name
                            </label>
                            <InputText
                                id="search-studio-primereact-forms-workspace-name"
                                value={formValues.workspaceName}
                                onChange={handleWorkspaceNameChanged}
                                invalid={Boolean(visibleValidation.workspaceName)}
                                className="search-studio-primereact-demo-page__input"
                            />
                            {visibleValidation.workspaceName ? (
                                <small className="search-studio-primereact-demo-page__validation-text">{visibleValidation.workspaceName}</small>
                            ) : null}
                        </div>
                        <div className="search-studio-primereact-demo-page__field-group">
                            <label htmlFor="search-studio-primereact-forms-access-code" className="search-studio-primereact-demo-page__field-label">
                                Mock access code
                            </label>
                            <Password
                                id="search-studio-primereact-forms-access-code"
                                value={`lane-${formValues.reviewMode}`}
                                onChange={() => {
                                    // Keep the password input read-only by ignoring edits while still rendering a representative secure field.
                                }}
                                feedback={false}
                                toggleMask
                                disabled
                                inputClassName="search-studio-primereact-demo-page__input"
                            />
                            <small className="search-studio-primereact-demo-page__helper-text">Disabled intentionally to show a representative locked field.</small>
                        </div>
                        <div className="search-studio-primereact-demo-page__field-group search-studio-primereact-demo-page__field-group--full-width">
                            <label htmlFor="search-studio-primereact-forms-release-notes" className="search-studio-primereact-demo-page__field-label">
                                Review notes
                            </label>
                            <InputTextarea
                                id="search-studio-primereact-forms-release-notes"
                                value={formValues.releaseNotes}
                                onChange={handleReleaseNotesChanged}
                                autoResize
                                rows={4}
                                invalid={Boolean(visibleValidation.releaseNotes)}
                            />
                            {visibleValidation.releaseNotes ? (
                                <small className="search-studio-primereact-demo-page__validation-text">{visibleValidation.releaseNotes}</small>
                            ) : null}
                        </div>
                        <div className="search-studio-primereact-demo-page__field-group search-studio-primereact-demo-page__field-group--full-width">
                            <span className="search-studio-primereact-demo-page__field-label">Review mode</span>
                            <SelectButton
                                value={formValues.reviewMode}
                                options={reviewModeOptions}
                                optionLabel="label"
                                optionValue="value"
                                onChange={handleReviewModeChanged}
                                className="search-studio-primereact-demo-page__select-button"
                            />
                            <small className="search-studio-primereact-demo-page__helper-text">The expedite mode unlocks an additional publish request checkbox below.</small>
                        </div>
                        <div className="search-studio-primereact-demo-page__field-group search-studio-primereact-demo-page__field-group--full-width">
                            <span className="search-studio-primereact-demo-page__field-label">Source profile</span>
                            <div className="search-studio-primereact-demo-page__choice-grid">
                                {sourceProfileOptions.map(option => (
                                    <label key={option.value} className="search-studio-primereact-demo-page__choice-card">
                                        <RadioButton
                                            inputId={`search-studio-primereact-source-${option.value}`}
                                            name="search-studio-primereact-source-profile"
                                            value={option.value}
                                            onChange={handleSourceProfileChanged}
                                            checked={formValues.sourceProfile === option.value}
                                        />
                                        <span>{option.label}</span>
                                    </label>
                                ))}
                            </div>
                        </div>
                        <div className="search-studio-primereact-demo-page__field-group search-studio-primereact-demo-page__field-group--full-width">
                            <span className="search-studio-primereact-demo-page__field-label">Capabilities</span>
                            <div className="search-studio-primereact-demo-page__choice-stack">
                                {capabilityOptions.map(option => (
                                    <label key={option.value} className="search-studio-primereact-demo-page__checkbox-card">
                                        <Checkbox
                                            inputId={`search-studio-primereact-capability-${option.value}`}
                                            value={option.value}
                                            onChange={handleCapabilityChanged}
                                            checked={formValues.selectedCapabilities.includes(option.value)}
                                        />
                                        <div className="search-studio-primereact-demo-page__checkbox-copy">
                                            <span>{option.label}</span>
                                            <small className="search-studio-primereact-demo-page__helper-text">{option.summary}</small>
                                        </div>
                                    </label>
                                ))}
                            </div>
                            {visibleValidation.selectedCapabilities ? (
                                <small className="search-studio-primereact-demo-page__validation-text">{visibleValidation.selectedCapabilities}</small>
                            ) : null}
                        </div>
                    </div>
                </article>

                <article className="search-studio-primereact-demo-page__surface">
                    <div className="search-studio-primereact-demo-page__section-heading-row">
                        <h2 className="search-studio-primereact-demo-page__section-title">Inline feedback and workflow options</h2>
                        <Tag value="Validation + disabled" severity="info" rounded className="search-studio-primereact-demo-page__section-tag" />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">
                        This side panel keeps the UX review focused on input grouping, inline feedback, boolean state, and lightweight loading behavior.
                    </p>
                    <div className="search-studio-primereact-demo-page__field-group">
                        <label htmlFor="search-studio-primereact-forms-batch-size" className="search-studio-primereact-demo-page__field-label">
                            Batch size
                        </label>
                        <InputNumber
                            id="search-studio-primereact-forms-batch-size"
                            value={formValues.batchSize}
                            onValueChange={handleBatchSizeChanged}
                            showButtons
                            min={0}
                            max={600}
                            invalid={Boolean(visibleValidation.batchSize)}
                        />
                        {visibleValidation.batchSize ? (
                            <small className="search-studio-primereact-demo-page__validation-text">{visibleValidation.batchSize}</small>
                        ) : null}
                    </div>
                    <div className="search-studio-primereact-demo-page__field-group">
                        <span className="search-studio-primereact-demo-page__field-label">Quality threshold</span>
                        <Slider value={formValues.qualityThreshold} onChange={handleQualityThresholdChanged} min={0} max={100} />
                        <div className="search-studio-primereact-demo-page__theme-row">
                            <span className="search-studio-primereact-demo-page__helper-text">Current threshold</span>
                            <strong>{formValues.qualityThreshold}%</strong>
                        </div>
                        {visibleValidation.qualityThreshold ? (
                            <small className="search-studio-primereact-demo-page__validation-text">{visibleValidation.qualityThreshold}</small>
                        ) : null}
                    </div>
                    <Divider />
                    <div className="search-studio-primereact-demo-page__choice-stack">
                        <div className="search-studio-primereact-demo-page__toggle-row search-studio-primereact-demo-page__toggle-row--surface">
                            <div className="search-studio-primereact-demo-page__toggle-copy">
                                <span className="search-studio-primereact-demo-page__field-label">Send notifications</span>
                                <small className="search-studio-primereact-demo-page__helper-text">Required automatically when the expedite lane is selected.</small>
                            </div>
                            <InputSwitch checked={formValues.sendNotifications} onChange={handleSendNotificationsChanged} />
                        </div>
                        {visibleValidation.sendNotifications ? (
                            <small className="search-studio-primereact-demo-page__validation-text">{visibleValidation.sendNotifications}</small>
                        ) : null}
                        <label className={`search-studio-primereact-demo-page__checkbox-card ${!canRequestPublish ? 'search-studio-primereact-demo-page__checkbox-card--disabled' : ''}`}>
                            <Checkbox
                                inputId="search-studio-primereact-request-publish"
                                value="publish"
                                onChange={handleRequestPublishChanged}
                                checked={formValues.requestPublish}
                                disabled={!canRequestPublish}
                            />
                            <div className="search-studio-primereact-demo-page__checkbox-copy">
                                <span>Request publish path</span>
                                <small className="search-studio-primereact-demo-page__helper-text">
                                    {canRequestPublish ? 'Enabled only for the expedite lane to demonstrate a conditional enabled state.' : 'Disabled until the expedite lane is selected.'}
                                </small>
                            </div>
                        </label>
                    </div>
                    <Divider />
                    <div className="search-studio-primereact-demo-page__callout-row">
                        <Tag value={`${validationCount} validation`} severity={validationCount > 0 ? 'warning' : 'success'} rounded />
                        <Tag value={`${formValues.selectedCapabilities.length} selected`} severity="info" rounded />
                        <Tag value={formValues.requestPublish ? 'Publish requested' : 'Publish disabled'} severity="contrast" rounded />
                    </div>
                    <p className="search-studio-primereact-demo-page__section-summary">{lastActionSummary}</p>
                    <div className="search-studio-primereact-demo-page__action-row search-studio-primereact-demo-page__action-row--start">
                        <Button label="Mock save" icon="pi pi-save" onClick={handleMockSave} loading={submissionState === 'saving'} />
                        <Button label="Preview validation" icon="pi pi-exclamation-circle" severity="secondary" onClick={handlePreviewValidation} />
                        <Button label="Reset form" icon="pi pi-refresh" text severity="secondary" onClick={handleResetForm} />
                    </div>
                </article>
            </section>
        </div>
    );
}
