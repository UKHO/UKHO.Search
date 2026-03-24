import * as React from '@theia/core/shared/react';
import { injectable } from '@theia/core/shared/inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import ukhoLogoPath = require('../assets/ukho-logo-transparent.png');
import {
    SearchStudioHomeWidgetIconClass,
    SearchStudioHomeWidgetId,
    SearchStudioHomeWidgetLabel
} from '../search-studio-home-constants';
import './search-studio-home-widget.css';

/**
 * Renders the lightweight Studio Home document that orients users when the fresh Theia shell starts.
 */
@injectable()
export class SearchStudioHomeWidget extends ReactWidget {

    /**
     * Creates the Home widget and configures it as a normal closable document tab.
     */
    constructor() {
        super();

        // Reuse the previous shell identifiers so commands, menus, and tests align with the established Home contract.
        this.id = SearchStudioHomeWidgetId;
        this.title.label = SearchStudioHomeWidgetLabel;
        this.title.caption = 'Search Studio home';
        this.title.iconClass = SearchStudioHomeWidgetIconClass;
        this.title.closable = true;
        this.addClass('search-studio-home-widget');

        // Request the initial render immediately so the startup-open experience shows the branded content without extra interaction.
        this.update();
    }

    /**
     * Renders the cleaned Studio Home layout using the copied runtime-served UKHO logo asset and short orientation text.
     *
     * @returns The React node tree for the Home document tab.
     */
    protected render(): React.ReactNode {
        // Keep the presentation intentionally lightweight for this slice by rendering only the Studio-owned branding and orientation surface.
        return (
            <div className="search-studio-home-widget__content">
                <header className="search-studio-home-widget__hero">
                    <div className="search-studio-home-widget__copy">
                        <h1 className="search-studio-home-widget__title">Search Studio</h1>
                        <p className="search-studio-home-widget__summary">
                            Welcome to the Studio shell for provider operations, rules exploration, and ingestion workflow review.
                            Use the activity bar to move between the current work areas, and use View -&gt; Home whenever you want to reopen this Home tab.
                        </p>
                    </div>
                    <img
                        className="search-studio-home-widget__logo"
                        src={ukhoLogoPath}
                        alt="UKHO Search Studio"
                    />
                </header>
                <section className="search-studio-home-widget__section">
                    <h2 className="search-studio-home-widget__section-title">Current scope</h2>
                    <p className="search-studio-home-widget__summary">
                        This restored Home page stays intentionally lightweight for now. It provides branding and orientation while later work items bring back deeper Studio-specific workflows.
                    </p>
                </section>
            </div>
        );
    }
}
