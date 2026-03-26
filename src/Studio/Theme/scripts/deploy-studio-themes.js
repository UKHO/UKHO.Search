const fs = require('fs');
const path = require('path');
const sass = require(path.resolve(__dirname, '..', 'node_modules', 'sass'));

const themeWorkspaceRoot = path.resolve(__dirname, '..');
const studioThemeSourceRoot = path.resolve(
    themeWorkspaceRoot,
    '..',
    'Server',
    'search-studio',
    'src',
    'browser',
    'primereact-theme',
    'source'
);
const studioGeneratedThemeRoot = path.resolve(
    themeWorkspaceRoot,
    '..',
    'Server',
    'search-studio',
    'src',
    'browser',
    'primereact-theme',
    'generated'
);
const generatedThemeContentModuleFileName = 'search-studio-generated-primereact-theme-content.ts';
const studioRuntimePrimeReactVersion = '10.9.7';
const upstreamSassThemeVersion = require(path.resolve(themeWorkspaceRoot, 'package.json')).version;
const studioThemeArtifacts = [
    {
        themeVariant: 'light',
        baselineSourceSegments: ['build', 'themes', 'lara', 'lara-light', 'blue', 'theme.css'],
        customThemeSourceSegments: ['ukho-theia-light', 'theme.scss'],
        outputFileName: 'ukho-theia-light.css'
    },
    {
        themeVariant: 'dark',
        baselineSourceSegments: ['build', 'themes', 'lara', 'lara-dark', 'blue', 'theme.css'],
        customThemeSourceSegments: ['ukho-theia-dark', 'theme.scss'],
        outputFileName: 'ukho-theia-dark.css'
    }
];

/**
 * Builds the generated file banner that records the current baseline relationship and Studio-owned source path.
 *
 * @param {{ themeVariant: string, baselineSourceSegments: string[], customThemeSourceSegments: string[], outputFileName: string }} studioThemeArtifact Describes the generated Studio theme artifact being written.
 * @returns {string} A CSS comment banner that captures the upstream baseline and Studio-owned source composition for the generated file.
 */
function createGeneratedThemeBanner(studioThemeArtifact) {
    const baselineSourceRelativePath = studioThemeArtifact.baselineSourceSegments.join('/');
    const customThemeSourceRelativePath = studioThemeArtifact.customThemeSourceSegments.join('/');

    return [
        '/*',
        ' * Generated for UKHO Search Studio from the PrimeReact SASS workspace baseline and Studio-owned theme source.',
        ` * Runtime PrimeReact package version: ${studioRuntimePrimeReactVersion}.`,
        ` * Upstream/reference primereact-sass-theme workspace version: ${upstreamSassThemeVersion}.`,
        ` * Generated theme variant: ${studioThemeArtifact.themeVariant}.`,
        ` * Upstream baseline artifact: ${baselineSourceRelativePath}.`,
        ` * Studio-owned theme source: ${customThemeSourceRelativePath}.`,
        ' * The first Studio-owned iteration intentionally reuses Theia\'s UI font family contract via --theia-ui-font-family and does not introduce hosted font assets.',
        ' */',
        ''
    ].join('\n');
}

/**
 * Ensures a required file exists before the deploy workflow continues.
 *
 * @param {string} filePath Supplies the file path that must already exist.
 * @param {string} fileDescription Describes the file in the validation error message.
 */
function ensureFileExists(filePath, fileDescription) {
    if (!fs.existsSync(filePath)) {
        throw new Error(`${fileDescription} was not found at ${filePath}. Run \"Set-Location .\\src\\Studio\\Theme\" and then \"npm run build\" before deploy.`);
    }
}

/**
 * Ensures the generated output directory exists before files are copied into it.
 *
 * @param {string} directoryPath Supplies the directory path that should host the generated Studio theme files.
 */
function ensureDirectoryExists(directoryPath) {
    // Create the Studio-consumed generated asset directory lazily so the workflow can bootstrap a fresh clone cleanly.
    fs.mkdirSync(directoryPath, {
        recursive: true
    });
}

/**
 * Compiles the Studio-owned SASS source for the requested theme variant into CSS that can be layered onto the upstream Lara baseline.
 *
 * @param {{ themeVariant: string, baselineSourceSegments: string[], customThemeSourceSegments: string[], outputFileName: string }} studioThemeArtifact Describes the Studio-owned theme source entry to compile.
 * @returns {string} The compiled Studio-owned CSS override content for the requested theme variant.
 */
function compileStudioThemeSource(studioThemeArtifact) {
    const customThemeSourceFilePath = path.resolve(studioThemeSourceRoot, ...studioThemeArtifact.customThemeSourceSegments);

    ensureFileExists(customThemeSourceFilePath, `Studio-owned ${studioThemeArtifact.themeVariant} theme source`);

    // Compile the Studio-owned entrypoint with the Theme workspace Sass toolchain so the generated output stays aligned to the validated upstream baseline tooling.
    return sass.compile(customThemeSourceFilePath, {
        style: 'expanded',
        loadPaths: [studioThemeSourceRoot]
    }).css;
}

/**
 * Removes upstream Lara typography ownership that would otherwise reintroduce hosted fonts and override the Studio-owned Theia font contract.
 *
 * @param {string} baselineThemeContent Supplies the built upstream Lara baseline CSS content.
 * @returns {string} The baseline CSS with hosted font-face blocks and root-level typography ownership removed.
 */
function removeUpstreamTypographyOwnership(baselineThemeContent) {
    // Remove the upstream hosted Inter font registrations because the Studio-owned theme deliberately relies on Theia's font contract instead.
    const baselineWithoutFontFaces = baselineThemeContent.replace(/@font-face\s*\{[\s\S]*?\}\s*/g, '');

    // Remove only the root typography declarations that make the upstream baseline authoritative for font family selection.
    return baselineWithoutFontFaces
        .replace(/^\s*font-family:\s*"Inter var",\s*sans-serif;\r?\n/m, '')
        .replace(/^\s*font-feature-settings:\s*"cv02",\s*"cv03",\s*"cv04",\s*"cv11";\r?\n/m, '')
        .replace(/^\s*font-variation-settings:\s*normal;\r?\n/m, '')
        .replace(/^\s*--font-family:\s*"Inter var",\s*sans-serif;\r?\n/m, '')
        .replace(/^\s*--font-feature-settings:\s*"cv02",\s*"cv03",\s*"cv04",\s*"cv11";\r?\n/m, '');
}

/**
 * Escapes CSS content so it can be emitted safely inside a generated JavaScript template literal.
 *
 * @param {string} content Supplies the CSS content that will be embedded into the generated TypeScript module.
 * @returns {string} The escaped template-literal-safe CSS content.
 */
function escapeTemplateLiteralContent(content) {
    return content
        .replace(/\\/g, '\\\\')
        .replace(/`/g, '\\`')
        .replace(/\$\{/g, '\\${');
}

/**
 * Builds the generated TypeScript module that exposes the light and dark UKHO/Theia theme definitions to the browser theme service.
 *
 * @param {ReadonlyArray<{ themeVariant: string, outputFileName: string, stylesheetContent: string }>} generatedThemeArtifacts Supplies the generated theme file names and stylesheet content that should be exported to the browser code.
 * @returns {string} The generated TypeScript module source.
 */
function createGeneratedThemeContentModule(generatedThemeArtifacts) {
    const generatedThemeDefinitions = generatedThemeArtifacts.map(generatedThemeArtifact => {
        return [
            `    ${generatedThemeArtifact.themeVariant}: {`,
            `        themeVariant: '${generatedThemeArtifact.themeVariant}',`,
            `        fileName: '${generatedThemeArtifact.outputFileName}',`,
            `        stylesheetContent: \`${escapeTemplateLiteralContent(generatedThemeArtifact.stylesheetContent)}\``,
            '    }'
        ].join('\n');
    }).join(',\n');

    return [
        '/**',
        ' * Identifies the generated UKHO/Theia PrimeReact theme variants that Studio can load at runtime.',
        ' */',
        'export type SearchStudioPrimeReactGeneratedThemeVariant = \'light\' | \'dark\';',
        '',
        '/**',
        ' * Describes one generated UKHO/Theia PrimeReact theme asset that can be selected by the Studio browser theme service.',
        ' */',
        'export interface SearchStudioPrimeReactGeneratedThemeDefinition {',
        '    /**',
        '     * Stores the logical light or dark variant represented by the generated theme asset.',
        '     */',
        '    readonly themeVariant: SearchStudioPrimeReactGeneratedThemeVariant;',
        '',
        '    /**',
        '     * Stores the generated CSS file name that was written into the generated asset folder.',
        '     */',
        '    readonly fileName: string;',
        '',
        '    /**',
        '     * Stores the generated stylesheet content that the runtime theme service injects into the page head.',
        '     */',
        '    readonly stylesheetContent: string;',
        '}',
        '',
        'const searchStudioPrimeReactGeneratedThemeDefinitions: Record<SearchStudioPrimeReactGeneratedThemeVariant, SearchStudioPrimeReactGeneratedThemeDefinition> = {',
        generatedThemeDefinitions,
        '};',
        '',
        '/**',
        ' * Gets the generated UKHO/Theia PrimeReact theme definition that matches the requested light or dark Theia context.',
        ' *',
        ' * @param themeVariant Identifies which generated UKHO/Theia theme variant should be returned.',
        ' * @returns The generated theme definition that the browser theme service should load for the requested variant.',
        ' */',
        'export function getSearchStudioPrimeReactGeneratedThemeDefinition(',
        '    themeVariant: SearchStudioPrimeReactGeneratedThemeVariant',
        '): SearchStudioPrimeReactGeneratedThemeDefinition {',
        '    // Resolve the generated theme descriptor from one stable dictionary so the runtime service can switch variants without duplicating path or content knowledge.',
        '    return searchStudioPrimeReactGeneratedThemeDefinitions[themeVariant];',
        '}',
        ''
    ].join('\n');
}

/**
 * Writes the generated TypeScript module that exposes the generated CSS content to the browser theme service.
 *
 * @param {ReadonlyArray<{ themeVariant: string, outputFileName: string, stylesheetContent: string }>} generatedThemeArtifacts Supplies the generated theme definitions that should be exported to the browser code.
 */
function writeGeneratedThemeContentModule(generatedThemeArtifacts) {
    const generatedThemeContentModulePath = path.resolve(studioGeneratedThemeRoot, generatedThemeContentModuleFileName);

    // Emit a generated TypeScript companion module so the browser theme service can inject the current light or dark CSS without relying on external CDN theme URLs.
    fs.writeFileSync(generatedThemeContentModulePath, createGeneratedThemeContentModule(generatedThemeArtifacts), 'utf8');
    console.info('Generated Studio PrimeReact theme content module.', {
        generatedThemeContentModulePath
    });
}

/**
 * Builds and writes one Studio-generated PrimeReact theme artifact by composing the upstream Lara baseline with the Studio-owned theme source.
 *
 * @param {{ themeVariant: string, baselineSourceSegments: string[], customThemeSourceSegments: string[], outputFileName: string }} studioThemeArtifact Describes the built baseline artifact, Studio-owned source entry, and output file to write.
 * @returns {{ themeVariant: string, outputFileName: string, stylesheetContent: string }} The generated theme descriptor used to emit the companion TypeScript module.
 */
function deployStudioThemeArtifact(studioThemeArtifact) {
    const baselineSourceFilePath = path.resolve(themeWorkspaceRoot, ...studioThemeArtifact.baselineSourceSegments);
    const destinationFilePath = path.resolve(studioGeneratedThemeRoot, studioThemeArtifact.outputFileName);

    ensureFileExists(baselineSourceFilePath, `Built ${studioThemeArtifact.themeVariant} baseline theme artifact`);
    ensureDirectoryExists(studioGeneratedThemeRoot);

    // Compose the final generated theme CSS by layering the Studio-owned theme source output after the validated upstream Lara baseline.
    const baselineThemeContent = removeUpstreamTypographyOwnership(fs.readFileSync(baselineSourceFilePath, 'utf8'));
    const customThemeContent = compileStudioThemeSource(studioThemeArtifact);
    const generatedThemeContent = `${createGeneratedThemeBanner(studioThemeArtifact)}${baselineThemeContent}\n\n${customThemeContent}`;

    fs.writeFileSync(destinationFilePath, generatedThemeContent, 'utf8');
    console.info('Deployed Studio PrimeReact theme artifact.', {
        themeVariant: studioThemeArtifact.themeVariant,
        baselineSourceFilePath,
        destinationFilePath
    });

    return {
        themeVariant: studioThemeArtifact.themeVariant,
        outputFileName: studioThemeArtifact.outputFileName,
        stylesheetContent: generatedThemeContent
    };
}

/**
 * Verifies that every expected Studio-generated CSS artifact and generated TypeScript theme module exists after the deploy workflow completes.
 */
function verifyGeneratedStudioThemeArtifacts() {
    for (const studioThemeArtifact of studioThemeArtifacts) {
        const destinationFilePath = path.resolve(studioGeneratedThemeRoot, studioThemeArtifact.outputFileName);

        ensureFileExists(destinationFilePath, `Generated ${studioThemeArtifact.themeVariant} Studio theme artifact`);
        console.info('Verified generated Studio PrimeReact theme artifact.', {
            themeVariant: studioThemeArtifact.themeVariant,
            destinationFilePath
        });
    }

    const generatedThemeContentModulePath = path.resolve(studioGeneratedThemeRoot, generatedThemeContentModuleFileName);
    ensureFileExists(generatedThemeContentModulePath, 'Generated Studio theme content module');
    console.info('Verified generated Studio PrimeReact theme content module.', {
        generatedThemeContentModulePath
    });
}

/**
 * Executes the Studio theme deploy workflow or verification-only mode based on the provided command-line arguments.
 */
function main() {
    const verifyOnly = process.argv.includes('--verify');

    if (!verifyOnly) {
        // Build the Studio-consumed light and dark generated themes from the upstream Lara baseline plus the Studio-owned UKHO/Theia source folders.
        const generatedThemeArtifacts = studioThemeArtifacts.map(studioThemeArtifact => deployStudioThemeArtifact(studioThemeArtifact));
        writeGeneratedThemeContentModule(generatedThemeArtifacts);
    }

    verifyGeneratedStudioThemeArtifacts();
}

try {
    main();
} catch (error) {
    console.error('Studio PrimeReact theme deploy workflow failed.', error);
    process.exitCode = 1;
}
