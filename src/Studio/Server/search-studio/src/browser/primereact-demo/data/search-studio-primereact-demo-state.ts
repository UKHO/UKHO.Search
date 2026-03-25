import { SearchStudioPrimeReactDemoTreeNode } from './search-studio-primereact-demo-data';

/**
 * Identifies the high-level demo scenario currently shown by a temporary data-heavy PrimeReact page.
 */
export type SearchStudioPrimeReactDemoScenario = 'ready' | 'loading' | 'empty';

/**
 * Describes the PrimeReact-compatible expansion keys used by the tree-shaped demo pages.
 */
export type SearchStudioPrimeReactDemoExpandedKeys = Record<string, boolean>;

/**
 * Describes the supported value shapes returned by PrimeReact checkbox-tree selection APIs.
 */
export type SearchStudioPrimeReactDemoTreeSelectionValue = boolean | {
    /**
     * Indicates that the node is fully selected.
     */
    readonly checked?: boolean;

    /**
     * Indicates that the node is partially selected because only descendants are selected.
     */
    readonly partialChecked?: boolean;
};

/**
 * Describes the checkbox selection dictionary used by the tree-shaped demo pages.
 */
export type SearchStudioPrimeReactDemoTreeSelectionKeys = Record<string, SearchStudioPrimeReactDemoTreeSelectionValue>;

/**
 * Describes the resolved state that page components consume after applying a high-level scenario mode.
 */
export interface SearchStudioPrimeReactDemoScenarioSnapshot<TItem> {
    /**
     * Stores the items that should be rendered for the current scenario.
     */
    readonly items: ReadonlyArray<TItem>;

    /**
     * Stores whether the current scenario should surface PrimeReact loading affordances.
     */
    readonly isLoading: boolean;

    /**
     * Stores whether the current scenario intentionally renders zero items.
     */
    readonly isEmpty: boolean;
}

/**
 * Describes the controlled values rendered on the temporary PrimeReact forms demo page.
 */
export interface SearchStudioPrimeReactFormsDemoValues {
    /**
     * Stores the workspace name shown in the primary text field.
     */
    readonly workspaceName: string;

    /**
     * Stores the summary or note text shown in the larger multiline field.
     */
    readonly releaseNotes: string;

    /**
     * Stores the selected mock review lane shown by the segmented button group.
     */
    readonly reviewMode: 'guided' | 'audit' | 'expedite';

    /**
     * Stores the selected data source shown by the radio-button group.
     */
    readonly sourceProfile: 'provider' | 'catalogue' | 'operations';

    /**
     * Stores the selected capability values shown by the checkbox group.
     */
    readonly selectedCapabilities: ReadonlyArray<string>;

    /**
     * Stores the batch size used by the numeric input.
     */
    readonly batchSize: number;

    /**
     * Stores the threshold used by the slider summary.
     */
    readonly qualityThreshold: number;

    /**
     * Stores whether the mock workflow should surface notification state.
     */
    readonly sendNotifications: boolean;

    /**
     * Stores whether the mock publish path should be highlighted.
     */
    readonly requestPublish: boolean;
}

/**
 * Describes the inline validation messages rendered by the temporary PrimeReact forms demo page.
 */
export interface SearchStudioPrimeReactFormsDemoValidation {
    /**
     * Stores the validation message shown beneath the workspace field when needed.
     */
    workspaceName?: string;

    /**
     * Stores the validation message shown beneath the notes field when needed.
     */
    releaseNotes?: string;

    /**
     * Stores the validation message shown beneath the grouped checkbox section when needed.
     */
    selectedCapabilities?: string;

    /**
     * Stores the validation message shown beneath the numeric input when needed.
     */
    batchSize?: string;

    /**
     * Stores the validation message shown beside the slider summary when needed.
     */
    qualityThreshold?: string;

    /**
     * Stores the validation message shown beside the notification switch when needed.
     */
    sendNotifications?: string;
}

/**
 * Creates the initial controlled values used by the temporary PrimeReact forms demo page.
 *
 * @returns The deterministic initial form values used when the page first opens or resets.
 */
export function createSearchStudioPrimeReactFormsDemoInitialValues(): SearchStudioPrimeReactFormsDemoValues {
    // Centralize the initial values so the page and tests stay aligned when the forms demo evolves.
    return {
        workspaceName: 'North Sea workspace review',
        releaseNotes: 'Review the styled PrimeReact form spacing, labels, and inline validation flow.',
        reviewMode: 'guided',
        sourceProfile: 'provider',
        selectedCapabilities: ['quality', 'selection'],
        batchSize: 120,
        qualityThreshold: 72,
        sendNotifications: true,
        requestPublish: false
    };
}

/**
 * Validates the controlled values used by the temporary PrimeReact forms demo page.
 *
 * @param values Supplies the current controlled form values.
 * @returns The inline validation messages that should be rendered beside the relevant controls.
 */
export function validateSearchStudioPrimeReactFormsDemoValues(
    values: SearchStudioPrimeReactFormsDemoValues
): SearchStudioPrimeReactFormsDemoValidation {
    const validation: SearchStudioPrimeReactFormsDemoValidation = {};

    // Validate the text fields first so the page always surfaces the most obvious required guidance near the edited inputs.
    if (values.workspaceName.trim().length < 4) {
        validation.workspaceName = 'Enter a workspace name with at least four characters.';
    }

    if (values.releaseNotes.trim().length < 12) {
        validation.releaseNotes = 'Add at least twelve characters so the inline help reads like a realistic review note.';
    }

    // Require at least one grouped selection so the forms demo can show checkbox-level validation feedback.
    if (values.selectedCapabilities.length === 0) {
        validation.selectedCapabilities = 'Select at least one capability so the forms demo shows grouped checkbox feedback.';
    }

    // Keep the numeric control inside a realistic mock range so the inline feedback stays easy to understand.
    if (values.batchSize < 10 || values.batchSize > 500) {
        validation.batchSize = 'Choose a batch size between 10 and 500 items.';
    }

    if (values.qualityThreshold < 30 || values.qualityThreshold > 95) {
        validation.qualityThreshold = 'Keep the quality threshold between 30 and 95 for the mock review lane.';
    }

    // Require notifications for the expedite lane so the form demonstrates cross-field validation without calling any backend APIs.
    if (values.reviewMode === 'expedite' && !values.sendNotifications) {
        validation.sendNotifications = 'Enable notifications before running the expedite review path.';
    }

    return validation;
}

/**
 * Applies a high-level demo scenario to a supplied dataset.
 *
 * @param items Supplies the base dataset that would normally be rendered by the page.
 * @param scenario Identifies whether the page should render the normal, loading, or empty state.
 * @returns The resolved snapshot used by the page component.
 */
export function createScenarioSnapshot<TItem>(
    items: ReadonlyArray<TItem>,
    scenario: SearchStudioPrimeReactDemoScenario
): SearchStudioPrimeReactDemoScenarioSnapshot<TItem> {
    // Keep the scenario handling centralized so each demo page can show the same ready, loading, and empty patterns consistently.
    return {
        items: scenario === 'empty' ? [] : items,
        isLoading: scenario === 'loading',
        isEmpty: scenario === 'empty'
    };
}

/**
 * Expands every node that contains children so tree-based demos can offer a one-click expand-all action.
 *
 * @param nodes Supplies the current tree-shaped dataset.
 * @returns The expansion-key dictionary expected by PrimeReact tree components.
 */
export function createExpandedTreeKeys(
    nodes: ReadonlyArray<SearchStudioPrimeReactDemoTreeNode>
): SearchStudioPrimeReactDemoExpandedKeys {
    const expandedKeys: SearchStudioPrimeReactDemoExpandedKeys = {};

    // Walk the full hierarchy once so the page can switch between collapsed and fully expanded review states instantly.
    collectExpandedTreeKeys(nodes, expandedKeys);
    return expandedKeys;
}

/**
 * Counts how many tree nodes are fully selected in the PrimeReact checkbox selection dictionary.
 *
 * @param selectionKeys Supplies the current checkbox-tree selection dictionary when one exists.
 * @returns The number of fully selected keys.
 */
export function countSelectedTreeKeys(
    selectionKeys: SearchStudioPrimeReactDemoTreeSelectionKeys | undefined
): number {
    if (!selectionKeys) {
        // Treat missing selection state as no active selection so action buttons can disable cleanly.
        return 0;
    }

    let selectedCount = 0;

    // Count only fully selected entries so partial parent selection does not inflate the visible summary metrics.
    for (const selectionValue of Object.values(selectionKeys)) {
        if (selectionValue === true) {
            selectedCount += 1;
            continue;
        }

        if (typeof selectionValue === 'object' && selectionValue.checked) {
            selectedCount += 1;
        }
    }

    return selectedCount;
}

/**
 * Recursively records expansion keys for all non-leaf nodes in the supplied hierarchy.
 *
 * @param nodes Supplies the current tree-shaped dataset.
 * @param expandedKeys Receives the collected expansion keys.
 */
function collectExpandedTreeKeys(
    nodes: ReadonlyArray<SearchStudioPrimeReactDemoTreeNode>,
    expandedKeys: SearchStudioPrimeReactDemoExpandedKeys
): void {
    // Visit each node exactly once so the generated expansion dictionary stays deterministic and inexpensive.
    for (const node of nodes) {
        if (!node.children || node.children.length === 0) {
            continue;
        }

        expandedKeys[node.key] = true;
        collectExpandedTreeKeys(node.children, expandedKeys);
    }
}
