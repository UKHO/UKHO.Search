import { injectable } from '@theia/core/shared/inversify';
import {
    normalizeStudioApiHostBaseUrl,
    SearchStudioFutureApiConfiguration,
    SearchStudioFutureApiConfigurationEndpointPath
} from './search-studio-future-api-configuration';

@injectable()
export class SearchStudioApiConfigurationService {

    protected configuration?: SearchStudioFutureApiConfiguration;
    protected configurationRequest?: Promise<SearchStudioFutureApiConfiguration>;

    async getConfiguration(): Promise<SearchStudioFutureApiConfiguration> {
        if (this.configuration) {
            return this.configuration;
        }

        if (!this.configurationRequest) {
            this.configurationRequest = this.fetchConfiguration();
        }

        return this.configurationRequest;
    }

    protected async fetchConfiguration(): Promise<SearchStudioFutureApiConfiguration> {
        const response = await fetch(SearchStudioFutureApiConfigurationEndpointPath, {
            method: 'GET',
            headers: {
                Accept: 'application/json'
            }
        });

        if (!response.ok) {
            throw new Error(`Failed to load studio configuration: ${response.status} ${response.statusText}`);
        }

        const configuration = await response.json() as SearchStudioFutureApiConfiguration;

        this.configuration = {
            studioApiHostBaseUrl: normalizeStudioApiHostBaseUrl(configuration.studioApiHostBaseUrl),
            rawStudioApiHostBaseUrl: configuration.rawStudioApiHostBaseUrl,
            environmentVariableName: configuration.environmentVariableName
        };

        return this.configuration;
    }
}
