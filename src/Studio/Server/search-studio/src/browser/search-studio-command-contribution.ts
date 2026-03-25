import { CommandContribution, CommandRegistry } from '@theia/core/lib/common';
import { inject, injectable } from '@theia/core/shared/inversify';
import { SearchStudioHomeService } from './home/search-studio-home-service';
import { SearchStudioPrimeReactDemoService } from './primereact-demo/search-studio-primereact-demo-service';
import {
    getSearchStudioPrimeReactDemoCommandDefinitions
} from './primereact-demo/search-studio-primereact-demo-constants';
import { SearchStudioShowHomeCommand } from './search-studio-home-constants';

/**
 * Registers the Studio commands needed to reopen Home and the temporary PrimeReact demo pages from standard Theia surfaces.
 */
@injectable()
export class SearchStudioCommandContribution implements CommandContribution {

    /**
     * Creates the Studio command contribution.
     *
     * @param homeService Reopens the shared Home document widget when the user invokes the Home command.
     * @param primeReactDemoService Opens the temporary PrimeReact evaluation pages when the user invokes any demo command.
     */
    constructor(
        @inject(SearchStudioHomeService)
        protected readonly homeService: SearchStudioHomeService,
        @inject(SearchStudioPrimeReactDemoService)
        protected readonly primeReactDemoService: SearchStudioPrimeReactDemoService
    ) {
        // Store the shared Home and temporary demo services so both View menu actions can reopen their document tabs consistently.
    }

    /**
     * Registers the Studio Home command with the command registry.
     *
     * @param registry Stores the command definition and execution handler.
     */
    registerCommands(registry: CommandRegistry): void {
        // Keep the legacy command identity and label so the View menu can continue exposing Home with the expected wording.
        registry.registerCommand(SearchStudioShowHomeCommand, {
            execute: async () => {
                await this.homeService.openHome();
            }
        });

        // Register every temporary PrimeReact demo command from one ordered definition list so the disposable research surface stays easy to remove.
        for (const demoCommandDefinition of getSearchStudioPrimeReactDemoCommandDefinitions()) {
            registry.registerCommand(demoCommandDefinition.command, {
                execute: async () => {
                    await this.primeReactDemoService.openDemo(demoCommandDefinition.pageId);
                }
            });
        }
    }
}
