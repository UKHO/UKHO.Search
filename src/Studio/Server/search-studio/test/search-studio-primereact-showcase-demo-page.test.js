const test = require('node:test');
const assert = require('node:assert/strict');

const {
    createTreeDemoNodes
} = require('../lib/browser/primereact-demo/data/search-studio-primereact-demo-data.js');
const {
    createScenarioSnapshot
} = require('../lib/browser/primereact-demo/data/search-studio-primereact-demo-state.js');
const {
    getSearchStudioPrimeReactShowcaseTreeValue
} = require('../lib/browser/primereact-demo/pages/search-studio-primereact-showcase-demo-page.js');

/**
 * Verifies that the compact Showcase tree passes PrimeReact the same root array instance for ready-state snapshots.
 */
test('getSearchStudioPrimeReactShowcaseTreeValue_WhenReadyScenarioIsRendered_ShouldPreserveTheSnapshotRootArrayIdentity', () => {
    const treeNodes = createTreeDemoNodes(2, 2, 2);
    const scenarioSnapshot = createScenarioSnapshot(treeNodes, 'ready');

    assert.equal(
        getSearchStudioPrimeReactShowcaseTreeValue(scenarioSnapshot),
        scenarioSnapshot.items
    );
});

/**
 * Verifies that the compact Showcase tree still returns an empty array cleanly for empty-state snapshots.
 */
test('getSearchStudioPrimeReactShowcaseTreeValue_WhenEmptyScenarioIsRendered_ShouldReturnAnEmptyTreeArray', () => {
    const treeNodes = createTreeDemoNodes(1, 1, 1);
    const scenarioSnapshot = createScenarioSnapshot(treeNodes, 'empty');
    const treeValue = getSearchStudioPrimeReactShowcaseTreeValue(scenarioSnapshot);

    assert.equal(treeValue, scenarioSnapshot.items);
    assert.equal(treeValue.length, 0);
});
