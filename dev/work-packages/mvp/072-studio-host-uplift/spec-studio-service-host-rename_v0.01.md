# Studio Service Host Rename Specification

- Work Package: `072-studio-host-uplift`
- Version: `v0.01`
- Status: `Draft`
- Target Output Path: `dev/work-packages/mvp/072-studio-host-uplift/spec-studio-service-host-rename_v0.01.md`

## 1. Overview

### 1.1 Purpose

Define the requirements for renaming the existing `StudioApiHost` host project to `StudioServiceHost`, including the corresponding source folder, test project, test folder, namespaces, and related type names.

The requested scope also includes moving the minimal API endpoint mappings currently held in `StudioApiHostApplication.cs` into the `Api` namespace so the host bootstrap follows the same endpoint-organization style already used for `StudioIngestionApi` and `StudioOperationsApi`.

### 1.2 Scope

This specification covers the existing host project under `src/Studio/StudioApiHost`, the related test project under `test/StudioApiHost.Tests`, and the application/bootstrap types and namespaces directly tied to that host rename.

### 1.3 Stakeholders

- Studio host maintainers
- Studio shell and API consumers
- Test maintainers for the Studio host
- Repository maintainers responsible for naming and code organization consistency

### 1.4 Definitions

- **Current host project**: `src/Studio/StudioApiHost`
- **Target host project**: `src/Studio/StudioServiceHost`
- **Current test project**: `test/StudioApiHost.Tests`
- **Target test project**: `test/StudioServiceHost.Tests`

## 2. System context

### 2.1 Current state

The repository currently contains a host project named `StudioApiHost` and a corresponding test project named `StudioApiHost.Tests`. The host includes bootstrap and configuration logic in `StudioApiHostApplication.cs`, while some minimal API surface is already organized under the `Api` namespace and some endpoint mappings are still hosted directly in the application bootstrap file.

The rename is intended to be a full rename rather than a partial aliasing exercise.

### 2.2 Proposed state

A future implementation should rename the host and test projects to `StudioServiceHost`, align folders and namespaces with the new naming, rename `StudioApiHostApplication.cs` to match the new host naming, and move the remaining minimal API endpoint mappings out of the application bootstrap file into the `Api` namespace.

That future implementation should also rename the project files, default assembly naming, root namespaces, primary application/bootstrap type names, test project naming, and corresponding folder names so the host and its tests consistently use `StudioServiceHost` terminology throughout.

The remaining minimal API endpoints currently mapped directly in the application bootstrap should be moved into separate `Api` classes by concern rather than being kept together in one catch-all endpoint class.

That extraction should be treated as relocation only. The existing minimal API code, route paths, and endpoint behavior should stay the same; the implementation should simply move the existing endpoint definitions out of the bootstrap and into dedicated API classes.

The bootstrap reshaping should stay deliberately minimal. The work should move only the minimal API endpoint definitions out of the application bootstrap and should not use that move as a reason to broaden or otherwise redesign the remaining host bootstrap responsibilities.

The renamed bootstrap/application type should be `StudioServiceHostApplication`.

The rename should be clean with no temporary compatibility shims, legacy aliases, or transitional references retained for the old `StudioApiHost` naming.

### 2.3 Assumptions

- The rename should keep runtime behavior functionally equivalent while improving naming consistency.
- The existing `Api` namespace pattern should remain the model for endpoint organization.
- The work item is expected to produce a single specification document in this work package.

### 2.4 Constraints

- Keep the work package output as a single specification document.
- Avoid widening scope beyond the host, its tests, and the directly related rename/move work unless clarification requires it.

## 3. Component / service design (high level)

### 3.1 Components

- `StudioApiHost` production host project
- `StudioApiHost.Tests` host test project
- `StudioApiHostApplication.cs` bootstrap/application composition type
- Existing `Api` namespace endpoint classes

### 3.2 Data flows

At a high level, the renamed host should continue exposing the same Studio service endpoints and operational flows, but with updated project/type naming and cleaner separation between bootstrap/composition logic and minimal API endpoint mapping logic.

### 3.3 Key decisions

- The rename should be a full rename across folders, project files, namespaces, root namespaces, application/bootstrap type names, and test project naming.
- The remaining bootstrap-owned minimal API endpoints should be split into separate endpoint classes by concern within the `Api` namespace.
- The bootstrap file should change only as far as needed to move the inlined minimal API endpoint definitions out to the `Api` namespace; no broader bootstrap restructuring is intended.
- The renamed bootstrap/application type should be `StudioServiceHostApplication`.
- Host and test class names should also be fully renamed to match `StudioServiceHost` terminology.
- Naming should remain consistent throughout the rename, including projects, folders, namespaces, bootstrap types, API classes, and test class names.
- Existing API class names that already exist may remain as they are; only the new API classes extracted from the bootstrap need new names chosen for the `StudioServiceHost` shape.
- Existing URL paths should remain unchanged for compatibility, but endpoint names may be renamed where they currently reflect the old host naming.
- User-facing strings and diagnostics that currently mention `StudioApiHost` should also be renamed for consistency.
- The rename should be clean, with no compatibility shims for the old host naming.
- The extracted minimal API code should be moved as-is into dedicated API classes; this is relocation work, not endpoint redesign.
- The extracted endpoint split should be explicitly `ProvidersApi`, `RulesApi`, and `DiagnosticsApi`, with the existing `/providers`, `/rules`, and `/echo` endpoint definitions moved into those classes respectively.

## 4. Functional requirements

1. Rename `src/Studio/StudioApiHost` to `src/Studio/StudioServiceHost`.
2. Rename `test/StudioApiHost.Tests` to `test/StudioServiceHost.Tests`.
3. Rename the production host project file, related project identity, and root namespace from `StudioApiHost` to `StudioServiceHost`.
4. Rename the host test project file, related project identity, and root namespace from `StudioApiHost.Tests` to `StudioServiceHost.Tests`.
5. Rename `StudioApiHostApplication.cs` and its contained type to `StudioServiceHostApplication`.
6. Rename host and test class names that still use `StudioApiHost` terminology so naming remains consistent with `StudioServiceHost`.
7. Update namespaces throughout the renamed production and test projects so source code matches the new folder and project naming.
8. Keep existing URL paths unchanged for compatibility.
9. Rename endpoint names where they currently reflect old host terminology if that is needed to align with the renamed host.
10. Rename user-facing strings and diagnostics that currently mention `StudioApiHost` so runtime naming remains consistent.
11. Move only the minimal API endpoint definitions currently held directly in the bootstrap/application file out into the `Api` namespace.
12. Treat that extraction as code relocation only: the existing endpoint definitions, route paths, and behavior should remain unchanged apart from the necessary move into dedicated API classes.
13. Split those extracted endpoints into separate API classes by concern rather than a single catch-all class.
14. Use `ProvidersApi`, `RulesApi`, and `DiagnosticsApi` as the extracted endpoint class names, moving `/providers`, `/rules`, and `/echo` respectively into those classes.
15. Leave the extracted endpoint implementation logic materially unchanged unless a rename directly requires a naming update.
16. Leave existing API classes such as `StudioIngestionApi` and `StudioOperationsApi` in place, unless a rename is directly required elsewhere.
17. Do not introduce compatibility aliases, shim types, or transitional references for the old host naming.

## 5. Non-functional requirements

1. The rename must preserve existing runtime behavior aside from intentional naming updates.
2. The bootstrap reshape must stay minimal and must not become a broader host refactor.
3. Naming must be internally consistent across projects, folders, namespaces, types, API classes, tests, diagnostics, and user-facing strings.
4. Existing route compatibility must be preserved by keeping URL paths unchanged.
5. Extracted endpoint code should remain materially unchanged so the work is clearly a move out of startup rather than a behavior or structure redesign.
6. The resulting structure should improve discoverability by reducing ambiguity between API host naming and broader Studio service responsibilities.
7. The endpoint extraction should improve separation of concerns without increasing conceptual complexity in startup.

## 6. Data model

No data-model change is expected from the rename itself.

## 7. Interfaces & integration

1. Project references, solution entries, and internal code references must be updated to the new production and test project names.
2. Namespaces and type references in dependent code must be updated to the new host naming.
3. Existing HTTP URL paths should continue to resolve exactly as before.
4. Endpoint names may be updated if they contain old host terminology, provided route compatibility is preserved.
5. The extracted bootstrap endpoints should keep the same route mappings and request/response behavior after relocation.
6. The new `ProvidersApi`, `RulesApi`, and `DiagnosticsApi` classes should become the canonical homes for the extracted endpoint definitions.

## 8. Observability (logging/metrics/tracing)

1. Log and diagnostic text that explicitly mentions `StudioApiHost` should be renamed to `StudioServiceHost` where it is part of the renamed host surface.
2. No broader observability redesign is required beyond host-name consistency.
3. The `/echo` response text should be updated to match the renamed host while preserving the endpoint itself.

## 9. Security & compliance

1. No security model change is intended as part of this rename.
2. The rename and endpoint extraction must preserve existing authorization, CORS, and startup validation behavior.

## 10. Testing strategy

1. Update the host test project, test namespaces, and test class names to the new `StudioServiceHost` terminology.
2. Verify the renamed production host and renamed test project still build successfully.
3. Run the renamed host test suite to confirm behavior remains unchanged after the rename and endpoint extraction.
4. Verify that unchanged URL paths continue to behave as before.
5. Verify that tests covering `/providers`, `/rules`, and `/echo` still pass after those endpoint definitions are relocated into `ProvidersApi`, `RulesApi`, and `DiagnosticsApi`.

## 11. Rollout / migration

1. Perform the rename as a clean cut with no compatibility layer.
2. Update project, folder, namespace, and type names consistently in one coordinated change set.
3. Move the inlined minimal API endpoint definitions into separate `Api` classes by concern as part of the same host rename work, without redesigning their logic.
4. Validate builds and tests after the rename so dependent references are confirmed to be updated consistently.
5. Remove the old `StudioApiHost` naming cleanly once the rename is complete, without leaving transitional aliases behind.

## 12. Open questions

None at present.

## 13. Clarified decisions

- The rename should be a full rename: project file names, folders, namespaces, root namespaces, main application type names, and test project names should all move from `StudioApiHost` to `StudioServiceHost`.
- The remaining minimal API endpoints currently inside the application bootstrap should be split into separate `Api` classes by concern.
- Only the minimal API endpoint definitions should be moved out of the bootstrap; the rest of the bootstrap structure should remain otherwise unchanged unless a rename directly requires an update.
- The renamed bootstrap/application file and type should be `StudioServiceHostApplication`.
- Host and test class names should be fully renamed to align with `StudioServiceHost` naming.
- Naming should be kept consistent across the renamed host, tests, and API classes rather than mixing `StudioApiHost` and `StudioServiceHost` terminology.
- Existing API classes such as `StudioIngestionApi` and `StudioOperationsApi` do not need renaming; only newly extracted API classes should be named to fit the renamed host structure.
- URL paths should remain unchanged for compatibility, but endpoint names may be updated where they currently reflect old host terminology.
- User-facing strings and diagnostics that mention `StudioApiHost` should also be renamed for consistency.
- The rename should be clean only, with no compatibility shims, aliases, or transitional references retained for the old host naming.
- Moving the remaining inlined minimal API endpoints out of the bootstrap should not change their code shape or behavior beyond the necessary relocation into dedicated API classes.
- The extracted bootstrap endpoints should be relocated into `ProvidersApi`, `RulesApi`, and `DiagnosticsApi` respectively, without changing their existing paths or behavior.
