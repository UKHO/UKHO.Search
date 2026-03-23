const test = require('node:test');
const assert = require('node:assert/strict');
const { SearchStudioHomeJumpPoints } = require('../lib/browser/home/search-studio-home-jump-points.js');

test('SearchStudioHomeJumpPoints exposes the agreed task-focused Home actions', () => {
    assert.deepEqual(
        SearchStudioHomeJumpPoints.map(item => ({ id: item.id, label: item.label })),
        [
            { id: 'start-ingestion', label: 'Start ingestion' },
            { id: 'manage-rules', label: 'Manage rules' },
            { id: 'browse-providers', label: 'Browse providers' }
        ]);

    assert.equal(SearchStudioHomeJumpPoints.every(item => item.description.length > 0), true);
});
