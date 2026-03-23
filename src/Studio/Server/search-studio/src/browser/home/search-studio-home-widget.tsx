import * as React from '@theia/core/shared/react';
import { inject, injectable } from '@theia/core/shared/inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import ukhoLogoPath = require('../assets/ukho-logo-transparent.png');
import { SearchStudioHomeJumpPoints } from './search-studio-home-jump-points';
import { SearchStudioHomeNavigationService } from './search-studio-home-navigation-service';
import {
    SearchStudioHomeWidgetIconClass,
    SearchStudioHomeWidgetId,
    SearchStudioHomeWidgetLabel
} from '../search-studio-constants';

@injectable()
export class SearchStudioHomeWidget extends ReactWidget {

    @inject(SearchStudioHomeNavigationService)
    protected readonly _homeNavigationService!: SearchStudioHomeNavigationService;

    constructor() {
        super();
        this.id = SearchStudioHomeWidgetId;
        this.title.label = SearchStudioHomeWidgetLabel;
        this.title.caption = 'Search Studio home';
        this.title.iconClass = SearchStudioHomeWidgetIconClass;
        this.title.closable = true;
        this.addClass('search-studio-home-widget');
        this.update();
    }

    protected render(): React.ReactNode {
        return (
            <div style={{
                display: 'grid',
                gap: '1.5rem',
                padding: '2rem',
                maxWidth: '64rem'
            }}>
                <header style={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                    gap: '1rem',
                    padding: '1.5rem',
                    borderRadius: '12px',
                    border: '1px solid var(--theia-panel-border)',
                    background: 'linear-gradient(180deg, rgba(15, 23, 42, 0.92) 0%, rgba(15, 23, 42, 0.72) 100%)',
                    flexWrap: 'wrap'
                }}>
                    <div style={{ display: 'grid', gap: '0.75rem', flex: '1 1 26rem', minWidth: '18rem' }}>
                        <h1 style={{ margin: 0, fontSize: '2rem' }}>Search Studio</h1>
                        <p style={{
                            margin: 0,
                            color: 'var(--theia-descriptionForeground)',
                            lineHeight: 1.6
                        }}>
                            Welcome to the Studio shell for provider operations, rules exploration, and ingestion workflow review.
                            Use the activity bar to move between the current work areas, and use the Theia View menu whenever you want to reopen this Home tab.
                        </p>
                    </div>
                    <img
                        src={ukhoLogoPath}
                        alt="UKHO Search Studio"
                        style={{
                            display: 'block',
                            width: '100%',
                            maxWidth: '16rem',
                            height: 'auto',
                            flex: '0 0 auto'
                        }}
                    />
                </header>
                <section style={{ display: 'grid', gap: '0.75rem' }}>
                    <h2 style={{ margin: 0, fontSize: '1.1rem' }}>Get started</h2>
                    <p style={{ margin: 0, color: 'var(--theia-descriptionForeground)', lineHeight: 1.6 }}>
                        Jump straight into the current Studio workflows. Each action uses the same destination-opening behavior already used elsewhere in the shell.
                    </p>
                    <div style={{
                        display: 'grid',
                        gap: '0.75rem',
                        gridTemplateColumns: 'repeat(auto-fit, minmax(14rem, 1fr))'
                    }}>
                        {SearchStudioHomeJumpPoints.map(item => (
                            <button
                                key={item.id}
                                type="button"
                                className="theia-button"
                                onClick={() => void this._homeNavigationService.openJumpPoint(item.id)}
                                style={{
                                    display: 'grid',
                                    justifyItems: 'start',
                                    gap: '0.5rem',
                                    padding: '1rem',
                                    borderRadius: '10px',
                                    border: '1px solid var(--theia-panel-border)',
                                    background: item.emphasis === 'primary'
                                        ? 'var(--theia-button-background)'
                                        : 'var(--theia-editor-background)',
                                    color: item.emphasis === 'primary'
                                        ? 'var(--theia-button-foreground)'
                                        : 'var(--theia-editor-foreground)',
                                    textAlign: 'left',
                                    cursor: 'pointer'
                                }}
                            >
                                <strong>{item.label}</strong>
                                <span style={{
                                    color: item.emphasis === 'primary'
                                        ? 'var(--theia-button-foreground)'
                                        : 'var(--theia-descriptionForeground)',
                                    lineHeight: 1.5
                                }}>
                                    {item.description}
                                </span>
                            </button>
                        ))}
                    </div>
                </section>
                <section style={{
                    display: 'grid',
                    gap: '0.5rem',
                    padding: '1rem 1.25rem',
                    borderRadius: '10px',
                    border: '1px solid var(--theia-panel-border)',
                    background: 'var(--theia-editor-inactiveSelectionBackground)'
                }}>
                    <strong>Current scope</strong>
                    <span style={{ color: 'var(--theia-descriptionForeground)', lineHeight: 1.5 }}>
                        This first restored Home page stays intentionally lightweight. It provides branding and orientation only, without operational counts or dashboard summaries.
                    </span>
                </section>
            </div>
        );
    }

}
