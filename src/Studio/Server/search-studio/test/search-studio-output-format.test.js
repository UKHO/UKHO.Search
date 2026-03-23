const test = require('node:test');
const assert = require('node:assert/strict');
const {
    formatOutputEntryText,
    formatOutputEntryTextForTerminal,
    formatOutputSeverity,
    formatOutputTimestamp,
    getOutputSeverityAnsiSequence,
    getOutputSeverityColor,
    getRevealLatestScrollPosition,
    resolveOutputTerminalFontOptions,
    serializeOutputEntries,
    serializeOutputEntriesForTerminal
} = require('../lib/browser/panel/search-studio-output-format.js');

test('formatOutputTimestamp trims ISO timestamps to hh:mm:ss', () => {
    assert.equal(formatOutputTimestamp('2026-03-23T10:03:37.280Z'), '10:03:37');
});

test('formatOutputTimestamp returns the original value for invalid timestamps', () => {
    assert.equal(formatOutputTimestamp('not-a-date'), 'not-a-date');
});

test('formatOutputSeverity returns stable uppercase tokens', () => {
    assert.equal(formatOutputSeverity('info'), 'INFO');
    assert.equal(formatOutputSeverity('error'), 'ERROR');
});

test('getOutputSeverityColor returns the agreed pastel token colors', () => {
    assert.equal(getOutputSeverityColor('info'), 'var(--search-studio-output-info-severity, #A9C7FF)');
    assert.equal(getOutputSeverityColor('error'), 'var(--search-studio-output-error-severity, #FFB3BA)');
});

test('getOutputSeverityAnsiSequence returns terminal-compatible truecolor escape sequences', () => {
    assert.equal(getOutputSeverityAnsiSequence('info'), '\u001b[38;2;169;199;255m');
    assert.equal(getOutputSeverityAnsiSequence('error'), '\u001b[38;2;255;179;186m');
});

test('formatOutputEntryText preserves merged-stream line ordering and source metadata', () => {
    assert.equal(
        formatOutputEntryText({
            id: 'entry-7',
            timestamp: '2026-03-23T10:03:37.280Z',
            level: 'info',
            source: 'providers',
            message: 'Loaded provider metadata.'
        }),
        '10:03:37 INFO providers Loaded provider metadata.'
    );
});

test('formatOutputEntryTextForTerminal styles the severity token while preserving visible text', () => {
    assert.equal(
        formatOutputEntryTextForTerminal({
            id: 'entry-7',
            timestamp: '2026-03-23T10:03:37.280Z',
            level: 'error',
            source: 'rules',
            message: 'Rule validation failed.'
        }),
        '10:03:37 \u001b[38;2;255;179;186mERROR\u001b[39m rules Rule validation failed.'
    );
});

test('serializeOutputEntriesForTerminal preserves ordering with styled severity tokens', () => {
    assert.equal(
        serializeOutputEntriesForTerminal([
            {
                id: 'entry-1',
                timestamp: '2026-03-23T10:03:37.280Z',
                level: 'info',
                source: 'providers',
                message: 'Loaded provider metadata.'
            },
            {
                id: 'entry-2',
                timestamp: '2026-03-23T10:03:38.280Z',
                level: 'error',
                source: 'rules',
                message: 'Rule validation failed.'
            }
        ]),
        '10:03:37 \u001b[38;2;169;199;255mINFO\u001b[39m providers Loaded provider metadata.\r\n10:03:38 \u001b[38;2;255;179;186mERROR\u001b[39m rules Rule validation failed.'
    );
});

test('serializeOutputEntries preserves chronological terminal line serialization', () => {
    assert.equal(
        serializeOutputEntries([
            {
                id: 'entry-1',
                timestamp: '2026-03-23T10:03:37.280Z',
                level: 'info',
                source: 'providers',
                message: 'Loaded provider metadata.'
            },
            {
                id: 'entry-2',
                timestamp: '2026-03-23T10:03:38.280Z',
                level: 'error',
                source: 'rules',
                message: 'Rule validation failed.'
            }
        ]),
        '10:03:37 INFO providers Loaded provider metadata.\r\n10:03:38 ERROR rules Rule validation failed.'
    );
});

test('resolveOutputTerminalFontOptions prefers a resolved editor font family and size', () => {
    assert.deepEqual(
        resolveOutputTerminalFontOptions('Cascadia Mono, monospace', '14px'),
        {
            fontFamily: 'Cascadia Mono, monospace',
            fontSize: 14
        });
});

test('resolveOutputTerminalFontOptions falls back to the default monospace stack', () => {
    assert.deepEqual(
        resolveOutputTerminalFontOptions('   ', undefined),
        {
            fontFamily: 'Consolas, "Cascadia Mono", "Courier New", monospace',
            fontSize: 13
        });
});

test('getRevealLatestScrollPosition scrolls to the latest line when output exists', () => {
    assert.equal(getRevealLatestScrollPosition(3, 480), 480);
});

test('getRevealLatestScrollPosition resets to the top for an empty output pane', () => {
    assert.equal(getRevealLatestScrollPosition(0, 480), 0);
});
