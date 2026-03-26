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
            /\.p-tabview \.p-tabview-nav li \.p-tabview-nav-link:not\(\.p-disabled\):focus-visible\s*\{[^}]*box-shadow:\s*inset 0 0 0 0\.1rem/s,
            `Expected generated theme content to preserve a header-level keyboard focus treatment for tabs in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /\.p-tabview \.p-tabview-nav li\.p-highlight \.p-tabview-nav-link\s*\{[^}]*border-color:/s,
            `Expected generated theme content to preserve the selected-tab underline treatment in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /\.p-tabview \.p-tabview-panels,\s*\.p-tabview \.p-tabview-panels:focus,\s*\.p-tabview \.p-tabview-panels:focus-within,\s*\.p-tabview \.p-tabview-panel,\s*\.p-tabview \.p-tabview-panel:focus,\s*\.p-tabview \.p-tabview-panel:focus-within\s*\{[^}]*box-shadow:\s*none;/s,
            `Expected generated theme content to remove themed tab-panel focus chrome in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /padding:\s*0\.32rem\s+0\.45rem;/,
            `Expected generated theme content to include the generic data-heavy cell spacing refinement in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /\.p-tree \.p-treenode-content\s*\{[^}]*padding:\s*0\.12rem\s+0\.18rem;/s,
            `Expected generated theme content to include the generic compact tree node density refinement in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /\.p-tree \.p-treenode-children\s*\{[^}]*padding-left:\s*1rem;/s,
            `Expected generated theme content to include the generic compact tree indentation refinement in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /\.p-paginator\s*\{[^}]*padding:\s*0\.12rem\s+0;/s,
            `Expected generated theme content to include the generic compact paginator spacing refinement in ${generatedThemeFilePath}.`
        );

        assert.match(
            generatedThemeContent,
            /\.p-paginator \.p-paginator-pages \.p-paginator-page,[\s\S]*?min-height:\s*1\.7rem;/,
            `Expected generated theme content to include the generic compact paginator target sizing refinement in ${generatedThemeFilePath}.`
        );
    }
});
