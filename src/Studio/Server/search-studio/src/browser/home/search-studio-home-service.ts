import { ApplicationShell } from '@theia/core/lib/browser';
import { WidgetManager } from '@theia/core/lib/browser/widget-manager';
import { inject, injectable } from '@theia/core/shared/inversify';
import { Widget } from '@lumino/widgets';
import { SearchStudioOutputService } from '../common/search-studio-output-service';
import { SearchStudioHomeWidget } from './search-studio-home-widget';
import { SearchStudioHomeWidgetFactoryId } from '../search-studio-constants';

@injectable()
export class SearchStudioHomeService {

    @inject(ApplicationShell)
    protected readonly _shell!: ApplicationShell;

    @inject(WidgetManager)
    protected readonly _widgetManager!: WidgetManager;

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    async openHome(): Promise<void> {
        const widget = await this._widgetManager.getOrCreateWidget<SearchStudioHomeWidget>(SearchStudioHomeWidgetFactoryId);

        if (!widget.isAttached) {
            const firstMainAreaWidget = this.getFirstMainAreaWidget();
            await this._shell.addWidget(widget, {
                area: 'main',
                mode: firstMainAreaWidget ? 'tab-before' : undefined,
                ref: firstMainAreaWidget
            });
        }

        await this._shell.activateWidget(widget.id);
        this._outputService.info('Opened Home.', 'home');
    }

    protected getFirstMainAreaWidget(): Widget | undefined {
        for (const widget of this._shell.mainPanel.widgets()) {
            return widget;
        }

        return undefined;
    }
}
