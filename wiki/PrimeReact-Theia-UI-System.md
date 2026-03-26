# PrimeReact Theia UI System

## Purpose

This page is the authoritative implementation guide for the UKHO Search Studio PrimeReact and Theia UI system.

Use it when you need to:

- rebuild the Studio-owned PrimeReact theme assets
- understand where theme source, generated output, and layout helpers live
- verify the retained PrimeReact pages after theme or layout changes
- add a new PrimeReact page or window without rediscovering the current conventions

## Authority split

The current system has two separate authorities.

### Theme authority

Theme authority covers generic PrimeReact visual styling:

- component chrome
- colors
- spacing
- sizing
- generic typography alignment with the Theia shell
- shared compact density for reusable data-heavy controls such as `DataTable`, `Tree`, `TreeTable`, and their paginator chrome
- shared tab active and keyboard-focus chrome, including removing panel-level focus borders while keeping focus visible on the tab header itself

The authoritative source for this work is the Studio-owned theme source under `src/Studio/Server/search-studio/src/browser/primereact-theme/source`.

The theme layer must stay generic.

- Do not add page-named selectors to shared theme source.
- Do not treat `Showcase` as the styling authority.
- Keep the current Theia-aligned typography baseline unless evidence shows a real generic mismatch.
- Promote reusable density rules out of `Showcase` once they prove generic so retained pages inherit the same compact behavior without page-specific opt-ins.

### Layout authority

Layout authority covers desktop-style hosting behavior:

- full-height composition
- splitter ownership
- `min-width: 0` and `min-height: 0` containment
- inner scroll ownership for data-heavy controls
- retained tab-host wrapper behavior

The authoritative source for this work is the Studio-side layout helper layer under `src/Studio/Server/search-studio/src/browser/primereact-demo`.

`Showcase` remains the first proving surface for layout, but it is not the styling authority for the real theme.

## Folder structure

| Area | Path | Responsibility |
| --- | --- | --- |
| Upstream/reference SASS workspace | `src/Studio/Theme` | Holds the accepted PrimeReact SASS toolchain and validated Lara baseline used to compose Studio outputs. |
| Shared custom theme source | `src/Studio/Server/search-studio/src/browser/primereact-theme/source/shared` | Holds generic UKHO and Theia-aligned component rules that apply across retained pages. |
| Light variant source | `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-light` | Holds light-theme-specific generic overrides. |
| Dark variant source | `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-dark` | Holds dark-theme-specific generic overrides. |
| Generated theme output | `src/Studio/Server/search-studio/src/browser/primereact-theme/generated` | Receives generated `ukho-theia-light.css`, `ukho-theia-dark.css`, and the generated TypeScript content module consumed at runtime. |
| Theme deploy script | `src/Studio/Theme/scripts/deploy-studio-themes.js` | Composes the upstream Lara baseline with the Studio-owned UKHO and Theia source, removes upstream font ownership, and emits generated runtime assets. |
| Shared layout helper | `src/Studio/Server/search-studio/src/browser/primereact-demo/search-studio-primereact-demo-page-layout.tsx` | Resolves shared page host classes, retained tab wrapper classes, and data-heavy scroll-height behavior. |
| PrimeReact demo pages | `src/Studio/Server/search-studio/src/browser/primereact-demo/pages` | Hosts the retained PrimeReact pages that consume the generated theme and shared layout contract. |
| Theme and layout regression tests | `src/Studio/Server/search-studio/test` | Verifies generated assets, runtime theme loading, and retained page layout-host behavior. |

## Prerequisites

Before running the workflow, ensure the following tools are available:

- Node.js `18` or later
- npm
- Yarn `1.x`
- Visual Studio for running `AppHost`

## First-time bootstrap checklist

Use this checklist on a fresh clone or when the local Node dependencies have been cleared.

1. Install the PrimeReact SASS workspace dependencies.
   - `npm install --prefix .\src\Studio\Theme`
2. Install the Studio server workspace dependencies if needed.
   - `yarn --cwd .\src\Studio\Server install --ignore-engines`
3. Confirm the upstream/reference workspace and custom source folders exist.
   - `src/Studio/Theme`
   - `src/Studio/Server/search-studio/src/browser/primereact-theme/source/shared`
   - `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-light`
   - `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-dark`
4. Confirm the generated asset folder is available after the first deploy.
   - `src/Studio/Server/search-studio/src/browser/primereact-theme/generated`

## Standard theme rebuild and deploy workflow

Run these commands from the repository root when you update the Studio-owned theme source.

```powershell
npm install --prefix .\src\Studio\Theme
npm run build --prefix .\src\Studio\Theme
npm run deploy:studio --prefix .\src\Studio\Theme
npm run verify:studio --prefix .\src\Studio\Theme
yarn --cwd .\src\Studio\Server\search-studio test
yarn --cwd .\src\Studio\Server build:browser
```

### What each step does

1. `npm install --prefix .\src\Studio\Theme`
   - restores the upstream/reference PrimeReact SASS workspace tooling
2. `npm run build --prefix .\src\Studio\Theme`
   - builds the upstream Lara baseline CSS
3. `npm run deploy:studio --prefix .\src\Studio\Theme`
   - composes the built Lara baseline with the Studio-owned UKHO and Theia SASS source
   - removes upstream hosted font ownership so the generated output continues to follow `--theia-ui-font-family`
   - writes generated CSS and the generated TypeScript theme-content module into the Studio frontend tree
4. `npm run verify:studio --prefix .\src\Studio\Theme`
   - confirms the expected generated theme artifacts exist after deployment
5. `yarn --cwd .\src\Studio\Server\search-studio test`
   - validates the focused browser-side regression coverage for generated assets, runtime theme selection, and shared retained-page layout behavior
6. `yarn --cwd .\src\Studio\Server build:browser`
   - rebuilds the Theia browser bundle so Studio does not run stale frontend code

## Visual verification checklist

After the scripted workflow succeeds, start `AppHost` in Visual Studio and verify the retained pages.

1. Open the Studio shell.
2. Open `View` -> `PrimeReact Showcase Demo`.
3. Verify `Showcase`.
   - confirm the compact workspace still owns splitter sizing
   - confirm inner grid scrolling remains inside the workspace surface
   - confirm detail-pane scrolling stays inside the inner surface instead of escaping to the outer page
4. Verify `Forms`.
   - confirm headings, labels, inputs, tabs, and buttons still read coherently with the surrounding Theia shell
   - confirm page-level padding and focus behavior come from the shared retained-page host contract
5. Verify `Data View`.
   - confirm generic component chrome, spacing, and readability remain coherent with the same theme baseline
6. Verify `Data Table`, `Tree`, and `Tree Table`.
   - confirm the condensed shared theme styling now reads consistently beyond `Showcase`
   - confirm the shared retained-page contract keeps scrolling inside the grid or tree surfaces rather than pushing overflow to the outer tab page
7. Switch between light and dark themes.
   - confirm the same generic theme reads coherently in both modes
   - confirm the retained pages are not depending on `Showcase`-specific styling rules
8. Move keyboard focus across the retained tabs.
   - confirm the selected-tab underline remains visible
   - confirm no blue focus border appears around the tab content area
   - confirm keyboard focus remains understandable on the tab header itself

## Creating a new PrimeReact page or window

Use this checklist whenever you add a new retained PrimeReact surface.

1. Start with the shared layout contract.
   - use `getSearchStudioPrimeReactDemoPageClassName` from `search-studio-primereact-demo-page-layout.tsx` to resolve the page root class list
   - if the page is rendered inside the retained showcase tabs, pass `hostDisplayMode: 'tabbed'`
2. Use the shared retained tab wrapper when the page participates in the consolidated showcase tabs.
   - use `SearchStudioPrimeReactDemoTabContent` for focus transfer, accessibility metadata, and shared overflow behavior
   - use `overflowMode: 'contained'` only when the inner PrimeReact control should own scrolling
3. Use the shared data-heavy scroll contract when the page hosts grids or trees.
   - use `getSearchStudioPrimeReactDemoDataScrollHeight` to choose the correct PrimeReact scroll height for standalone versus tab-hosted rendering
4. Keep theme rules generic.
   - place reusable visual rules in `source/shared` or in the light and dark variant folders when the rule is genuinely theme-specific
   - do not add page-named selectors to the shared theme source
5. Keep page-local CSS narrow.
   - use page-local CSS only for layout mechanics or narrow exceptions that clearly do not belong in the generic theme or shared layout helper layer
6. Update regression coverage.
   - add or adjust focused tests in `src/Studio/Server/search-studio/test` when the new page changes generated asset expectations, runtime theme selection, or shared layout-host behavior
7. Re-run the standard workflow.
   - `yarn --cwd .\src\Studio\Server\search-studio test`
   - `yarn --cwd .\src\Studio\Server build:browser`
   - perform the visual verification checklist in Studio

## How to decide where a rule belongs

Use the following decision guide before adding a style or layout rule.

| If the rule changes... | Put it in... | Notes |
| --- | --- | --- |
| Generic PrimeReact component visuals across multiple retained pages | `src/Studio/Server/search-studio/src/browser/primereact-theme/source/shared` or the light and dark variant folders | This is theme authority. Keep selectors generic and reusable. |
| Theme-variant-specific colors or chrome for light versus dark | `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-light` or `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-dark` | Use only when the difference is genuinely variant-specific. |
| Full-height hosting, tab-panel containment, splitter behavior, or scroll ownership | `src/Studio/Server/search-studio/src/browser/primereact-demo/search-studio-primereact-demo-page-layout.tsx` and the related shared demo CSS | This is layout authority. |
| One-off behavior required by a single page and not suitable for reuse | The page-local CSS or component file for that page | Keep this narrow and document why it does not belong in the shared theme or layout layer. |

## Common rules for contributors

- Treat `src/Studio/Theme` as the upstream/reference workspace, not as the place for Studio-owned custom selectors.
- Do not hand-edit files under `src/Studio/Server/search-studio/src/browser/primereact-theme/generated`.
- Rebuild and redeploy generated assets instead of editing compiled CSS directly.
- Keep `Showcase` as the layout proving surface and first visual verification stop, but not as the styling authority.
- Validate generic styling primarily through cross-page comparison, especially `Forms` plus at least one additional retained page.
- Always run `yarn --cwd .\src\Studio\Server build:browser` after Theia Studio shell changes so the browser bundle reflects the latest frontend state.

## Related pages

- [Tools - UKHO Search Studio](./Tools-UKHO-Search-Studio.md)
