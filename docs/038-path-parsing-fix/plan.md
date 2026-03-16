# Implementation Plan

**Target path:** `docs/037-path-parsing-fix/plan.md`

## Project structure / touchpoints
This change is scoped to the ingestion rules templating subsystem.

Primary code locations:
- `src/UKHO.Search.Infrastructure.Ingestion/Rules/Templating/IngestionRulesTemplateExpander.cs`: update `$path:` argument scanning in `TryFindVariable()`.

Primary test locations (to confirm exact suite to extend during implementation):
- `test/UKHO.Search.Ingestion.Tests/Rules/...` (existing tests for rules/templating/path evaluation)

Naming / conventions:
- Keep changes minimal and localized.
- Maintain existing public API surface of `IngestionRulesTemplateExpander`.

## Feature Slice: `$path:` variable parsing supports spaces in `properties["..."]`

- [x] Work Item 1: Add regression tests that reproduce the defect and validate space-containing property keys - Completed
  - **Purpose**: Lock in expected behavior for `$path:` argument parsing with bracketed keys containing spaces, and prevent future regressions.
  - **Acceptance Criteria**:
    - A test fails on current code when using `$path:properties["week number"]`.
    - Tests demonstrate correct behavior for the required scenarios once the fix is applied.
    - Tests cover edge cases listed in the spec (not only happy path).
  - **Definition of Done**:
    - New/updated unit tests added to the existing test project.
    - Tests cover both string expansion (`Expand`) and int expansion (`ExpandToInt` / `toInt(...)`).
    - Tests include malformed/unbalanced inputs confirming non-throwing behavior.
    - All tests pass.
    - Can execute end-to-end via: `dotnet test` (or running tests in VS Test Explorer).
  - [x] Task 1.1: Identify existing test suite for templating - Completed
    - [x] Step 1: Located template expansion coverage in `test/UKHO.Search.Ingestion.Tests/Rules/TemplateExpanderTests.cs` and `test/UKHO.Search.Ingestion.Tests/Rules/TemplateExpanderPathLowercaseTests.cs`.
    - [x] Step 2: Reused the existing test class rather than introducing a new suite.
  - [x] Task 1.2: Add thorough tests for spaced property keys - Completed
    - [x] Step 1: Added tests using a minimal `TemplateContext` built around an `IndexRequest` payload with `properties` keys containing spaces.
    - [x] Step 2: Added tests covering spaced keys, multiple variables, whitespace separation, prefix/suffix adjacency, and malformed/unbalanced inputs.
    - [x] Step 3: Verified the tests fail on current implementation due to `$path:` argument termination at whitespace (demonstrates the defect).
  - **Files**:
    - `test/UKHO.Search.Ingestion.Tests/Rules/.../*.cs`: Add/extend tests for `IngestionRulesTemplateExpander` `$path:` parsing and `toInt(...)` behavior.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - Visual Studio Test Explorer: run the new/updated tests.
    - CLI (optional): `dotnet test`.
  - **User Instructions**: None.

  - **Summary (Work Item 1)**:
    - Added thorough regression tests in `test/UKHO.Search.Ingestion.Tests/Rules/TemplateExpanderPathLowercaseTests.cs` covering `$path:properties["..."]` where the key contains spaces and related edge cases.
    - Confirmed current behavior fails these tests, reproducing the reported issue and protecting the intended fix (Work Item 2).

- [x] Work Item 2: Implement robust `$path:` argument parsing with bracket/quote awareness - Completed
  - **Purpose**: Fix runtime rule evaluation so `$path:` can reference `properties["..."]` keys containing spaces, aligning templating with the path resolver’s capabilities.
  - **Acceptance Criteria**:
    - `IngestionRulesTemplateExpander.TryFindVariable()` extracts full `$path:` argument for inputs like `$path:properties["week number"]`.
    - Whitespace does not terminate `$path:` argument when inside bracketed lookup and/or quoted key.
    - `$` remains a hard terminator for variable parsing.
    - Malformed/unbalanced input does not throw; behavior remains “no expansions found” for the affected variable.
    - All tests from Work Item 1 pass.
  - **Definition of Done**:
    - Code updated in `IngestionRulesTemplateExpander` only (unless tests reveal a necessary adjacent change).
    - Implementation remains O(n) for scanning.
    - All unit tests pass.
    - Can execute end-to-end via: `dotnet test`.
  - [x] Task 2.1: Update `$path:` argument scanning - Completed
    - [x] Step 1: Updated `$path:` argument parsing in `IngestionRulesTemplateExpander.TryFindVariable()` to track `bracketDepth` and `inQuotes` so whitespace inside `properties["..."]` does not terminate the argument.
    - [x] Step 2: Updated termination rules to end the `$path:` argument on `$`, and on whitespace / common delimiters (e.g., `-`, `,`, `;`, `)`) when outside bracket/quote context, enabling multiple variable expansions and prefix/suffix adjacency.
    - [x] Step 3: Preserved existing semantics for `$val` and unknown variables.
  - [x] Task 2.2: Validate behavior against edge cases - Completed
    - [x] Step 1: Confirmed multiple variables in one template work (unit tests).
    - [x] Step 2: Confirmed prefix/suffix adjacency works (unit tests).
    - [x] Step 3: Confirmed malformed/unbalanced input does not throw and yields empty output (unit tests).
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Templating/IngestionRulesTemplateExpander.cs`: Update `$path:` parsing loop.
  - **Work Item Dependencies**:
    - Depends on Work Item 1 (tests define expected behavior).
  - **Run / Verification Instructions**:
    - Visual Studio Test Explorer: run templating and rules-related tests.
    - CLI (optional): `dotnet test`.
  - **User Instructions**:
    - None.

  - **Summary (Work Item 2)**:
    - Implemented bracket/quote-aware `$path:` argument scanning in `src/UKHO.Search.Infrastructure.Ingestion/Rules/Templating/IngestionRulesTemplateExpander.cs`.
    - All `UKHO.Search.Ingestion.Tests` tests now pass, including the new spaced-key regression suite.

- [x] Work Item 3: Add a rules-level regression test for the real-world scenario (optional but recommended) - Completed
  - **Purpose**: Validate the fix through the rule evaluation pipeline (template expansion → path resolution → action application) using an example mirroring the AVCS BESS rule case.
  - **Acceptance Criteria**:
    - Given an ingestion payload with `properties["week number"] = "10"` and a rule with `minorVersion.add = ["toInt($path:properties[\"week number\"])" ]`, the resulting `CanonicalDocument.MinorVersion` contains `10`.
  - **Definition of Done**:
    - A test exists at a higher level than `IngestionRulesTemplateExpander` (e.g., action applier / rules engine test) demonstrating the end-to-end mapping.
    - All tests pass.
  - [x] Task 3.1: Choose the most appropriate existing regression test harness - Completed
    - [x] Step 1: Reused the existing rules engine integration test harness based on `TempRulesRoot` in `RulesEngineSlice4ActionsIntegrationTests`.
    - [x] Step 2: Added a dedicated test that writes a minimal rule containing `minorVersion.add = ["toInt($path:properties[\"week number\"])" ]` and asserts `CanonicalDocument.MinorVersion` contains `10`.
  - **Files**:
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/*` or `test/UKHO.Search.Ingestion.Tests/Rules/*`: Add a pipeline/rules regression test.
  - **Work Item Dependencies**:
    - Depends on Work Item 2.
  - **Run / Verification Instructions**:
    - Visual Studio Test Explorer: run regression tests.
    - CLI (optional): `dotnet test`.
  - **User Instructions**: None.

  - **Summary (Work Item 3)**:
    - Added a rules-level regression test to validate the end-to-end pipeline (rules engine → template expansion → path resolution → action application) for `toInt($path:properties["week number"])`.

## Summary / key considerations
- The defect is caused by `$path:` parsing terminating at whitespace; the path resolver already supports spaced keys.
- The plan prioritizes tests first (including edge cases) to prevent partial/fragile fixes.
- Implementation should be localized to `IngestionRulesTemplateExpander.TryFindVariable()` with a small, stateful scan (bracket/quote-aware) and no changes to rule JSON or input normalization.
- Ensure non-throwing behavior is preserved for malformed templates, consistent with runtime “skip derived outputs” guidance.
