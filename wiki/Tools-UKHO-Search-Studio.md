# Tools: `UKHO Search Studio`

This page describes the initial Eclipse Theia-based studio shell introduced for local development.

See also:

- [Theia Knowledgebase](./Theia-Knowledgebase.md)

## Purpose

`UKHO Search Studio` is a browser-hosted Theia application rooted at:

- `src/Studio/Server`

It currently provides:

- a default `Home` tab document that opens on Studio startup and can be reopened from the Theia `View` menu
- task-focused `Home` jump points for `Start ingestion`, `Manage rules`, and `Browse providers`, each reusing the normal Studio destination-opening behavior for the current or first available provider
- a branded Theia shell with dedicated `Providers`, `Rules`, and `Ingestion` activity-bar work areas
- a native Theia extension named `search-studio`
- a reduced-size UKHO logo rendered from a copied runtime asset within the `search-studio` package rather than from the repository `docs/` folder
- provider-backed native Theia navigation trees for `Providers` and `Ingestion` using live `StudioApiHost` `GET /providers` data
- first-root-only default expansion in `Providers`, `Rules`, and `Ingestion` so the first visible provider root opens automatically while other top-level roots remain collapsed
- placeholder editor surfaces for provider overview, queue inspection, and dead-letter inspection
- a rules-backed native Theia `Rules` navigation tree using live `StudioApiHost` `GET /rules` data
- placeholder rules overview, rule-checker, existing-rule, and new-rule editor surfaces opened from the live rules tree
- an ingestion work area with provider overview plus explicit `By id`, `All unindexed`, and `By context` mode nodes beneath provider roots
- placeholder ingestion overview and mode-specific editor surfaces driven by live provider metadata
- native Theia view-toolbar actions for `New Rule`, `Refresh Rules`, and `Refresh Providers` where those actions remain visible
- a lower `Studio Output` panel for shell diagnostics and placeholder action feedback, now rendered through a read-only `xterm.js` surface with reveal-latest behavior, pastel `INFO` / `ERROR` severity styling, and native toolbar `Copy all` and `Clear output` actions
- runtime configuration for the local `StudioApiHost` API base URL
- access to `StudioApiHost` read-only rule discovery through `GET /rules`

This work package does **not** migrate existing repository tooling into the shell yet.

The current Studio skeleton deliberately keeps live backend usage narrow:

- `GET /providers` supplies real provider metadata for the shell navigation
- `GET /rules` supplies real provider-scoped rule discovery for the `Rules` work area
- queue, dead-letter, rules-authoring, and ingestion surfaces remain placeholder-driven for UX review

## How it is hosted locally

The shell is orchestrated by Aspire from:

- `src/Hosts/AppHost`

In `runmode=services`, `AppHost` registers the shell as a JavaScript application resource and binds it to the fixed local port configured under:

- `Studio:Server:Port`

Current default:

- `3000`

The current shell endpoint is exposed as a direct **HTTP** endpoint.

Use:

- `http://localhost:3000`

Do **not** use:

- `https://localhost:3000`

At the moment, the shell is not configured with its own HTTPS endpoint.

## Visual Studio build integration

The repository now includes a Theia build script at:

- `src/Studio/Server/build.ps1`

`src/Studio/StudioApiHost/StudioApiHost.csproj` invokes that script before build.

This was chosen so that, on a fresh clone, building the local Aspire solution in Visual Studio also prepares the Theia shell before the studio API host is built.

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
- `src/Studio/Server/search-studio/src/**`

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

`StudioApiHost` remains a separate API host, and the shell consumes a runtime configuration bridge so its browser-side services can discover the correct API base URL at startup.

## Current `StudioApiHost` integration

For work package `058-studio-config`, the shell introduced the local API callback mechanism end to end.

Later work packages extended `StudioApiHost` so that the shell now has read-only provider and rules discovery APIs for studio tooling.

For work package `064-studio-skeleton`, the shell actively uses:

- `GET /providers` for the live `Providers`, `Rules`, and `Ingestion` provider roots
- `GET /rules` for the live `Rules` provider grouping, rule lists, and rule overview counts

The shell intentionally does **not** yet call deeper queue, dead-letter, or ingestion APIs. Those screens are placeholders whose job is to validate workbench layout, editor behavior, and navigation before real functionality is lifted from existing Blazor tools.

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
3. Confirm the `Providers`, `Rules`, and `Ingestion` activity-bar items are visible
4. Open `Providers` and confirm it renders as a native tree fed by live `GET /providers` data
5. Confirm only the first visible provider root is expanded automatically, then collapse or expand a root manually and confirm a refresh preserves that top-level state
6. Double-click a provider root to open its overview, then open `Queue` and `Dead letters`
7. Open `Rules` and confirm it renders as a native tree with provider roots, `Rule checker`, a `Rules` grouping node, and live rule entries from `GET /rules`
8. Confirm the `Rules` view toolbar exposes `New Rule` and `Refresh Rules`, verify only the first visible provider root auto-expands, then open a rules overview, a rule checker placeholder, an existing rule, and `New Rule`
9. Open `Ingestion` and confirm it renders as a native tree with provider roots plus `By id`, `All unindexed`, and `By context` beneath each provider root
10. Confirm the `Ingestion` view toolbar exposes `Refresh Providers`, verify only the first visible provider root auto-expands, then open an ingestion overview, open each ingestion mode, and trigger `Reset indexing status`
11. Open the `Studio Output` panel and confirm loading, navigation, and placeholder action entries appear there in a dense log-style layout
12. Confirm the output toolbar exposes `Clear output`, use it, and verify the panel clears immediately without a body-level clear button
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
