# Tools: `UKHO Search Studio`

This page describes the initial Eclipse Theia-based studio shell introduced for local development.

## Purpose

`UKHO Search Studio` is a browser-hosted Theia application rooted at:

- `src/Studio/Server`

It currently provides:

- a minimal branded Theia shell
- a native Theia extension named `search-studio`
- a lightweight welcome panel in the standard Theia workbench
- a simple greeting action proving the custom extension wiring is active
- runtime configuration for the local `StudioApiHost` API base URL
- a temporary welcome-page proof that calls `StudioApiHost` `GET /echo` and displays the returned value
- access to `StudioApiHost` provider metadata discovery through `GET /providers`
- access to `StudioApiHost` read-only rule discovery through `GET /rules`

This work package does **not** migrate existing repository tooling into the shell yet.

The Theia UI currently surfaces only the echo connectivity proof directly. The provider and rules discovery endpoints are available from `StudioApiHost` for follow-on studio workflows and manual local inspection.

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

## Key workspace files

- `src/Studio/Server/package.json` — root workspace scripts
- `src/Studio/Server/browser-app/package.json` — browser application build/start scripts
- `src/Studio/Server/search-studio/package.json` — native Theia extension package metadata and scripts
- `src/Studio/Server/search-studio/src/browser/` — welcome panel, command, menu, and view contribution sources

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

`StudioApiHost` remains a separate API host, but the shell now consumes a temporary runtime configuration path so the welcome page can prove connectivity.

## Current `StudioApiHost` integration

For work package `058-studio-config`, the shell proves the local API callback mechanism end to end.

Later work packages extended `StudioApiHost` so that, alongside the temporary echo proof, it now also exposes read-only provider and rules discovery APIs for studio tooling.

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

The current proof flow is:

1. `AppHost` starts in `runmode=services`
2. Aspire allocates the effective `StudioApiHost` endpoints
3. `AppHost` injects `STUDIO_API_HOST_API_BASE_URL` into the Theia JavaScript app using the resolved `StudioApiHost` **HTTPS** endpoint
4. The Theia backend starts on `http://localhost:3000`
5. The browser loads the welcome widget
6. The welcome widget calls the Theia same-origin configuration endpoint:
   - `/search-studio/api/configuration`
7. The welcome widget calls the Theia same-origin probe endpoint:
   - `/search-studio/api/echo`
8. The Theia backend probe reads the configured `StudioApiHost` URL from process environment
9. The Theia backend probe performs the server-side request to `StudioApiHost` `GET /echo`
10. The probe returns structured diagnostics plus the echo result to the browser
11. The welcome widget renders:
   - success state with the returned echo value, or
   - failure state with detailed diagnostic information

### Why the welcome page uses a Theia backend probe instead of a direct browser call

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
  - temporary connectivity proof used by the current welcome page

The Theia welcome flow still uses only `GET /echo` today. The `/providers` and `/rules` endpoints are available for later studio features and for manual API inspection.

### Temporary proof endpoint

`StudioApiHost` now exposes:

- `GET /echo`

Current temporary response:

- `Hello from StudioApiHost echo.`

This endpoint is only a lightweight proof mechanism and is intended to be replaced by real studio APIs in later work.

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

### Debug information shown in the welcome page

The welcome panel intentionally shows detailed diagnostics while this work remains in proof mode.

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
  - renders the echo result and diagnostics

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
- the Theia UI itself still only surfaces the temporary echo proof, even though `StudioApiHost` now also exposes `/providers` and read-only `/rules`
- no migrated `RulesWorkbench`, `FileShareEmulator`, or other tooling workflows

Later work packages can expand the shell into a fuller studio experience.

## Quick verification checklist for the temporary API proof

1. Run `yarn build:browser` from `src/Studio/Server`
2. Run `dotnet build src/Hosts/AppHost/AppHost.csproj`
3. Start `AppHost` in `runmode=services`
4. In the Aspire dashboard, prefer the `StudioApiHost` **HTTPS** URL
5. Verify `StudioApiHost` responds on `/providers`, `/rules`, and `/echo` via HTTPS
6. Open `http://localhost:3000`
7. Confirm the welcome page shows the `StudioApiHost echo` value
8. Optionally inspect `StudioApiHost` `/rules` and confirm canonical provider names and rule summaries are returned
9. If the proof fails, inspect the welcome-page debug panel for:
   - raw environment value
   - configured base URL
   - attempted echo URL
   - probe status / error
