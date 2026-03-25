const test = require('node:test');
const assert = require('node:assert/strict');

const {
    SearchStudioPrimeReactDemoPresentationState
} = require('../lib/browser/primereact-demo/search-studio-primereact-demo-presentation-state.js');

/**
 * Verifies that the temporary PrimeReact demo presentation state defaults to the current dark-themed shell baseline.
 */
test('SearchStudioPrimeReactDemoPresentationState defaults to dark-mode theme tracking', () => {
    const state = new SearchStudioPrimeReactDemoPresentationState();

    assert.equal(state.getActiveThemeVariant(), 'dark');
});

/**
 * Verifies that the temporary PrimeReact demo presentation state follows Theia light-theme body classes.
 */
test('SearchStudioPrimeReactDemoPresentationState switches to the light theme variant when theia-light is present', () => {
    const state = new SearchStudioPrimeReactDemoPresentationState();

    const themeVariant = state.synchronizeThemeVariant(['theia-dark', 'theia-light']);

    assert.equal(themeVariant, 'light');
    assert.equal(state.getActiveThemeVariant(), 'light');
});

/**
 * Verifies that the temporary PrimeReact demo presentation state keeps the dark-theme variant when Theia light mode is absent.
 */
test('SearchStudioPrimeReactDemoPresentationState keeps the dark theme variant when theia-light is absent', () => {
    const state = new SearchStudioPrimeReactDemoPresentationState();

    const themeVariant = state.synchronizeThemeVariant(['theia-dark']);

    assert.equal(themeVariant, 'dark');
    assert.equal(state.getActiveThemeVariant(), 'dark');
});
