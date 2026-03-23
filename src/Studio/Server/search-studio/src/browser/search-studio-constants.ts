import { Command } from '@theia/core/lib/common';
import { MenuPath } from '@theia/core/lib/common/menu';

export const SearchStudioWidgetId = 'search-studio.providers.view';
export const SearchStudioWidgetLabel = 'Providers';
export const SearchStudioWidgetIconClass = 'codicon codicon-database';
export const SearchStudioToggleCommandId = 'search-studio.toggleProviders';
export const SearchStudioProvidersViewContainerId = 'search-studio.providers.container';
export const SearchStudioProvidersViewRank = 10;

export const SearchStudioRulesWidgetId = 'search-studio.rules.view';
export const SearchStudioRulesWidgetLabel = 'Rules';
export const SearchStudioRulesWidgetIconClass = 'codicon codicon-symbol-field';
export const SearchStudioRulesToggleCommandId = 'search-studio.toggleRules';
export const SearchStudioRulesViewContainerId = 'search-studio.rules.container';
export const SearchStudioRulesViewRank = 20;

export const SearchStudioIngestionWidgetId = 'search-studio.ingestion.view';
export const SearchStudioIngestionWidgetLabel = 'Ingestion';
export const SearchStudioIngestionWidgetIconClass = 'codicon codicon-cloud-upload';
export const SearchStudioIngestionToggleCommandId = 'search-studio.toggleIngestion';
export const SearchStudioIngestionViewContainerId = 'search-studio.ingestion.container';
export const SearchStudioIngestionViewRank = 30;

export const SearchStudioSearchWidgetId = 'search-studio.search.view';
export const SearchStudioSearchWidgetLabel = 'Search';
export const SearchStudioSearchWidgetIconClass = 'codicon codicon-search';
export const SearchStudioSearchToggleCommandId = 'search-studio.toggleSearch';
export const SearchStudioSearchViewContainerId = 'search-studio.search.container';
export const SearchStudioSearchViewRank = 40;

export const SearchStudioSearchResultsWidgetId = 'search-studio.search-results';
export const SearchStudioSearchResultsWidgetFactoryId = 'search-studio.search-results';
export const SearchStudioSearchResultsWidgetLabel = 'Search results';
export const SearchStudioSearchResultsWidgetIconClass = 'codicon codicon-list-tree';

export const SearchStudioSearchDetailsWidgetId = 'search-studio.search-details';
export const SearchStudioSearchDetailsWidgetLabel = 'Search Details';
export const SearchStudioSearchDetailsWidgetIconClass = 'codicon codicon-info';
export const SearchStudioSearchDetailsToggleCommandId = 'search-studio.toggleSearchDetails';

export const SearchStudioOutputWidgetId = 'search-studio.output.view';
export const SearchStudioOutputWidgetLabel = 'Studio Output';
export const SearchStudioOutputWidgetIconClass = 'codicon codicon-output';
export const SearchStudioOutputToggleCommandId = 'search-studio.toggleOutput';

export const SearchStudioHomeWidgetId = 'search-studio.home';
export const SearchStudioHomeWidgetFactoryId = 'search-studio.home';
export const SearchStudioHomeWidgetLabel = 'Home';
export const SearchStudioHomeWidgetIconClass = 'codicon codicon-home';

export const SearchStudioDocumentWidgetFactoryId = 'search-studio.document';
export const SearchStudioProviderOverviewDocumentIconClass = 'codicon codicon-dashboard';
export const SearchStudioProviderQueueDocumentIconClass = 'codicon codicon-list-unordered';
export const SearchStudioProviderDeadLettersDocumentIconClass = 'codicon codicon-archive';
export const SearchStudioRulesDocumentIconClass = 'codicon codicon-symbol-field';
export const SearchStudioIngestionDocumentIconClass = 'codicon codicon-cloud-upload';

export const SearchStudioShowHomeCommand: Command = {
    id: 'search-studio.home.show',
    category: 'UKHO Search Studio',
    label: 'Home'
};

export const SearchStudioRefreshProvidersCommand: Command = {
    id: 'search-studio.providers.refresh',
    category: 'UKHO Search Studio',
    label: 'Refresh Providers'
};

export const SearchStudioClearOutputCommand: Command = {
    id: 'search-studio.output.clear',
    category: 'UKHO Search Studio',
    label: 'Clear Studio Output'
};

export const SearchStudioCopyAllOutputCommand: Command = {
    id: 'search-studio.output.copyAll',
    category: 'UKHO Search Studio',
    label: 'Copy All Studio Output'
};

export const SearchStudioRefreshRulesCommand: Command = {
    id: 'search-studio.rules.refresh',
    category: 'UKHO Search Studio',
    label: 'Refresh Rules'
};

export const SearchStudioNewRuleCommand: Command = {
    id: 'search-studio.rules.newRule',
    category: 'UKHO Search Studio',
    label: 'New Rule'
};

export const SearchStudioRulesContextMenuPath: MenuPath = ['search-studio-context-menu', 'rules'];
export const SearchStudioProvidersContextMenuPath: MenuPath = ['search-studio-context-menu', 'providers'];
export const SearchStudioIngestionRootContextMenuPath: MenuPath = ['search-studio-context-menu', 'ingestion-root'];
export const SearchStudioIngestionModeContextMenuPath: MenuPath = ['search-studio-context-menu', 'ingestion-mode'];

export const SearchStudioOpenProviderOverviewCommand: Command = {
    id: 'search-studio.providers.openOverview',
    category: 'UKHO Search Studio',
    label: 'Open Provider Overview'
};

export const SearchStudioOpenRulesOverviewCommand: Command = {
    id: 'search-studio.rules.openOverview',
    category: 'UKHO Search Studio',
    label: 'Open Rules Overview'
};

export const SearchStudioOpenIngestionOverviewCommand: Command = {
    id: 'search-studio.ingestion.openOverview',
    category: 'UKHO Search Studio',
    label: 'Open Ingestion Overview'
};

export const SearchStudioOpenIngestionModeCommand: Command = {
    id: 'search-studio.ingestion.openMode',
    category: 'UKHO Search Studio',
    label: 'Open Ingestion Mode'
};

export const SearchStudioResetIngestionStatusCommand: Command = {
    id: 'search-studio.ingestion.resetStatus',
    category: 'UKHO Search Studio',
    label: 'Reset Indexing Status'
};

export const SearchStudioRuleCheckerDocumentIconClass = 'codicon codicon-symbol-event';
export const SearchStudioRuleEditorDocumentIconClass = 'codicon codicon-symbol-property';
export const SearchStudioNewRuleDocumentIconClass = 'codicon codicon-add';
export const SearchStudioIngestionOverviewDocumentIconClass = 'codicon codicon-dashboard';
export const SearchStudioIngestionByIdDocumentIconClass = 'codicon codicon-symbol-numeric';
export const SearchStudioIngestionAllUnindexedDocumentIconClass = 'codicon codicon-layers';
export const SearchStudioIngestionByContextDocumentIconClass = 'codicon codicon-symbol-key';
