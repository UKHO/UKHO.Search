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

const { CommonMenus } = require('@theia/core/lib/browser');
const { SearchStudioMenuContribution } = require('../lib/browser/search-studio-menu-contribution.js');
const {
    SearchStudioRefreshProvidersCommand,
    SearchStudioRefreshRulesCommand,
    SearchStudioShowHomeCommand
} = require('../lib/browser/search-studio-constants.js');

test('SearchStudioMenuContribution registers Show Home and refresh actions in the View menu', () => {
    const contribution = new SearchStudioMenuContribution();
    const actions = [];

    contribution.registerMenus({
        registerMenuAction: (menuPath, action) => {
            actions.push({ menuPath, action });
        }
    });

    const viewActions = actions.filter(entry => entry.menuPath === CommonMenus.VIEW);

    assert.deepEqual(
        viewActions.map(entry => ({ commandId: entry.action.commandId, label: entry.action.label })),
        [
            {
                commandId: SearchStudioShowHomeCommand.id,
                label: 'Home'
            },
            {
                commandId: SearchStudioRefreshProvidersCommand.id,
                label: 'Refresh Providers'
            },
            {
                commandId: SearchStudioRefreshRulesCommand.id,
                label: 'Refresh Rules'
            }
        ]);
});
