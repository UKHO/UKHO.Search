const test = require('node:test');
const assert = require('node:assert/strict');

global.navigator = { platform: 'Win32', userAgent: 'node.js' };
global.document = {
    createElement: () => ({
        style: {},
        classList: { add() {}, remove() {} },
        setAttribute() {},
        removeAttribute() {},
        appendChild() {},
        removeChild() {},
        addEventListener() {},
        removeEventListener() {},
        nodeType: 1,
        ownerDocument: null
    }),
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
require.extensions['.png'] = module => {
    module.exports = 'ukho-logo-transparent.png';
};

const { SearchStudioHomeWidget } = require('../lib/browser/home/search-studio-home-widget.js');

test('SearchStudioHomeWidget requests an initial render and stays closable', () => {
    const originalUpdate = SearchStudioHomeWidget.prototype.update;
    let updateCalls = 0;

    SearchStudioHomeWidget.prototype.update = function () {
        updateCalls += 1;
    };

    try {
        const widget = new SearchStudioHomeWidget();

        assert.equal(widget.title.label, 'Home');
        assert.equal(widget.title.closable, true);
        assert.equal(updateCalls, 1);
    } finally {
        SearchStudioHomeWidget.prototype.update = originalUpdate;
    }
});
