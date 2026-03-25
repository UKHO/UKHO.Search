const test = require('node:test');
const assert = require('node:assert/strict');

// Provide the minimal browser-like globals that Theia service imports expect when the tests run under Node.
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

const { SearchStudioPrimeReactDemoService } = require('../lib/browser/primereact-demo/search-studio-primereact-demo-service.js');

/**
 * Verifies that the temporary PrimeReact demo service attaches the demo as a standard main-area document and reuses the same tab on reopen.
 */
test('SearchStudioPrimeReactDemoService opens the temporary PrimeReact demo in the main area and activates the tab', async () => {
    const widget = {
        id: 'search-studio.primereact-demo',
        isAttached: false,
        openedPages: [],
        setActiveDemoPage(pageId) {
            this.openedPages.push(pageId);
        }
    };
    const existingFirstWidget = {
        id: 'search-studio.document.providers.first'
    };
    const addWidgetCalls = [];
    const activateWidgetCalls = [];
    const originalConsoleInfo = console.info;
    const infoMessages = [];

    console.info = message => {
        infoMessages.push(message);
    };

    try {
        // Use lightweight fakes so the test can focus on the temporary demo tab placement contract instead of real Theia services.
        const service = new SearchStudioPrimeReactDemoService(
            {
                mainPanel: {
                    *widgets() {
                        yield existingFirstWidget;
                    }
                },
                async addWidget(targetWidget, options) {
                    addWidgetCalls.push({ targetWidget, options });
                    targetWidget.isAttached = true;
                },
                async activateWidget(widgetId) {
                    activateWidgetCalls.push(widgetId);
                }
            },
            {
                async getOrCreateWidget(factoryId) {
                    assert.equal(factoryId, 'search-studio.primereact-demo');
                    return widget;
                }
            }
        );

        await service.openDemo('datatable');
        await service.openDemo('tree');

        assert.deepEqual(addWidgetCalls, [
            {
                targetWidget: widget,
                options: {
                    area: 'main',
                    mode: 'tab-before',
                    ref: existingFirstWidget
                }
            }
        ]);
        assert.deepEqual(widget.openedPages, ['datatable', 'tree']);
        assert.deepEqual(activateWidgetCalls, ['search-studio.primereact-demo', 'search-studio.primereact-demo']);
        assert.deepEqual(infoMessages, ['Opened temporary PrimeReact demo page.', 'Opened temporary PrimeReact demo page.']);
    } finally {
        // Restore console state so later tests keep the normal logging implementation.
        console.info = originalConsoleInfo;
    }
});
