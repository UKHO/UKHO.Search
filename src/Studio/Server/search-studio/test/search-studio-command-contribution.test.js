const test = require('node:test');
const assert = require('node:assert/strict');

global.navigator = { platform: 'Win32', userAgent: 'node.js' };
global.document = {
    createElement: () => ({ style: {}, classList: { add() {}, remove() {} }, setAttribute() {}, removeAttribute() {}, nodeType: 1, ownerDocument: null }),
    documentElement: { style: {} },
    body: { classList: { add() {}, remove() {} } },
    queryCommandSupported() {
        return false;
    },
    addEventListener() {},
    removeEventListener() {}
};
global.window = {
    navigator: global.navigator,
    document: global.document,
    localStorage: {
        getItem() {
            return undefined;
        },
        setItem() {},
        removeItem() {}
    }
};
global.HTMLElement = class HTMLElement {};
global.Element = global.HTMLElement;
global.DragEvent = class DragEvent {};
require.extensions['.css'] = () => {};

const {
    SearchStudioCommandContribution
} = require('../lib/browser/search-studio-command-contribution.js');
const {
    SearchStudioCopyAllOutputCommand
} = require('../lib/browser/search-studio-constants.js');

test('SearchStudioCommandContribution copies the full merged output stream to the clipboard', async () => {
    const originalNavigator = global.navigator;
    const clipboardWrites = [];
    global.navigator = {
        clipboard: {
            async writeText(value) {
                clipboardWrites.push(value);
            }
        }
    };

    try {
        const contribution = new SearchStudioCommandContribution();
        const registeredCommands = new Map();
        const infoMessages = [];

        contribution._messageService = {
            info: message => infoMessages.push(message),
            warn() {},
            error() {}
        };
        contribution._outputService = {
            entries: [
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
            ]
        };
        contribution._providerCatalogService = {
            refresh: async () => {},
            ensureLoaded: async () => {},
            snapshot: { providers: [] }
        };
        contribution._rulesCatalogService = {
            refresh: async () => {}
        };
        contribution._providerSelectionService = {
            selectedProviderName: undefined,
            selectProvider() {}
        };
        contribution._documentService = {};

        contribution.registerCommands({
            registerCommand: (command, handler) => {
                registeredCommands.set(command.id, handler);
            }
        });

        await registeredCommands.get(SearchStudioCopyAllOutputCommand.id).execute();

        assert.deepEqual(clipboardWrites, [
            '10:03:37 INFO providers Loaded provider metadata.\r\n10:03:38 ERROR rules Rule validation failed.'
        ]);
        assert.deepEqual(infoMessages, ['Studio output copied to the clipboard.']);
    } finally {
        global.navigator = originalNavigator;
    }
});

test('SearchStudioCommandContribution warns when clipboard copy is unavailable', async () => {
    const originalNavigator = global.navigator;
    global.navigator = {};

    try {
        const contribution = new SearchStudioCommandContribution();
        const registeredCommands = new Map();
        const warnMessages = [];

        contribution._messageService = {
            info() {},
            warn: message => warnMessages.push(message),
            error() {}
        };
        contribution._outputService = { entries: [] };
        contribution._providerCatalogService = {
            refresh: async () => {},
            ensureLoaded: async () => {},
            snapshot: { providers: [] }
        };
        contribution._rulesCatalogService = {
            refresh: async () => {}
        };
        contribution._providerSelectionService = {
            selectedProviderName: undefined,
            selectProvider() {}
        };
        contribution._documentService = {};

        contribution.registerCommands({
            registerCommand: (command, handler) => {
                registeredCommands.set(command.id, handler);
            }
        });

        await registeredCommands.get(SearchStudioCopyAllOutputCommand.id).execute();

        assert.deepEqual(warnMessages, ['Clipboard access is unavailable for Studio Output.']);
    } finally {
        global.navigator = originalNavigator;
    }
});
