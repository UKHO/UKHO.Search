const test = require('node:test');
const assert = require('node:assert/strict');
const browserAppPackage = require('../../browser-app/package.json');

/**
 * Verifies that the active browser application composition no longer includes the scaffold-owned Theia getting-started package.
 */
test('browser-app composition excludes the scaffold-owned Theia getting-started package', () => {
    // Inspect the committed browser-app package metadata because that file controls the active Theia frontend composition.
    assert.equal(Object.hasOwn(browserAppPackage.dependencies, '@theia/getting-started'), false);
});
