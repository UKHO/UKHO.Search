import { SearchStudioOutputEntry } from '../common/search-studio-shell-types';

const defaultOutputTerminalFontFamily = 'Consolas, "Cascadia Mono", "Courier New", monospace';
const defaultOutputTerminalFontSize = 13;
const outputSeverityRgb: Record<SearchStudioOutputEntry['level'], readonly [number, number, number]> = {
    info: [169, 199, 255],
    error: [255, 179, 186]
};
const outputTerminalForegroundResetSequence = '\x1b[39m';

const outputSeverityLabels: Record<SearchStudioOutputEntry['level'], string> = {
    info: 'INFO',
    error: 'ERROR'
};

const outputSeverityColors: Record<SearchStudioOutputEntry['level'], string> = {
    info: 'var(--search-studio-output-info-severity, #A9C7FF)',
    error: 'var(--search-studio-output-error-severity, #FFB3BA)'
};

export function formatOutputTimestamp(timestamp: string): string {
    const date = new Date(timestamp);

    if (Number.isNaN(date.getTime())) {
        return timestamp;
    }

    return date.toISOString().slice(11, 19);
}

export function formatOutputSeverity(level: SearchStudioOutputEntry['level']): string {
    return outputSeverityLabels[level];
}

export function getOutputSeverityColor(level: SearchStudioOutputEntry['level']): string {
    return outputSeverityColors[level];
}

export function formatOutputEntryText(entry: SearchStudioOutputEntry): string {
    return [
        formatOutputTimestamp(entry.timestamp),
        formatOutputSeverity(entry.level),
        entry.source,
        entry.message
    ].join(' ');
}

export function serializeOutputEntries(entries: readonly SearchStudioOutputEntry[]): string {
    return entries.map(formatOutputEntryText).join('\r\n');
}

export function getOutputSeverityAnsiSequence(level: SearchStudioOutputEntry['level']): string {
    const [red, green, blue] = outputSeverityRgb[level];

    return `\x1b[38;2;${red};${green};${blue}m`;
}

export function formatOutputEntryTextForTerminal(entry: SearchStudioOutputEntry): string {
    return [
        formatOutputTimestamp(entry.timestamp),
        `${getOutputSeverityAnsiSequence(entry.level)}${formatOutputSeverity(entry.level)}${outputTerminalForegroundResetSequence}`,
        entry.source,
        entry.message
    ].join(' ');
}

export function serializeOutputEntriesForTerminal(entries: readonly SearchStudioOutputEntry[]): string {
    return entries.map(formatOutputEntryTextForTerminal).join('\r\n');
}

export function resolveOutputTerminalFontOptions(editorFontFamily?: string, editorFontSize?: string): { fontFamily: string; fontSize: number } {
    const normalizedFontFamily = editorFontFamily?.trim();
    const parsedFontSize = Number.parseFloat(editorFontSize?.trim() ?? '');

    return {
        fontFamily: normalizedFontFamily || defaultOutputTerminalFontFamily,
        fontSize: Number.isFinite(parsedFontSize) && parsedFontSize > 0 ? parsedFontSize : defaultOutputTerminalFontSize
    };
}

export function getRevealLatestScrollPosition(entryCount: number, scrollHeight: number): number {
    return entryCount === 0 ? 0 : scrollHeight;
}
