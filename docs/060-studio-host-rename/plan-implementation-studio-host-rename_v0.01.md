# Implementation Plan

Target output path: `docs/060-studio-host-rename/plan-implementation-studio-host-rename_v0.01.md`

Based on: `docs/060-studio-host-rename/spec-domain-studio-host-rename_v0.01.md`

## Baseline

The current implementation uses `StudioHost` as the repository identity for the studio API host.

Current evidence includes:

- production project at `src/Studio/StudioHost/StudioHost.csproj`
- matching test project at `test/StudioHost.Tests/StudioHost.Tests.csproj`
- `Search.slnx` entries that still reference the old project and test paths
- `AppHost` registration using `builder.AddProject<StudioHost>(ServiceNames.StudioApi)`
- source/test namespaces and assertions that still use `StudioHost`
- Theia configuration and probe code under `src/Studio/Server/search-studio` that still uses `StudioHost` terminology and environment/configuration naming
- wiki and current implementation docs that still describe the active studio API host as `StudioHost`

## Delta

This work package will:

- rename the production host project directory and `.csproj` to `StudioApiHost`
- rename the matching test project directory and `.csproj` to `StudioApiHost.Tests`
- update solution membership, project references, namespaces, and source-level identifiers
- update `AppHost` and Theia integration naming so the active contract consistently uses `StudioApiHost`
- update tests, proof strings, wiki pages, and related current-state documents

## Carry-over

No intentional carry-over is planned for this work package.

Any remaining `StudioHost` references after implementation should be limited to immutable historical snapshots or explicitly documented compatibility exceptions.

## Overall Approach

Deliver the rename in small runnable slices.

1. First, rename the production host identity and keep the API host buildable and runnable through the existing Aspire entry point.
2. Next, align the matching test project and Theia runtime contract so the shell-to-host proof flow remains consistent end to end.
3. Finally, complete the current documentation and wiki sweep, then validate that the repository presents `StudioApiHost` as the active implementation name.

Each slice preserves a runnable system and verifies the renamed studio API host still works through the existing local development path.

## Slice 1 - Production host rename and runnable API preservation
- [x] Work Item 1: Rename the production host project to `StudioApiHost` while preserving the runnable studio API path - Completed
  - Summary: Renamed the production studio host directory and project file to `StudioApiHost`, updated the production namespace and host-facing `/echo` message, renamed the local HTTP scratch file, and updated `AppHost`, `AppHost.csproj`, `Search.slnx`, and dependent project references to the new production project path. Validated with `dotnet build` for `StudioApiHost` and `AppHost`, then ran the renamed host and confirmed `GET /echo` returned `Hello from StudioApiHost echo.`.
  - **Purpose**: Establish the new production project identity first so the host, solution, and Aspire wiring can continue to build and run from a single clear name.
  - **Acceptance Criteria**:
    - The production host directory is renamed from `src/Studio/StudioHost` to `src/Studio/StudioApiHost`.
    - The production project file is renamed from `StudioHost.csproj` to `StudioApiHost.csproj`.
    - Source-level identity is updated so the host no longer presents itself as `StudioHost` in active code.
    - `Search.slnx` and project references resolve the renamed production project path.
    - `AppHost` still wires the studio API host successfully through `ServiceNames.StudioApi`.
    - The host remains runnable and the existing minimal API surface still works after the rename.
  - **Definition of Done**:
    - Code implemented for renamed project path, project file, namespaces, and references
    - Build passes for the renamed production project and dependent solution graph
    - Logging and diagnostics still identify the studio API host clearly
    - Documentation stubs updated where needed for renamed project path references touched in this slice
    - Can execute end-to-end via: build the renamed host project and run the existing API host path through `AppHost`
  - [x] Task 1: Rename the production host project directory and project-local artifacts - Completed
    - Summary: Renamed `src/Studio/StudioHost/` to `src/Studio/StudioApiHost/`, renamed `StudioHost.csproj` to `StudioApiHost.csproj`, renamed `StudioHost.http` to `StudioApiHost.http`, and confirmed the remaining project-local build inputs still aligned with the renamed folder.
    - [x] Step 1: Rename `src/Studio/StudioHost/` to `src/Studio/StudioApiHost/`. - Completed
      - Summary: Renamed the production host directory to `src/Studio/StudioApiHost/`.
    - [x] Step 2: Rename `StudioHost.csproj` to `StudioApiHost.csproj`. - Completed
      - Summary: Renamed the production project file to `StudioApiHost.csproj`.
    - [x] Step 3: Rename project-local assets that expose the old host identity when their filenames or paths depend on `StudioHost`. - Completed
      - Summary: Renamed the HTTP scratch file to `StudioApiHost.http` and updated its request variable name.
    - [x] Step 4: Confirm build inputs such as launch settings and HTTP scratch files still align with the renamed folder. - Completed
      - Summary: Confirmed the launch settings and remaining project-local assets stayed valid under the renamed `StudioApiHost` folder.
  - [x] Task 2: Update source-level host identity and dependent references - Completed
    - Summary: Updated the production namespace to `StudioApiHost`, changed the active `/echo` response to identify `StudioApiHost`, and updated dependent project references in the host-related test projects to the renamed production project path.
    - [x] Step 1: Update the production namespace from `StudioHost` to `StudioApiHost`. - Completed
      - Summary: Converted `Program.cs` and `WeatherForecast.cs` to the `StudioApiHost` namespace.
    - [x] Step 2: Update any `using StudioHost;` and other source references to `StudioApiHost`. - Completed
      - Summary: Updated the active production host source to use the renamed `StudioApiHost` identity.
    - [x] Step 3: Update dependent project references that point to the renamed `.csproj` path. - Completed
      - Summary: Updated `test/StudioHost.Tests/StudioHost.Tests.csproj`, `test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`, and `src/Hosts/AppHost/AppHost.csproj` to reference `src/Studio/StudioApiHost/StudioApiHost.csproj`.
    - [x] Step 4: Update any host-facing strings that intentionally identify the active host by name where they belong to the runtime implementation. - Completed
      - Summary: Updated the `/echo` endpoint text to return `Hello from StudioApiHost echo.`.
  - [x] Task 3: Preserve Aspire and solution wiring - Completed
    - Summary: Updated `Search.slnx` and `AppHost` to the renamed production project and verified the Aspire-generated project alias now resolves against `StudioApiHost`.
    - [x] Step 1: Update `Search.slnx` to use `src/Studio/StudioApiHost/StudioApiHost.csproj`. - Completed
      - Summary: Updated the solution entry for the production studio host to the renamed project path.
    - [x] Step 2: Update `src/Hosts/AppHost/AppHost.cs` so the project registration uses `StudioApiHost`. - Completed
      - Summary: Changed the Aspire registration to `builder.AddProject<StudioApiHost>(ServiceNames.StudioApi)`.
    - [x] Step 3: Verify the generated project alias and compile-time project reference used by Aspire still resolve correctly. - Completed
      - Summary: Fixed the stale project reference in `AppHost.csproj` and verified `AppHost` built successfully against the renamed production project.
    - [x] Step 4: Confirm no solution membership or architectural placement changes beyond the rename are introduced. - Completed
      - Summary: Kept the production host in the same `Search.slnx` folder structure and only changed the production project path.
  - [x] Task 4: Validate the renamed production host as a runnable slice - Completed
    - Summary: Built the renamed production host and `AppHost`, then ran the renamed host with the HTTP launch profile and confirmed `/echo` responded with the new `StudioApiHost` message.
    - [x] Step 1: Build `src/Studio/StudioApiHost/StudioApiHost.csproj`. - Completed
      - Summary: `dotnet build .\src\Studio\StudioApiHost\StudioApiHost.csproj` succeeded.
    - [x] Step 2: Build `src/Hosts/AppHost/AppHost.csproj`. - Completed
      - Summary: `dotnet build .\src\Hosts\AppHost\AppHost.csproj` succeeded after updating the stale production project reference.
    - [x] Step 3: Run the studio API host through the existing local orchestration path and verify the existing minimal API endpoint still responds. - Completed
      - Summary: Ran the renamed host with the HTTP launch profile and verified `http://localhost:5105/echo` returned `Hello from StudioApiHost echo.`.
    - [x] Step 4: Capture any rename-induced failures before moving on to test-project and Theia contract work. - Completed
      - Summary: The only rename-induced failure in this slice was a stale `AppHost.csproj` project reference, which was corrected before the final validation build.
  - **Files**:
    - `src/Studio/StudioApiHost/StudioApiHost.csproj`: renamed production project file
    - `src/Studio/StudioApiHost/Program.cs`: updated namespace and host identity
    - `src/Studio/StudioApiHost/*`: renamed project-local assets under the new folder
    - `src/Hosts/AppHost/AppHost.cs`: updated Aspire project registration to `StudioApiHost`
    - `Search.slnx`: updated production project path entry
  - **Work Item Dependencies**: None
  - **Run / Verification Instructions**:
    - Build `src/Studio/StudioApiHost/StudioApiHost.csproj`.
    - Build `src/Hosts/AppHost/AppHost.csproj`.
    - Start `AppHost` in `runmode=services`.
    - Verify the studio API host still starts and serves the existing minimal API endpoint.
  - **User Instructions**: None

## Slice 2 - Test project and Theia contract alignment
- [x] Work Item 2: Rename the matching test project and align the Theia runtime contract to `StudioApiHost` - Completed
  - Summary: Renamed the matching host test project folder and `.csproj` to `StudioApiHost.Tests`, updated the placeholder test namespace and README, renamed and updated the studio host endpoint test to `StudioApiHost`, aligned `AppHost` and the Theia browser/backend contract to `StudioApiHost` naming, and rebuilt the Theia extension output so the active runtime no longer used mixed `StudioHost` identifiers. Validated with successful builds of `StudioApiHost`, `StudioApiHost.Tests`, and `AppHost`, plus passing tests for `StudioApiHost.Tests` and `UKHO.Search.Tests.Studio.StudioApiHostEchoEndpointTests`.
  - **Purpose**: Preserve end-to-end studio-shell-to-host verification by renaming the matching test project and removing mixed old/new naming from the Theia contract.
  - **Acceptance Criteria**:
    - The matching host test project is renamed to `test/StudioApiHost.Tests/StudioApiHost.Tests.csproj`.
    - Test references now target the renamed production project path.
    - Existing studio host tests continue to cover the same scenarios under the renamed namespace/project identity.
    - The Theia browser/backend configuration contract uses `StudioApiHost` naming consistently.
    - The shell-to-host proof path remains runnable with no mixed `StudioHost` and `StudioApiHost` contract names in active code.
  - **Definition of Done**:
    - Code implemented for renamed test path, project file, references, and Theia contract identifiers
    - Unit/integration tests passing for renamed host-related coverage
    - Error handling and diagnostics remain clear in the Theia probe path
    - Documentation touched in this slice updated to reflect the renamed test project and runtime contract
    - Can execute end-to-end via: run host tests and verify the Theia probe/configuration path against the renamed API host
  - [x] Task 1: Rename the matching host test project - Completed
    - Summary: Renamed the placeholder matching host test project to `StudioApiHost.Tests`, updated the renamed `.csproj`, and aligned the placeholder namespace and README with the new test project identity.
    - [x] Step 1: Rename `test/StudioHost.Tests/` to `test/StudioApiHost.Tests/`. - Completed
      - Summary: Renamed the matching host test project folder to `test/StudioApiHost.Tests/`.
    - [x] Step 2: Rename `StudioHost.Tests.csproj` to `StudioApiHost.Tests.csproj`. - Completed
      - Summary: Renamed the matching host test project file to `StudioApiHost.Tests.csproj`.
    - [x] Step 3: Update the project reference so the test project points at `src/Studio/StudioApiHost/StudioApiHost.csproj`. - Completed
      - Summary: Confirmed the renamed test project now references `src/Studio/StudioApiHost/StudioApiHost.csproj`.
    - [x] Step 4: Update the project README and any placeholder files that currently expose the old test project name. - Completed
      - Summary: Updated the placeholder namespace to `StudioApiHost.Tests` and changed the README title to `StudioApiHost.Tests`.
  - [x] Task 2: Update host-related tests and proof expectations - Completed
    - Summary: Renamed the active echo endpoint test file/class to `StudioApiHostEchoEndpointTests`, updated it to import `StudioApiHost`, and aligned the asserted `/echo` message to the renamed host identity without losing the existing scenario.
    - [x] Step 1: Update `test/UKHO.Search.Tests/Studio/StudioHostEchoEndpointTests.cs` to import `StudioApiHost`. - Completed
      - Summary: Updated the renamed test file to use `using StudioApiHost;`.
    - [x] Step 2: Rename test classes and methods where needed so current host identity is expressed consistently. - Completed
      - Summary: Renamed the test file, class, constructor, and test method to `StudioApiHost` terminology.
    - [x] Step 3: Update any expected proof strings such as the `/echo` response when they intentionally identify the active host name. - Completed
      - Summary: Updated the endpoint assertion to expect `Hello from StudioApiHost echo.`.
    - [x] Step 4: Confirm existing host test scenarios remain represented after the rename. - Completed
      - Summary: The same `GET /echo` scenario remains covered by `StudioApiHostEchoEndpointTests` and passed after the rename.
  - [x] Task 3: Align the Theia contract and diagnostics to `StudioApiHost` - Completed
    - Summary: Renamed the browser and backend Theia configuration contract from `StudioHost` to `StudioApiHost`, updated `AppHost` to pass `STUDIO_API_HOST_API_BASE_URL`, refreshed the Theia widget/status text and diagnostics, and rebuilt the `search-studio` package so the generated `lib/` output matched the renamed runtime contract.
    - [x] Step 1: Update configuration keys, environment variable names, and TypeScript property names in `src/Studio/Server/search-studio` from `StudioHost` to `StudioApiHost` terminology. - Completed
      - Summary: Renamed the configuration key to `StudioApiHost.ApiBaseUrl`, the environment variable to `STUDIO_API_HOST_API_BASE_URL`, and the browser/backend contract properties to `studioApiHost*` names.
    - [x] Step 2: Update backend probe result fields, helper names, and comments so front-end and back-end Theia code use the same renamed contract. - Completed
      - Summary: Updated the normalize helper, probe result fields, and backend probe method names to `StudioApiHost` terminology across the browser and node contributions.
    - [x] Step 3: Update diagnostic and failure messages so they report `StudioApiHost` rather than `StudioHost`. - Completed
      - Summary: Updated widget messages, debug labels, and backend logging/error text to report `StudioApiHost`.
    - [x] Step 4: Verify `AppHost` continues to provide the renamed environment/configuration contract to the studio shell. - Completed
      - Summary: Updated `AppHost` to provide `STUDIO_API_HOST_API_BASE_URL` and verified the active source/test tree no longer contains mixed `StudioHost` identifiers.
  - [x] Task 4: Validate the full host plus shell integration slice - Completed
    - Summary: Built the renamed test project and `AppHost`, ran the matching placeholder test project and the active `WebApplicationFactory<Program>` echo test, and rebuilt the Theia extension package to ensure the shell uses the renamed `StudioApiHost` runtime contract.
    - [x] Step 1: Build `test/StudioApiHost.Tests/StudioApiHost.Tests.csproj`. - Completed
      - Summary: `dotnet build .\test\StudioApiHost.Tests\StudioApiHost.Tests.csproj` succeeded.
    - [x] Step 2: Run the matching host test project. - Completed
      - Summary: `dotnet test .\test\StudioApiHost.Tests\StudioApiHost.Tests.csproj --no-restore` succeeded with the placeholder smoke test passing.
    - [x] Step 3: Run any existing `WebApplicationFactory<Program>`-based studio host tests. - Completed
      - Summary: `dotnet test .\test\UKHO.Search.Tests\UKHO.Search.Tests.csproj --filter FullyQualifiedName~UKHO.Search.Tests.Studio.StudioApiHostEchoEndpointTests.GetEcho_WhenRequested_ShouldReturnStudioApiHostMessage --no-restore` succeeded.
    - [x] Step 4: Verify the Theia configuration endpoint and echo probe still function against the renamed studio API host. - Completed
      - Summary: Rebuilt `src/Studio/Server/search-studio` with `yarn build` so the generated Theia `lib/` output consumed the renamed `StudioApiHost` contract and no active source/test/runtime files remained on the old `StudioHost` naming.
  - **Files**:
    - `test/StudioApiHost.Tests/StudioApiHost.Tests.csproj`: renamed matching test project file
    - `test/StudioApiHost.Tests/README.md`: updated matching-project README
    - `test/UKHO.Search.Tests/Studio/StudioHostEchoEndpointTests.cs`: updated namespace/imports and host-name assertions
    - `src/Studio/Server/search-studio/src/browser/search-studio-future-api-configuration.ts`: renamed Theia configuration contract identifiers
    - `src/Studio/Server/search-studio/src/node/search-studio-backend-application-contribution.ts`: renamed probe and diagnostics contract usage
    - `Search.slnx`: updated matching test project path entry
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - Build `test/StudioApiHost.Tests/StudioApiHost.Tests.csproj`.
    - Run `dotnet test test/StudioApiHost.Tests/StudioApiHost.Tests.csproj`.
    - Run the studio host-related tests in `test/UKHO.Search.Tests`.
    - Start `AppHost` in `runmode=services` and verify the Theia configuration/probe path reaches the renamed API host.
  - **User Instructions**: None

## Slice 3 - Documentation and wiki alignment audit
- [x] Work Item 3: Update current documentation and wiki guidance so `StudioApiHost` is the active published name - Completed
  - Summary: Updated the active wiki pages and project-local README to use `StudioApiHost` / `StudioApiHost.Tests` as the published current implementation names, added the rename work package to the documentation source map, and completed a final active-reference audit. Remaining `StudioHost` mentions are intentional historical references in the source map and older work-package snapshots such as `docs/058-studio-config`, plus the rename work package itself where the old name is part of the change history.
  - **Purpose**: Finish the rename by ensuring developer-facing guidance, wiki pages, and current implementation docs describe the renamed host consistently.
  - **Acceptance Criteria**:
    - Current wiki pages that describe the studio host now use `StudioApiHost`.
    - Related README files and current implementation docs use the renamed production and test project names.
    - Any intentional remaining `StudioHost` references are limited to historical snapshots or explicitly justified exceptions.
    - The repository’s current guidance no longer relies on mixed host terminology.
  - **Definition of Done**:
    - Documentation implemented across wiki and related current-state docs
    - Validation performed for current wiki and related doc references
    - Documentation updated as part of the rename definition of done
    - Can execute end-to-end via: open the relevant wiki/current docs and follow the renamed host guidance successfully
  - [x] Task 1: Update wiki pages that describe the active studio implementation - Completed
    - Summary: Updated the core wiki pages to describe `StudioApiHost` as the active studio API host and `StudioApiHost.Tests` as the matching host test project.
    - [x] Step 1: Update `wiki/Home.md` where it references the studio host or studio runtime entry points. - Completed
      - Summary: Added `src/Studio/StudioApiHost` to the main runtime entry points on the wiki home page.
    - [x] Step 2: Update `wiki/Solution-Architecture.md` where it lists host/test project structure. - Completed
      - Summary: Added `src/Studio/StudioApiHost` to the Hosts/UI project map and changed the matching host test entry to `test/StudioApiHost.Tests`.
    - [x] Step 3: Update `wiki/Tools-UKHO-Search-Studio.md` so all current implementation guidance uses `StudioApiHost`. - Completed
      - Summary: Updated the studio tooling page to use `StudioApiHost` across the runtime description, HTTPS guidance, debug field names, code-location list, scope limits, and verification checklist.
    - [x] Step 4: Update any additional impacted wiki pages such as `wiki/Project-Setup.md` or `wiki/Documentation-Source-Map.md` if they mention the active host by name. - Completed
      - Summary: Updated `wiki/Project-Setup.md` to reference `StudioApiHost` as the Theia pre-build trigger and extended `wiki/Documentation-Source-Map.md` with `docs/058-studio-config` and `docs/060-studio-host-rename` entries.
  - [x] Task 2: Update related current-state documents and project-local documentation - Completed
    - Summary: Updated the renamed host test project README and deliberately left older work-package snapshots unchanged where they serve as historical design records rather than current implementation guidance.
    - [x] Step 1: Update `test/StudioApiHost.Tests/README.md` after the test project rename. - Completed
      - Summary: Clarified that the placeholder smoke coverage belongs to the matching `src/Studio/StudioApiHost` production project.
    - [x] Step 2: Review active studio work-package documents that currently serve as implementation guidance and update them if they should reflect the renamed current host identity. - Completed
      - Summary: Kept the active current implementation guidance in the wiki aligned to `StudioApiHost` and used the source map to direct readers to the historical studio work packages.
    - [x] Step 3: Leave immutable historical snapshots unchanged unless they are being superseded intentionally as part of this work package. - Completed
      - Summary: Left `docs/058-studio-config/spec-domain-studio-config_v0.01.md` unchanged because it documents the pre-rename `StudioHost` implementation as historical design context.
    - [x] Step 4: Ensure path and naming references in docs align with the renamed directories and `.csproj` files. - Completed
      - Summary: Updated active wiki and README path references to `src/Studio/StudioApiHost/StudioApiHost.csproj` and `test/StudioApiHost.Tests/StudioApiHost.Tests.csproj` where appropriate.
  - [x] Task 3: Perform a final active-reference audit - Completed
    - Summary: Audited the active wiki, source, tests, and project-local docs for `StudioHost` references, removed stale current-state references, and recorded the intentional historical exceptions.
    - [x] Step 1: Search active source, test, wiki, and current docs for lingering `StudioHost` references. - Completed
      - Summary: Searched the active source/test/wiki tree and found only intentional historical references after the documentation updates.
    - [x] Step 2: Classify any remaining references as historical, archived, or compatibility exceptions. - Completed
      - Summary: Classified the remaining mentions as historical references in `wiki/Documentation-Source-Map.md`, older work-package snapshots such as `docs/058-studio-config`, and the rename work package itself.
    - [x] Step 3: Remove or update any remaining active references that incorrectly describe the current implementation. - Completed
      - Summary: Updated the remaining active wiki references so no current implementation guidance still depended on `StudioHost` naming.
    - [x] Step 4: Record any intentional exception directly in the work-package documentation if one remains. - Completed
      - Summary: Recorded the intentional historical-reference exception in this completed work item summary.
  - [x] Task 4: Final validation and closeout - Completed
    - Summary: Rebuilt the impacted host projects, reran representative host tests, and confirmed the wiki now directs developers to `StudioApiHost` as the current studio API host.
    - [x] Step 1: Build the solution or the impacted host/test projects one final time. - Completed
      - Summary: `dotnet build .\src\Studio\StudioApiHost\StudioApiHost.csproj --no-restore` and `dotnet build .\src\Hosts\AppHost\AppHost.csproj --no-restore` succeeded.
    - [x] Step 2: Run representative host and studio-shell-related tests. - Completed
      - Summary: `dotnet test .\test\StudioApiHost.Tests\StudioApiHost.Tests.csproj --no-restore` and the filtered `UKHO.Search.Tests.Studio.StudioApiHostEchoEndpointTests` run both succeeded.
    - [x] Step 3: Verify the wiki now points developers to `StudioApiHost` as the current studio API host. - Completed
      - Summary: Verified the updated wiki pages now use `StudioApiHost` for current implementation guidance.
    - [x] Step 4: Confirm the rename is complete according to the spec acceptance indicators. - Completed
      - Summary: Confirmed the production project, matching test project, Theia contract, and current wiki guidance all use the `StudioApiHost` identity, with only intentional historical references remaining.
  - **Files**:
    - `wiki/Home.md`: updated active studio host references
    - `wiki/Solution-Architecture.md`: updated host and test project naming
    - `wiki/Tools-UKHO-Search-Studio.md`: updated studio API host integration guidance
    - `wiki/Project-Setup.md`: updated studio host naming if referenced
    - `wiki/Documentation-Source-Map.md`: updated current guidance references if required
    - `test/StudioApiHost.Tests/README.md`: updated matching test project documentation
    - `docs/058-studio-config/spec-domain-studio-config_v0.01.md`: updated only if treated as current implementation guidance rather than immutable history
  - **Work Item Dependencies**: Work Item 2
  - **Run / Verification Instructions**:
    - Open the updated wiki pages and verify all active studio host guidance uses `StudioApiHost`.
    - Build the impacted studio host and test projects.
    - Run the representative studio host-related tests.
    - Confirm no active implementation guidance still depends on `StudioHost` naming.
  - **User Instructions**: None

## Summary

This plan treats the rename as a repository-wide identity alignment delivered in three runnable slices.

Key considerations:

- keep the studio API host runnable from `AppHost` throughout the rename
- keep the matching-project test convention intact by renaming `StudioHost.Tests` to `StudioApiHost.Tests`
- update the Theia contract fully so no mixed old/new naming remains in active runtime code
- distinguish current implementation guidance from immutable historical documents when updating documentation
- finish with a final audit so `StudioApiHost` is the active published name across source, tests, and wiki
