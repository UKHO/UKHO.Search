# Tools: `UKHO Search Studio`

This page describes the active Eclipse Theia-based Studio shell used for local development.

See also:

- [Theia Knowledgebase](./Theia-Knowledgebase.md)

## Purpose

`UKHO Search Studio` is a browser-hosted Theia application rooted at:

- `src/Studio/Server`

The current bootstrap slice provides:

- a fresh active Theia workspace at `src/Studio/Server`
- the preserved root workspace script names `build:browser` and `start:browser`
- the preserved package names `browser-app` and `search-studio`
- Studio product branding through the browser-app Theia frontend configuration
- a lightweight Studio `Home` document that opens by default and can be reopened from `View -> Home`
- a temporary `PrimeReact Showcase Demo` document that can be reopened from `View -> PrimeReact Showcase Demo`
- the copied UKHO logo served from the active runtime asset pipeline under `search-studio`
- a same-origin runtime configuration bridge at `/search-studio/api/configuration`
- browser-side startup validation that logs the resolved `STUDIO_API_HOST_API_BASE_URL` handoff without blocking shell startup
- the preserved Visual Studio incremental build entrypoint `src/Studio/Server/build.ps1`

This work package does **not** migrate existing repository tooling into the shell yet.

The current retained Studio shell is intentionally lightweight:

- the visible Theia experience currently centers on `Home`
- the retained review surface is the temporary `PrimeReact Showcase Demo`
- the runtime configuration bridge is active so future browser features can discover the Studio API base URL safely
- `StudioServiceHost` remains available beside the shell for provider, rules, ingestion, and operations APIs even though those APIs are not yet surfaced through the retained `Home` and demo documents

## How it is hosted locally

The shell is orchestrated by Aspire from:

- `src/Hosts/AppHost`

In `runmode=services`, `AppHost` registers the shell as a JavaScript application resource and binds it to the fixed local port configured under:

- `Studio:Server:Port`

Current default:

- `3000`

The current shell endpoint is exposed on the fixed local **HTTP** endpoint.

Use:

- `http://localhost:3000`

Do **not** use:

- `https://localhost:3000`

The shell receives the Aspire-provided `StudioServiceHost` HTTPS base URL through `STUDIO_API_HOST_API_BASE_URL`, while the browser shell itself stays on fixed-port HTTP for local development.

## Visual Studio build integration

The repository now includes a Theia build script at:

- `src/Studio/Server/build.ps1`

`src/Studio/StudioServiceHost/StudioServiceHost.csproj` invokes that script before build.

This was chosen so that, on a fresh clone, building the local Aspire solution in Visual Studio also prepares the active Theia shell before the Studio service host is built.

The integration is incremental:

- it watches the main Theia workspace inputs
- it rebuilds when relevant Theia files change
- it skips when the Theia inputs have not changed since the last successful build

The tracked inputs currently include:

- `src/Studio/Server/build.ps1`
- `src/Studio/Server/package.json`
- `src/Studio/Server/yarn.lock`
- `src/Studio/Server/lerna.json`
- `src/Studio/Server/browser-app/package.json`
- `src/Studio/Server/search-studio/package.json`
- `src/Studio/Server/search-studio/tsconfig.json`
- `src/Studio/Server/search-studio/scripts/copy-assets.js`
- `src/Studio/Server/search-studio/src/**`

## Current startup shell behavior

The current shell intentionally keeps the generated Theia workbench structure while making the Studio-owned `Home` document the sole landing surface:

- the `Home` document opens automatically in the main workbench area as a normal closable tab
- `View -> Home` reopens the same Home document after it is closed
- `View -> PrimeReact Showcase Demo` opens the single retained PrimeReact research surface that is not shown by default
- the active browser-app composition no longer includes the scaffold-owned Theia `Welcome` / getting-started surface
- the `search-studio` extension preloads the same-origin runtime configuration bridge during startup
- startup logs report whether the Studio API base URL handoff was resolved successfully
- the Home surface uses the copied runtime-served UKHO logo plus lightweight orientation text only
- the temporary PrimeReact surface remains a review-only document and is not part of normal startup

## Temporary PrimeReact evaluation pages

Work package `074-primereact-research` adds the temporary PrimeReact research surfaces under:

- `src/Studio/Server/search-studio/src/browser/primereact-demo`

Current behavior:

- the demo research surface is exposed only from `View -> PrimeReact Showcase Demo`
- the demo pages do **not** open by default on startup
- every page now assumes full styled PrimeReact only
- every page follows the active Theia `light` / `dark` theme by switching between the stock PrimeReact Lara light and dark themes
- the temporary demo surfaces use in-memory datasets only and keep all actions mock-only for look-and-feel review
- the `PrimeReact Showcase Demo` now uses a showcase-scoped compact density pass so the page reads more like a desktop workbench surface than a long-form web page
- the `PrimeReact Showcase Demo` now configures its widget root, splitters, and pane wrappers for pane-owned scrolling so the hierarchy and grid can scroll internally instead of pushing the outer page downward
- the `Showcase` grid now uses a dedicated stable inner-height contract so narrowing the workbench still leaves vertical scrolling owned by the grid instead of depending on resize side effects
- the `Showcase` grid now uses lighter header/body typography, a tighter paginator, reduced row padding, and a centralized compact control font-size baseline so the table reads more like a compact Theia workbench surface
- the `Showcase` hierarchy tree now uses smaller node labels, tighter node padding, and denser expander/checkbox spacing so it sits closer to the `Filter showcase hierarchy` textbox baseline
- the remaining `Showcase` labels, badges, and detail headings now use lighter weight, and the secondary publish-follow-up control no longer sits inside a heavy card panel, so the whole page reads as one calmer Theia-aligned workbench surface
- the `PrimeReact Showcase Demo` now hosts the retained review surface inside one root tab shell with the fixed order `Showcase`, `Forms`, `Data View`, `Data Table`, `Tree`, and `Tree Table`
- the root tab control now owns the whole showcase page without an extra toolbar or decorative header band above it
- the `Forms`, `Data View`, `Data Table`, `Tree`, and `Tree Table` tabs now render inside that consolidated shell using the same tighter spacing baseline as the compact showcase page
- switching retained tabs now moves keyboard focus into the newly displayed tab content so keyboard review can continue inside the active surface
- retired standalone PrimeReact source pages and dead runtime branches are removed so the research package now reflects the final single-page review model directly

Manual review steps:

1. Start `AppHost` and open the Studio shell.
2. Open `View -> PrimeReact Showcase Demo` and confirm the page opens directly on a root tab strip with no toolbar or decorative header band above the tabs.
3. Confirm the tab order is `Showcase`, `Forms`, `Data View`, `Data Table`, `Tree`, and `Tree Table`, and that `Showcase` is selected by default.
4. Review the default `Showcase` tab and confirm the upper summary/status strip plus the local showcase hero/theme-sync header are absent, and the combined page still shows tree, grid, and edit/detail form surfaces together with compact density, flatter chrome, smaller action controls, tighter row spacing, pane-owned grid scrolling, selection, filtering, mock editing, and styled-theme following.
5. Switch to the `Forms` tab and confirm the migrated content keeps the same tighter shell density and does not introduce a new page toolbar above the root tabs.
6. Switch to the `Data View` tab and confirm the migrated content keeps the same compact baseline and remains inside the shared tab shell rather than acting like a separate page.
7. Switch to the `Data Table` tab and confirm sorting, filtering, pagination, selection, inline editing, loading, empty, and disabled states remain available with scrolling owned by the inner grid region.
8. Switch to the `Tree` tab and confirm expand/collapse, checkbox selection, filtering, mock toolbar actions, loading, and empty states remain available with scrolling kept inside the hierarchy region.
9. Switch to the `Tree Table` tab and confirm hierarchical rows, columns, checkbox selection, loading, empty, and expansion states remain available with scrolling kept inside the hierarchical grid region.
10. After each tab switch, confirm keyboard focus moves into the newly displayed tab content instead of staying on the tab header.
11. Resize the Studio content area vertically and confirm the outer page remains stable while the hierarchy and grid-heavy regions keep their own scrollbars.
12. Narrow the content area moderately and confirm the `Showcase` grid still presents its own vertical scrollbar when the visible viewport becomes insufficient rather than requiring a widen-then-shrink repaint cycle.
13. While reviewing that narrower layout, confirm the grid header/body text, paginator size, rows-per-page dropdown alignment, and row spacing feel lighter and tighter than before while the `Grid filter` textbox still looks acceptable.
14. Confirm the publish-follow-up checkbox remains available in the detail area without the previous heavy filled panel around it.
15. Compare the hierarchy node text and spacing to the `Filter showcase hierarchy` textbox watermark and confirm the tree now feels materially denser without harming readability.
16. Review the remaining detail labels, badges, and section titles and confirm the overall page feels lighter and more consistent rather than like isolated demo cards.
17. Confirm horizontal overflow stays with the root tab strip or the inner grid region rather than introducing duplicate outer scrollbars.
18. Switch Theia between light and dark themes and confirm the consolidated showcase updates to the matching stock PrimeReact theme.

## PrimeReact theme build/deploy baseline

Work package `075-primereact-system` currently treats `src/Studio/Theme` as the accepted upstream/reference PrimeReact SASS workspace for Studio's first custom theme slice.

For the authoritative workflow, checklists, and theme-versus-layout decision guide, see [PrimeReact Theia UI System](./PrimeReact-Theia-UI-System.md).

### PrimeReact/Theia UI system summary

- `src/Studio/Theme` is the upstream/reference SASS workspace and build toolchain.
- Studio-owned editable theme source lives under `src/Studio/Server/search-studio/src/browser/primereact-theme/source`.
- Generated Studio-consumed theme outputs live under `src/Studio/Server/search-studio/src/browser/primereact-theme/generated`.
- Shared desktop layout behavior lives in the Studio frontend layer, especially `src/Studio/Server/search-studio/src/browser/primereact-demo/search-studio-primereact-demo-page-layout.tsx`.
- `Showcase` remains the first layout proving surface and review entry point for retained pages.
- `Showcase` is not the styling authority for the real UKHO/Theia theme; generic theme authority stays with the Studio-owned theme source.

Current practical version relationship:

- Studio runtime `primereact` package in `search-studio`: `10.9.7`
- upstream/reference `primereact-sass-theme` workspace in `src/Studio/Theme`: `10.8.5`

Current ownership boundary:

- `src/Studio/Theme` stays read-only in intent as the upstream/reference SASS baseline and toolchain
- Studio-owned UKHO/Theia theme source now lives under `src/Studio/Server/search-studio/src/browser/primereact-theme/source`
- generated Studio-consumed theme assets currently land under `src/Studio/Server/search-studio/src/browser/primereact-theme/generated`

Current Studio-owned theme source structure:

- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/shared`
- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-light`
- `src/Studio/Server/search-studio/src/browser/primereact-theme/source/ukho-theia-dark`

Current manual-on-demand bootstrap/build/deploy workflow:

```powershell
Set-Location .\src\Studio\Theme
npm install
npm run build
npm run deploy:studio
npm run verify:studio
```

Equivalent one-line forms from the repository root:

```powershell
npm install --prefix .\src\Studio\Theme
npm run build --prefix .\src\Studio\Theme
npm run deploy:studio --prefix .\src\Studio\Theme
npm run verify:studio --prefix .\src\Studio\Theme
```

Optional wrapper scripts:

- Windows: `src/Studio/Theme/build.bat`
- Unix-like shells: `src/Studio/Theme/build.sh`

Current generated outputs:

- `src/Studio/Server/search-studio/src/browser/primereact-theme/generated/ukho-theia-light.css`
- `src/Studio/Server/search-studio/src/browser/primereact-theme/generated/ukho-theia-dark.css`
- `src/Studio/Server/search-studio/src/browser/primereact-theme/generated/search-studio-generated-primereact-theme-content.ts`

The current deploy step composes the validated upstream Lara light/dark baseline outputs with the Studio-owned UKHO/Theia light/dark SASS source. The temporary PrimeReact research surface now injects that generated local theme content at runtime so the page follows Theia light/dark mode without relying on the stock PrimeReact CDN theme CSS for the active theme layer.

The existing `search-studio` asset copy pipeline already copies `.css` files from `src` into `lib`, so no extra asset-copy extension is currently required once the generated CSS files exist.

## Prerequisite tooling

To build the current Theia workspace successfully on this repository, use the following tooling baseline:

- Node `18.20.4`
- `yarn` classic (`1.x`)
- Visual Studio Build Tools 2022 with C++ build support available for native Node module compilation

Helpful optional tooling:

- `nvm` / `nvm-windows` to switch to the required Node version quickly
- Visual Studio / Aspire tooling for launching the wider local stack

## Build the Theia components

The Theia workspace root is:

- `src/Studio/Server`

### 1. Switch to the validated Node version

```powershell
nvm use 18.20.4
```

### 2. Ensure `yarn` is available

```powershell
yarn --version
```

If needed:

```powershell
npm install -g yarn@1.22.22
```

### 3. Restore JavaScript dependencies

From `src/Studio/Server`:

```powershell
yarn install --ignore-engines
```

`--ignore-engines` is currently required for this generated Theia stack in this repository.

### 4. Build the browser shell

From `src/Studio/Server`:

```powershell
yarn build:browser
```

This builds:

- the workspace packages
- the native Theia extension `search-studio`
- the copied static frontend assets used by the Home document
- the browser application bundle under `browser-app`

### 5. Restart or refresh after frontend changes

When `search-studio` frontend code changes, rebuild the browser bundle and then restart the shell or hard-refresh the browser page so the latest Theia assets are loaded:

```powershell
yarn build:browser
```

Then:

- restart the Aspire-managed `tools-studio-shell` resource, or
- hard refresh the open Studio browser page with `Ctrl+F5`

This is particularly important for `Home`, runtime configuration, and PrimeReact demo work because stale browser bundles can make the shell appear to ignore recent frontend changes.

## Key workspace files

- `src/Studio/Server/package.json` — root workspace scripts
- `src/Studio/Server/browser-app/package.json` — browser application build/start scripts
- `src/Studio/Server/search-studio/package.json` — native Theia extension package metadata and scripts
- `src/Studio/Server/search-studio/src/browser/` — Studio shell browser-side services, commands, runtime configuration, and document surfaces
- `src/Studio/Server/search-studio/src/browser/assets/` — source assets copied into the extension build output for runtime use, including the `Home` logo

## Workspace structure

The Theia workspace contains:

- `src/Studio/Server/package.json` — root workspace scripts
- `src/Studio/Server/browser-app/` — browser application package
- `src/Studio/Server/search-studio/` — native Theia extension package

## Run with Aspire

The shell is designed to run as part of the wider local Aspire stack.

1. Open `src/Hosts/AppHost/appsettings.json`
2. Confirm `Parameters:runmode` is set to `services`
3. Confirm `Studio:Server:Port` is set as required (current default is `3000`)
4. Start `AppHost`
5. In the Aspire dashboard, verify the `tools-studio-shell` resource is healthy
6. Open the shell with `http://localhost:3000`
7. Confirm only the `Home` tab opens automatically, the default Theia `Welcome` page does not appear, and the lower explanatory box is absent
8. Confirm the `Home` tab still shows the UKHO logo and Studio orientation text
9. Close the `Home` tab and reopen it from `View -> Home`

`StudioServiceHost` remains a separate API host, and the shell consumes a runtime configuration bridge so browser code can discover the correct API base URL at startup.

## Current `StudioServiceHost` integration

For work package `058-studio-config`, the shell introduced the local API callback mechanism end to end.

Later work packages extended the Studio API host so the repository now has provider, rules, ingestion, operations, OpenAPI, and diagnostics endpoints available through `StudioServiceHost`.

The current retained Theia shell actively uses only the runtime configuration bridge:

- `GET /search-studio/api/configuration` to read the normalized `STUDIO_API_HOST_API_BASE_URL` value through the Theia backend

The retained shell intentionally does **not** yet call `StudioServiceHost` directly from its visible UI. The current `Home` and demo documents are review surfaces only.

`StudioServiceHost` itself currently exposes:

- `GET /providers`
- `GET /rules`
- `GET /ingestion/{provider}/{id}`
- `POST /ingestion/{provider}/payload`
- `PUT /ingestion/{provider}/all`
- `POST /ingestion/{provider}/operations/reset-indexing-status`
- `GET /ingestion/{provider}/contexts`
- `PUT /ingestion/{provider}/context/{context}`
- `POST /ingestion/{provider}/context/{context}/operations/reset-indexing-status`
- `GET /operations/active`
- `GET /operations/{operationId}`
- `GET /operations/{operationId}/events`
- `GET /echo`
- `GET /openapi/v1.json`
- `GET /scalar/v1`

The host also validates Studio provider registration and loads provider-aware rules during startup so provider/rule mismatches fail early.

### Why this mechanism exists

The studio shell runs in a browser-hosted Theia application on:

- `http://localhost:3000`

`StudioServiceHost` is orchestrated separately by Aspire and exposes developer-facing endpoints that can vary by machine and session.

The shell therefore must **not** hard-code:

- host names
- ports
- protocol (`http` / `https`)
- launch-profile URLs

Instead, Aspire remains the source of truth for the effective `StudioServiceHost` endpoint and passes that value into the shell at startup.

### Runtime configuration bridge

`AppHost` resolves the `StudioServiceHost` **HTTPS** endpoint and passes it into the Theia JavaScript application as the environment variable:

- `STUDIO_API_HOST_API_BASE_URL`

The `search-studio` backend contribution exposes that runtime value to the browser through:

- `/search-studio/api/configuration`

That configuration payload currently includes:

- the normalized `studioApiHostBaseUrl`
- the raw environment value received by the Theia backend
- the environment variable name used for the handoff

This means:

- Aspire resolves the endpoint once
- Theia backend receives it as process environment
- browser code reads it through a controlled same-origin endpoint

The browser does **not** read Node.js process environment directly.

### End-to-end request flow

The current shell flow is:

1. `AppHost` starts in `runmode=services`
2. Aspire allocates the effective `StudioServiceHost` endpoints
3. `AppHost` injects `STUDIO_API_HOST_API_BASE_URL` into the Theia JavaScript app using the resolved `StudioServiceHost` **HTTPS** endpoint
4. The Theia backend starts on `http://localhost:3000`
5. The browser loads the Theia shell
6. the browser-side runtime configuration service calls `/search-studio/api/configuration`
7. startup logging records whether the Studio API base URL was resolved successfully
8. the `Home` document opens automatically, while `PrimeReact Showcase Demo` remains available from `View`

### Theia backend implementation details

The backend contribution lives in:

- `src/Studio/Server/search-studio/src/node/search-studio-backend-application-contribution.ts`

The browser-side runtime configuration service lives in:

- `src/Studio/Server/search-studio/src/browser/search-studio-runtime-configuration-service.ts`

The default startup contribution that validates the bridge and opens `Home` lives in:

- `src/Studio/Server/search-studio/src/browser/search-studio-frontend-application-contribution.ts`

### Current `StudioServiceHost` endpoints

`StudioServiceHost` currently exposes:

- `GET /providers`
  - returns the shared `ProviderDescriptor` metadata for all known providers
- `GET /rules`
  - returns a read-only rule discovery response for all known providers
- `GET /ingestion/{provider}/{id}`
  - fetches a provider-neutral payload envelope by provider-defined id
- `POST /ingestion/{provider}/payload`
  - submits a provider-neutral payload envelope
- `PUT /ingestion/{provider}/all`
  - starts a provider-wide ingestion operation
- `POST /ingestion/{provider}/operations/reset-indexing-status`
  - starts a provider-wide reset operation
- `GET /ingestion/{provider}/contexts`
  - returns provider-neutral contexts
- `PUT /ingestion/{provider}/context/{context}`
  - starts a context-scoped ingestion operation
- `POST /ingestion/{provider}/context/{context}/operations/reset-indexing-status`
  - starts a context-scoped reset operation
- `GET /operations/active`
  - returns the currently active tracked operation when one exists
- `GET /operations/{operationId}`
  - returns a retained operation snapshot by id
- `GET /operations/{operationId}/events`
  - streams server-sent operation events
- `GET /echo`
  - returns `Hello from StudioServiceHost echo.` as a lightweight smoke check
- `GET /openapi/v1.json`
  - returns the generated OpenAPI document
- `GET /scalar/v1`
  - returns the Scalar UI for local exploration

### HTTPS selection

For `StudioServiceHost` integration, the configured base URL should prefer the Aspire-resolved **HTTPS** endpoint.

The shell itself still runs on plain HTTP:

- `http://localhost:3000`

`StudioServiceHost` should normally be consumed over the Aspire-provided HTTPS endpoint handed off through `STUDIO_API_HOST_API_BASE_URL`.

### Main code locations for the mechanism

The current mechanism is spread across the following files:

- `src/Hosts/AppHost/AppHost.cs`
  - resolves the Aspire `StudioServiceHost` endpoint
  - passes it into the Theia JavaScript app as `STUDIO_API_HOST_API_BASE_URL`
- `src/Studio/StudioServiceHost/StudioServiceHostApplication.cs`
  - exposes provider, rules, ingestion, operations, diagnostics, and OpenAPI endpoints
  - validates Studio provider registration and forces rules loading during startup
- `src/Studio/StudioServiceHost/StudioServiceHost.csproj`
  - runs `src/Studio/Server/build.ps1` before build using incremental inputs and a stamp file
- `src/Studio/Server/search-studio/src/node/search-studio-backend-application-contribution.ts`
  - exposes the Theia configuration endpoint
- `src/Studio/Server/search-studio/src/browser/search-studio-runtime-configuration-service.ts`
  - reads the configuration endpoint from the browser side
- `src/Studio/Server/search-studio/src/browser/search-studio-frontend-application-contribution.ts`
  - validates the bridge at startup and opens `Home`
- `src/Studio/Server/search-studio/src/browser/search-studio-command-contribution.ts`
  - exposes `Home` and `PrimeReact Showcase Demo` reopen commands

If you build the solution in Visual Studio first, the Theia shell should already have been prepared by the `StudioServiceHost` pre-build target.

### About the installer resource

When Aspire starts the JavaScript application resource, you may also see a companion installer resource:

- `tools-studio-shell-installer`

That installer is responsible for JavaScript dependency restore when Aspire determines the workspace may need it.

### Current scope limits

The retained Studio shell intentionally remains lightweight:

- no visible provider, rules, ingestion, search, or output work areas are currently active in the retained Theia shell
- the visible default experience is `Home`
- the retained review-only extra surface is `PrimeReact Showcase Demo`
- `StudioServiceHost` is ready beside the shell for future browser-backed functionality

## Quick verification checklist for the current Studio shell

1. Run `yarn --cwd .\src\Studio\Server build:browser`
2. Run `dotnet build src/Hosts/AppHost/AppHost.csproj`
3. Start `AppHost` in `runmode=services`
4. In the Aspire dashboard, verify `tools-studio-api` and `tools-studio-shell` are healthy
5. Open `http://localhost:3000`
6. Confirm only the `Home` tab opens automatically and the default Theia `Welcome` page does not appear
7. Confirm `View -> Home` reopens the same document after it is closed
8. Open `View -> PrimeReact Showcase Demo` and confirm the temporary review surface opens on demand
9. Verify `StudioServiceHost` responds over HTTPS on `/providers`, `/rules`, `/echo`, and `/openapi/v1.json`
10. If the shell startup logs warn about missing runtime configuration, inspect the `STUDIO_API_HOST_API_BASE_URL` handoff from `AppHost`
## Current build caveats

The current generated Theia stack has a few practical constraints in this repository:

- use Node `18.20.4`
- use `yarn` classic
- restore with `yarn install --ignore-engines`
- native module compilation may depend on Visual Studio 2022 C++ build tooling being available
- if you run builds inside a Visual Studio developer shell, inherited Visual Studio 2026 VC toolset environment variables can interfere with native Node builds
- the current Aspire integration exposes the shell on plain HTTP, so browsing to `https://localhost:3000` will fail even when the shell process is healthy
- Aspire may show a separate installer resource for the JavaScript app; the first startup or dependency changes can therefore add noticeable startup time

If native dependency restore fails, retry from a clean PowerShell session with the validated Node version selected.

