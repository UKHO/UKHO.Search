const test = require('node:test');
const assert = require('node:assert/strict');

const {
    createDataTableDemoRecords,
    createDataViewDemoRecords,
    createTreeDemoNodes,
    createTreeTableDemoNodes
} = require('../lib/browser/primereact-demo/data/search-studio-primereact-demo-data.js');
const {
    countSelectedTreeKeys,
    createExpandedTreeKeys,
    createScenarioSnapshot,
    createSearchStudioPrimeReactFormsDemoInitialValues,
    validateSearchStudioPrimeReactFormsDemoValues
} = require('../lib/browser/primereact-demo/data/search-studio-primereact-demo-state.js');

/**
 * Verifies that the generated `DataTable` demo records are dense enough for filtering, scrolling, and editing review.
 */
test('PrimeReact demo data creates dense DataTable records with mixed editable states', () => {
    const records = createDataTableDemoRecords();

    assert.equal(records.length, 180);
    assert.ok(records.some(record => record.isEditable));
    assert.ok(records.some(record => !record.isEditable));
    assert.ok(new Set(records.map(record => record.status)).size > 2);
    assert.ok(new Set(records.map(record => record.provider)).size > 3);
});

/**
 * Verifies that the generated `DataView` demo records cover card/list review with varied statuses and owners.
 */
test('PrimeReact demo data creates varied DataView records for card and list review', () => {
    const records = createDataViewDemoRecords();

    assert.equal(records.length, 18);
    assert.ok(new Set(records.map(record => record.status)).size > 2);
    assert.ok(new Set(records.map(record => record.owner)).size > 3);
    assert.ok(records.some(record => record.emphasis === 'Spotlight'));
});

/**
 * Verifies that the generated tree datasets are large enough for hierarchy and tree-table review.
 */
test('PrimeReact demo data creates dense tree and tree-table hierarchies', () => {
    const treeNodes = createTreeDemoNodes();
    const treeTableNodes = createTreeTableDemoNodes();

    const expandedTreeKeys = createExpandedTreeKeys(treeNodes);
    const expandedTreeTableKeys = createExpandedTreeKeys(treeTableNodes);

    assert.equal(treeNodes.length, 6);
    assert.equal(treeTableNodes.length, 5);
    assert.ok(Object.keys(expandedTreeKeys).length > treeNodes.length);
    assert.ok(Object.keys(expandedTreeTableKeys).length > treeTableNodes.length);
});

/**
 * Verifies that the shared scenario and selection helpers expose the expected page state transitions.
 */
test('PrimeReact demo state helpers expose loading, empty, and selected-tree summaries', () => {
    const readySnapshot = createScenarioSnapshot(['a', 'b'], 'ready');
    const loadingSnapshot = createScenarioSnapshot(['a', 'b'], 'loading');
    const emptySnapshot = createScenarioSnapshot(['a', 'b'], 'empty');

    assert.deepEqual(readySnapshot, {
        items: ['a', 'b'],
        isLoading: false,
        isEmpty: false
    });
    assert.deepEqual(loadingSnapshot, {
        items: ['a', 'b'],
        isLoading: true,
        isEmpty: false
    });
    assert.deepEqual(emptySnapshot, {
        items: [],
        isLoading: false,
        isEmpty: true
    });
    assert.equal(countSelectedTreeKeys({
        root: { checked: true },
        child: { partialChecked: true },
        leaf: true
    }), 2);
});

/**
 * Verifies that the shared forms helper exposes deterministic initial values and inline validation guidance.
 */
test('PrimeReact demo form helpers expose initial values and validation feedback', () => {
    const initialValues = createSearchStudioPrimeReactFormsDemoInitialValues();
    const invalidValues = {
        ...initialValues,
        workspaceName: 'QA',
        releaseNotes: 'Short',
        selectedCapabilities: [],
        batchSize: 5,
        qualityThreshold: 20,
        sendNotifications: false,
        reviewMode: 'expedite'
    };

    assert.equal(initialValues.workspaceName, 'North Sea workspace review');
    assert.deepEqual(validateSearchStudioPrimeReactFormsDemoValues(invalidValues), {
        workspaceName: 'Enter a workspace name with at least four characters.',
        releaseNotes: 'Add at least twelve characters so the inline help reads like a realistic review note.',
        selectedCapabilities: 'Select at least one capability so the forms demo shows grouped checkbox feedback.',
        batchSize: 'Choose a batch size between 10 and 500 items.',
        qualityThreshold: 'Keep the quality threshold between 30 and 95 for the mock review lane.',
        sendNotifications: 'Enable notifications before running the expedite review path.'
    });
});
