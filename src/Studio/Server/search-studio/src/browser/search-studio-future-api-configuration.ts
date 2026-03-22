export interface SearchStudioFutureApiConfiguration {
    readonly studioApiHostBaseUrl?: string;
    readonly rawStudioApiHostBaseUrl?: string;
    readonly environmentVariableName?: string;
}

export interface SearchStudioEchoProbeResult {
    readonly transport: 'browser-direct' | 'theia-backend-proxy';
    readonly configurationEndpointUrl: string;
    readonly probeEndpointUrl?: string;
    readonly studioApiHostBaseUrl?: string;
    readonly rawStudioApiHostBaseUrl?: string;
    readonly environmentVariableName?: string;
    readonly studioApiHostEchoUrl?: string;
    readonly echoValue?: string;
    readonly error?: string;
    readonly statusCode?: number;
    readonly statusText?: string;
}

export const SearchStudioFutureApiConfigurationKey = 'StudioApiHost.ApiBaseUrl';
export const SearchStudioFutureApiEnvironmentVariableName = 'STUDIO_API_HOST_API_BASE_URL';
export const SearchStudioFutureApiConfigurationEndpointPath = '/search-studio/api/configuration';
export const SearchStudioEchoProbeEndpointPath = '/search-studio/api/echo';
export const SearchStudioBackendRequestTimeoutMilliseconds = 3000;
export const SearchStudioFrontendRequestTimeoutMilliseconds = 8000;

export function normalizeStudioApiHostBaseUrl(studioApiHostBaseUrl?: string): string | undefined {
    const trimmedStudioApiHostBaseUrl = studioApiHostBaseUrl?.trim();

    if (!trimmedStudioApiHostBaseUrl) {
        return undefined;
    }

    return trimmedStudioApiHostBaseUrl.endsWith('/')
        ? trimmedStudioApiHostBaseUrl.slice(0, -1)
        : trimmedStudioApiHostBaseUrl;
}
