# Tools: `UKHO Search Studio`

This page describes the active Eclipse Theia-based Studio shell used for local development.

See also:

- [Theia Knowledgebase](./Theia-Knowledgebase.md)

## Purpose

`UKHO Search Studio` is a browser-hosted Theia application rooted at:

- `src/Studio/Server`

The current bootstrap slice provides:

- a fresh active Theia workspace at `src/Studio/Server`
- the previous Studio shell preserved at `src/Studio/OldServer` as reference-only source
- the preserved root workspace script names `build:browser` and `start:browser`
- the preserved package names `browser-app` and `search-studio`
- Studio product branding through the browser-app Theia frontend configuration
- a lightweight Studio `Home` document that opens by default and can be reopened from `View -> Home`
- the copied UKHO logo served from the active runtime asset pipeline under `search-studio`
- a same-origin runtime configuration bridge at `/search-studio/api/configuration`
- browser-side startup validation that logs the resolved `STUDIO_API_HOST_API_BASE_URL` handoff
- the preserved Visual Studio incremental build entrypoint `src/Studio/Server/build.ps1`

This work package does **not** migrate existing repository tooling into the shell yet.

The current Studio shell deliberately keeps live backend usage focused on Studio-safe provider-neutral contracts:

- `GET /providers` supplies real provider metadata for the shell navigation
- `GET /rules` supplies real provider-scoped rule discovery for the `Rules` work area
- queue and dead-letter surfaces remain placeholder-driven for UX review
- ingestion surfaces actively use the provider-neutral Studio ingestion and operations APIs for fetch-by-id, payload submit, context discovery, long-running operations, SSE progress, conflict handling, and final-state readback

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

The shell continues to call `StudioServiceHost` over its Aspire-provided HTTPS endpoint through `STUDIO_API_HOST_API_BASE_URL`, while the browser shell itself stays on fixed-port HTTP for local development.

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
- `View -> PrimeReact Demo` opens a temporary research page that is not shown by default
- the active browser-app composition no longer includes the scaffold-owned Theia `Welcome` / getting-started surface
- the `search-studio` extension preloads the same-origin runtime configuration bridge during startup
- startup logs report whether the Studio API base URL handoff was resolved successfully
- the Home surface uses the copied runtime-served UKHO logo plus lightweight orientation text only, without the previous lower explanatory box
- later work items build on the restored Home document with follow-on Studio UI surfaces

## Temporary PrimeReact evaluation pages

Work package `074-primereact-research` adds the temporary PrimeReact research surfaces under:

- `src/Studio/Server/search-studio/src/browser/primereact-demo`

Current behavior:

- the demo pages are exposed only from `View -> PrimeReact Demo`, `View -> PrimeReact Data Table Demo`, `View -> PrimeReact Forms Demo`, `View -> PrimeReact Data View Demo`, `View -> PrimeReact Layout Demo`, `View -> PrimeReact Showcase Demo`, `View -> PrimeReact Tree Demo`, and `View -> PrimeReact Tree Table Demo`
- the demo pages do **not** open by default on startup
- every page now assumes full styled PrimeReact only
- every page follows the active Theia `light` / `dark` theme by switching between the stock PrimeReact Lara light and dark themes
- the temporary demo surfaces use in-memory datasets only and keep all actions mock-only for look-and-feel review
- the temporary demo command and menu registrations are kept inside the isolated `primereact-demo` contribution area so the research package remains easy to remove later as one focused change set

Manual review steps:

1. Start `AppHost` and open the Studio shell.
2. Open `View -> PrimeReact Demo` and confirm the bootstrap page shows the initial controlled input, lane selector, status tags, buttons, and progress bars.
3. Open `View -> PrimeReact Data Table Demo` and confirm sorting, filtering, pagination, selection, inline editing, loading, empty, and disabled states are all visible.
4. Open `View -> PrimeReact Forms Demo` and confirm controlled inputs, inline validation, grouped selection controls, disabled controls, and the mock loading save action are all visible.
5. Open `View -> PrimeReact Data View Demo` and confirm card/list layout switching, density changes, selection state, pagination, and empty-state presentation are all visible.
6. Open `View -> PrimeReact Layout Demo` and confirm `TabView`, `Splitter`, `Panel`, and `Divider` composition plus draggable resizing interactions are all visible.
7. Open `View -> PrimeReact Showcase Demo` and confirm the combined page shows tree, grid, and edit/detail form surfaces together, including selection, filtering, mock editing, and styled-theme following.
8. Open `View -> PrimeReact Tree Demo` and confirm expand/collapse, checkbox selection, filter, mock toolbar actions, loading, and empty states are all visible.
9. Open `View -> PrimeReact Tree Table Demo` and confirm hierarchical rows, columns, checkbox selection, loading, empty, and expansion states are all visible.
10. Switch Theia between light and dark themes and confirm each demo updates to the matching stock PrimeReact theme.

## Prerequisite tooling

To build the current Theia workspace successfully on this repository, use the following tooling baseline:

- Node `18.20.4`
- `yarn` classic (`1.x`)
- the globally installed `generator-theia-extension` package used to scaffold the workspace
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

This is particularly important for `Studio Output` work because stale browser bundles can make the panel appear to ignore xterm-related styling or behavior changes.

## Key workspace files

- `src/Studio/Server/package.json` — root workspace scripts
- `src/Studio/Server/browser-app/package.json` — browser application build/start scripts
- `src/Studio/Server/search-studio/package.json` — native Theia extension package metadata and scripts
- `src/Studio/Server/search-studio/src/browser/` — Studio shell browser-side services, views, trees, commands, and document surfaces
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

`StudioApiHost` remains a separate API host, and the shell consumes a runtime configuration bridge so its browser-side services can discover the correct API base URL at startup.

## Current `StudioApiHost` integration

For work package `058-studio-config`, the shell introduced the local API callback mechanism end to end.

Later work packages extended `StudioApiHost` so that the shell now has read-only provider and rules discovery APIs for studio tooling.

For work package `064-studio-skeleton`, the shell actively uses:

- `GET /providers` for the live `Providers`, `Rules`, and `Ingestion` provider roots
- `GET /rules` for the live `Rules` provider grouping, rule lists, and rule overview counts

The shell intentionally does **not** yet call deeper queue or dead-letter APIs. Those screens remain placeholders whose job is to validate workbench layout, editor behavior, and navigation before real functionality is lifted from existing Blazor tools.

The ingestion work area now actively uses:

- `GET /ingestion/{provider}/{id}` to fetch a provider-neutral payload envelope by id
- `POST /ingestion/{provider}/payload` to submit a fetched payload synchronously
- `PUT /ingestion/{provider}/all` and `POST /ingestion/{provider}/operations/reset-indexing-status` for provider-wide long-running ingestion/reset flows
- `GET /ingestion/{provider}/contexts`, `PUT /ingestion/{provider}/context/{context}`, and `POST /ingestion/{provider}/context/{context}/operations/reset-indexing-status` for context-scoped flows
- `GET /operations/active`, `GET /operations/{operationId}`, and `GET /operations/{operationId}/events` for recovery, live progress, shared conflict handling, and final-state readback across all ingestion screens

The shell also intentionally does **not** write rules yet. `New Rule` currently opens a placeholder authoring surface only.

This section describes the implemented mechanism in detail, including why it uses a Theia backend proxy and how Aspire, Theia, and `StudioApiHost` each participate.

### Why this mechanism exists

The studio shell runs in a browser-hosted Theia application on:

- `http://localhost:3000`

`StudioApiHost` is orchestrated separately by Aspire and exposes developer-facing endpoints that can vary by machine and session.

The shell therefore must **not** hard-code:

- host names
- ports
- protocol (`http` / `https`)
- launch-profile URLs

Instead, Aspire remains the source of truth for the effective `StudioApiHost` endpoint and passes that value into the shell at startup.

### Runtime configuration bridge

`AppHost` resolves the `StudioApiHost` **HTTPS** endpoint and passes it into the Theia JavaScript application as the environment variable:

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
2. Aspire allocates the effective `StudioApiHost` endpoints
3. `AppHost` injects `STUDIO_API_HOST_API_BASE_URL` into the Theia JavaScript app using the resolved `StudioApiHost` **HTTPS** endpoint
4. The Theia backend starts on `http://localhost:3000`
5. The browser loads the Theia shell and the `Providers` work area
6. the browser-side Studio API client calls the Theia same-origin configuration endpoint:
   - `/search-studio/api/configuration`
7. the browser-side Studio API client calls `StudioApiHost` `GET /providers` using the configured base URL
8. provider metadata is mapped into the shell trees for `Providers`, `Rules`, and `Ingestion`
9. the first visible provider root in each work area is expanded automatically on first render while later top-level roots remain collapsed until the user changes them
10. selecting provider nodes opens placeholder editor surfaces in the main workbench area
11. shell loading and placeholder actions are written to the lower `Studio Output` panel

## `Studio Output` baseline

Work package [`067-studio-output-enhancements`](../docs/067-studio-output-enhancements/plan-studio-output-enhancements_v0.03.md) established the current `Studio Output` baseline.

The panel is intentionally:

- Studio-owned
- read-only
- non-terminal in semantics
- rendered through `xterm.js` for dense output presentation

Current baseline behaviors:

- a single merged chronological stream
- explicit visible `time`, `severity`, `source`, and `message` text on each line
- reveal-latest behavior for newly appended output
- pastel blue `INFO` and pastel red `ERROR` severity tokens
- direct text selection from the output surface
- toolbar `Copy all` and `Clear output` actions

The panel deliberately does **not** expose:

- shell prompts
- command entry
- stdin-driven interaction
- explicit output-channel switching

## Reviewing the Studio skeleton

The purpose of the current shell is to review the overall look, navigation model, and workbench shape before real `RulesWorkbench` and `FileShareEmulator` functionality is lifted into Studio.

### Recommended manual smoke path

1. Start the local stack through `AppHost`
2. Open `http://localhost:3000`
3. Confirm the `Providers`, `Rules`, `Ingestion`, and `Search` activity-bar items are visible, and that built-in `Explore` sits below them
4. Open `Providers` and confirm it renders as a native tree fed by live `GET /providers` data
5. Confirm only the first visible provider root is expanded automatically, then collapse or expand a root manually and confirm a refresh preserves that top-level state
6. Double-click a provider root to open its overview, then open `Queue` and `Dead letters`
7. Open `Rules` and confirm it renders as a native tree with provider roots, `Rule checker`, a `Rules` grouping node, and live rule entries from `GET /rules`
8. Confirm the `Rules` view toolbar exposes `New Rule` and `Refresh Rules`, verify only the first visible provider root auto-expands, then open a rules overview, a rule checker placeholder, an existing rule, and `New Rule`
9. Open `Ingestion` and confirm it renders as a native tree with provider roots plus `By id`, `All unindexed`, and `By context` beneath each provider root
10. Confirm the `Ingestion` view toolbar exposes `Refresh Providers`, verify only the first visible provider root auto-expands, then open an ingestion overview, open each ingestion mode, and trigger `Reset indexing status`
11. Open `Search`, enter a query, confirm the `Search` button enables only when text is present, then trigger search and verify the mock `Search results` document plus empty `Search Details` panel appear
12. Select a mock result and confirm the right-hand `Search Details` panel updates with fake values
13. Open the `Studio Output` panel and confirm loading, navigation, and placeholder action entries appear there in a dense log-style layout
14. Confirm the output toolbar exposes `Clear output`, use it, and verify the panel clears immediately without a body-level clear button
13. Note that the current `067-studio-output-enhancements` baseline also plans a toolbar `Copy all` action for the merged output stream once that slice is delivered
14. Confirm all three work areas and the output panel feel visually consistent, with no body-level CTA buttons, duplicate in-body titles, or persistent provider description text in the side bar

At this stage, success means the shell feels coherent and reviewable, not that queue, rules, or ingestion functionality has been implemented yet.

### Why the Theia backend uses a probe/configuration bridge

The first implementation attempted to have the browser call `StudioApiHost` directly.

That approach is fragile in local development because it depends on:

- browser CORS rules
- certificate trust for the browser-facing shell
- certificate trust for the browser-to-`StudioApiHost` HTTPS call
- protocol mismatches between shell and API endpoints

The current implementation uses a **same-origin Theia backend proxy/probe** instead:

- browser -> `http://localhost:3000/search-studio/api/echo`
- Theia backend -> `https://localhost:<studioapihost-port>/echo`

Advantages of this approach:

- the browser only talks to the same origin as the shell
- the probe can return structured diagnostics to the UI
- the probe can handle local HTTPS certificate behaviour explicitly
- the UI can show the actual configured URL and attempted target URL

This is still only a temporary proof mechanism, but it is more diagnosable and more reliable for local orchestration.

### Theia backend implementation details

The backend contribution lives in:

- `src/Studio/Server/search-studio/src/node/search-studio-backend-application-contribution.ts`

It exposes two local endpoints:

1. configuration endpoint
   - `/search-studio/api/configuration`
   - returns the configured `StudioApiHost` base URL and related environment diagnostics

2. echo probe endpoint
   - `/search-studio/api/echo`
   - performs the server-side request to `StudioApiHost`
   - returns:
     - configured base URL
     - raw environment value
     - attempted `/echo` URL
     - HTTP status if obtained
     - returned echo body when successful
     - error text when unsuccessful

The probe uses Node.js `http` / `https` request handling rather than browser `fetch` so it can better manage local HTTPS behavior and provide clearer error reporting.

### Current `StudioApiHost` endpoints

`StudioApiHost` currently exposes:

- `GET /providers`
  - returns the shared `ProviderDescriptor` metadata for all known providers
  - uses canonical provider names from `UKHO.Search.ProviderModel`
- `GET /rules`
  - returns a read-only rule discovery response for all known providers
  - includes the shared rules `schemaVersion`
  - returns canonical provider names plus rule summaries (`id`, `context`, `title`, `description`, `enabled`)
  - includes known providers with no rules as empty `rules` arrays
  - fails startup clearly if configured rules reference an unknown provider
- `GET /echo`
  - temporary connectivity proof kept for backend-bridge diagnosis

The visible Studio shell now uses `/providers` and `/rules` for navigation. `GET /echo` remains available only as a lightweight backend-bridge diagnostic endpoint.

### Temporary proof endpoint

`StudioApiHost` now exposes:

- `GET /echo`

Current temporary response:

- `Hello from StudioApiHost echo.`

This endpoint remains only a lightweight diagnostic mechanism and is not part of the stakeholder-reviewable Studio workbench experience.

### HTTPS selection

For `StudioApiHost` integration, the configured base URL should prefer the Aspire-resolved **HTTPS** endpoint.

In practice this means the environment handoff uses the HTTPS `StudioApiHost` address, for example:

- `https://localhost:7073`

and not the HTTP address, for example:

- `http://localhost:5105`

This matters because in local runs the HTTP endpoint may not behave as expected for the proof flow, while the HTTPS endpoint is the intended developer-facing endpoint.

### CORS and protocol note

The shell itself still runs on plain HTTP:

- `http://localhost:3000`

`StudioApiHost` is probed over HTTPS.

The current `StudioApiHost` CORS policy still allows the local shell origin:

- `http://localhost:3000`

That allowance is harmless for the current proof and preserves flexibility, but the important detail is that the browser-facing widget now relies on the Theia same-origin backend probe rather than a direct cross-origin browser call.

### Debug information available through the shell and backend bridge

The current shell keeps the backend probe/configuration path available for local diagnosis while the visible Studio experience has moved on to the multi-work-area skeleton.

Current fields include:

- `Browser origin`
- `Probe transport`
- `Theia probe endpoint`
- `Theia config endpoint`
- `StudioApiHost env var`
- `Raw StudioApiHost env value`
- `Configured StudioApiHost base URL`
- `Attempted StudioApiHost echo URL`
- `Probe HTTP status`
- `Probe error`

This information is intended for local diagnosis and should make it obvious whether a failure is caused by:

- missing environment propagation from Aspire
- incorrect protocol selection
- wrong host or port
- backend timeout
- TLS / certificate issues
- `StudioApiHost` not responding on the expected endpoint

### Main code locations for the mechanism

The current proof path is spread across the following files:

- `src/Hosts/AppHost/AppHost.cs`
  - resolves the Aspire `StudioApiHost` endpoint
  - passes it into the Theia JavaScript app as `STUDIO_API_HOST_API_BASE_URL`
- `src/Studio/StudioApiHost/StudioApiHostApplication.cs`
  - exposes `GET /providers`, `GET /rules`, and `GET /echo`
  - forces shared provider-aware rules loading during startup so invalid rule-provider identities fail early
- `src/Studio/Server/search-studio/src/node/search-studio-backend-application-contribution.ts`
  - exposes the Theia config and probe endpoints
- `src/Studio/Server/search-studio/src/browser/search-studio-api-configuration-service.ts`
  - reads the configuration endpoint from the browser side
- `src/Studio/Server/search-studio/src/browser/search-studio-echo-probe-service.ts`
  - calls the same-origin Theia probe endpoint
- `src/Studio/Server/search-studio/src/browser/search-studio-widget.tsx`
  - renders the live provider-backed `Providers` work area

If you build the solution in Visual Studio first, the Theia shell should already have been prepared by the `StudioApiHost` pre-build target.

### About the installer resource

When Aspire starts the JavaScript application resource, you may also see a companion installer resource:

- `tools-studio-shell-installer`

That installer is responsible for JavaScript dependency restore when Aspire determines the workspace may need it.

Practical guidance:

- on a fresh clone, expect the installer step to run and take noticeable time
- after the workspace has already been restored and built, startup should usually be faster
- if `package.json`, `yarn.lock`, or other key workspace inputs change, Aspire may run the installer again

The separate Visual Studio pre-build integration exists so the shell is usually prepared before normal local debugging starts.

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

## Current scope limits

The initial shell intentionally remains lightweight:

- no bundled VS Code extensions
- queue, dead-letter, rule-authoring persistence, and ingestion execution remain placeholder-only despite the live navigation data
- no migrated `RulesWorkbench`, `FileShareEmulator`, or other tooling workflows

Later work packages can replace the remaining placeholder editors with real lifted tooling functionality.

## Quick verification checklist for the Studio skeleton

1. Run `yarn build:browser` from `src/Studio/Server`
2. Run `dotnet build src/Hosts/AppHost/AppHost.csproj`
3. Start `AppHost` in `runmode=services`
4. In the Aspire dashboard, prefer the `StudioApiHost` **HTTPS** URL
5. Verify `StudioApiHost` responds on `/providers`, `/rules`, and `/echo` via HTTPS
6. Open `http://localhost:3000`
7. Confirm the `Providers`, `Rules`, and `Ingestion` activity-bar items appear
8. Walk through the manual smoke path above for one provider across all three work areas
9. If the shell cannot load its live data, inspect the Theia backend configuration/probe diagnostics for:
   - raw environment value
   - configured base URL
   - attempted echo URL
   - probe status / error
