const test = require('node:test');
const assert = require('node:assert/strict');

// Provide the minimal browser-like globals that Theia command imports expect when the tests run under Node.
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
    SearchStudioShowHomeCommand
} = require('../lib/browser/search-studio-home-constants.js');
const {
    SearchStudioShowPrimeReactDataTableDemoCommand,
    SearchStudioShowPrimeReactDataViewDemoCommand,
    SearchStudioShowPrimeReactDemoCommand,
    SearchStudioShowPrimeReactFormsDemoCommand,
    SearchStudioShowPrimeReactLayoutDemoCommand,
    SearchStudioShowPrimeReactShowcaseDemoCommand,
    SearchStudioShowPrimeReactTreeDemoCommand,
    SearchStudioShowPrimeReactTreeTableDemoCommand
} = require('../lib/browser/primereact-demo/search-studio-primereact-demo-constants.js');

/**
 * Creates a lightweight command contribution plus tracking state for the PrimeReact demo command tests.
 *
 * @returns The fake contribution dependencies plus the registered command map.
 */
function createPrimeReactCommandTestContext() {
    const registeredCommands = new Map();
    let openHomeCalls = 0;
    const openedDemoPages = [];

    // Register the commands against lightweight fake services so each test can invoke the exact handler stored by Theia.
    const contribution = new SearchStudioCommandContribution({
        async openHome() {
            openHomeCalls += 1;
        }
    }, {
        async openDemo(pageId) {
            openedDemoPages.push(pageId);
        }
    });

    contribution.registerCommands({
        registerCommand: (command, handler) => {
            registeredCommands.set(command.id, handler);
        }
    });

    return {
        registeredCommands,
        getOpenHomeCalls() {
            return openHomeCalls;
        },
        openedDemoPages
    };
}

/**
 * Verifies that the registered Home command reuses the shared Home service reopen behavior.
 */
test('SearchStudioCommandContribution opens Home from the registered Show Home command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    await commandTestContext.registeredCommands.get(SearchStudioShowHomeCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 1);
    assert.deepEqual(commandTestContext.openedDemoPages, []);
});

/**
 * Verifies that the registered PrimeReact demo command reuses the shared demo service open behavior.
 */
test('SearchStudioCommandContribution opens the PrimeReact demo from the registered demo command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    await commandTestContext.registeredCommands.get(SearchStudioShowPrimeReactDemoCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 0);
    assert.deepEqual(commandTestContext.openedDemoPages, ['bootstrap']);
});

/**
 * Verifies that the registered PrimeReact DataTable demo command opens the data-heavy grid page.
 */
test('SearchStudioCommandContribution opens the PrimeReact DataTable demo from the registered demo command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    await commandTestContext.registeredCommands.get(SearchStudioShowPrimeReactDataTableDemoCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 0);
    assert.deepEqual(commandTestContext.openedDemoPages, ['datatable']);
});

/**
 * Verifies that the registered PrimeReact Forms demo command opens the controlled-form page.
 */
test('SearchStudioCommandContribution opens the PrimeReact Forms demo from the registered demo command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    await commandTestContext.registeredCommands.get(SearchStudioShowPrimeReactFormsDemoCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 0);
    assert.deepEqual(commandTestContext.openedDemoPages, ['forms']);
});

/**
 * Verifies that the registered PrimeReact DataView demo command opens the card-list page.
 */
test('SearchStudioCommandContribution opens the PrimeReact DataView demo from the registered demo command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    await commandTestContext.registeredCommands.get(SearchStudioShowPrimeReactDataViewDemoCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 0);
    assert.deepEqual(commandTestContext.openedDemoPages, ['dataview']);
});

/**
 * Verifies that the registered PrimeReact Layout demo command opens the container-composition page.
 */
test('SearchStudioCommandContribution opens the PrimeReact Layout demo from the registered demo command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    await commandTestContext.registeredCommands.get(SearchStudioShowPrimeReactLayoutDemoCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 0);
    assert.deepEqual(commandTestContext.openedDemoPages, ['layout']);
});

/**
 * Verifies that the registered PrimeReact Showcase demo command opens the combined review page.
 */
test('SearchStudioCommandContribution opens the PrimeReact Showcase demo from the registered demo command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    await commandTestContext.registeredCommands.get(SearchStudioShowPrimeReactShowcaseDemoCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 0);
    assert.deepEqual(commandTestContext.openedDemoPages, ['showcase']);
});

/**
 * Verifies that the registered PrimeReact Tree demo command opens the hierarchy page.
 */
test('SearchStudioCommandContribution opens the PrimeReact Tree demo from the registered demo command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    await commandTestContext.registeredCommands.get(SearchStudioShowPrimeReactTreeDemoCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 0);
    assert.deepEqual(commandTestContext.openedDemoPages, ['tree']);
});

/**
 * Verifies that the registered PrimeReact TreeTable demo command opens the hierarchical grid page.
 */
test('SearchStudioCommandContribution opens the PrimeReact TreeTable demo from the registered demo command', async () => {
    const commandTestContext = createPrimeReactCommandTestContext();

    await commandTestContext.registeredCommands.get(SearchStudioShowPrimeReactTreeTableDemoCommand.id).execute();

    assert.equal(commandTestContext.getOpenHomeCalls(), 0);
    assert.deepEqual(commandTestContext.openedDemoPages, ['treetable']);
});
