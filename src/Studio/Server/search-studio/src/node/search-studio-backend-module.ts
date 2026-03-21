import { BackendApplicationContribution } from '@theia/core/lib/node';
import { ContainerModule } from '@theia/core/shared/inversify';
import { SearchStudioBackendApplicationContribution } from './search-studio-backend-application-contribution';

export default new ContainerModule(bind => {
    bind(SearchStudioBackendApplicationContribution).toSelf().inSingletonScope();
    bind(BackendApplicationContribution).toService(SearchStudioBackendApplicationContribution);
});
