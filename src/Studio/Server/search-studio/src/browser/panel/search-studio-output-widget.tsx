import * as React from '@theia/core/shared/react';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { Message } from '@theia/core/lib/browser/widgets/widget';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { FitAddon } from '@xterm/addon-fit';
import { WebglAddon } from '@xterm/addon-webgl';
import { Terminal } from '@xterm/xterm';
import { SearchStudioOutputService } from '../common/search-studio-output-service';
import { SearchStudioOutputWidgetIconClass, SearchStudioOutputWidgetId, SearchStudioOutputWidgetLabel } from '../search-studio-constants';
import { resolveOutputTerminalFontOptions, serializeOutputEntriesForTerminal } from './search-studio-output-format';

@injectable()
export class SearchStudioOutputWidget extends ReactWidget {

    @inject(SearchStudioOutputService)
    protected readonly _outputService!: SearchStudioOutputService;

    protected _terminalHost: HTMLDivElement | undefined;
    protected _terminal: Terminal | undefined;
    protected _fitAddon: FitAddon | undefined;
    protected _webglAddon: WebglAddon | undefined;
    protected _resizeObserver: ResizeObserver | undefined;
    protected _hasPendingRevealLatest = false;
    protected _renderedEntryIds: string[] = [];

    protected readonly _captureTerminalHost = (element: HTMLDivElement | null): void => {
        if (element === this._terminalHost) {
            return;
        }

        if (!element) {
            this._terminalHost = undefined;
            this.disposeTerminal();
            return;
        }

        this._terminalHost = element;
        this.initializeTerminalHost();
        this.syncTerminalContent();
        this.scheduleFitTerminal();
        this.revealLatestIfNeeded();
    };

    constructor() {
        super();
        this.id = SearchStudioOutputWidgetId;
        this.title.label = SearchStudioOutputWidgetLabel;
        this.title.caption = 'Studio shell output and diagnostics';
        this.title.iconClass = SearchStudioOutputWidgetIconClass;
        this.title.closable = true;
        this.addClass('search-studio-output-widget');
    }

    @postConstruct()
    protected init(): void {
        this.toDispose.push(this.onDidChangeVisibility(isVisible => {
            if (isVisible) {
                this.scheduleTerminalLayoutRefresh();
                this.scheduleDelayedTerminalLayoutRefresh();
            }
        }));
        this.toDispose.push(this._outputService.onDidChangeEntries(() => {
            this._hasPendingRevealLatest = true;
            this.syncTerminalContent();
            this.scheduleRevealLatest();
        }));

        this._hasPendingRevealLatest = this._outputService.entries.length > 0;
        this.update();
    }

    protected override onAfterAttach(msg: Message): void {
        super.onAfterAttach(msg);
        this.scheduleTerminalLayoutRefresh();
        this.scheduleDelayedTerminalLayoutRefresh();
    }

    override dispose(): void {
        this.disposeTerminal();
        super.dispose();
    }

    protected initializeTerminalHost(): void {
        if (!this._terminalHost || this._terminal) {
            return;
        }

        const computedStyle = typeof window !== 'undefined' && typeof window.getComputedStyle === 'function'
            ? window.getComputedStyle(this._terminalHost)
            : undefined;
        const fontOptions = resolveOutputTerminalFontOptions(
            computedStyle?.getPropertyValue('--theia-editor-font-family'),
            computedStyle?.getPropertyValue('--theia-editor-font-size'));
        const resolvedBackgroundColor = computedStyle?.backgroundColor && computedStyle.backgroundColor !== 'rgba(0, 0, 0, 0)'
            ? computedStyle.backgroundColor
            : 'rgb(30, 30, 30)';
        const resolvedForegroundColor = computedStyle?.color || 'rgb(255, 255, 255)';
        const fitAddon = new FitAddon();
        const terminal = new Terminal({
            allowTransparency: false,
            convertEol: true,
            cursorBlink: false,
            cursorStyle: 'bar',
            disableStdin: true,
            fontFamily: fontOptions.fontFamily,
            fontSize: fontOptions.fontSize,
            lineHeight: 1,
            letterSpacing: 0,
            scrollback: 200,
            theme: {
                background: resolvedBackgroundColor,
                foreground: resolvedForegroundColor,
                cursor: 'transparent',
                cursorAccent: 'transparent'
            }
        });

        terminal.loadAddon(fitAddon);
        terminal.open(this._terminalHost);

        const webglAddon = this.initializeWebglAddon(terminal);
        this.observeTerminalHost();
        this.refreshTerminalLayoutWhenFontsAreReady();
        this._terminalHost.setAttribute('data-search-studio-output-surface', 'readonly');

        this._fitAddon = fitAddon;
        this._webglAddon = webglAddon;
        this._terminal = terminal;
        this._renderedEntryIds = [];
    }

    protected initializeWebglAddon(terminal: Terminal): WebglAddon | undefined {
        try {
            const webglAddon = new WebglAddon();

            terminal.loadAddon(webglAddon);

            return webglAddon;
        } catch {
            return undefined;
        }
    }

    protected disposeTerminal(): void {
        this._renderedEntryIds = [];
        this._fitAddon = undefined;
        this._webglAddon = undefined;
        this._resizeObserver?.disconnect();
        this._resizeObserver = undefined;
        this._terminal?.dispose();
        this._terminal = undefined;
    }

    protected observeTerminalHost(): void {
        if (!this._terminalHost || typeof ResizeObserver === 'undefined') {
            return;
        }

        this._resizeObserver?.disconnect();
        this._resizeObserver = new ResizeObserver(() => this.scheduleTerminalLayoutRefresh());
        this._resizeObserver.observe(this._terminalHost);
    }

    protected refreshTerminalLayoutWhenFontsAreReady(): void {
        const fontSet = typeof document !== 'undefined' ? document.fonts : undefined;

        if (!fontSet?.ready) {
            return;
        }

        void fontSet.ready.then(() => {
            this.scheduleTerminalLayoutRefresh();
            this.scheduleDelayedTerminalLayoutRefresh();
        });
    }

    protected syncTerminalContent(): void {
        this.initializeTerminalHost();

        if (!this._terminal) {
            return;
        }

        const entries = this._outputService.entries;
        const entryIds = entries.map(entry => entry.id);

        if (entryIds.length === this._renderedEntryIds.length && entryIds.every((entryId, index) => entryId === this._renderedEntryIds[index])) {
            return;
        }

        const canAppendOnly = this._renderedEntryIds.length > 0
            && this._renderedEntryIds.every((entryId, index) => entryIds[index] === entryId)
            && entryIds.length >= this._renderedEntryIds.length;
        const entriesToWrite = canAppendOnly ? entries.slice(this._renderedEntryIds.length) : entries;
        const serializedEntries = serializeOutputEntriesForTerminal(entriesToWrite);

        if (!canAppendOnly) {
            this._terminal.reset();
        }

        if (serializedEntries) {
            this._terminal.write(`${serializedEntries}\r\n`);
        }

        this._renderedEntryIds = [...entryIds];
        this.scheduleTerminalLayoutRefresh();
    }

    protected scheduleRevealLatest(): void {
        this.scheduleTerminalCallback(() => this.revealLatestIfNeeded());
    }

    protected scheduleFitTerminal(): void {
        this.scheduleTerminalCallback(() => {
            try {
                this._fitAddon?.fit();
            } catch {
            }
        });
    }

    protected scheduleTerminalLayoutRefresh(): void {
        this.scheduleTerminalCallback(() => this.refreshTerminalLayout());
    }

    protected scheduleDelayedTerminalLayoutRefresh(): void {
        setTimeout(() => this.refreshTerminalLayout(), 50);
    }

    protected refreshTerminalLayout(): void {
        if (!this._terminal) {
            return;
        }

        try {
            this._fitAddon?.fit();
        } catch {
        }

        try {
            this._terminal.refresh(0, Math.max(this._terminal.rows - 1, 0));
        } catch {
        }
    }

    protected scheduleTerminalCallback(callback: () => void): void {
        if (typeof window !== 'undefined' && typeof window.requestAnimationFrame === 'function') {
            window.requestAnimationFrame(callback);
            return;
        }

        setTimeout(callback, 0);
    }

    protected revealLatestIfNeeded(): void {
        if (!this._hasPendingRevealLatest || !this._terminal) {
            return;
        }

        if (this._outputService.entries.length === 0) {
            this._hasPendingRevealLatest = false;
            return;
        }

        this._terminal.scrollToBottom();
        this._hasPendingRevealLatest = false;
    }

    protected render(): React.ReactNode {
        return (
            <div
                role="log"
                aria-label={SearchStudioOutputWidgetLabel}
                aria-readonly="true"
                style={{
                    boxSizing: 'border-box',
                    height: '100%',
                    width: '100%',
                    overflow: 'hidden'
                }}
            >
                <div
                    ref={this._captureTerminalHost}
                    style={{
                        height: '100%',
                        width: '100%',
                        padding: '6px 8px',
                        boxSizing: 'border-box'
                    }}
                />
            </div>
        );
    }
}
