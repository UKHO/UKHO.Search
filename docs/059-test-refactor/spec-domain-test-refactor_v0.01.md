# Specification: Test refactor and test asset consolidation

Target output path: `docs/059-test-refactor/spec-domain-test-refactor_v0.01.md`

Version: `v0.01`

## 1. Overview

### 1.1 Purpose

Refactor the test estate so that:

- shared test data is stored in one canonical location under `test/sample-data`
- obsolete duplicate test-data folders are removed
- each production project with tests has a corresponding dedicated test project
- tests are moved into the test project aligned to the project they verify
- solution membership is updated so the resulting structure is explicit and maintainable

### 1.2 Goals

- Consolidate `test/TestData` into `test/sample-data`.
- Remove `test/TestData` after all consumers are updated.
- Identify and fix tests currently using `test/TestData`, including `S57EnricherTests`, `S57BatchContentHandlerTests`, and `test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`.
- Clarify the intended role of `test/sample-data/s101-CATALOG.XML` and preserve or reposition it consistently.
- Ensure each project under test has its own dedicated test project.
- Remove incorrect cross-project coupling in `test/UKHO.Search.Ingestion.Tests`.
- Add any new test projects to `Search.slnx`.
- Leave the solution with a clear, repeatable naming convention for test projects under `./test`.
- Ensure no existing tests are lost during the refactor; every current test scenario must survive, either in its original form or via split/reworked tests that preserve the same functional coverage.

### 1.3 Non-goals

- Changing production behavior unrelated to test ownership or test-data pathing.
- Rewriting stable tests unless required to move them to the correct test project.
- Renaming production projects.
- Introducing a new test framework.
- Making broader changes to test style, assertion libraries, or coverage tooling.

### 1.4 Current-state evidence

Observed from the workspace:

- `test/sample-data` currently contains:
  - `s101-CATALOG.XML`
  - `s57-US5SC2AC.000`
- `test/TestData` currently contains:
  - `sample.000`
- `test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj` currently references:
  - `src/Providers/UKHO.Search.Ingestion.Providers.FileShare/UKHO.Search.Ingestion.Providers.FileShare.csproj`
  - `src/UKHO.Search.Ingestion/UKHO.Search.Ingestion.csproj`
  - `src/UKHO.Search.Infrastructure.Ingestion/UKHO.Search.Infrastructure.Ingestion.csproj`
  - `tools/FileShareEmulator/FileShareEmulator.csproj`
- `S57EnricherTests` and `S57BatchContentHandlerTests` currently search for fixtures under `test/TestData`.
- Existing test projects already include:
  - `test/FileShareEmulator.Tests/FileShareEmulator.Tests.csproj`
  - `test/FileShareEmulator.Common.Tests/FileShareEmulator.Common.Tests.csproj`
  - `test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`

### 1.5 Stakeholders

- Engineers maintaining ingestion, file-share provider, infrastructure, and emulator code.
- Contributors adding new tests and fixtures.
- CI maintainers who need predictable solution and test project structure.

### 1.6 Risks and considerations

- Moving tests between projects can break namespace assumptions, helper visibility, and fixture copy-to-output behavior.
- Duplicate or ambiguous sample files may hide whether tests depend on file content, filename, or both.
- Test project reorganization can accidentally weaken intended Onion Architecture boundaries if references are not kept aligned to the project under test.
- Existing `FileShareEmulator.Tests` may overlap with tests currently located elsewhere, so ownership boundaries must be explicit.

## 2. System / Component scope

### 2.1 In scope

- Test-data folder consolidation from `test/TestData` into `test/sample-data`.
- Removal of `test/TestData` after migration.
- Refactoring of tests and test project files affected by the data move.
- Creation of dedicated test projects where a production project currently lacks one.
- Moving tests to the test project corresponding to the production project under test.
- Updating `Search.slnx`.

### 2.2 Out of scope

- Production feature changes unrelated to enabling the test move.
- Reorganizing test projects that are already correctly aligned unless needed for consistency.
- Arbitrary renaming of existing sample fixtures unless required by an agreed naming convention.

### 2.3 High-level change summary

The solution will move from a partially centralized test project model to a project-aligned test model:

- `UKHO.Search.Ingestion.Tests` will retain only tests for `src/UKHO.Search.Ingestion`.
- Tests for `src/Providers/UKHO.Search.Ingestion.Providers.FileShare` will move to a dedicated `test/UKHO.Search.Ingestion.Providers.FileShare.Tests` project.
- Tests for `src/UKHO.Search.Infrastructure.Ingestion` will move to a dedicated `test/UKHO.Search.Infrastructure.Ingestion.Tests` project.
- Tests that truly belong to `tools/FileShareEmulator` will move to the existing `test/FileShareEmulator.Tests` project.
- Test fixtures will be resolved from `test/sample-data` only.

## 3. Functional requirements

### 3.1 Test data consolidation

- The solution SHALL use `test/sample-data` as the canonical shared fixture location for this work.
- Content currently under `test/TestData` SHALL be moved into `test/sample-data`.
- `test/TestData` SHALL be deleted after all references are removed.
- Tests and project files SHALL no longer reference `test/TestData`.
- The implementation SHALL preserve the fixture file actually used by the test code and update consumers consistently rather than replacing it speculatively with a different sample.
- `test/sample-data/s101-CATALOG.XML` SHALL remain in `test/sample-data` as shared canonical sample data.

### 3.2 Test ownership alignment

- Each production project that has tests SHALL have a dedicated test project whose name matches the production project name with a `.Tests` suffix.
- A matching test project SHALL contain tests only for its corresponding production project, unless a test is intentionally classified as cross-project integration coverage and moved to `UKHO.Search.IntegrationTests`.
- A test project SHALL reference the project it is testing and only the additional projects strictly required for valid test execution.
- `UKHO.Search.Ingestion.Tests` SHALL stop acting as a shared container for provider, infrastructure, and emulator tests.
- `UKHO.Search.Ingestion.Tests` SHALL be retained as the matching test project for `src/UKHO.Search.Ingestion/UKHO.Search.Ingestion.csproj`.
- Tests SHALL be relocated so every test ends up in the project that exactly matches the production project under test.
- The refactor MAY split or repeat tests where needed to preserve clear ownership and equivalent verification in the correct test projects.
- Tests that exercise ingestion pipeline behavior but involve provider-specific components SHALL move to the provider test project whenever a provider-specific component is part of the behavior under test.
- Tests that verify infrastructure behavior through ingestion-facing APIs MAY be split or repeated between ingestion and infrastructure test projects where needed to preserve clear ownership of both behaviors.
- Cross-project integration tests that intentionally verify behavior spanning multiple production projects SHALL live in a single outer-layer project named `UKHO.Search.IntegrationTests`.
- `UKHO.Search.IntegrationTests` SHOULD primarily contain genuinely cross-project integration tests, but borderline higher-level tests MAY also move there when that simplifies ownership and keeps project boundaries clearer.
- Existing test projects whose names already match a production project SHALL also be cleaned up so they contain only tests that belong to their matching project, unless a test is intentionally promoted to `UKHO.Search.IntegrationTests`.
- The test project that matches the production project named `FileShareEmulator.Common` SHALL remain a normal project-specific test project and SHALL NOT be treated as the shared helper project for the wider test estate.
- Exceptions to the one-production-project to one-matching-test-project rule MAY be allowed only when strictly necessary during implementation, and each exception SHALL be documented explicitly in this specification.

### 3.3 Solution structure

- The refactor SHALL audit the whole solution for production projects that already have tests but do not yet have a matching test project.
- Matching test projects SHALL be created wherever needed across the audited solution, not just for the currently evidenced cases.
- When the audit finds a production project with no existing tests at all, a matching test project SHALL still be created even if it starts empty.
- Empty matching test projects created by the audit SHALL include a clearly named placeholder smoke test class with an explanatory code comment so they can be identified and cleaned up later.
- The repository standard for this refactor SHALL remain `./test`.
- Any new test projects created for this work SHALL be placed under `./test`.
- New test projects SHALL be added to `Search.slnx`.
- Existing matching test projects, including `UKHO.Search.Ingestion.Tests`, SHALL remain in `Search.slnx`.
- Solution organization within `Search.slnx` is out of scope for this refactor beyond adding the required projects; manual solution grouping and folder organization will be handled separately.
- The solution SHALL include `test/UKHO.Search.IntegrationTests` as the designated outer-layer integration test project.

### 3.4 Verification expectations

- The refactor SHALL preserve equivalent test intent and coverage for the moved tests.
- The refactor SHALL NOT drop existing tests or silently reduce coverage; every existing test scenario SHALL remain represented after the refactor, even if implemented through split, duplicated, or otherwise reshaped tests.
- The refactor SHALL verify that fixture files required by migrated tests are copied or resolved correctly in their new project locations.
- The refactor SHALL explicitly address the role of `s101-CATALOG.XML` so its placement and consumption are intentional.
- Placeholder smoke tests created for otherwise empty matching test projects SHALL pass trivially and include a comment explaining that the project is a placeholder pending real tests.

## 4. Technical requirements

### 4.1 Naming and foldering

- New test projects SHALL follow the pattern `<ProductionProjectName>.Tests`.
- New test project directories SHALL be created directly under `test/`.
- Shared sample fixtures for this work SHALL live under `test/sample-data` unless a narrower provider-specific fixture location is agreed.
- Shared sample fixtures under `test/sample-data` SHALL remain in a single flat folder rather than being reorganized into subfolders as part of this refactor.

### 4.2 Reference hygiene

- Each test project SHALL directly reference its corresponding production project.
- Additional references SHALL be minimized and justified by the test scope.
- The resulting test project graph SHOULD avoid a single test project referencing multiple unrelated production projects.
- Test projects MAY reference other test projects where that reuse follows Onion Architecture direction, especially to share helpers, fixture utilities, and test support classes from outer-layer test projects.
- `UKHO.Search.IntegrationTests` MAY reference multiple production projects and test projects because it is the designated outermost integration layer for this refactor.
- Broad shared test helpers, fixture utilities, and common test support code SHALL live in a dedicated shared test assembly named `UKHO.Search.Tests.Common`.
- `UKHO.Search.Tests.Common` SHALL contain helper code only and SHALL NOT contain real tests.
- `UKHO.Search.Tests.Common` SHALL primarily contain broadly reusable helpers shared across multiple test projects, but it MAY also contain project-specific helpers when doing so is a deliberate convenience decision.
- `UKHO.Search.Tests.Common` SHALL NOT reference production projects; it SHALL remain pure test infrastructure.
- Test projects SHALL reference `UKHO.Search.Tests.Common` only when they actually need shared helpers from it.

### 4.3 Migration approach

- The implementation SHOULD inventory all test files in `test/UKHO.Search.Ingestion.Tests` and classify them by project under test before moving them.
- Helper classes and shared test support code MAY be reused across test projects by reference where that respects Onion Architecture direction; duplication is optional rather than required.
- Fixture lookup logic SHOULD be standardized on a shared helper for resolving files from `test/sample-data` so that future folder moves are low risk.
- The shared fixture-resolution helper SHOULD be implemented in `UKHO.Search.Tests.Common`.
- If a helper depends on production types, it SHALL remain in the owning test project rather than being moved into `UKHO.Search.Tests.Common`.
- If a test currently spans multiple project responsibilities, the implementation MAY split it into multiple tests or repeat equivalent assertions in the appropriate test projects rather than preserving an incorrect ownership boundary.

### 4.4 Acceptance indicators

- No remaining references to `test/TestData`.
- `test/TestData` removed.
- Fixture-dependent tests updated to resolve files from `test/sample-data`.
- No existing test scenario lost; the functional coverage represented by the pre-refactor tests remains present after the refactor.
- Dedicated test projects exist for projects that currently rely on `UKHO.Search.Ingestion.Tests` as an umbrella.
- `Search.slnx` includes the resulting test projects.
- Test responsibilities are separated by project under test.

## 5. Candidate project impacts (draft)

### 5.1 Likely retained

- `test/UKHO.Search.Ingestion.Tests`
- `test/FileShareEmulator.Tests`
- `test/FileShareEmulator.Common.Tests`

### 5.2 Likely new

- `test/UKHO.Search.Ingestion.Providers.FileShare.Tests`
- `test/UKHO.Search.Infrastructure.Ingestion.Tests`
- `test/UKHO.Search.IntegrationTests`
- `test/UKHO.Search.Tests.Common`

### 5.3 Likely moved test areas from `UKHO.Search.Ingestion.Tests`

- File-share provider enrichment/handler tests.
- File-share provider pipeline graph tests.
- Infrastructure-ingestion-specific tests.
- Emulator-specific tests, if any remain in this project.

## 6. Open questions and decisions log

### Q1 (answered)

A boundary decision is still needed for emulator-related tests:

- Decision: Reuse the existing `test/FileShareEmulator.Tests` project for all tests that verify `tools/FileShareEmulator`.

Status: Confirmed.

### Q6 (answered)

For cross-project integration tests:

- Create one project named `UKHO.Search.IntegrationTests`.
- Treat it as the outer layer of the Onion for test purposes.
- Allow it to reference domain, infrastructure, and test projects as needed.
- Put the integration tests in that project.

Status: Confirmed.

### Q7 (answered)

For the scope of `UKHO.Search.IntegrationTests`:

- Start with genuinely cross-project integration tests.
- Borderline higher-level tests may also move there if that simplifies ownership.

Status: Confirmed.

### Q8 (answered)

For `UKHO.Search.Ingestion.Tests` after the refactor:

- Retain it as the matching test project for `UKHO.Search.Ingestion.csproj`.
- Remove from it only the tests that belong to other production projects.
- Keep the ingestion and pipeline-focused tests there.

Status: Confirmed.

### Q9 (answered)

For creation of new project-specific test projects:

- Audit the whole solution.
- Create matching test projects wherever needed.

Status: Confirmed.

### Q10 (answered)

For the `./test` versus `./tests` folder name:

- Standardize on the existing `./test` folder.

Status: Confirmed.

### Q11 (answered)

When the whole-solution audit finds a production project with no existing tests at all:

- Create the matching test project anyway, even if it starts empty.
- Mark the placeholder using a clearly named smoke test class and an explanatory code comment.
- Make the placeholder smoke test pass trivially with a comment explaining that the project is a placeholder pending real tests.

Status: Confirmed.

### Q12 (answered)

For existing matching test projects that currently contain misowned tests:

- Clean them up as part of this refactor.
- Ensure each matching test project contains only tests for its own production project, unless a test is intentionally moved to `UKHO.Search.IntegrationTests`.

Status: Confirmed.

### Q13 (answered)

For the `Search.slnx` update:

- Add all new test projects.
- Do not remove existing matching test projects such as `UKHO.Search.Ingestion.Tests`.

Status: Confirmed.

### Q14 (answered)

For tests that exercise ingestion pipeline behavior but use provider-specific components:

- Move them to the provider test project whenever a provider-specific component is involved in the behavior under test.

Status: Confirmed.

### Q15 (answered)

For tests that verify infrastructure behavior through ingestion-facing APIs:

- Split or repeat them between ingestion and infrastructure test projects as needed.

Status: Confirmed.

### Q16 (answered)

For organization of shared sample data under `test/sample-data` after the move:

- Keep the shared samples flat in one folder.

Status: Confirmed.

### Q17 (answered)

For tests that currently depend on fixture path discovery logic:

- Standardize on a shared helper for resolving files from `test/sample-data`.

Status: Confirmed.

### Q18 (answered)

For the scope of the shared fixture helper:

- Broad shared helpers shall live in a dedicated common assembly named `UKHO.Search.Tests.Common`.
- This includes the shared fixture-resolution helper.

Status: Confirmed.

### Q19 (answered)

For the contents of `UKHO.Search.Tests`:

- It shall contain the tests that pertain to `UKHO.Search.csproj`.
- The refactor shall follow the general rule that `ProjectA.csproj` implies `ProjectA.Tests.csproj` for that project's tests.

Status: Confirmed.

### Q20 (answered)

For the contents of `UKHO.Search.Tests.Common`:

- It shall contain helper code only.
- It shall not contain tests.

Status: Confirmed.

### Q21 (answered)

For the scope of helpers allowed in `UKHO.Search.Tests.Common`:

- It shall primarily contain broadly reusable helpers shared across multiple test projects.
- It may also contain project-specific helpers for convenience.

Status: Confirmed.

### Q22 (answered)

For the new shared test helper project:

- It may contain helpers reused by multiple test projects.
- It may also contain project-specific helpers for convenience.

Status: Confirmed.

### Q23 (answered)

For the existing test project that matches the production project named `FileShareEmulator.Common`:

- Keep it as a normal project-specific test project.
- Use the new shared helper project only for reusable helper code shared more broadly.

Status: Confirmed.

### Q24 (answered)

For the new shared helper project:

- It shall not reference production projects.
- It shall remain pure test infrastructure.

Status: Confirmed.

### Q25 (answered)

If a helper needs production types:

- Keep that helper in the owning test project.
- Do not move it into the shared helper project.

Status: Confirmed.

### Q26 (answered)

For references to the new shared helper project:

- Reference it only when needed.

Status: Confirmed.

### Q27 (answered)

For the solution file update:

- Only add the required projects.
- Leave solution grouping and folder organization for manual follow-up.

Status: Confirmed.

### Q28 (answered)

For exceptions to the general one-production-project to one-matching-test-project rule:

- Allow exceptions only when strictly necessary during implementation.
- Document each exception explicitly in this specification.

Status: Confirmed.

### Q4 (answered)

For strictness of the move out of `UKHO.Search.Ingestion.Tests`:

- Move tests so each one aligns exactly to the production project under test.
- Split and/or repeat tests if needed to preserve correct ownership.

Status: Confirmed.

### Q5 (answered)

For helpers, fixture utilities, and test support classes shared by multiple test projects:

- Test projects may reference other test projects.
- Reuse is acceptable where it follows Onion Architecture direction in the outer layers.
- Shared helpers, fixture utilities, and test support classes do not have to be duplicated if reuse keeps the ownership and dependency direction acceptable.

Status: Confirmed.

### Q3 (answered)

For `test/sample-data/s101-CATALOG.XML`:

- Keep it in `test/sample-data` regardless.
- Treat it as shared canonical sample data.

Status: Confirmed.

### Q2 (answered)

For the S-57 sample fixture currently in `test/TestData/sample.000`, the migration rule is:

- Preserve the fixture actually used by the current test code.
- Move it into `test/sample-data` and update the tests to use that canonical location.
- Do not replace it speculatively with a different S-57 sample unless the implementation evidence shows they are the same fixture and the tests already rely on that identity.

Status: Confirmed.
