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
const { SearchStudioViewContribution } = require('../lib/browser/search-studio-view-contribution.js');
const { SearchStudioRulesViewContribution } = require('../lib/browser/rules/search-studio-rules-view-contribution.js');
const { SearchStudioIngestionViewContribution } = require('../lib/browser/ingestion/search-studio-ingestion-view-contribution.js');
const { SearchStudioSearchViewContribution } = require('../lib/browser/search/search-studio-search-view-contribution.js');

test('SearchStudio left activity contributions rank ahead of Explore', () => {
    const providers = new SearchStudioViewContribution();
    const rules = new SearchStudioRulesViewContribution();
    const ingestion = new SearchStudioIngestionViewContribution();
    const search = new SearchStudioSearchViewContribution();

    assert.equal(providers.defaultViewOptions.rank, 10);
    assert.equal(rules.defaultViewOptions.rank, 20);
    assert.equal(ingestion.defaultViewOptions.rank, 30);
    assert.equal(search.defaultViewOptions.rank, 40);
    assert.ok(search.defaultViewOptions.rank < 100);
});

test('SearchStudioShellLayoutContribution reveals Studio views and defaults to Providers', async () => {
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
    contribution._searchViewContribution = {
        async openView(options) {
            calls.push({ target: 'search', options });
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
        { target: 'search', options: { activate: false, reveal: true } },
        { target: 'output', options: { activate: false, reveal: true } },
        { target: 'home' },
        { target: 'providers', options: { activate: true, reveal: true } }
    ]);
});
