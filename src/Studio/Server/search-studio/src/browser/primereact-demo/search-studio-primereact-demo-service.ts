import { ApplicationShell } from '@theia/core/lib/browser';
import { WidgetManager } from '@theia/core/lib/browser/widget-manager';
import { inject, injectable } from '@theia/core/shared/inversify';
import { Widget } from '@lumino/widgets';
import {
    getSearchStudioPrimeReactDemoPageDefinition,
    SearchStudioPrimeReactDemoPageId,
    SearchStudioPrimeReactDemoWidgetFactoryId
} from './search-studio-primereact-demo-constants';
import { SearchStudioPrimeReactDemoWidget } from './search-studio-primereact-demo-widget';

/**
 * Opens and reactivates the temporary PrimeReact research pages from the Theia View menu.
 */
@injectable()
export class SearchStudioPrimeReactDemoService {

    /**
     * Creates the PrimeReact demo document service.
     *
     * @param shell Supplies access to the Theia main area where the temporary demo document tab is hosted.
     * @param widgetManager Reuses the single temporary PrimeReact demo widget instance across repeated View menu opens.
     */
    constructor(
        @inject(ApplicationShell)
        protected readonly shell: ApplicationShell,
        @inject(WidgetManager)
        protected readonly widgetManager: WidgetManager
    ) {
        // Store only the workbench services needed to open the temporary research page as a normal document tab.
    }

    /**
     * Opens the temporary PrimeReact demo document in the main workbench area and activates the tab.
     *
     * @param pageId Identifies which temporary PrimeReact page should be shown inside the shared demo widget.
     * @returns A promise that completes after the PrimeReact demo widget is attached and focused.
     */
    async openDemo(pageId: SearchStudioPrimeReactDemoPageId = 'bootstrap'): Promise<void> {
        try {
            // Reuse the same temporary widget instance so toggled demo state behaves like a normal reopenable Theia document.
            const widget = await this.widgetManager.getOrCreateWidget<SearchStudioPrimeReactDemoWidget>(SearchStudioPrimeReactDemoWidgetFactoryId);
            const pageDefinition = getSearchStudioPrimeReactDemoPageDefinition(pageId);

            // Switch the shared widget to the requested page before it is attached or reactivated so the tab title and content stay aligned.
            widget.setActiveDemoPage(pageId);

            if (!widget.isAttached) {
                // Insert the temporary demo before the first main-area widget when possible so it opens as a normal peer document.
                const firstMainAreaWidget = this.getFirstMainAreaWidget();
                await this.shell.addWidget(widget, {
                    area: 'main',
                    mode: firstMainAreaWidget ? 'tab-before' : undefined,
                    ref: firstMainAreaWidget
                });
            }

            // Always reactivate the temporary demo tab so repeated View menu opens bring the evaluation page to the foreground.
            await this.shell.activateWidget(widget.id);
            console.info('Opened temporary PrimeReact demo page.', { pageId: pageDefinition.pageId });
        } catch (error) {
            // Surface failures in the browser console so temporary research page activation problems remain diagnosable during manual review.
            console.error('Failed to open temporary PrimeReact demo page.', error);
            throw error;
        }
    }

    /**
     * Gets the first existing main-area widget so the temporary demo can be inserted before it.
     *
     * @returns The first widget in the main workbench area when one exists; otherwise, `undefined`.
     */
    protected getFirstMainAreaWidget(): Widget | undefined {
        // Stop after the first widget because the temporary research page only needs one reference tab for insertion ordering.
        for (const widget of this.shell.mainPanel.widgets()) {
            return widget;
        }

        return undefined;
    }
}
