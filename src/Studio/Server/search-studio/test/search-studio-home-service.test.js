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

const { SearchStudioHomeService } = require('../lib/browser/home/search-studio-home-service.js');

test('SearchStudioHomeService opens Home in the main area and activates the tab', async () => {
    const service = new SearchStudioHomeService();
    const widget = {
        id: 'search-studio.home',
        isAttached: false
    };
    const existingFirstWidget = {
        id: 'search-studio.document.providers.first'
    };
    const addWidgetCalls = [];
    const activateWidgetCalls = [];
    const outputMessages = [];

    service._widgetManager = {
        async getOrCreateWidget(factoryId) {
            assert.equal(factoryId, 'search-studio.home');
            return widget;
        }
    };
    service._shell = {
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
    };
    service._outputService = {
        info(message, source) {
            outputMessages.push({ message, source });
        }
    };

    await service.openHome();
    await service.openHome();

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
    assert.deepEqual(activateWidgetCalls, ['search-studio.home', 'search-studio.home']);
    assert.deepEqual(outputMessages, [
        { message: 'Opened Home.', source: 'home' },
        { message: 'Opened Home.', source: 'home' }
    ]);
});
