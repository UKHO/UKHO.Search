import { FrontendApplication, FrontendApplicationContribution } from '@theia/core/lib/browser';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioHomeService } from './home/search-studio-home-service';
import { SearchStudioIngestionViewContribution } from './ingestion/search-studio-ingestion-view-contribution';
import { SearchStudioOutputViewContribution } from './panel/search-studio-output-view-contribution';
import { SearchStudioRulesViewContribution } from './rules/search-studio-rules-view-contribution';
import { SearchStudioViewContribution } from './search-studio-view-contribution';

@injectable()
export class SearchStudioShellLayoutContribution implements FrontendApplicationContribution {

    protected _layoutInitialized = false;

    @inject(SearchStudioViewContribution)
    protected readonly _providersViewContribution!: SearchStudioViewContribution;

    @inject(SearchStudioRulesViewContribution)
    protected readonly _rulesViewContribution!: SearchStudioRulesViewContribution;

    @inject(SearchStudioIngestionViewContribution)
    protected readonly _ingestionViewContribution!: SearchStudioIngestionViewContribution;

    @inject(SearchStudioOutputViewContribution)
    protected readonly _outputViewContribution!: SearchStudioOutputViewContribution;

    @inject(SearchStudioHomeService)
    protected readonly _homeService!: SearchStudioHomeService;

    async initializeLayout(_app: FrontendApplication): Promise<void> {
        await this.ensureStudioShellViews();
    }

    async onDidInitializeLayout(_app: FrontendApplication): Promise<void> {
        await this.ensureStudioShellViews();
    }

    protected async ensureStudioShellViews(): Promise<void> {
        if (this._layoutInitialized) {
            return;
        }

        this._layoutInitialized = true;
        await this._providersViewContribution.openView({ activate: false, reveal: true });
        await this._rulesViewContribution.openView({ activate: false, reveal: true });
        await this._ingestionViewContribution.openView({ activate: false, reveal: true });
        await this._outputViewContribution.openView({ activate: false, reveal: true });
        await this._homeService.openHome();
    }
}
