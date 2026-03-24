const test = require('node:test');
const assert = require('node:assert/strict');

// Provide the minimal browser-like globals that Theia widget imports expect when the tests run under Node.
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

/**
 * Collects every string child from the rendered React element tree so the tests can verify visible copy.
 *
 * @param {unknown} node Supplies the current React node or primitive value being traversed.
 * @param {string[]} values Stores the flattened text fragments discovered during traversal.
 */
function collectRenderedText(node, values) {
    // Ignore empty nodes because they do not contribute any rendered copy to the Home document.
    if (node === undefined || node === null || typeof node === 'boolean') {
        return;
    }

    // Record primitive text nodes directly so the assertions can reason about the final visible content.
    if (typeof node === 'string' || typeof node === 'number') {
        values.push(String(node));
        return;
    }

    // Walk array children recursively because React can return multiple sibling nodes from a single parent.
    if (Array.isArray(node)) {
        for (const childNode of node) {
            collectRenderedText(childNode, values);
        }

        return;
    }

    // Continue into element children whenever the current object looks like a React element payload.
    if (node && typeof node === 'object' && node.props) {
        collectRenderedText(node.props.children, values);
    }
}

/**
 * Collects every rendered class name from the React element tree so the tests can verify removed panels stay absent.
 *
 * @param {unknown} node Supplies the current React node or primitive value being traversed.
 * @param {string[]} values Stores the flattened class names discovered during traversal.
 */
function collectRenderedClassNames(node, values) {
    // Ignore primitives because only React element objects can contribute class names.
    if (!node || typeof node !== 'object') {
        return;
    }

    // Walk array children recursively because React can return multiple sibling nodes from a single parent.
    if (Array.isArray(node)) {
        for (const childNode of node) {
            collectRenderedClassNames(childNode, values);
        }

        return;
    }

    if (node.props) {
        // Record the current element class name before traversing its children.
        if (typeof node.props.className === 'string') {
            values.push(node.props.className);
        }

        collectRenderedClassNames(node.props.children, values);
    }
}

/**
 * Verifies that the restored Home widget remains a normal closable document tab and requests its first render.
 */
test('SearchStudioHomeWidget requests an initial render and stays closable', () => {
    const originalUpdate = SearchStudioHomeWidget.prototype.update;
    let updateCalls = 0;

    SearchStudioHomeWidget.prototype.update = function () {
        updateCalls += 1;
    };

    try {
        // Construct the widget once so the test can validate the restored Home tab contract.
        const widget = new SearchStudioHomeWidget();

        assert.equal(widget.id, 'search-studio.home');
        assert.equal(widget.title.label, 'Home');
        assert.equal(widget.title.closable, true);
        assert.equal(widget.title.iconClass, 'codicon codicon-home');
        assert.equal(updateCalls, 1);
    } finally {
        // Restore the original widget update method so later tests keep the real implementation.
        SearchStudioHomeWidget.prototype.update = originalUpdate;
    }
});

/**
 * Verifies that the cleaned Home widget keeps the primary Studio orientation content while removing the lower explanatory box.
 */
test('SearchStudioHomeWidget renders Studio-owned landing content without the lower explanatory box', () => {
    const originalUpdate = SearchStudioHomeWidget.prototype.update;

    SearchStudioHomeWidget.prototype.update = function () {
        // Suppress the inherited asynchronous refresh scheduling so the test can inspect the render tree deterministically.
    };

    try {
        // Construct the widget once so the test can inspect the current Home document render tree.
        const widget = new SearchStudioHomeWidget();
        const renderedNode = widget.render();
        const renderedText = [];
        const renderedClassNames = [];

        collectRenderedText(renderedNode, renderedText);
        collectRenderedClassNames(renderedNode, renderedClassNames);

        const flattenedText = renderedText.join(' ');

        assert.match(flattenedText, /Search Studio/);
        assert.match(flattenedText, /Current scope/);
        assert.doesNotMatch(flattenedText, /What to expect/);
        assert.doesNotMatch(flattenedText, /Generated Theia welcome surfaces may still appear alongside this tab while the new shell is rebuilt incrementally\./);
        assert.equal(renderedClassNames.includes('search-studio-home-widget__note-panel'), false);
    } finally {
        // Restore the original widget update method so later tests keep the real implementation.
        SearchStudioHomeWidget.prototype.update = originalUpdate;
    }
});
