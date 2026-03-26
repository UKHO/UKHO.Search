const test = require('node:test');
const assert = require('node:assert/strict');

const {
    SearchStudioPrimeReactDemoTabContent,
    getSearchStudioPrimeReactDemoDataScrollHeight,
    getSearchStudioPrimeReactDemoPageClassName,
    getSearchStudioPrimeReactDemoTabContentClassName
} = require('../lib/browser/primereact-demo/search-studio-primereact-demo-page-layout.js');

/**
 * Verifies that standalone pages use the shared styled page contract without any tab-hosted overflow classes.
 */
test('getSearchStudioPrimeReactDemoPageClassName_WhenPageIsStandalone_ShouldReturnStyledBaseContract', () => {
    const className = getSearchStudioPrimeReactDemoPageClassName({});

    assert.equal(
        className,
        'search-studio-primereact-demo-page search-studio-primereact-demo-page--styled'
    );
});

/**
 * Verifies that tab-hosted data-heavy pages opt into the shared inner-scroll layout contract.
 */
test('getSearchStudioPrimeReactDemoPageClassName_WhenPageIsTabbedAndDataHeavy_ShouldReturnTabbedDataHeavyContract', () => {
    const className = getSearchStudioPrimeReactDemoPageClassName({
        hostDisplayMode: 'tabbed',
        usesDataHeavyLayout: true
    });

    assert.equal(
        className,
        'search-studio-primereact-demo-page search-studio-primereact-demo-page--styled search-studio-primereact-demo-page--tab-hosted search-studio-primereact-demo-page--tab-hosted-data-heavy'
    );
});

/**
 * Verifies that the shared tab-panel helper can request contained overflow for tabs whose inner controls own scrolling.
 */
test('getSearchStudioPrimeReactDemoTabContentClassName_WhenContainedOverflowIsRequested_ShouldReturnContainedOverflowContract', () => {
    const className = getSearchStudioPrimeReactDemoTabContentClassName('contained');

    assert.equal(
        className,
        'search-studio-primereact-demo-page__tab-panel-content search-studio-primereact-demo-page__tab-panel-content--contained'
    );
});

/**
 * Verifies that data-heavy pages switch to PrimeReact flex scrolling only when they are hosted inside the retained tab shell.
 */
test('getSearchStudioPrimeReactDemoDataScrollHeight_WhenHostDisplayModeChanges_ShouldReturnTheExpectedScrollHeight', () => {
    assert.equal(getSearchStudioPrimeReactDemoDataScrollHeight(undefined), '32rem');
    assert.equal(getSearchStudioPrimeReactDemoDataScrollHeight('standalone'), '32rem');
    assert.equal(getSearchStudioPrimeReactDemoDataScrollHeight('tabbed'), 'flex');
});

/**
 * Verifies that the shared tab-panel wrapper applies the requested focus target, accessibility metadata, and overflow contract.
 */
test('SearchStudioPrimeReactDemoTabContent_WhenRendered_ShouldApplyTheSharedTabPanelContract', () => {
    const element = SearchStudioPrimeReactDemoTabContent({
        focusTargetId: 'demo-focus-target',
        ariaLabel: 'PrimeReact demo content',
        overflowMode: 'contained',
        children: 'demo child'
    });

    assert.equal(element.type, 'section');
    assert.equal(element.props.id, 'demo-focus-target');
    assert.equal(element.props.tabIndex, -1);
    assert.equal(element.props['aria-label'], 'PrimeReact demo content');
    assert.equal(
        element.props.className,
        'search-studio-primereact-demo-page__tab-panel-content search-studio-primereact-demo-page__tab-panel-content--contained'
    );
    assert.equal(element.props.children, 'demo child');
});
