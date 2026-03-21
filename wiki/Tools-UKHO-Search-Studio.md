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

This work package does **not** migrate existing repository tooling into the shell yet.

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

`src/Studio/StudioHost/StudioHost.csproj` invokes that script before build.

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

`StudioHost` remains a separate future API host and is not yet wired into the shell.

If you build the solution in Visual Studio first, the Theia shell should already have been prepared by the `StudioHost` pre-build target.

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
- no active `StudioHost` API integration
- no migrated `RulesWorkbench`, `FileShareEmulator`, or other tooling workflows

Later work packages can expand the shell into a fuller studio experience.
