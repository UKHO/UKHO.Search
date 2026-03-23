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

const { SearchStudioShellLayoutContribution } = require('../lib/browser/search-studio-shell-layout-contribution.js');

test('SearchStudioShellLayoutContribution reveals Studio views and opens Home once', async () => {
    const contribution = new SearchStudioShellLayoutContribution();
    const calls = [];

    contribution._providersViewContribution = {
        async openView(options) {
            calls.push({ target: 'providers', options });
        }
    };
    contribution._rulesViewContribution = {
        async openView(options) {
            calls.push({ target: 'rules', options });
        }
    };
    contribution._ingestionViewContribution = {
        async openView(options) {
            calls.push({ target: 'ingestion', options });
        }
    };
    contribution._outputViewContribution = {
        async openView(options) {
            calls.push({ target: 'output', options });
        }
    };
    contribution._homeService = {
        async openHome() {
            calls.push({ target: 'home' });
        }
    };

    await contribution.initializeLayout({});
    await contribution.onDidInitializeLayout({});

    assert.deepEqual(calls, [
        { target: 'providers', options: { activate: false, reveal: true } },
        { target: 'rules', options: { activate: false, reveal: true } },
        { target: 'ingestion', options: { activate: false, reveal: true } },
        { target: 'output', options: { activate: false, reveal: true } },
        { target: 'home' }
    ]);
});
