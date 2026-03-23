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

const { SearchStudioHomeNavigationService } = require('../lib/browser/home/search-studio-home-navigation-service.js');

const provider = {
    name: 'file-share',
    displayName: 'File Share'
};

test('SearchStudioHomeNavigationService opens ingestion from the preferred provider', async () => {
    const service = new SearchStudioHomeNavigationService();
    const selections = [];
    const opened = [];

    service._messageService = { warn() {} };
    service._outputService = { error() {} };
    service._providerCatalogService = {
        snapshot: { providers: [provider] },
        async ensureLoaded() {}
    };
    service._providerSelectionService = {
        selectedProviderName: provider.name,
        selectProvider(targetProvider, workArea) {
            selections.push({ targetProvider, workArea });
        }
    };
    service._documentService = {
        async openIngestionOverview(targetProvider) {
            opened.push({ targetProvider, kind: 'ingestion' });
        }
    };

    await service.openJumpPoint('start-ingestion');

    assert.deepEqual(selections, [{ targetProvider: provider, workArea: 'ingestion' }]);
    assert.deepEqual(opened, [{ targetProvider: provider, kind: 'ingestion' }]);
});

test('SearchStudioHomeNavigationService falls back to the first provider for rules and providers actions', async () => {
    const service = new SearchStudioHomeNavigationService();
    const selections = [];
    const opened = [];

    service._messageService = { warn() {} };
    service._outputService = { error() {} };
    service._providerCatalogService = {
        snapshot: { providers: [provider] },
        async ensureLoaded() {}
    };
    service._providerSelectionService = {
        selectedProviderName: undefined,
        selectProvider(targetProvider, workArea) {
            selections.push({ targetProvider, workArea });
        }
    };
    service._documentService = {
        async openRulesOverview(targetProvider) {
            opened.push({ targetProvider, kind: 'rules' });
        },
        async openProviderOverview(targetProvider) {
            opened.push({ targetProvider, kind: 'providers' });
        }
    };

    await service.openJumpPoint('manage-rules');
    await service.openJumpPoint('browse-providers');

    assert.deepEqual(selections, [
        { targetProvider: provider, workArea: 'rules' },
        { targetProvider: provider, workArea: 'providers' }
    ]);
    assert.deepEqual(opened, [
        { targetProvider: provider, kind: 'rules' },
        { targetProvider: provider, kind: 'providers' }
    ]);
});

test('SearchStudioHomeNavigationService warns when no providers are available', async () => {
    const service = new SearchStudioHomeNavigationService();
    const warnings = [];
    const errors = [];

    service._messageService = {
        warn(message) {
            warnings.push(message);
        }
    };
    service._outputService = {
        error(message, source) {
            errors.push({ message, source });
        }
    };
    service._providerCatalogService = {
        snapshot: { providers: [] },
        async ensureLoaded() {}
    };
    service._providerSelectionService = {
        selectedProviderName: undefined,
        selectProvider() {
            throw new Error('selectProvider should not be called when no providers are available.');
        }
    };
    service._documentService = {
        async openIngestionOverview() {
            throw new Error('No document should open when no providers are available.');
        }
    };

    await service.openJumpPoint('start-ingestion');

    assert.deepEqual(warnings, ['No Studio provider is available for Home navigation.']);
    assert.deepEqual(errors, [
        { message: 'No Studio provider is available for Home navigation.', source: 'home' }
    ]);
});
