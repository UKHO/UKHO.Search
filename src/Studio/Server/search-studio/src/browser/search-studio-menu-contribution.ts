import { injectable } from '@theia/core/shared/inversify';
import { MenuContribution, MenuModelRegistry } from '@theia/core/lib/common';
import { CommonMenus } from '@theia/core/lib/browser';
import {
    SearchStudioNewRuleCommand,
    SearchStudioOpenIngestionModeCommand,
    SearchStudioOpenIngestionOverviewCommand,
    SearchStudioOpenProviderOverviewCommand,
    SearchStudioOpenRulesOverviewCommand,
    SearchStudioProvidersContextMenuPath,
    SearchStudioResetIngestionStatusCommand,
    SearchStudioRulesContextMenuPath,
    SearchStudioIngestionRootContextMenuPath,
    SearchStudioIngestionModeContextMenuPath,
    SearchStudioRefreshProvidersCommand,
    SearchStudioRefreshRulesCommand,
    SearchStudioShowHomeCommand
} from './search-studio-constants';

@injectable()
export class SearchStudioMenuContribution implements MenuContribution {

    registerMenus(menus: MenuModelRegistry): void {
        menus.registerMenuAction(CommonMenus.FILE_NEW, {
            commandId: SearchStudioNewRuleCommand.id,
            label: 'New Rule'
        });

        menus.registerMenuAction(CommonMenus.VIEW, {
            commandId: SearchStudioShowHomeCommand.id,
            label: 'Home'
        });

        menus.registerMenuAction(CommonMenus.VIEW, {
            commandId: SearchStudioRefreshProvidersCommand.id,
            label: 'Refresh Providers'
        });

        menus.registerMenuAction(CommonMenus.VIEW, {
            commandId: SearchStudioRefreshRulesCommand.id,
            label: 'Refresh Rules'
        });

        menus.registerMenuAction(SearchStudioProvidersContextMenuPath, {
            commandId: SearchStudioOpenProviderOverviewCommand.id,
            label: 'Open Provider Overview'
        });

        menus.registerMenuAction(SearchStudioProvidersContextMenuPath, {
            commandId: SearchStudioRefreshProvidersCommand.id,
            label: 'Refresh Providers'
        });

        menus.registerMenuAction(SearchStudioRulesContextMenuPath, {
            commandId: SearchStudioOpenRulesOverviewCommand.id,
            label: 'Open Rules Overview'
        });

        menus.registerMenuAction(SearchStudioRulesContextMenuPath, {
            commandId: SearchStudioNewRuleCommand.id,
            label: 'New Rule'
        });

        menus.registerMenuAction(SearchStudioRulesContextMenuPath, {
            commandId: SearchStudioRefreshRulesCommand.id,
            label: 'Refresh Rules'
        });

        menus.registerMenuAction(SearchStudioIngestionRootContextMenuPath, {
            commandId: SearchStudioOpenIngestionOverviewCommand.id,
            label: 'Open Ingestion Overview'
        });

        menus.registerMenuAction(SearchStudioIngestionRootContextMenuPath, {
            commandId: SearchStudioResetIngestionStatusCommand.id,
            label: 'Reset Indexing Status'
        });

        menus.registerMenuAction(SearchStudioIngestionModeContextMenuPath, {
            commandId: SearchStudioOpenIngestionModeCommand.id,
            label: 'Open Ingestion Mode'
        });

        menus.registerMenuAction(SearchStudioIngestionModeContextMenuPath, {
            commandId: SearchStudioResetIngestionStatusCommand.id,
            label: 'Reset Indexing Status'
        });
    }
}
