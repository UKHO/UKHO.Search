export interface SearchStudioFutureApiConfiguration {
    readonly studioHostBaseUrl?: string;
    readonly rawStudioHostBaseUrl?: string;
    readonly environmentVariableName?: string;
}

export interface SearchStudioEchoProbeResult {
    readonly transport: 'browser-direct' | 'theia-backend-proxy';
    readonly configurationEndpointUrl: string;
    readonly probeEndpointUrl?: string;
    readonly studioHostBaseUrl?: string;
    readonly rawStudioHostBaseUrl?: string;
    readonly environmentVariableName?: string;
    readonly studioHostEchoUrl?: string;
    readonly echoValue?: string;
    readonly error?: string;
    readonly statusCode?: number;
    readonly statusText?: string;
}

export const SearchStudioFutureApiConfigurationKey = 'StudioHost.ApiBaseUrl';
export const SearchStudioFutureApiEnvironmentVariableName = 'STUDIO_HOST_API_BASE_URL';
export const SearchStudioFutureApiConfigurationEndpointPath = '/search-studio/api/configuration';
export const SearchStudioEchoProbeEndpointPath = '/search-studio/api/echo';
export const SearchStudioBackendRequestTimeoutMilliseconds = 3000;
export const SearchStudioFrontendRequestTimeoutMilliseconds = 8000;

export function normalizeStudioHostBaseUrl(studioHostBaseUrl?: string): string | undefined {
    const trimmedStudioHostBaseUrl = studioHostBaseUrl?.trim();

    if (!trimmedStudioHostBaseUrl) {
        return undefined;
    }

    return trimmedStudioHostBaseUrl.endsWith('/')
        ? trimmedStudioHostBaseUrl.slice(0, -1)
        : trimmedStudioHostBaseUrl;
}
