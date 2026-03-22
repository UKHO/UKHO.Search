import { injectable } from '@theia/core/shared/inversify';
import {
    SearchStudioFutureApiConfigurationEndpointPath,
    SearchStudioEchoProbeEndpointPath,
    SearchStudioEchoProbeResult,
    SearchStudioFrontendRequestTimeoutMilliseconds
} from './search-studio-future-api-configuration';

@injectable()
export class SearchStudioEchoProbeService {

    protected probeResult?: SearchStudioEchoProbeResult;
    protected probeRequest?: Promise<SearchStudioEchoProbeResult>;

    async getProbeResult(): Promise<SearchStudioEchoProbeResult> {
        if (this.probeResult) {
            return this.probeResult;
        }

        if (!this.probeRequest) {
            this.probeRequest = this.fetchProbeResult();
        }

        return this.probeRequest;
    }

    protected async fetchProbeResult(): Promise<SearchStudioEchoProbeResult> {
        const abortController = new AbortController();
        const timeout = window.setTimeout(() => abortController.abort(), SearchStudioFrontendRequestTimeoutMilliseconds);

        try {
            const response = await fetch(SearchStudioEchoProbeEndpointPath, {
                method: 'GET',
                headers: {
                    Accept: 'application/json'
                },
                signal: abortController.signal
            });

            if (!response.ok) {
                throw new Error(`Failed to load StudioApiHost echo probe: ${response.status} ${response.statusText}`);
            }

            this.probeResult = await response.json() as SearchStudioEchoProbeResult;
            return this.probeResult;
        } catch (error) {
            const errorMessage = error instanceof Error
                ? error.message
                : 'StudioApiHost echo probe request failed.';

            this.probeResult = {
                transport: 'theia-backend-proxy',
                configurationEndpointUrl: `${window.location.origin}${SearchStudioFutureApiConfigurationEndpointPath}`,
                probeEndpointUrl: `${window.location.origin}${SearchStudioEchoProbeEndpointPath}`,
                error: errorMessage
            };

            return this.probeResult;
        } finally {
            window.clearTimeout(timeout);
        }
    }
}
