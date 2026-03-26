# Theia Knowledgebase

This page captures practical Eclipse Theia lessons learned while implementing work packages `064-studio-skeleton`, `065-studio-tree-widget`, and later Studio shell refinements such as `067-studio-output-enhancements` and `069-search-ui` in this repository.

It is intentionally an internal working knowledge base, not a replacement for official Theia documentation.

Use it when:

- setting up another Theia solution in this repository or elsewhere
- integrating a Theia shell into Aspire
- wiring Theia into Visual Studio `F5` workflows
- debugging a Theia `TreeWidget` that compiles but does not render
- deciding which Theia patterns were proven to work here

## Purpose

The goal of this page is to preserve the practical details that are easy to rediscover the hard way:

- what worked reliably in this repository
- what failed or behaved unexpectedly
- what commands and file wiring were actually required
- what to verify when a Theia UI looks correct in code but does not appear at runtime

## Official references still come first

This page complements, but does not replace, official sources:

- [Theia extensions overview](https://theia-ide.org/docs/extensions/)
- [Authoring Theia extensions](https://theia-ide.org/docs/authoring_extensions/)
- [Theia widgets](https://theia-ide.org/docs/widgets/)
- [Theia tree widget](https://theia-ide.org/docs/tree_widget/)
- [Theia architecture](https://theia-ide.org/docs/architecture/)
- [Theia API docs](https://eclipse-theia.github.io/theia/docs/next/index.html)

## What the retained shell implements today

The current retained Studio shell keeps a smaller active surface than some earlier experimental work packages.

Today it provides:

- a Theia browser application under `src/Studio/Server`
- a native Theia extension package `search-studio`
- Aspire integration for local hosting
- a runtime bridge from Aspire to the Theia backend and then into browser configuration
- a default `Home` document
- a temporary `PrimeReact Showcase Demo` document for UI review
- a sibling `StudioServiceHost` process that exposes provider, rules, ingestion, operations, and OpenAPI endpoints for future shell work

Earlier tree-heavy `Providers`, `Rules`, `Ingestion`, and `Search` workbench slices were exploratory and are not part of the current retained shell baseline.

## Setup pattern that worked with Aspire

### Hosting model

The Theia shell is started from Aspire in `src/Hosts/AppHost/AppHost.cs`.

The key working pattern was:

- host the shell as a JavaScript app resource
- pass the resolved `StudioServiceHost` endpoint into the Theia process as environment
- expose the shell over fixed local HTTP for predictable developer access

Relevant code:

- `src/Hosts/AppHost/AppHost.cs`
- `src/Hosts/AppHost/appsettings.json`

### Fixed port configuration

The shell port is read from Aspire configuration:

- `Studio:Server:Port`

Current location:

- `src/Hosts/AppHost/appsettings.json`

Current value used here:

- `3000`

That made the shell URL stable for local usage:

- `http://localhost:3000`

### Runtime environment bridge

The working pattern was:

1. Aspire resolves the `StudioServiceHost` endpoint
2. `AppHost` passes it into the JavaScript app as `STUDIO_API_HOST_API_BASE_URL`
3. the Theia backend exposes the value via `/search-studio/api/configuration`
4. browser code reads configuration from that same-origin endpoint

This avoided hard-coding hostnames and reduced browser-side environment assumptions.

## What made Visual Studio `F5` work reliably

### Problem

A normal .NET build does not automatically guarantee that the Theia browser assets are present and current.

That is especially painful on:

- a fresh clone
- a machine where JavaScript dependencies are missing
- a normal Visual Studio `F5` flow where developers expect the shell to be ready once the host project runs

### Working solution

The working solution in this repository is to make `StudioServiceHost.csproj` build the Theia shell before build.

Relevant file:

- `src/Studio/StudioServiceHost/StudioServiceHost.csproj`

The target that made this work:

- `BuildTheiaStudioShell`

It runs:

- `src/Studio/Server/build.ps1`

before normal build, while skipping design-time builds.

### Why this worked well

It gave the repository a practical `F5` experience:

- Visual Studio build prepares the shell
- the shell is not forgotten on a clean clone
- subsequent builds are incremental rather than always rebuilding the full Theia browser bundle

### Incremental behavior

The Theia build script and `.csproj` target both watch concrete inputs such as:

- `build.ps1`
- `package.json`
- `yarn.lock`
- `lerna.json`
- `browser-app/package.json`
- `search-studio/package.json`
- `search-studio/tsconfig.json`
- `search-studio/src/**`

and emit a stamp file under:

- `src/Studio/StudioServiceHost/obj/<Configuration>/<TFM>/theia-shell-build.stamp`

This meant:

- clean clone: full restore/build happens
- unchanged workspace: build is skipped
- changed Theia sources: browser build reruns

## Clean clone vs existing workspace initialization

### Clean clone behavior to expect

On a clean clone, expect at least these steps to matter:

1. correct Node version must be available
2. `yarn install --ignore-engines` may need to run
3. native Node dependencies may compile
4. `yarn build:browser` must produce the browser bundle
5. Aspire may also show a companion installer resource on first run

This is normal and can take noticeable time.

### Existing workspace behavior to expect

On an already-restored workspace:

- dependency restore is usually skipped
- the incremental stamp often prevents unnecessary browser rebuilds
- startup is much faster

### Practical takeaway

When diagnosing a problem, always ask first:

- is this a clean clone problem?
- is this a stale browser bundle problem?
- is this a dependency restore problem?
- is this a runtime wiring problem?

## Node, Yarn, and native build constraints that mattered here

The working baseline in this repository was:

- Node `18.20.4`
- Yarn classic `1.x`
- restore with `yarn install --ignore-engines`

The working script also had to defend against Visual Studio toolchain contamination.

Relevant file:

- `src/Studio/Server/build.ps1`

Important behavior inside that script:

- clears selected Visual Studio C++ environment variables before JavaScript/native builds
- forces `npm_config_msvs_version` and `GYP_MSVS_VERSION` to `2022`
- validates the active Node version explicitly

Practical takeaway:

- if Theia restore/build fails from a VS developer shell, retry from a clean PowerShell session
- if Node version is wrong, fix that before debugging anything else

## Symptom: extension TypeScript builds, but the running UI still looks old

### Cause

`yarn --cwd .\src\Studio\Server\search-studio build` only compiles the extension package.

It does **not** prove the served browser bundle has been rebuilt.

### Fix

Run:

```powershell
yarn --cwd .\src\Studio\Server build:browser
```

Then restart the shell or hard-refresh the browser page.

### Practical rule

If a Theia UI change is not visible, do not trust package compilation alone. Rebuild the browser bundle.

## Current retained-shell patterns that worked well

### Keep startup focused on one obvious default document

The current shell works best when startup stays simple:

1. preload runtime configuration
2. log whether the Studio API base URL was resolved
3. open `Home`

That keeps failures diagnosable without blocking the workbench.

Relevant files:

- `src/Studio/Server/search-studio/src/browser/search-studio-frontend-application-contribution.ts`
- `src/Studio/Server/search-studio/src/browser/home/search-studio-home-service.ts`

### Use same-origin configuration bridging instead of browser-side environment assumptions

The working pattern in this repository is:

1. `AppHost` resolves the `StudioServiceHost` HTTPS endpoint
2. Theia receives it as `STUDIO_API_HOST_API_BASE_URL`
3. the backend exposes `/search-studio/api/configuration`
4. browser code reads that endpoint through `SearchStudioRuntimeConfigurationService`

Relevant files:

- `src/Hosts/AppHost/AppHost.cs`
- `src/Studio/Server/search-studio/src/node/search-studio-backend-application-contribution.ts`
- `src/Studio/Server/search-studio/src/browser/search-studio-runtime-configuration-service.ts`

### Keep optional review surfaces disposable

The temporary PrimeReact review surface is intentionally easy to remove later:

- commands are registered from a small ordered definition list
- the `View` menu exposes the demo explicitly
- the widget is not opened during normal startup

Relevant files:

- `src/Studio/Server/search-studio/src/browser/search-studio-command-contribution.ts`
- `src/Studio/Server/search-studio/src/browser/search-studio-menu-contribution.ts`
- `src/Studio/Server/search-studio/src/browser/primereact-demo/`

## Recommended verification sequence for current Theia work

### Build verification

```powershell
yarn --cwd .\src\Studio\Server build:browser
dotnet build .\src\Hosts\AppHost\AppHost.csproj
```

### Runtime verification

1. start `AppHost` in `runmode=services`
2. open `http://localhost:3000`
3. verify only `Home` opens automatically
4. close and reopen `Home` from `View -> Home`
5. open `View -> PrimeReact Showcase Demo`
6. verify `StudioServiceHost` responds over HTTPS on `/providers`, `/rules`, `/echo`, and `/openapi/v1.json`

## Symptom -> likely cause -> fix quick table

| Symptom | Likely cause | Fix |
|---|---|---|
| UI still shows old layout after code changes | only extension package was built | run `yarn --cwd .\src\Studio\Server build:browser` and restart |
| Runtime configuration warning appears at startup | `STUDIO_API_HOST_API_BASE_URL` was not handed off correctly | inspect `AppHost` startup and the Theia configuration endpoint |
| Native build fails unexpectedly in VS shell | inherited VS toolchain environment interferes | use clean PowerShell and let `build.ps1` clear toolchain env |

## Suggested reuse checklist for other solutions

If another solution adopts Theia, start with this checklist:

1. decide fixed local port strategy early
2. decide how Aspire or the host passes runtime API configuration into Theia
3. add a pre-build/incremental shell build path for Visual Studio `F5`
4. lock Node and Yarn versions early
5. keep the default startup experience intentionally small
6. keep temporary review surfaces easy to remove
7. document symptom-to-fix knowledge as it is discovered

## Key files in this repository

### Hosting and build integration

- `src/Hosts/AppHost/AppHost.cs`
- `src/Hosts/AppHost/appsettings.json`
- `src/Studio/StudioServiceHost/StudioServiceHost.csproj`
- `src/Studio/Server/build.ps1`

### Theia backend/runtime bridge

- `src/Studio/Server/search-studio/src/node/search-studio-backend-application-contribution.ts`
- `src/Studio/Server/search-studio/src/browser/search-studio-runtime-configuration-service.ts`

### Default shell surfaces

- `src/Studio/Server/search-studio/src/browser/home/search-studio-home-service.ts`
- `src/Studio/Server/search-studio/src/browser/home/search-studio-home-widget.tsx`
- `src/Studio/Server/search-studio/src/browser/search-studio-frontend-module.ts`

### Optional review surface

- `src/Studio/Server/search-studio/src/browser/primereact-demo/*`

## Future additions to this page

As new work packages land, extend this page with:

- additional Theia-specific gotchas
- backend/frontend split decisions that proved important
- testing patterns that worked well for Theia extensions
- packaging/distribution lessons if the shell moves beyond local-only developer usage
