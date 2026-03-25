import { CommonMenus } from '@theia/core/lib/browser';
import { MenuContribution, MenuModelRegistry } from '@theia/core/lib/common';
import { injectable } from '@theia/core/shared/inversify';
import {
    getSearchStudioPrimeReactDemoCommandDefinitions
} from './primereact-demo/search-studio-primereact-demo-constants';
import { SearchStudioShowHomeCommand } from './search-studio-home-constants';

/**
 * Adds the Studio Home and temporary PrimeReact demo reopen actions to the standard Theia View menu.
 */
@injectable()
export class SearchStudioMenuContribution implements MenuContribution {

    /**
     * Registers the Home reopen action plus temporary PrimeReact demo entries in the View menu.
     *
     * @param menus Stores menu actions contributed by the Studio frontend extension.
     */
    registerMenus(menus: MenuModelRegistry): void {
        // Preserve the simple legacy View menu wording so users can reopen Home with the same label as the previous shell.
        menus.registerMenuAction(CommonMenus.VIEW, {
            commandId: SearchStudioShowHomeCommand.id,
            label: 'Home'
        });

        // Add every temporary PrimeReact evaluation entry from one ordered definition list so the disposable research menu stays easy to review and remove.
        for (const demoCommandDefinition of getSearchStudioPrimeReactDemoCommandDefinitions()) {
            menus.registerMenuAction(CommonMenus.VIEW, {
                commandId: demoCommandDefinition.command.id,
                label: demoCommandDefinition.menuLabel
            });
        }
    }
}
