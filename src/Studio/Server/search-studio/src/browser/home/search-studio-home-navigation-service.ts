import { MessageService } from '@theia/core/lib/common';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioProviderCatalogService } from '../api/search-studio-provider-catalog-service';
import { SearchStudioDocumentService } from '../common/search-studio-document-service';
import { SearchStudioOutputService } from '../common/search-studio-output-service';
import { SearchStudioProviderSelectionService } from '../common/search-studio-provider-selection-service';
import { resolvePreferredProvider } from '../common/search-studio-provider-resolution';
import { SearchStudioHomeJumpPointId } from './search-studio-home-types';

@injectable()
export class SearchStudioHomeNavigationService {

    @inject(MessageService)
    protected readonly _messageService!: MessageService;

    @inject(SearchStudioProviderCatalogService)
    protected readonly _providerCatalogService!: SearchStudioProviderCatalogService;

    @inject(SearchStudioProviderSelectionService)
    protected readonly _providerSelectionService!: SearchStudioProviderSelectionService;

    @inject(SearchStudioDocumentService)
    protected readonly _documentService!: SearchStudioDocumentService;

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    async openJumpPoint(jumpPointId: SearchStudioHomeJumpPointId): Promise<void> {
        await this._providerCatalogService.ensureLoaded();

        const provider = resolvePreferredProvider(
            this._providerCatalogService.snapshot.providers,
            this._providerSelectionService.selectedProviderName);

        if (!provider) {
            const message = 'No Studio provider is available for Home navigation.';
            this._outputService.error(message, 'home');
            this._messageService.warn(message);
            return;
        }

        switch (jumpPointId) {
            case 'start-ingestion':
                this._providerSelectionService.selectProvider(provider, 'ingestion');
                await this._documentService.openIngestionOverview(provider);
                return;
            case 'manage-rules':
                this._providerSelectionService.selectProvider(provider, 'rules');
                await this._documentService.openRulesOverview(provider);
                return;
            case 'browse-providers':
                this._providerSelectionService.selectProvider(provider, 'providers');
                await this._documentService.openProviderOverview(provider);
                return;
        }
    }
}
