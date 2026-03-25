import { SearchStudioPrimeReactDemoThemeVariant } from './search-studio-primereact-demo-presentation-state';

/**
 * Describes the immutable configuration supplied by the hosting widget to each temporary PrimeReact demo page.
 */
export interface SearchStudioPrimeReactDemoPageProps {
    /**
     * Identifies the active stock PrimeReact theme variant that matches the current Theia theme.
     */
    readonly activeThemeVariant: SearchStudioPrimeReactDemoThemeVariant;
}
