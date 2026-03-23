import { inject, injectable } from '@theia/core/shared/inversify';
import { CommandContribution, CommandRegistry, MessageService } from '@theia/core/lib/common';
import { SearchStudioProviderCatalogService } from './api/search-studio-provider-catalog-service';
import { SearchStudioRulesCatalogService } from './api/search-studio-rules-catalog-service';
import { SearchStudioDocumentService } from './common/search-studio-document-service';
import { SearchStudioHomeService } from './home/search-studio-home-service';
import { SearchStudioOutputService } from './common/search-studio-output-service';
import { SearchStudioProviderSelectionService } from './common/search-studio-provider-selection-service';
import { resolvePreferredProvider } from './common/search-studio-provider-resolution';
import {
    SearchStudioClearOutputCommand,
    SearchStudioCopyAllOutputCommand,
    SearchStudioNewRuleCommand,
    SearchStudioOpenIngestionModeCommand,
    SearchStudioOpenIngestionOverviewCommand,
    SearchStudioOpenProviderOverviewCommand,
    SearchStudioOpenRulesOverviewCommand,
    SearchStudioRefreshProvidersCommand,
    SearchStudioRefreshRulesCommand,
    SearchStudioResetIngestionStatusCommand,
    SearchStudioShowHomeCommand
} from './search-studio-constants';
import { SearchStudioApiProviderDescriptor } from './api/search-studio-api-types';
import { SearchStudioIngestionNodeKind } from './ingestion/search-studio-ingestion-types';
import { serializeOutputEntries } from './panel/search-studio-output-format';

@injectable()
export class SearchStudioCommandContribution implements CommandContribution {

    @inject(MessageService)
    protected readonly _messageService!: MessageService;

    @inject(SearchStudioProviderCatalogService)
    protected readonly _providerCatalogService!: SearchStudioProviderCatalogService;

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    @inject(SearchStudioRulesCatalogService)
    protected readonly _rulesCatalogService!: SearchStudioRulesCatalogService;

    @inject(SearchStudioProviderSelectionService)
    protected readonly _providerSelectionService!: SearchStudioProviderSelectionService;

    @inject(SearchStudioDocumentService)
    protected readonly _documentService!: SearchStudioDocumentService;

    @inject(SearchStudioHomeService)
    protected readonly _homeService!: SearchStudioHomeService;

    registerCommands(registry: CommandRegistry): void {
        registry.registerCommand(SearchStudioShowHomeCommand, {
            execute: async () => {
                await this._homeService.openHome();
            }
        });

        registry.registerCommand(SearchStudioRefreshProvidersCommand, {
            execute: async () => {
                await this._providerCatalogService.refresh();
                this._messageService.info('Studio providers refreshed.');
            }
        });

        registry.registerCommand(SearchStudioClearOutputCommand, {
            execute: () => {
                this._outputService.clear();
                this._messageService.info('Studio output cleared.');
            }
        });

        registry.registerCommand(SearchStudioCopyAllOutputCommand, {
            execute: async () => {
                const clipboard = globalThis.navigator?.clipboard;

                if (!clipboard?.writeText) {
                    this._messageService.warn('Clipboard access is unavailable for Studio Output.');
                    return;
                }

                try {
                    await clipboard.writeText(serializeOutputEntries(this._outputService.entries));
                    this._messageService.info('Studio output copied to the clipboard.');
                } catch {
                    this._messageService.error('Unable to copy Studio output to the clipboard.');
                }
            }
        });

        registry.registerCommand(SearchStudioRefreshRulesCommand, {
            execute: async () => {
                await this._rulesCatalogService.refresh();
                this._messageService.info('Studio rules refreshed.');
            }
        });

        registry.registerCommand(SearchStudioNewRuleCommand, {
            execute: async (providerNameOrWidget?: unknown) => {
                const providerName = typeof providerNameOrWidget === 'string'
                    ? providerNameOrWidget
                    : undefined;
                const provider = await this.resolveProvider(providerName);

                if (!provider) {
                    this._messageService.warn('No Studio provider is available for New Rule.');
                    return;
                }

                this._providerSelectionService.selectProvider(provider, 'rules');
                await this._documentService.openNewRuleEditor(provider);
            }
        });

        registry.registerCommand(SearchStudioOpenProviderOverviewCommand, {
            execute: async (providerName?: string) => {
                const provider = await this.resolveProvider(providerName);

                if (!provider) {
                    return;
                }

                this._providerSelectionService.selectProvider(provider, 'providers');
                await this._documentService.openProviderOverview(provider);
            }
        });

        registry.registerCommand(SearchStudioOpenRulesOverviewCommand, {
            execute: async (providerName?: string) => {
                const provider = await this.resolveProvider(providerName);

                if (!provider) {
                    return;
                }

                this._providerSelectionService.selectProvider(provider, 'rules');
                await this._documentService.openRulesOverview(provider);
            }
        });

        registry.registerCommand(SearchStudioOpenIngestionOverviewCommand, {
            execute: async (providerName?: string) => {
                const provider = await this.resolveProvider(providerName);

                if (!provider) {
                    return;
                }

                this._providerSelectionService.selectProvider(provider, 'ingestion');
                await this._documentService.openIngestionOverview(provider);
            }
        });

        registry.registerCommand(SearchStudioOpenIngestionModeCommand, {
            execute: async (providerName?: string, mode?: SearchStudioIngestionNodeKind) => {
                const provider = await this.resolveProvider(providerName);

                if (!provider || !mode) {
                    return;
                }

                this._providerSelectionService.selectProvider(provider, 'ingestion');

                switch (mode) {
                    case 'by-id':
                        await this._documentService.openIngestionById(provider);
                        return;
                    case 'all-unindexed':
                        await this._documentService.openIngestionAllUnindexed(provider);
                        return;
                    case 'by-context':
                        await this._documentService.openIngestionByContext(provider);
                        return;
                }
            }
        });

        registry.registerCommand(SearchStudioResetIngestionStatusCommand, {
            execute: async (providerName?: string) => {
                const provider = await this.resolveProvider(providerName);

                if (!provider) {
                    return;
                }

                this._providerSelectionService.selectProvider(provider, 'ingestion');
                await this._documentService.runResetIngestionStatusPlaceholder(provider);
                this._messageService.info(`Reset indexing status remains placeholder-only for ${provider.displayName}.`);
            }
        });
    }

    protected async resolveProvider(providerName?: string): Promise<SearchStudioApiProviderDescriptor | undefined> {
        await this._providerCatalogService.ensureLoaded();

        return resolvePreferredProvider(
            this._providerCatalogService.snapshot.providers,
            this._providerSelectionService.selectedProviderName,
            providerName);
    }
}
