import * as http from 'http';
import * as https from 'https';
import * as express from 'express';
import { BackendApplicationContribution } from '@theia/core/lib/node';
import { injectable } from '@theia/core/shared/inversify';
import {
    normalizeStudioHostBaseUrl,
    SearchStudioEchoProbeEndpointPath,
    SearchStudioEchoProbeResult,
    SearchStudioFutureApiConfiguration,
    SearchStudioFutureApiConfigurationEndpointPath,
    SearchStudioFutureApiEnvironmentVariableName,
    SearchStudioBackendRequestTimeoutMilliseconds
} from '../browser/search-studio-future-api-configuration';

@injectable()
export class SearchStudioBackendApplicationContribution implements BackendApplicationContribution {

    configure(app: express.Application): void {
        app.get(SearchStudioFutureApiConfigurationEndpointPath, (_request, response) => {
            const rawStudioHostBaseUrl = process.env[SearchStudioFutureApiEnvironmentVariableName];

            // The browser cannot read the Node.js process environment directly.
            // Expose only the normalized StudioHost base URL needed by the welcome proof.
            const configuration: SearchStudioFutureApiConfiguration = {
                studioHostBaseUrl: normalizeStudioHostBaseUrl(rawStudioHostBaseUrl),
                rawStudioHostBaseUrl,
                environmentVariableName: SearchStudioFutureApiEnvironmentVariableName
            };

            response.json(configuration);
        });

        app.get(SearchStudioEchoProbeEndpointPath, async (request, response) => {
            const rawStudioHostBaseUrl = process.env[SearchStudioFutureApiEnvironmentVariableName];
            const studioHostBaseUrl = normalizeStudioHostBaseUrl(rawStudioHostBaseUrl);
            const requestOrigin = `${request.protocol}://${request.get('host')}`;
            const probeResult: SearchStudioEchoProbeResult = {
                transport: 'theia-backend-proxy',
                configurationEndpointUrl: `${requestOrigin}${SearchStudioFutureApiConfigurationEndpointPath}`,
                probeEndpointUrl: `${requestOrigin}${SearchStudioEchoProbeEndpointPath}`,
                studioHostBaseUrl,
                rawStudioHostBaseUrl,
                environmentVariableName: SearchStudioFutureApiEnvironmentVariableName,
                studioHostEchoUrl: studioHostBaseUrl
                    ? new URL('/echo', `${studioHostBaseUrl}/`).toString()
                    : undefined
            };

            if (!probeResult.studioHostBaseUrl || !probeResult.studioHostEchoUrl) {
                response.json({
                    ...probeResult,
                    error: 'StudioHost base URL is not configured for the studio shell.'
                });
                return;
            }

            try {
                console.info('Running StudioHost echo probe.', probeResult);

                const studioHostResponse = await this.getStudioHostEchoResponse(
                    probeResult.studioHostEchoUrl,
                    SearchStudioBackendRequestTimeoutMilliseconds);

                response.json({
                    ...probeResult,
                    statusCode: studioHostResponse.status,
                    statusText: studioHostResponse.statusText,
                    echoValue: studioHostResponse.ok ? studioHostResponse.body : undefined,
                    error: studioHostResponse.ok
                        ? undefined
                        : `StudioHost echo request failed: ${studioHostResponse.status} ${studioHostResponse.statusText}${studioHostResponse.body ? ` - ${studioHostResponse.body}` : ''}`
                });
            } catch (error) {
                const errorMessage = error instanceof Error
                    ? error.message
                    : 'StudioHost echo request failed.';

                console.error('Failed to run the StudioHost echo probe.', error);

                response.json({
                    ...probeResult,
                    error: errorMessage
                });
            }
        });
    }

    protected async getStudioHostEchoResponse(
        studioHostEchoUrl: string,
        timeoutMilliseconds: number
    ): Promise<{ ok: boolean; status: number; statusText: string; body: string; }>
    {
        const requestUrl = new URL(studioHostEchoUrl);
        const isHttpsRequest = requestUrl.protocol === 'https:';
        const requestFactory = isHttpsRequest ? https.request : http.request;

        // Local Aspire uses ASP.NET development certificates.
        // Allow the local HTTPS probe to succeed even when Node does not trust that certificate chain.
        const allowLocalhostSelfSignedCertificate = isHttpsRequest
            && (requestUrl.hostname === 'localhost' || requestUrl.hostname === '127.0.0.1');

        return await new Promise((resolve, reject) => {
            const request = requestFactory(requestUrl, {
                method: 'GET',
                headers: {
                    Accept: 'text/plain'
                },
                rejectUnauthorized: allowLocalhostSelfSignedCertificate ? false : undefined
            }, response => {
                const chunks: Buffer[] = [];

                response.on('data', chunk => {
                    chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
                });

                response.on('end', () => {
                    resolve({
                        ok: (response.statusCode ?? 0) >= 200 && (response.statusCode ?? 0) < 300,
                        status: response.statusCode ?? 0,
                        statusText: response.statusMessage ?? 'Unknown status',
                        body: Buffer.concat(chunks).toString('utf8')
                    });
                });
            });

            request.setTimeout(timeoutMilliseconds, () => {
                request.destroy(new Error(`StudioHost echo request timed out after ${timeoutMilliseconds}ms.`));
            });

            request.on('error', reject);
            request.end();
        });
    }
}
