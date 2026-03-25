import { FrontendApplicationContribution, WidgetFactory } from '@theia/core/lib/browser';
import { CommandContribution, MenuContribution } from '@theia/core/lib/common';
import { ContainerModule } from '@theia/core/shared/inversify';
import { SearchStudioCommandContribution } from './search-studio-command-contribution';
import { SearchStudioFrontendApplicationContribution } from './search-studio-frontend-application-contribution';
import { SearchStudioHomeService } from './home/search-studio-home-service';
import { SearchStudioHomeWidget } from './home/search-studio-home-widget';
import { SearchStudioMenuContribution } from './search-studio-menu-contribution';
import { SearchStudioPrimeReactDemoThemeService } from './primereact-demo/search-studio-primereact-demo-theme-service';
import { SearchStudioPrimeReactDemoService } from './primereact-demo/search-studio-primereact-demo-service';
import { SearchStudioPrimeReactDemoWidget } from './primereact-demo/search-studio-primereact-demo-widget';
import { SearchStudioPrimeReactDemoWidgetFactoryId } from './primereact-demo/search-studio-primereact-demo-constants';
import { SearchStudioHomeWidgetFactoryId } from './search-studio-home-constants';
import { SearchStudioRuntimeConfigurationService } from './search-studio-runtime-configuration-service';

/**
 * Registers the minimal frontend services that the fresh Studio shell needs during the bootstrap slice.
 */
export default new ContainerModule(bind => {
    // Register the Studio command and menu contributions that expose Home through the standard Theia View menu.
    bind(CommandContribution).to(SearchStudioCommandContribution).inSingletonScope();
    bind(MenuContribution).to(SearchStudioMenuContribution).inSingletonScope();

    // Register the runtime configuration service as a singleton so the browser reuses one cached configuration payload.
    bind(SearchStudioRuntimeConfigurationService).toSelf().inSingletonScope();

    // Register the Home service and widget so startup and View menu reopen actions share one normal document instance.
    bind(SearchStudioHomeService).toSelf().inSingletonScope();
    bind(SearchStudioHomeWidget).toSelf();
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioHomeWidgetFactoryId,
        createWidget: () => context.container.get<SearchStudioHomeWidget>(SearchStudioHomeWidget)
    })).inSingletonScope();

    // Register the temporary PrimeReact demo service and widget so reviewers can open the disposable research page from View.
    bind(SearchStudioPrimeReactDemoThemeService).toSelf().inSingletonScope();
    bind(SearchStudioPrimeReactDemoService).toSelf().inSingletonScope();
    bind(SearchStudioPrimeReactDemoWidget).toSelf();
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioPrimeReactDemoWidgetFactoryId,
        createWidget: () => context.container.get<SearchStudioPrimeReactDemoWidget>(SearchStudioPrimeReactDemoWidget)
    })).inSingletonScope();

    // Register the startup contribution that validates the runtime bridge and opens the Studio Home tab.
    bind(SearchStudioFrontendApplicationContribution).toSelf().inSingletonScope();
    bind(FrontendApplicationContribution).toService(SearchStudioFrontendApplicationContribution);
});
