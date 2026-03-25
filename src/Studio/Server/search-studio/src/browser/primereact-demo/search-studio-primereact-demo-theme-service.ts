import { injectable } from '@theia/core/shared/inversify';
import { SearchStudioPrimeReactDemoThemeVariant } from './search-studio-primereact-demo-presentation-state';

const PrimeReactVersion = '10.9.7';
const PrimeIconsVersion = '7.0.0';
const PrimeReactCoreStylesheetId = 'search-studio-primereact-core-stylesheet';
const PrimeReactThemeStylesheetId = 'search-studio-primereact-theme-stylesheet';
const PrimeIconsStylesheetId = 'search-studio-primereact-icons-stylesheet';
const TemporaryDemoStylesheetAttribute = 'data-search-studio-temporary';
const PrimeReactCoreStylesheetUrl = `https://cdn.jsdelivr.net/npm/primereact@${PrimeReactVersion}/resources/primereact.min.css`;
const PrimeIconsStylesheetUrl = `https://cdn.jsdelivr.net/npm/primeicons@${PrimeIconsVersion}/primeicons.css`;

/**
 * Manages the temporary PrimeReact stock stylesheets used by the research demo page.
 */
@injectable()
export class SearchStudioPrimeReactDemoThemeService {

    /**
     * Enables PrimeReact styled mode by attaching the required stock PrimeReact stylesheets.
     *
     * @param themeVariant Identifies which stock PrimeReact light or dark theme should match the active Theia theme.
     * @returns A promise that completes when the requested theme stylesheet has loaded or failed.
     */
    async enableStyledMode(themeVariant: SearchStudioPrimeReactDemoThemeVariant): Promise<void> {
        const headElement = document.head;

        if (!headElement) {
            // Fail loudly when the browser head is unavailable because the temporary research page cannot attach PrimeReact styles without it.
            throw new Error('PrimeReact styled mode cannot be enabled because the document head is unavailable.');
        }

        // Ensure the shared PrimeReact core styles are present before loading the theme-specific stylesheet.
        this.ensureStylesheet(headElement, PrimeReactCoreStylesheetId, PrimeReactCoreStylesheetUrl);

        // Ensure the icon stylesheet is present so PrimeReact button/icon affordances render correctly during styled-mode review.
        this.ensureStylesheet(headElement, PrimeIconsStylesheetId, PrimeIconsStylesheetUrl);

        // Swap the theme stylesheet to the variant that matches the current Theia light or dark workbench theme.
        await this.ensureStylesheet(headElement, PrimeReactThemeStylesheetId, this.getThemeStylesheetUrl(themeVariant), true);
        console.info('Enabled PrimeReact styled mode for the temporary demo.', { themeVariant });
    }

    /**
     * Disables PrimeReact styled mode by removing the temporary stock stylesheets from the document head.
     */
    disableStyledMode(): void {
        // Remove the theme stylesheet first so the temporary page immediately falls back to the local unstyled presentation.
        this.removeStylesheet(PrimeReactThemeStylesheetId);

        // Remove the shared PrimeReact core stylesheet so no styled-mode rules leak into the rest of the shell after the demo is closed or toggled off.
        this.removeStylesheet(PrimeReactCoreStylesheetId);

        // Remove the icon stylesheet as part of the same cleanup because the temporary demo is the only page currently using it.
        this.removeStylesheet(PrimeIconsStylesheetId);
        console.info('Disabled PrimeReact styled mode for the temporary demo.');
    }

    /**
     * Resolves the stock PrimeReact theme stylesheet URL that matches the current Theia theme variant.
     *
     * @param themeVariant Identifies whether the temporary demo should use the stock light or dark PrimeReact theme.
     * @returns The CDN stylesheet URL for the requested stock PrimeReact theme.
     */
    protected getThemeStylesheetUrl(themeVariant: SearchStudioPrimeReactDemoThemeVariant): string {
        // Map directly to the stock Lara light and dark themes so reviewers see an uncustomized PrimeReact baseline.
        return themeVariant === 'light'
            ? `https://cdn.jsdelivr.net/npm/primereact@${PrimeReactVersion}/resources/themes/lara-light-blue/theme.css`
            : `https://cdn.jsdelivr.net/npm/primereact@${PrimeReactVersion}/resources/themes/lara-dark-blue/theme.css`;
    }

    /**
     * Creates or updates a stylesheet link element in the current document head.
     *
     * @param headElement Supplies the document head that hosts the temporary stylesheet links.
     * @param stylesheetId Identifies the stylesheet link element so it can be updated or removed later.
     * @param stylesheetUrl Supplies the stylesheet URL that should be applied to the link element.
     * @param waitForLoad Indicates whether the caller needs to await the browser load result for the stylesheet.
     * @returns A promise that completes once the stylesheet load has finished when `waitForLoad` is `true`; otherwise, a resolved promise.
     */
    protected ensureStylesheet(
        headElement: HTMLHeadElement,
        stylesheetId: string,
        stylesheetUrl: string,
        waitForLoad = false
    ): Promise<void> {
        const existingStylesheet = document.getElementById(stylesheetId) as HTMLLinkElement | null;

        if (existingStylesheet) {
            // When the stylesheet is already present with the same URL, avoid unnecessary DOM churn and network work.
            if (existingStylesheet.href === stylesheetUrl) {
                return Promise.resolve();
            }

            // Repoint the existing temporary stylesheet link so the browser can switch between the light and dark stock PrimeReact themes.
            return this.updateStylesheet(existingStylesheet, stylesheetUrl, waitForLoad);
        }

        // Create the temporary stylesheet link lazily so the shell does not carry PrimeReact styled assets until the demo actually needs them.
        const stylesheet = document.createElement('link');
        stylesheet.id = stylesheetId;
        stylesheet.rel = 'stylesheet';
        stylesheet.setAttribute(TemporaryDemoStylesheetAttribute, 'true');
        headElement.appendChild(stylesheet);

        // Apply the URL after appending so the same load logic can be reused for both new and existing stylesheets.
        return this.updateStylesheet(stylesheet, stylesheetUrl, waitForLoad);
    }

    /**
     * Updates a stylesheet link URL and optionally waits for the browser load result.
     *
     * @param stylesheet Supplies the existing stylesheet link element to update.
     * @param stylesheetUrl Supplies the new stylesheet URL that should be requested.
     * @param waitForLoad Indicates whether the caller needs to await the browser load result for the stylesheet.
     * @returns A promise that completes once the stylesheet has been updated and, when requested, loaded.
     */
    protected updateStylesheet(
        stylesheet: HTMLLinkElement,
        stylesheetUrl: string,
        waitForLoad: boolean
    ): Promise<void> {
        if (!waitForLoad) {
            // For non-blocking stylesheet updates, set the new URL immediately and let the browser fetch it in the background.
            stylesheet.href = stylesheetUrl;
            return Promise.resolve();
        }

        // When the caller needs a reliable completion signal, hook the load and error callbacks around the href update.
        return new Promise((resolve, reject) => {
            stylesheet.onload = () => {
                resolve();
            };
            stylesheet.onerror = () => {
                reject(new Error(`PrimeReact theme stylesheet failed to load: ${stylesheetUrl}`));
            };
            stylesheet.href = stylesheetUrl;
        });
    }

    /**
     * Removes a temporary PrimeReact stylesheet link when it exists.
     *
     * @param stylesheetId Identifies the stylesheet link element that should be removed from the document head.
     */
    protected removeStylesheet(stylesheetId: string): void {
        const stylesheet = document.getElementById(stylesheetId);

        if (!stylesheet) {
            // Ignore missing stylesheets because the temporary demo may be cleaning up after a failed or partial attach sequence.
            return;
        }

        // Remove the existing temporary stylesheet so the shell returns to its normal non-PrimeReact state when styled mode is off.
        stylesheet.remove();
    }
}
