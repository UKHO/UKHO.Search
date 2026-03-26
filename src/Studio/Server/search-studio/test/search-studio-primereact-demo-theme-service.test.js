const test = require('node:test');
const assert = require('node:assert/strict');

const {
    SearchStudioPrimeReactDemoThemeService
} = require('../lib/browser/primereact-demo/search-studio-primereact-demo-theme-service.js');
const {
    getSearchStudioPrimeReactGeneratedThemeDefinition
} = require('../lib/browser/primereact-theme/generated/search-studio-generated-primereact-theme-content.js');

/**
 * Creates a minimal document/head stub that can capture stylesheet elements for the PrimeReact theme service tests.
 *
 * @returns {{ document: any, headChildren: any[] }} The fake document and the current ordered head children collection used by the tests.
 */
function createDocumentStub() {
    const elementsById = new Map();
    const headChildren = [];

    /**
     * Creates a minimal DOM element stub for the requested tag name.
     *
     * @param {string} tagName Supplies the DOM tag name to emulate.
     * @returns {any} A lightweight element object that supports the methods used by the theme service.
     */
    function createElement(tagName) {
        const element = {
            tagName: tagName.toUpperCase(),
            rel: '',
            href: '',
            textContent: '',
            attributes: new Map(),
            setAttribute(name, value) {
                this.attributes.set(name, value);
            },
            getAttribute(name) {
                return this.attributes.get(name) ?? null;
            },
            remove() {
                const currentIndex = headChildren.indexOf(this);

                if (currentIndex >= 0) {
                    headChildren.splice(currentIndex, 1);
                }

                if (this.id) {
                    elementsById.delete(this.id);
                }
            }
        };

        Object.defineProperty(element, 'id', {
            get() {
                return this._id;
            },
            set(value) {
                this._id = value;

                if (value) {
                    elementsById.set(value, this);
                }
            }
        });

        return element;
    }

    const document = {
        head: {
            appendChild(element) {
                headChildren.push(element);

                if (element.id) {
                    elementsById.set(element.id, element);
                }
            }
        },
        createElement,
        getElementById(elementId) {
            return elementsById.get(elementId) ?? null;
        }
    };

    return {
        document,
        headChildren
    };
}

/**
 * Verifies that the generated theme definition helper resolves the expected file names and Theia font-aware content for both variants.
 */
test('getSearchStudioPrimeReactGeneratedThemeDefinition_WhenCalled_ShouldResolveGeneratedUkhoTheiaThemeDefinitions', () => {
    const lightThemeDefinition = getSearchStudioPrimeReactGeneratedThemeDefinition('light');
    const darkThemeDefinition = getSearchStudioPrimeReactGeneratedThemeDefinition('dark');

    assert.equal(lightThemeDefinition.fileName, 'ukho-theia-light.css');
    assert.match(lightThemeDefinition.stylesheetContent, /--ukho-primereact-theme-name:\s*ukho-theia-light/);
    assert.match(lightThemeDefinition.stylesheetContent, /var\(--theia-ui-font-family/);
    assert.doesNotMatch(lightThemeDefinition.stylesheetContent, /@font-face|Inter var/);
    assert.doesNotMatch(lightThemeDefinition.stylesheetContent, /showcase/i);
    assert.match(lightThemeDefinition.stylesheetContent, /min-height:\s*2\.25rem;/);

    assert.equal(darkThemeDefinition.fileName, 'ukho-theia-dark.css');
    assert.match(darkThemeDefinition.stylesheetContent, /--ukho-primereact-theme-name:\s*ukho-theia-dark/);
    assert.match(darkThemeDefinition.stylesheetContent, /var\(--theia-ui-font-family/);
    assert.doesNotMatch(darkThemeDefinition.stylesheetContent, /@font-face|Inter var/);
    assert.doesNotMatch(darkThemeDefinition.stylesheetContent, /showcase/i);
    assert.match(darkThemeDefinition.stylesheetContent, /min-height:\s*2\.25rem;/);
});

/**
 * Verifies that enabling styled mode attaches the shared PrimeReact links and injects the generated UKHO/Theia theme stylesheet content for the active variant.
 */
test('SearchStudioPrimeReactDemoThemeService_WhenStyledModeIsEnabled_ShouldAttachGeneratedUkhoTheiaThemeContent', async () => {
    const originalDocument = global.document;
    const { document, headChildren } = createDocumentStub();
    const service = new SearchStudioPrimeReactDemoThemeService();

    global.document = document;

    try {
        await service.enableStyledMode('light');

        const themeStylesheet = document.getElementById('search-studio-primereact-theme-stylesheet');

        assert.notEqual(themeStylesheet, null);
        assert.equal(themeStylesheet.tagName, 'STYLE');
        assert.equal(themeStylesheet.getAttribute('data-search-studio-generated-theme-file-name'), 'ukho-theia-light.css');
        assert.match(themeStylesheet.textContent, /--ukho-primereact-theme-name:\s*ukho-theia-light/);
        assert.match(themeStylesheet.textContent, /var\(--theia-ui-font-family/);
        assert.doesNotMatch(themeStylesheet.textContent, /@font-face|Inter var/);

        const coreStylesheet = document.getElementById('search-studio-primereact-core-stylesheet');
        const iconsStylesheet = document.getElementById('search-studio-primereact-icons-stylesheet');

        assert.notEqual(coreStylesheet, null);
        assert.equal(coreStylesheet.tagName, 'LINK');
        assert.notEqual(iconsStylesheet, null);
        assert.equal(iconsStylesheet.tagName, 'LINK');
        assert.equal(headChildren.length, 3);
    } finally {
        global.document = originalDocument;
    }
});

/**
 * Verifies that switching variants reuses the same generated theme element and replaces its content with the matching dark-theme output.
 */
test('SearchStudioPrimeReactDemoThemeService_WhenThemeVariantChanges_ShouldReuseTheGeneratedThemeElementAndSwapContent', async () => {
    const originalDocument = global.document;
    const { document, headChildren } = createDocumentStub();
    const service = new SearchStudioPrimeReactDemoThemeService();

    global.document = document;

    try {
        await service.enableStyledMode('light');
        const originalThemeStylesheet = document.getElementById('search-studio-primereact-theme-stylesheet');

        await service.enableStyledMode('dark');
        const updatedThemeStylesheet = document.getElementById('search-studio-primereact-theme-stylesheet');

        assert.equal(updatedThemeStylesheet, originalThemeStylesheet);
        assert.equal(updatedThemeStylesheet.getAttribute('data-search-studio-generated-theme-file-name'), 'ukho-theia-dark.css');
        assert.match(updatedThemeStylesheet.textContent, /--ukho-primereact-theme-name:\s*ukho-theia-dark/);
        assert.equal(headChildren.length, 3);
    } finally {
        global.document = originalDocument;
    }
});
