import * as React from '@theia/core/shared/react';
import { inject, injectable } from '@theia/core/shared/inversify';
import { CommandRegistry } from '@theia/core/lib/common';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { SearchStudioApiConfigurationService } from './search-studio-api-configuration-service';
import { SearchStudioEchoProbeService } from './search-studio-echo-probe-service';
import { SearchStudioGreetingCommand, SearchStudioWidgetId, SearchStudioWidgetLabel } from './search-studio-constants';
import { SearchStudioEchoProbeResult, SearchStudioFutureApiConfiguration } from './search-studio-future-api-configuration';

@injectable()
export class SearchStudioWidget extends ReactWidget {

    protected echoStatus: 'loading' | 'ready' | 'error' = 'loading';
    protected echoValue?: string;
    protected echoStatusMessage = 'Loading StudioApiHost echo value...';
    protected echoRequest?: Promise<void>;
    protected echoProbeResult?: SearchStudioEchoProbeResult;
    protected studioConfiguration?: SearchStudioFutureApiConfiguration;

    @inject(CommandRegistry)
    protected readonly commandRegistry!: CommandRegistry;

    @inject(SearchStudioApiConfigurationService)
    protected readonly apiConfigurationService!: SearchStudioApiConfigurationService;

    @inject(SearchStudioEchoProbeService)
    protected readonly echoProbeService!: SearchStudioEchoProbeService;

    constructor() {
        super();
        this.id = SearchStudioWidgetId;
        this.title.label = SearchStudioWidgetLabel;
        this.title.caption = 'Welcome to UKHO Search Studio';
        this.title.closable = true;
        this.addClass('search-studio-welcome-widget');
        this.update();
    }

    protected loadEchoValue(): void {
        if (!this.echoRequest) {
            this.echoRequest = this.fetchEchoValue();
        }
    }

    protected async fetchEchoValue(): Promise<void> {
        try {
            this.studioConfiguration = await this.apiConfigurationService.getConfiguration();
            this.echoProbeResult = await this.echoProbeService.getProbeResult();

            if (!this.echoProbeResult.echoValue) {
                this.echoStatus = 'error';
                this.echoStatusMessage = this.echoProbeResult.error ?? 'StudioApiHost echo value could not be loaded.';
                return;
            }

            this.echoValue = this.echoProbeResult.echoValue;
            this.echoStatus = 'ready';
            this.echoStatusMessage = 'StudioApiHost echo value loaded.';
        } catch (error) {
            console.error('Failed to load the StudioApiHost echo value.', error);
            this.echoStatus = 'error';
            this.echoStatusMessage = error instanceof Error
                ? error.message
                : 'StudioApiHost echo value could not be loaded.';
        } finally {
            this.update();
        }
    }

    protected render(): React.ReactNode {
        this.loadEchoValue();

        return (
            <div style={{ padding: '16px', display: 'grid', gap: '12px', maxWidth: '720px' }}>
                <h1 style={{ margin: 0 }}>{SearchStudioWidgetLabel}</h1>
                <p style={{ margin: 0 }}>
                    Welcome to the first Eclipse Theia shell for UKHO Search. This starter view keeps the standard workbench intact
                    while proving that the custom native Theia extension is active.
                </p>
                <p style={{ margin: 0 }}>
                    This work package intentionally stays lightweight and does not migrate existing tooling workflows into the shell.
                </p>
                {this.renderEchoStatus()}
                <div>
                    <button
                        type="button"
                        className="theia-button"
                        onClick={() => this.commandRegistry.executeCommand(SearchStudioGreetingCommand.id)}
                    >
                        {SearchStudioGreetingCommand.label}
                    </button>
                </div>
            </div>
        );
    }

    protected renderEchoStatus(): React.ReactNode {
        if (this.echoStatus === 'ready') {
            return (
                <div style={{ padding: '12px', border: '1px solid var(--theia-panel-border)', borderRadius: '4px' }}>
                    <strong>StudioApiHost echo:</strong>
                    <div>{this.echoValue}</div>
                    {this.renderDebugInfo()}
                </div>
            );
        }

        if (this.echoStatus === 'error') {
            return (
                <div style={{ padding: '12px', border: '1px solid var(--theia-errorForeground)', borderRadius: '4px' }}>
                    <strong>StudioApiHost echo unavailable</strong>
                    <div>{this.echoStatusMessage}</div>
                    {this.renderDebugInfo()}
                </div>
            );
        }

        return (
            <div style={{ padding: '12px', border: '1px solid var(--theia-panel-border)', borderRadius: '4px' }}>
                <strong>StudioApiHost echo</strong>
                <div>{this.echoStatusMessage}</div>
                {this.renderDebugInfo()}
            </div>
        );
    }

    protected renderDebugInfo(): React.ReactNode {
        const probeResult = this.echoProbeResult;
        const configuration = this.studioConfiguration;
        const browserOrigin = typeof window === 'undefined' ? undefined : window.location.origin;

        return (
            <div style={{ marginTop: '12px', display: 'grid', gap: '4px', fontSize: '0.9em', opacity: 0.9 }}>
                <div><strong>Browser origin:</strong> {browserOrigin ?? 'Unavailable'}</div>
                <div><strong>Probe transport:</strong> {probeResult?.transport ?? 'theia-backend-proxy'}</div>
                <div><strong>Theia probe endpoint:</strong> {probeResult?.probeEndpointUrl ?? `${browserOrigin ?? ''}/search-studio/api/echo`}</div>
                <div><strong>Theia config endpoint:</strong> {probeResult?.configurationEndpointUrl ?? `${browserOrigin ?? ''}/search-studio/api/configuration`}</div>
                <div><strong>StudioApiHost env var:</strong> {probeResult?.environmentVariableName ?? configuration?.environmentVariableName ?? 'Unavailable'}</div>
                <div><strong>Raw StudioApiHost env value:</strong> {probeResult?.rawStudioApiHostBaseUrl ?? configuration?.rawStudioApiHostBaseUrl ?? 'Unavailable'}</div>
                <div><strong>Configured StudioApiHost base URL:</strong> {probeResult?.studioApiHostBaseUrl ?? configuration?.studioApiHostBaseUrl ?? 'Unavailable'}</div>
                <div><strong>Attempted StudioApiHost echo URL:</strong> {probeResult?.studioApiHostEchoUrl ?? 'Unavailable'}</div>
                <div><strong>Probe HTTP status:</strong> {probeResult?.statusCode ? `${probeResult.statusCode} ${probeResult.statusText ?? ''}` : 'Unavailable'}</div>
                {probeResult?.error ? <div><strong>Probe error:</strong> {probeResult.error}</div> : undefined}
            </div>
        );
    }
}
