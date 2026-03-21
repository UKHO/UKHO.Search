/**
 * Generated using theia-extension-generator
 */
import { FrontendApplicationContribution, WidgetFactory, bindViewContribution } from '@theia/core/lib/browser';
import { CommandContribution, MenuContribution } from '@theia/core/lib/common';
import { ContainerModule } from '@theia/core/shared/inversify';
import { SearchStudioApiConfigurationService } from './search-studio-api-configuration-service';
import { SearchStudioCommandContribution } from './search-studio-command-contribution';
import { SearchStudioEchoProbeService } from './search-studio-echo-probe-service';
import { SearchStudioWidgetId } from './search-studio-constants';
import { SearchStudioMenuContribution } from './search-studio-menu-contribution';
import { SearchStudioViewContribution } from './search-studio-view-contribution';
import { SearchStudioWidget } from './search-studio-widget';

export default new ContainerModule(bind => {
    bind(CommandContribution).to(SearchStudioCommandContribution);
    bind(MenuContribution).to(SearchStudioMenuContribution);
    bind(SearchStudioApiConfigurationService).toSelf().inSingletonScope();
    bind(SearchStudioEchoProbeService).toSelf().inSingletonScope();
    bind(SearchStudioWidget).toSelf();
    bind(WidgetFactory).toDynamicValue(context => ({
        id: SearchStudioWidgetId,
        createWidget: () => context.container.get<SearchStudioWidget>(SearchStudioWidget)
    }));
    bindViewContribution(bind, SearchStudioViewContribution);
    bind(FrontendApplicationContribution).toService(SearchStudioViewContribution);
});
