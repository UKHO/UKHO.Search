const test = require('node:test');
const assert = require('node:assert/strict');

// Provide the minimal browser-like globals that Theia menu imports expect when the tests run under Node.
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

const { CommonMenus } = require('@theia/core/lib/browser');
const { SearchStudioMenuContribution } = require('../lib/browser/search-studio-menu-contribution.js');
const { SearchStudioShowHomeCommand } = require('../lib/browser/search-studio-home-constants.js');
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
 * Verifies that the View menu exposes the legacy-named Home reopen action.
 */
test('SearchStudioMenuContribution registers Home in the View menu', () => {
    const actions = [];
    const contribution = new SearchStudioMenuContribution();

    contribution.registerMenus({
        registerMenuAction: (menuPath, action) => {
            actions.push({ menuPath, action });
        }
    });

    assert.deepEqual(actions, [
        {
            menuPath: CommonMenus.VIEW,
            action: {
                commandId: SearchStudioShowHomeCommand.id,
                label: 'Home'
            }
        },
        {
            menuPath: CommonMenus.VIEW,
            action: {
                commandId: SearchStudioShowPrimeReactDemoCommand.id,
                label: 'PrimeReact Demo'
            }
        },
        {
            menuPath: CommonMenus.VIEW,
            action: {
                commandId: SearchStudioShowPrimeReactDataTableDemoCommand.id,
                label: 'PrimeReact Data Table Demo'
            }
        },
        {
            menuPath: CommonMenus.VIEW,
            action: {
                commandId: SearchStudioShowPrimeReactFormsDemoCommand.id,
                label: 'PrimeReact Forms Demo'
            }
        },
        {
            menuPath: CommonMenus.VIEW,
            action: {
                commandId: SearchStudioShowPrimeReactDataViewDemoCommand.id,
                label: 'PrimeReact Data View Demo'
            }
        },
        {
            menuPath: CommonMenus.VIEW,
            action: {
                commandId: SearchStudioShowPrimeReactLayoutDemoCommand.id,
                label: 'PrimeReact Layout Demo'
            }
        },
        {
            menuPath: CommonMenus.VIEW,
            action: {
                commandId: SearchStudioShowPrimeReactShowcaseDemoCommand.id,
                label: 'PrimeReact Showcase Demo'
            }
        },
        {
            menuPath: CommonMenus.VIEW,
            action: {
                commandId: SearchStudioShowPrimeReactTreeDemoCommand.id,
                label: 'PrimeReact Tree Demo'
            }
        },
        {
            menuPath: CommonMenus.VIEW,
            action: {
                commandId: SearchStudioShowPrimeReactTreeTableDemoCommand.id,
                label: 'PrimeReact Tree Table Demo'
            }
        }
    ]);
});
