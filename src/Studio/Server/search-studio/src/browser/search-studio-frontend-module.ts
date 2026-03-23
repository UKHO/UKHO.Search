/**
 * Generated using theia-extension-generator
 */
import '@xterm/xterm/css/xterm.css';
import { FrontendApplicationContribution, WidgetFactory, bindViewContribution } from '@theia/core/lib/browser';
import { TabBarToolbarContribution } from '@theia/core/lib/browser/shell/tab-bar-toolbar/tab-bar-toolbar-registry';
import { createTreeContainer } from '@theia/core/lib/browser/tree/tree-container';
import { CommandContribution, MenuContribution } from '@theia/core/lib/common';
import { ContainerModule } from '@theia/core/shared/inversify';
import { SearchStudioApiClient } from './api/search-studio-api-client';
import { SearchStudioProviderCatalogService } from './api/search-studio-provider-catalog-service';
import { SearchStudioRulesCatalogService } from './api/search-studio-rules-catalog-service';
import { SearchStudioDocumentService } from './common/search-studio-document-service';
import { SearchStudioDocumentWidget } from './common/search-studio-document-widget';
import { SearchStudioOutputService } from './common/search-studio-output-service';
import { SearchStudioProviderSelectionService } from './common/search-studio-provider-selection-service';
import { SearchStudioDocumentOptions } from './common/search-studio-shell-types';
import { SearchStudioHomeService } from './home/search-studio-home-service';
import { SearchStudioHomeNavigationService } from './home/search-studio-home-navigation-service';
import { SearchStudioHomeWidget } from './home/search-studio-home-widget';
import { SearchStudioIngestionToolbarContribution } from './ingestion/search-studio-ingestion-toolbar-contribution';
import { SearchStudioIngestionTreeModel } from './ingestion/search-studio-ingestion-tree-model';
import { SearchStudioIngestionViewContainerFactory } from './ingestion/search-studio-ingestion-view-container-factory';
import { SearchStudioIngestionViewContribution } from './ingestion/search-studio-ingestion-view-contribution';
import { SearchStudioIngestionWidget } from './ingestion/search-studio-ingestion-widget';
import { SearchStudioOutputToolbarContribution } from './panel/search-studio-output-toolbar-contribution';
import { SearchStudioOutputViewContribution } from './panel/search-studio-output-view-contribution';
import { SearchStudioOutputWidget } from './panel/search-studio-output-widget';
import { SearchStudioProviderTreeModel } from './providers/search-studio-provider-tree-model';
import { SearchStudioProvidersViewContainerFactory } from './providers/search-studio-providers-view-container-factory';
import { SearchStudioRulesViewContainerFactory } from './rules/search-studio-rules-view-container-factory';
import { SearchStudioRulesToolbarContribution } from './rules/search-studio-rules-toolbar-contribution';
import { SearchStudioRulesTreeModel } from './rules/search-studio-rules-tree-model';
import { SearchStudioRulesViewContribution } from './rules/search-studio-rules-view-contribution';
import { SearchStudioRulesWidget } from './rules/search-studio-rules-widget';
import { SearchStudioApiConfigurationService } from './search-studio-api-configuration-service';
import { SearchStudioCommandContribution } from './search-studio-command-contribution';
import { SearchStudioShellLayoutContribution } from './search-studio-shell-layout-contribution';
import {
    SearchStudioDocumentWidgetFactoryId,
    SearchStudioHomeWidgetFactoryId,
    SearchStudioHomeWidgetId,
    SearchStudioIngestionWidgetId,
    SearchStudioOutputWidgetId,
    SearchStudioProvidersContextMenuPath,
    SearchStudioRulesContextMenuPath,
    SearchStudioRulesWidgetId,
    SearchStudioWidgetId
} from './search-studio-constants';
import { SearchStudioMenuContribution } from './search-studio-menu-contribution';
import { SearchStudioViewContribution } from './search-studio-view-contribution';
import { SearchStudioWidget } from './search-studio-widget';

export default new ContainerModule(bind => {
    bind(CommandContribution).to(SearchStudioCommandContribution);
    bind(MenuContribution).to(SearchStudioMenuContribution);
    bind(SearchStudioApiConfigurationService).toSelf().inSingletonScope();
    bind(SearchStudioApiClient).toSelf().inSingletonScope();
    bind(SearchStudioProviderCatalogService).toSelf().inSingletonScope();
    bind(SearchStudioRulesCatalogService).toSelf().inSingletonScope();
    bind(SearchStudioProviderSelectionService).toSelf().inSingletonScope();
    bind(SearchStudioOutputService).toSelf().inSingletonScope();
    bind(SearchStudioDocumentService).toSelf().inSingletonScope();
    bind(SearchStudioHomeService).toSelf().inSingletonScope();
    bind(SearchStudioHomeNavigationService).toSelf().inSingletonScope();
    bind(SearchStudioProviderTreeModel).toSelf().inSingletonScope();
    bind(SearchStudioHomeWidget).toSelf();
    bind(SearchStudioWidget).toSelf();
    bind(SearchStudioRulesTreeModel).toSelf().inSingletonScope();
    bind(SearchStudioRulesWidget).toSelf();
    bind(SearchStudioIngestionTreeModel).toSelf().inSingletonScope();
    bind(SearchStudioIngestionWidget).toSelf();
    bind(SearchStudioOutputWidget).toSelf();
    bind(SearchStudioDocumentWidget).toSelf();
    bind(SearchStudioRulesToolbarContribution).toSelf().inSingletonScope();
    bind(SearchStudioIngestionToolbarContribution).toSelf().inSingletonScope();
    bind(SearchStudioOutputToolbarContribution).toSelf().inSingletonScope();
    bind(SearchStudioProvidersViewContainerFactory).toSelf().inSingletonScope();
    bind(SearchStudioRulesViewContainerFactory).toSelf().inSingletonScope();
    bind(SearchStudioIngestionViewContainerFactory).toSelf().inSingletonScope();
    bind(SearchStudioShellLayoutContribution).toSelf().inSingletonScope();
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioWidgetId,
        createWidget: () => {
            const childContainer = createTreeContainer(context.container, {
                props: {
                    contextMenuPath: SearchStudioProvidersContextMenuPath,
                    leftPadding: 8,
                    expansionTogglePadding: 16,
                    virtualized: false
                },
                model: SearchStudioProviderTreeModel,
                widget: SearchStudioWidget
            });

            return childContainer.get<SearchStudioWidget>(SearchStudioWidget);
        }
    })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioRulesWidgetId,
        createWidget: () => {
            const childContainer = createTreeContainer(context.container, {
                props: {
                    contextMenuPath: SearchStudioRulesContextMenuPath,
                    leftPadding: 8,
                    expansionTogglePadding: 16,
                    virtualized: false
                },
                model: SearchStudioRulesTreeModel,
                widget: SearchStudioRulesWidget
            });

            return childContainer.get<SearchStudioRulesWidget>(SearchStudioRulesWidget);
        }
    })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioIngestionWidgetId,
        createWidget: () => {
            const childContainer = createTreeContainer(context.container, {
                props: {
                    leftPadding: 8,
                    expansionTogglePadding: 16,
                    virtualized: false
                },
                model: SearchStudioIngestionTreeModel,
                widget: SearchStudioIngestionWidget
            });

            return childContainer.get<SearchStudioIngestionWidget>(SearchStudioIngestionWidget);
        }
    })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioOutputWidgetId,
        createWidget: () => context.container.get<SearchStudioOutputWidget>(SearchStudioOutputWidget)
    }));
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioHomeWidgetFactoryId,
        createWidget: () => {
            const widget = context.container.get<SearchStudioHomeWidget>(SearchStudioHomeWidget);
            widget.id = SearchStudioHomeWidgetId;
            return widget;
        }
    }));
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioDocumentWidgetFactoryId,
        createWidget: (options?: unknown) => {
            const widget = context.container.get<SearchStudioDocumentWidget>(SearchStudioDocumentWidget);
            widget.setDocument(options as SearchStudioDocumentOptions);
            return widget;
        }
    }));
    bind(WidgetFactory).toDynamicValue(context => context.container.get<SearchStudioProvidersViewContainerFactory>(SearchStudioProvidersViewContainerFactory));
    bind(WidgetFactory).toDynamicValue(context => context.container.get<SearchStudioRulesViewContainerFactory>(SearchStudioRulesViewContainerFactory));
    bind(WidgetFactory).toDynamicValue(context => context.container.get<SearchStudioIngestionViewContainerFactory>(SearchStudioIngestionViewContainerFactory));
    bindViewContribution(bind, SearchStudioViewContribution);
    bindViewContribution(bind, SearchStudioRulesViewContribution);
    bindViewContribution(bind, SearchStudioIngestionViewContribution);
    bindViewContribution(bind, SearchStudioOutputViewContribution);
    bind(TabBarToolbarContribution).toService(SearchStudioRulesToolbarContribution);
    bind(TabBarToolbarContribution).toService(SearchStudioIngestionToolbarContribution);
    bind(TabBarToolbarContribution).toService(SearchStudioOutputToolbarContribution);
    bind(FrontendApplicationContribution).toService(SearchStudioShellLayoutContribution);
});
