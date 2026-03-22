# Work Package: `060-studio-host-rename` — rename `StudioHost` to `StudioApiHost`

**Target output path:** `docs/060-studio-host-rename/spec-domain-studio-host-rename_v0.01.md`

**Version:** `v0.01` (Draft)

## 1. Overview

### 1.1 Purpose

This work package defines the functional and technical requirements for renaming the studio API host from `StudioHost` to `StudioApiHost` across the repository.

The rename must cover:

- the production project name and containing directory
- the matching test project name and containing directory
- code, configuration, orchestration, and Theia references that currently use the `StudioHost` name
- current implementation documentation and wiki pages that describe the studio host

The intent is to make the role of the project explicit: it is the API host for the studio shell rather than the shell itself.

### 1.2 Scope

This specification covers:

- renaming `src/Studio/StudioHost/StudioHost.csproj` to `src/Studio/StudioApiHost/StudioApiHost.csproj`
- renaming `test/StudioHost.Tests/StudioHost.Tests.csproj` to `test/StudioApiHost.Tests/StudioApiHost.Tests.csproj`
- updating namespaces, project references, solution membership, and host/orchestration references that currently use `StudioHost`
- updating Theia extension/app configuration contracts, diagnostics, and proof-flow text that currently refer to `StudioHost`
- updating related current-state documentation and `wiki/` content so they describe `StudioApiHost`

This specification does not cover:

- changing the product name `UKHO Search Studio`
- changing the Theia shell folder `src/Studio/Server`
- adding new studio business APIs beyond rename-related touch points
- changing the architectural role of the studio shell or the studio API host
- reorganizing unrelated solution structure

### 1.3 Stakeholders

- maintainers of `src/Studio/*`
- maintainers of `src/Hosts/AppHost`
- maintainers of Theia extension/app code under `src/Studio/Server`
- maintainers of the matching host test projects
- developers using the wiki and studio documentation as the current implementation guide

### 1.4 Definitions

- `StudioHost`: the current studio API host project identity in source, tests, configuration, and documentation
- `StudioApiHost`: the target renamed identity for that same host
- Theia shell: the browser-hosted studio application rooted at `src/Studio/Server`
- current implementation documentation: wiki pages and current repo documents intended to describe the present repository state, as opposed to immutable historical work-package snapshots

## 2. System context

### 2.1 Current state

Observed in the workspace:

- the production project currently exists at `src/Studio/StudioHost/StudioHost.csproj`
- the matching test project currently exists at `test/StudioHost.Tests/StudioHost.Tests.csproj`
- `Search.slnx` currently includes `src/Studio/StudioHost/StudioHost.csproj` and `test/StudioHost.Tests/StudioHost.Tests.csproj`
- `AppHost` currently registers the project via `builder.AddProject<StudioHost>(ServiceNames.StudioApi)`
- the production project currently uses the namespace `StudioHost`
- `test/UKHO.Search.Tests/Studio/StudioHostEchoEndpointTests.cs` currently imports `StudioHost` and asserts the response text `Hello from StudioHost echo.`
- the Theia configuration contract currently includes names such as:
  - `StudioHost.ApiBaseUrl`
  - `studioHostBaseUrl`
  - `rawStudioHostBaseUrl`
  - `STUDIO_HOST_API_BASE_URL`
  - `studioHostEchoUrl`
- the Theia backend contribution currently logs and reports messages such as `StudioHost base URL is not configured for the studio shell.` and `Running StudioHost echo probe.`
- the wiki currently describes the current implementation using `StudioHost`, especially in `wiki/Tools-UKHO-Search-Studio.md`, `wiki/Home.md`, and `wiki/Solution-Architecture.md`
- recent work-package documents such as `docs/058-studio-config/spec-domain-studio-config_v0.01.md` also describe the current studio API host using the `StudioHost` name

### 2.2 Proposed state

The repository will use `StudioApiHost` as the canonical current name for the studio API host.

After the rename:

- the production project path will be `src/Studio/StudioApiHost/StudioApiHost.csproj`
- the matching test project path will be `test/StudioApiHost.Tests/StudioApiHost.Tests.csproj`
- code references, namespaces, and project references will use `StudioApiHost`
- Theia configuration contracts and diagnostics will use `StudioApiHost` terminology
- the wiki and other current implementation documents will describe the renamed host consistently

The rename is a naming and consistency change, not a functional redesign.

### 2.3 Assumptions

- `StudioApiHost` remains the backend location for studio-facing minimal APIs
- the Theia shell remains rooted at `src/Studio/Server`
- the current `AppHost` orchestration role and service wiring remain conceptually the same after the rename
- the matching-project test convention introduced by the repository-wide test refactor remains in force
- historical work-package documents may remain as historical snapshots unless they are explicitly updated or superseded as part of current implementation guidance

### 2.4 Constraints

- the rename must preserve buildability of the solution
- the rename must preserve the current host/test project ownership alignment
- the rename must update both file-system names and in-file identities where those identities currently expose `StudioHost`
- the rename must not leave the Theia extension/app on a mixed `StudioHost`/`StudioApiHost` contract
- current documentation and wiki pages must not describe the active implementation using the old host name after the change is complete

## 3. Component / service design (high level)

### 3.1 Renamed production host identity

The studio API host remains the same logical component, but its repository identity becomes `StudioApiHost`.

This includes:

- directory name
- `.csproj` name
- assembly/root namespace identity where applicable
- source-level references
- local development and test references

### 3.2 Renamed matching test project identity

The matching test project will follow the same rename so the production-to-test naming convention remains explicit.

### 3.3 Theia integration contract alignment

The Theia shell must stop referring to the backend as `StudioHost` in configuration keys, property names, diagnostics, and proof-flow text where those values represent the current API host identity.

### 3.4 Documentation alignment

The wiki and current implementation documentation must present `StudioApiHost` as the active project name and describe the rename consistently.

## 4. Functional requirements

### 4.1 Production project rename

- The solution SHALL rename the production project directory from `src/Studio/StudioHost` to `src/Studio/StudioApiHost`.
- The solution SHALL rename the project file from `StudioHost.csproj` to `StudioApiHost.csproj`.
- The solution SHALL update source-level project identity so that current code no longer refers to the production project as `StudioHost`.
- The solution SHALL update any namespace, import, root namespace, or assembly-facing reference that relies on the project being named `StudioHost`.
- The solution SHALL update adjacent project artifacts under the renamed folder when their names currently expose `StudioHost`, including items such as launch settings, HTTP scratch files, or other project-local assets where applicable.

### 4.2 Matching test project rename

- The solution SHALL rename the matching test project directory from `test/StudioHost.Tests` to `test/StudioApiHost.Tests`.
- The solution SHALL rename the test project file from `StudioHost.Tests.csproj` to `StudioApiHost.Tests.csproj`.
- The solution SHALL update test project references to target `src/Studio/StudioApiHost/StudioApiHost.csproj`.
- The solution SHALL update README, placeholder smoke tests, namespaces, and any other test-local references that currently use `StudioHost.Tests`.
- The resulting host test project SHALL still satisfy the repository rule that the matching test project name is `<ProductionProjectName>.Tests`.

### 4.3 Solution and project reference updates

- `Search.slnx` SHALL be updated to reference the renamed production and test project paths.
- Any source project that currently references `StudioHost` SHALL be updated to reference `StudioApiHost`.
- Any generated or compile-time project alias used by Aspire project registration SHALL resolve to the renamed project identity.
- The rename SHALL preserve the existing solution membership and architectural placement of the project under the studio/host area.

### 4.4 AppHost and orchestration updates

- `src/Hosts/AppHost` SHALL be updated so the registered studio API project reference uses `StudioApiHost` rather than `StudioHost`.
- Existing orchestration intent SHALL be preserved, including the current `ServiceNames.StudioApi` role unless a separate change explicitly renames service identities.
- Runtime behavior for the local studio shell and studio API host integration SHALL remain equivalent after the rename.

### 4.5 Theia extension/app rename propagation

Anything in the Theia extension/app or related studio integration code that currently refers to `StudioHost` as the current API host SHALL be updated to `StudioApiHost` terminology.

This includes, where applicable:

- configuration keys
- environment variable names
- TypeScript property names
- diagnostic messages
- probe result field names
- helper function names
- comments that describe the active host identity
- welcome/proof-flow text shown to developers

Examples of expected contract updates include renaming items such as:

- `StudioHost.ApiBaseUrl` -> `StudioApiHost.ApiBaseUrl`
- `STUDIO_HOST_API_BASE_URL` -> `STUDIO_API_HOST_API_BASE_URL`
- `studioHostBaseUrl` -> `studioApiHostBaseUrl`
- `rawStudioHostBaseUrl` -> `rawStudioApiHostBaseUrl`
- `studioHostEchoUrl` -> `studioApiHostEchoUrl`

If a current identifier is intentionally user-facing or contractual within the active implementation, it SHALL be renamed unless there is a documented technical reason not to do so.

### 4.6 Test and source code rename propagation

- Current code tests that import the `StudioHost` namespace SHALL be updated to import `StudioApiHost`.
- Test classes, method names, and assertion messages that currently refer to `StudioHost` as the current host identity SHOULD be updated to `StudioApiHost` where that improves consistency.
- Current proof endpoint text and related expectations SHOULD be renamed consistently, for example from `Hello from StudioHost echo.` to `Hello from StudioApiHost echo.` when the message is intended to describe the active host name.
- The rename SHALL not remove existing test scenarios; any existing host coverage shall remain represented after the rename.

### 4.7 Documentation and wiki updates

The implementation SHALL update current implementation documentation to use `StudioApiHost` consistently.

This SHALL include:

- relevant wiki pages under `wiki/`
- project-level README files that describe the renamed production or test project
- current work-package documentation that is intended to describe the active implementation state

Documentation updates SHALL cover at least:

- project paths
- project names
- test project names
- Theia integration wording
- AppHost/orchestration descriptions where the studio API host is named explicitly

Where an older work-package document is treated as an immutable historical snapshot, the implementation MAY leave that document unchanged, provided the current wiki and current-state documentation clearly present `StudioApiHost` as the active name.

### 4.8 Acceptance indicators

The rename SHALL be considered complete when:

- the repository builds with `StudioApiHost` and `StudioApiHost.Tests`
- no active code path still depends on the old `StudioHost` project or test project path
- Theia runtime configuration no longer uses a mixed old/new host naming contract
- current wiki and related documentation no longer present `StudioHost` as the current project name
- any remaining `StudioHost` references are limited to intentional historical context, archived material, or explicitly documented compatibility exceptions

## 5. Non-functional requirements

### NFR-001 Consistency

The rename shall be repository-consistent across source, tests, orchestration, and current documentation.

### NFR-002 Minimal behavioral churn

The change shall primarily be a rename and identity-alignment exercise rather than a functional redesign.

### NFR-003 Discoverability

Developers shall be able to locate the studio API host and its matching test project using the repository naming convention without needing knowledge of the prior `StudioHost` name.

### NFR-004 Documentation clarity

Current documentation shall describe the active implementation using the renamed host identity without ambiguous mixed terminology.

## 6. Naming and path model

### 6.1 Required path mapping

| Current | Target |
|---|---|
| `src/Studio/StudioHost/` | `src/Studio/StudioApiHost/` |
| `src/Studio/StudioHost/StudioHost.csproj` | `src/Studio/StudioApiHost/StudioApiHost.csproj` |
| `test/StudioHost.Tests/` | `test/StudioApiHost.Tests/` |
| `test/StudioHost.Tests/StudioHost.Tests.csproj` | `test/StudioApiHost.Tests/StudioApiHost.Tests.csproj` |

### 6.2 Required identity mapping

| Current | Target |
|---|---|
| `StudioHost` | `StudioApiHost` |
| `StudioHost.Tests` | `StudioApiHost.Tests` |
| `namespace StudioHost` | `namespace StudioApiHost` |
| `using StudioHost;` | `using StudioApiHost;` |
| `StudioHost.ApiBaseUrl` | `StudioApiHost.ApiBaseUrl` |
| `STUDIO_HOST_API_BASE_URL` | `STUDIO_API_HOST_API_BASE_URL` |
| `studioHostBaseUrl` | `studioApiHostBaseUrl` |
| `rawStudioHostBaseUrl` | `rawStudioApiHostBaseUrl` |
| `studioHostEchoUrl` | `studioApiHostEchoUrl` |

## 7. Interfaces & integration

### 7.1 Aspire integration

`AppHost` shall continue to be the source of truth for wiring the studio API host into the local development topology.

The studio shell startup path shall receive the renamed studio API host configuration contract without relying on legacy `StudioHost` naming.

### 7.2 Theia configuration contract

The Theia shell shall expose a single renamed configuration contract for the studio API base URL.

Front-end and back-end Theia code shall consume the same renamed contract rather than mixing old and new names.

### 7.3 Test host integration

Any `WebApplicationFactory<Program>` or equivalent test integration for the studio API host shall remain valid after the rename and shall reference the renamed project namespace and path.

## 8. Observability and diagnostics

- Logs, probe results, and failure messages that currently name `StudioHost` as the active host SHOULD be updated to `StudioApiHost`.
- Diagnostics should remain clear for developers validating the Theia-to-host connectivity proof flow.
- The rename should not reduce diagnosability of missing or invalid studio API host configuration.

## 9. Documentation strategy

### 9.1 Wiki

The implementation SHALL update any wiki page that currently presents `StudioHost` as part of the active repository structure or studio workflow.

Likely impacted pages include:

- `wiki/Home.md`
- `wiki/Solution-Architecture.md`
- `wiki/Project-Setup.md` if it references the studio host by name
- `wiki/Tools-UKHO-Search-Studio.md`
- `wiki/Documentation-Source-Map.md` if it references work packages that are now better described through the renamed current host identity

### 9.2 Related documents

The implementation SHALL review related current-state docs and project-local READMEs for `StudioHost` references.

Likely impacted files include:

- `test/StudioApiHost.Tests/README.md` after rename
- active studio work-package docs that describe the current host name and are intended as current implementation guidance

Historical docs may remain unchanged where they are intentionally preserved snapshots, but current guidance shall not rely on the old name.

## 10. Testing strategy

Minimum validation shall include:

1. build the renamed production project
2. build the renamed matching test project
3. verify `Search.slnx` resolves the renamed project paths
4. run the matching host test project after rename
5. run any existing studio-host-related tests that use `WebApplicationFactory<Program>`
6. verify `AppHost` still wires the studio shell to the renamed API host
7. verify the Theia configuration/probe path uses the renamed `StudioApiHost` contract consistently
8. verify current documentation and wiki no longer describe the active implementation as `StudioHost`

## 11. Rollout / migration

Recommended migration sequence:

1. rename the production project directory and `.csproj`
2. rename the matching test project directory and `.csproj`
3. update namespaces, project references, and solution entries
4. update `AppHost` and any generated project alias usage
5. update Theia extension/app naming and diagnostics
6. update tests and proof strings that expose the old host identity
7. update wiki pages and related current implementation documents
8. validate build, test, and studio-shell integration behavior

## 12. Open questions

No additional open questions are currently recorded.
