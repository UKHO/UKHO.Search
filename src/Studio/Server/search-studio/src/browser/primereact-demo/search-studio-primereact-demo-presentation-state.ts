export type SearchStudioPrimeReactDemoThemeVariant = 'light' | 'dark';

/**
 * Stores the per-widget presentation state for the temporary PrimeReact research page.
 */
export class SearchStudioPrimeReactDemoPresentationState {
    /**
     * Tracks which PrimeReact stock theme variant matches the active Theia workbench theme.
     */
    protected activeThemeVariant: SearchStudioPrimeReactDemoThemeVariant = 'dark';

    /**
     * Creates the presentation state using the temporary demo defaults.
     */
    constructor() {
        // Start from the current dark-theme shell baseline until Theia body classes are inspected after the widget attaches.
    }

    /**
     * Gets the current Theia-aligned theme variant used when styled mode is enabled.
     *
     * @returns The active PrimeReact theme variant that matches the current Theia theme.
     */
    getActiveThemeVariant(): SearchStudioPrimeReactDemoThemeVariant {
        // Return the cached theme variant so the widget can display the current mapping and reapply styles when needed.
        return this.activeThemeVariant;
    }

    /**
     * Maps the current Theia body classes to the PrimeReact light or dark theme variant used by the demo.
     *
     * @param bodyClassNames Supplies the active body class names that Theia applies for the current workbench theme.
     * @returns The detected PrimeReact theme variant that should be used for styled mode.
     */
    synchronizeThemeVariant(bodyClassNames: Iterable<string>): SearchStudioPrimeReactDemoThemeVariant {
        // Default to the dark theme because the current Studio browser-app composition starts in the dark Theia theme.
        let nextThemeVariant: SearchStudioPrimeReactDemoThemeVariant = 'dark';

        // Inspect the live body classes rather than storing a separate theme preference so the demo follows actual Theia theme changes.
        for (const className of bodyClassNames) {
            if (className === 'theia-light') {
                nextThemeVariant = 'light';
                break;
            }
        }

        // Cache the resolved variant so the widget can reapply the correct stock PrimeReact theme when styled mode is active.
        this.activeThemeVariant = nextThemeVariant;
        return this.activeThemeVariant;
    }
}
