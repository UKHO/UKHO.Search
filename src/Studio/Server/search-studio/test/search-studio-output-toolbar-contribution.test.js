const test = require('node:test');
const assert = require('node:assert/strict');
const { SearchStudioOutputToolbarContribution } = require('../lib/browser/panel/search-studio-output-toolbar-contribution.js');
const {
    SearchStudioClearOutputCommand,
    SearchStudioCopyAllOutputCommand,
    SearchStudioOutputWidgetId
} = require('../lib/browser/search-studio-constants.js');

test('SearchStudioOutputToolbarContribution registers toolbar actions for Studio Output only', () => {
    const contribution = new SearchStudioOutputToolbarContribution();
    const items = [];

    contribution.registerToolbarItems({
        registerItem: item => {
            items.push(item);
            return { dispose() {} };
        }
    });

    assert.deepEqual(
        items.map(item => ({ id: item.id, command: item.command, tooltip: item.tooltip })),
        [
            {
                id: 'search-studio.output.copy-all.toolbar',
                command: SearchStudioCopyAllOutputCommand.id,
                tooltip: 'Copy all'
            },
            {
                id: 'search-studio.output.clear.toolbar',
                command: SearchStudioClearOutputCommand.id,
                tooltip: 'Clear output'
            }
        ]);

    const outputWidget = { id: SearchStudioOutputWidgetId };

    assert.equal(items.every(item => item.isVisible(outputWidget)), true);
    assert.equal(items.every(item => item.isVisible({})), false);
});
