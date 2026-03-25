const test = require('node:test');
const assert = require('node:assert/strict');

// Provide the minimal browser-like globals that the Theia React widget base class expects when the tests run under Node.
function createElementStub() {
    return {
        style: {},
        classList: { add() {}, remove() {} },
        setAttribute() {},
        removeAttribute() {},
        appendChild() {},
        remove() {},
        addEventListener() {},
        removeEventListener() {},
        nodeType: 1,
        nodeName: 'DIV',
        tagName: 'DIV',
        ownerDocument: global.document
    };
}

global.navigator = { platform: 'Win32', userAgent: 'node.js' };
global.document = {
    createElement: () => createElementStub(),
    documentElement: { style: {} },
    head: { appendChild() {} },
    body: { classList: { add() {}, remove() {} }, addEventListener() {}, removeEventListener() {} },
    getElementById() {
        return null;
    },
    queryCommandSupported() {
        return false;
    },
    addEventListener() {},
    removeEventListener() {}
};
global.window = {
    navigator: global.navigator,
    document: global.document,
    addEventListener() {},
    removeEventListener() {},
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

const { SearchStudioPrimeReactDemoWidget } = require('../lib/browser/primereact-demo/search-studio-primereact-demo-widget.js');

/**
 * Verifies that the PrimeReact demo widget requests an initial render immediately so restored Theia tabs do not reopen blank.
 */
test('SearchStudioPrimeReactDemoWidget requests an initial render during construction', () => {
    let updateCalls = 0;

    class TestSearchStudioPrimeReactDemoWidget extends SearchStudioPrimeReactDemoWidget {
        update() {
            updateCalls += 1;
        }
    }

    const widget = new TestSearchStudioPrimeReactDemoWidget({
        async enableStyledMode() {},
        disableStyledMode() {}
    });

    assert.ok(widget);
    assert.equal(updateCalls, 1);
});

/**
 * Verifies that the PrimeReact demo widget persists and restores the active logical page for Theia layout restoration.
 */
test('SearchStudioPrimeReactDemoWidget restores the previously active page from persisted state', () => {
    class TestSearchStudioPrimeReactDemoWidget extends SearchStudioPrimeReactDemoWidget {
        update() {}
    }

    const widget = new TestSearchStudioPrimeReactDemoWidget({
        async enableStyledMode() {},
        disableStyledMode() {}
    });

    widget.setActiveDemoPage('showcase');
    const storedState = widget.storeState();

    assert.deepEqual(storedState, {
        activeDemoPageId: 'showcase'
    });

    widget.restoreState({
        activeDemoPageId: 'tree'
    });

    assert.equal(widget.title.label, 'PrimeReact Tree Demo');
    assert.equal(widget.title.caption, 'Temporary PrimeReact Tree evaluation demo');
    assert.deepEqual(widget.storeState(), {
        activeDemoPageId: 'tree'
    });
});
