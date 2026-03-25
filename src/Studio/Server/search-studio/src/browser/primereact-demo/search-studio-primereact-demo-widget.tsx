import * as React from '@theia/core/shared/react';
import { injectable, inject } from '@theia/core/shared/inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { Message } from '@lumino/messaging';
import {
    getSearchStudioPrimeReactDemoPageDefinition,
    SearchStudioPrimeReactDemoWidgetIconClass,
    SearchStudioPrimeReactDemoPageId,
    SearchStudioPrimeReactDemoWidgetId,
    SearchStudioPrimeReactDemoWidgetLabel
} from './search-studio-primereact-demo-constants';
import type { SearchStudioPrimeReactDemoPageProps } from './search-studio-primereact-demo-page-props';
import { SearchStudioPrimeReactDemoPresentationState } from './search-studio-primereact-demo-presentation-state';
import { SearchStudioPrimeReactDemoThemeService } from './search-studio-primereact-demo-theme-service';
import './search-studio-primereact-demo-widget.css';

/**
 * Describes the serializable Theia layout state persisted for the temporary PrimeReact demo widget.
 */
interface SearchStudioPrimeReactDemoWidgetState {
    /**
     * Stores the logical PrimeReact demo page that should be restored when Theia reopens the widget from layout state.
     */
    readonly activeDemoPageId: SearchStudioPrimeReactDemoPageId;
}

/**
 * Hosts the temporary PrimeReact research pages inside one shared normal closable Theia document tab.
 */
@injectable()
export class SearchStudioPrimeReactDemoWidget extends ReactWidget {
    /**
     * Tracks which logical PrimeReact demo page is currently shown inside the shared temporary widget.
     */
    protected activeDemoPageId: SearchStudioPrimeReactDemoPageId = 'bootstrap';

    /**
     * Stores the per-widget styled and theme-following state for the temporary PrimeReact demo page.
     */
    protected readonly presentationState = new SearchStudioPrimeReactDemoPresentationState();

    /**
     * Stores the optional body-class observer used to follow Theia light and dark theme changes at runtime.
     */
    protected themeObserver: MutationObserver | undefined;

    /**
     * Creates the temporary PrimeReact demo widget and configures it as a normal closable document tab.
     *
     * @param themeService Manages the temporary PrimeReact stylesheets required when the page enters styled mode.
     */
    constructor(
        @inject(SearchStudioPrimeReactDemoThemeService)
        protected readonly themeService: SearchStudioPrimeReactDemoThemeService
    ) {
        super();

        // Configure the temporary research page using its own stable workbench identifiers so it can be reopened consistently from View.
        this.id = SearchStudioPrimeReactDemoWidgetId;
        this.title.label = SearchStudioPrimeReactDemoWidgetLabel;
        this.title.caption = 'Temporary PrimeReact evaluation demo';
        this.title.iconClass = SearchStudioPrimeReactDemoWidgetIconClass;
        this.title.closable = true;
        this.addClass('search-studio-primereact-demo-widget');

        // Request the first render immediately so restored workbench tabs also repaint their React content without waiting for a manual page switch.
        this.update();
    }

    /**
     * Switches the shared temporary widget to a different logical PrimeReact demo page.
     *
     * @param pageId Identifies which temporary page should be shown inside the shared widget.
     */
    setActiveDemoPage(pageId: SearchStudioPrimeReactDemoPageId): void {
        // Apply the requested page metadata first so the active tab always reflects the page content that is about to render.
        this.applyActiveDemoPageDefinition(pageId);

        // Request a rerender so the shared React surface swaps to the newly selected demo page immediately.
        this.update();
    }

    /**
     * Captures the minimal widget state that Theia should persist for workbench layout restoration.
     *
     * @returns The serializable widget state needed to restore the last open PrimeReact demo page.
     */
    storeState(): SearchStudioPrimeReactDemoWidgetState {
        // Persist only the logical page identifier because the theme variant can be recomputed from the current Theia body classes on attach.
        return {
            activeDemoPageId: this.activeDemoPageId
        };
    }

    /**
     * Restores the last open PrimeReact demo page when Theia recreates the widget from persisted layout state.
     *
     * @param oldState Supplies the serialized widget state captured during the previous workbench session.
     */
    restoreState(oldState: SearchStudioPrimeReactDemoWidgetState): void {
        if (!oldState || !oldState.activeDemoPageId) {
            // Fall back to the bootstrap page when no persisted state exists because the widget still needs a deterministic render target.
            this.applyActiveDemoPageDefinition('bootstrap');
            this.update();
            return;
        }

        // Restore the previous page metadata before the widget is attached so the reopened tab title and rendered content stay aligned.
        this.applyActiveDemoPageDefinition(oldState.activeDemoPageId);
        this.update();
    }

    /**
     * Starts theme synchronization after Theia attaches the temporary demo widget to the DOM.
     *
     * @param msg Supplies the lumino attach lifecycle message.
     */
    protected onAfterAttach(msg: Message): void {
        super.onAfterAttach(msg);

        // Synchronize immediately with the current Theia body classes so the page header reports the correct styled-mode mapping on first render.
        this.synchronizeThemeVariant();

        // Apply the stock PrimeReact theme immediately because the temporary research page now always runs in full styled mode.
        void this.applyPresentationMode();

        // Start observing body class changes so styled mode follows Theia light and dark theme changes without rebuilding the frontend.
        this.installThemeObserver();
    }

    /**
     * Stops temporary stylesheet usage and theme observation when the widget is disposed.
     */
    dispose(): void {
        if (this.isDisposed) {
            // Avoid duplicate cleanup when Theia or tests attempt to dispose the same widget instance more than once.
            return;
        }

        // Stop observing body class changes first so no additional theme callbacks fire while the widget is being torn down.
        this.disposeThemeObserver();

        // Always remove the temporary PrimeReact stylesheets so styled-mode assets do not leak into unrelated shell pages after close.
        this.themeService.disableStyledMode();
        super.dispose();
    }

    /**
     * Renders the temporary PrimeReact research page using the current per-widget presentation state.
     *
     * @returns The React node tree for the active temporary PrimeReact evaluation surface.
     */
    protected render(): React.ReactNode {
        const pageProps: SearchStudioPrimeReactDemoPageProps = {
            activeThemeVariant: this.presentationState.getActiveThemeVariant()
        };

        // Render the page through dedicated React components so the widget owns workbench integration while each page owns its own demo behavior.
        return this.renderActiveDemoPage(pageProps);
    }

    /**
     * Applies the current Theia-aligned stock PrimeReact styled theme to the temporary demo page.
     *
     * @returns A promise that completes after the temporary stylesheets have been attached for the current theme variant.
     */
    protected async applyPresentationMode(): Promise<void> {
        // Ensure the temporary stock PrimeReact theme matches the current Theia light or dark mode before the page rerenders.
        await this.themeService.enableStyledMode(this.presentationState.getActiveThemeVariant());
    }

    /**
     * Applies the metadata for the requested logical PrimeReact demo page to the shared widget.
     *
     * @param pageId Identifies which logical demo page metadata should drive the active widget title and render target.
     */
    protected applyActiveDemoPageDefinition(pageId: SearchStudioPrimeReactDemoPageId): void {
        const pageDefinition = getSearchStudioPrimeReactDemoPageDefinition(pageId);

        // Update the active page and title metadata together so restored tabs and command-driven navigation stay consistent.
        this.activeDemoPageId = pageId;
        this.title.label = pageDefinition.widgetLabel;
        this.title.caption = pageDefinition.widgetCaption;
    }

    /**
     * Synchronizes the cached PrimeReact theme variant with the current Theia body classes.
     */
    protected synchronizeThemeVariant(): void {
        if (!document.body) {
            // Skip synchronization when the document body is unavailable because the widget cannot infer the active Theia theme yet.
            return;
        }

        // Recompute the light or dark mapping from the current Theia body classes so the page header and styled mode stay aligned to the shell.
        this.presentationState.synchronizeThemeVariant(Array.from(document.body.classList));
    }

    /**
     * Installs a body-class observer so the styled PrimeReact theme can follow Theia theme changes while the page remains open.
     */
    protected installThemeObserver(): void {
        if (!document.body || this.themeObserver) {
            // Skip installation when the body is unavailable or the observer already exists for this widget instance.
            return;
        }

        // Observe the body class attribute because Theia theme switching is reflected there rather than through a PrimeReact-specific event source.
        this.themeObserver = new MutationObserver(() => {
            void this.handleThemeClassesChanged();
        });
        this.themeObserver.observe(document.body, {
            attributes: true,
            attributeFilter: ['class']
        });
    }

    /**
     * Stops the body-class observer used for Theia theme synchronization when it exists.
     */
    protected disposeThemeObserver(): void {
        if (!this.themeObserver) {
            // Ignore missing observers because the widget may be disposed before it is ever attached to the DOM.
            return;
        }

        // Disconnect the existing observer so no additional callbacks fire after the widget closes.
        this.themeObserver.disconnect();
        this.themeObserver = undefined;
    }

    /**
     * Responds to body-class changes by resynchronizing the PrimeReact theme variant and reapplying the styled theme when needed.
     *
     * @returns A promise that completes after the updated Theia theme mapping has been applied.
     */
    protected async handleThemeClassesChanged(): Promise<void> {
        const previousThemeVariant = this.presentationState.getActiveThemeVariant();
        this.synchronizeThemeVariant();
        const nextThemeVariant = this.presentationState.getActiveThemeVariant();

        if (previousThemeVariant === nextThemeVariant) {
            // Avoid unnecessary stylesheet work when Theia mutates the body class list without changing the effective light or dark theme.
            return;
        }

        try {
            // Reapply the current stock PrimeReact theme so the demo tracks the new Theia light or dark theme immediately.
            await this.applyPresentationMode();

            console.info('Synchronized temporary PrimeReact demo theme variant.', { nextThemeVariant });
        } catch (error) {
            // Log the failure so theme-sync issues remain diagnosable while preserving the last successfully applied styled theme.
            console.error('Failed to synchronize the temporary PrimeReact demo theme.', error);
        }

        // Rerender so the header text always reflects the latest Theia theme mapping.
        this.update();
    }

    /**
     * Renders the currently selected logical PrimeReact demo page.
     *
     * @param pageProps Supplies the Theia-aligned theme mapping shared by every temporary demo page.
     * @returns The React node tree for the selected temporary PrimeReact page.
     */
    protected renderActiveDemoPage(pageProps: SearchStudioPrimeReactDemoPageProps): React.ReactNode {
        switch (this.activeDemoPageId) {
            case 'datatable': {
                // Load the DataTable page lazily so the shell only evaluates the heavier grid code when the reviewer explicitly opens it.
                const { SearchStudioPrimeReactDataTableDemoPage } = require('./pages/search-studio-primereact-data-table-demo-page') as typeof import('./pages/search-studio-primereact-data-table-demo-page');
                return <SearchStudioPrimeReactDataTableDemoPage {...pageProps} />;
            }
            case 'forms': {
                // Load the forms page lazily so the shell only evaluates the broader input demo when the reviewer explicitly opens it.
                const { SearchStudioPrimeReactFormsDemoPage } = require('./pages/search-studio-primereact-forms-demo-page') as typeof import('./pages/search-studio-primereact-forms-demo-page');
                return <SearchStudioPrimeReactFormsDemoPage {...pageProps} />;
            }
            case 'dataview': {
                // Load the DataView page lazily so the shell only evaluates the card-list demo when the reviewer explicitly opens it.
                const { SearchStudioPrimeReactDataViewDemoPage } = require('./pages/search-studio-primereact-data-view-demo-page') as typeof import('./pages/search-studio-primereact-data-view-demo-page');
                return <SearchStudioPrimeReactDataViewDemoPage {...pageProps} />;
            }
            case 'layout': {
                // Load the layout page lazily so the shell only evaluates the splitter and tabs demo when the reviewer explicitly opens it.
                const { SearchStudioPrimeReactLayoutDemoPage } = require('./pages/search-studio-primereact-layout-demo-page') as typeof import('./pages/search-studio-primereact-layout-demo-page');
                return <SearchStudioPrimeReactLayoutDemoPage {...pageProps} />;
            }
            case 'showcase': {
                // Load the combined showcase page lazily so the shell only evaluates the broadest composite demo when the reviewer explicitly opens it.
                const { SearchStudioPrimeReactShowcaseDemoPage } = require('./pages/search-studio-primereact-showcase-demo-page') as typeof import('./pages/search-studio-primereact-showcase-demo-page');
                return <SearchStudioPrimeReactShowcaseDemoPage {...pageProps} />;
            }
            case 'tree': {
                // Load the Tree page lazily so the shell only evaluates the hierarchy demo when the reviewer explicitly opens it.
                const { SearchStudioPrimeReactTreeDemoPage } = require('./pages/search-studio-primereact-tree-demo-page') as typeof import('./pages/search-studio-primereact-tree-demo-page');
                return <SearchStudioPrimeReactTreeDemoPage {...pageProps} />;
            }
            case 'treetable': {
                // Load the TreeTable page lazily so the shell only evaluates the hierarchical grid code when the reviewer explicitly opens it.
                const { SearchStudioPrimeReactTreeTableDemoPage } = require('./pages/search-studio-primereact-tree-table-demo-page') as typeof import('./pages/search-studio-primereact-tree-table-demo-page');
                return <SearchStudioPrimeReactTreeTableDemoPage {...pageProps} />;
            }
            default: {
                // Load the bootstrap page lazily so the Theia shell startup path does not evaluate PrimeReact component modules until the demo is explicitly opened.
                const { SearchStudioPrimeReactDemoPage } = require('./search-studio-primereact-demo-page') as typeof import('./search-studio-primereact-demo-page');
                return <SearchStudioPrimeReactDemoPage {...pageProps} />;
            }
        }
    }
}
