const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const generatedThemeDirectoryPath = path.resolve(__dirname, '..', 'lib', 'browser', 'primereact-theme', 'generated');
const expectedGeneratedThemeFileNames = [
    'ukho-theia-light.css',
    'ukho-theia-dark.css'
];

// Verify the frontend asset copy pipeline carries the generated Studio PrimeReact theme CSS into the emitted lib tree.
test('generatedPrimeReactThemeAssets_WhenSearchStudioBuildRuns_ShouldBeCopiedIntoLib', () => {
    assert.equal(
        fs.existsSync(generatedThemeDirectoryPath),
        true,
        `Expected generated theme directory to exist at ${generatedThemeDirectoryPath}.`
    );

    for (const expectedGeneratedThemeFileName of expectedGeneratedThemeFileNames) {
        const generatedThemeFilePath = path.resolve(generatedThemeDirectoryPath, expectedGeneratedThemeFileName);

        assert.equal(
            fs.existsSync(generatedThemeFilePath),
            true,
            `Expected generated theme asset to exist at ${generatedThemeFilePath}.`
        );

        const generatedThemeContent = fs.readFileSync(generatedThemeFilePath, 'utf8');

        assert.match(
            generatedThemeContent,
            /Generated for UKHO Search Studio from the PrimeReact SASS workspace baseline and Studio-owned theme source\./,
            `Expected generated theme banner to be present in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /var\(--theia-ui-font-family/,
            `Expected generated theme content to use the Theia UI font contract in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /--ukho-primereact-theme-name:/,
            `Expected generated theme content to declare the generated UKHO theme marker in ${generatedThemeFilePath}.`
        );

        assert.doesNotMatch(
            generatedThemeContent,
            /@font-face|Inter var/,
            `Expected generated theme content to preserve the Theia typography contract without reintroducing hosted PrimeReact font assets in ${generatedThemeFilePath}.`
        );

        assert.doesNotMatch(
            generatedThemeContent,
            /showcase/i,
            `Expected generated theme content to stay generic and avoid page-named selectors in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /min-height:\s*2\.25rem;/,
            `Expected generated theme content to include the compact generic control sizing pass in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /padding:\s*0\.625rem\s+0\.875rem;/,
            `Expected generated theme content to include the generic tab spacing refinement in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /padding:\s*0\.625rem\s+0\.75rem;/,
            `Expected generated theme content to include the generic data-heavy cell spacing refinement in ${generatedThemeFilePath}.`
        );
    }
});
