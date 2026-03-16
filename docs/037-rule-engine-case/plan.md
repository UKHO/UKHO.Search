# Implementation Plan

Target output path: `docs/037-rule-engine-case/plan.md`

Based on: `docs/037-rule-engine-case/spec.md`

## Case-insensitive rules (via canonical lowercase properties) + IngestionPropertyList encapsulation

- [x] Work Item 1: Introduce `IngestionPropertyList` and migrate ingestion request construction end-to-end - Completed
  - **Purpose**: Provide a single, safe API for adding ingestion properties that enforces case-insensitive uniqueness and enables canonical lowercase property names across the ingestion pipeline.
  - **Acceptance Criteria**:
    - `IngestionPropertyList.Add(...)` rejects duplicates whose names differ only by case.
    - `IndexRequest.Properties` is `IngestionPropertyList` while remaining wire-format compatible (`"Properties"` JSON array).
    - `CanonicalDocument` JSON shape remains unchanged (including `Source.Properties`).
    - File-share emulator request construction uses `IngestionPropertyList`.
  - **Definition of Done**:
    - Code implemented and compiled.
    - Unit tests added/updated and passing.
    - Backward-compatible serialization verified by tests.
    - Can execute end-to-end via: run `IngestionServiceHost` and evaluate a known batch in `RulesWorkbench`.
  - **Completed summary**:
    - Added `IngestionPropertyList` which enforces case-insensitive uniqueness on addition.
    - Updated `IndexRequest.Properties` to be `IngestionPropertyList` while keeping JSON `Properties` as an array via a dedicated JSON converter registered in `IngestionJsonSerializerOptions`.
    - Updated RulesWorkbench mapping/copying to use `IngestionPropertyList`.
    - Updated FileShare emulator request construction to use `IngestionPropertyList`.
    - Preserved `CanonicalDocument` serialization shape; adjusted `CanonicalDocument` round-trip test to use ingestion serializer options.
    - Added unit tests for `IngestionPropertyList` duplicate behavior.

  - [x] Task 1.1: Add `IngestionPropertyList` type - Completed
    - [x] Step 1: Create `src/UKHO.Search.Ingestion/Requests/IngestionPropertyList.cs` (single public type; Allman braces) - Completed
    - [x] Step 2: Implement:
      - internal storage list
      - name index with `StringComparer.OrdinalIgnoreCase`
      - `Add(IngestionProperty)` enforcing non-empty name and case-insensitive uniqueness
      - `IReadOnlyList<IngestionProperty>` (indexer + enumerator)
      - Optional convenience: `TryGetValue(string name, out object? value)`
    - [x] Step 3: Ensure behavior matches current validation message intent (names are case-insensitive) - Completed
  - [x] Task 1.2: Update `IndexRequest` to use `IngestionPropertyList` - Completed
    - [x] Step 1: Change constructor signature from `IReadOnlyList<IngestionProperty>` to `IngestionPropertyList` - Completed
    - [x] Step 2: Change `Properties` property type to `IngestionPropertyList` - Completed
    - [x] Step 3: Ensure JSON serialization stays as an array under `"Properties"`:
      - Add/adjust JSON attributes or converters if needed
      - Ensure deserialization constructs `IngestionPropertyList` and enforces uniqueness
    - [x] Step 4: Remove/adjust duplicate-name validation logic in `IndexRequest.Validate()` so it relies on `IngestionPropertyList` - Completed
  - [x] Task 1.3: Update `CanonicalDocument` usage - Completed
    - [x] Step 1: Confirm no serialization shape changes for `CanonicalDocument` (Source.Properties still array) - Completed
    - [x] Step 2: Update any code paths that build `CanonicalDocument.Source` or inspect `IndexRequest.Properties` to use the new type - Completed
  - [x] Task 1.4: Update emulator and any producers building properties - Completed
    - [x] Step 1: Update `tools/FileShareEmulator/Services/IndexService.cs` to build `IngestionPropertyList` instead of `List<IngestionProperty>` - Completed
    - [x] Step 2: Update any factories (e.g., `tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs`) signatures/usages accordingly - Completed
  - **Files**:
    - `src/UKHO.Search.Ingestion/Requests/IngestionPropertyList.cs`: new collection type enforcing uniqueness
    - `src/UKHO.Search.Ingestion/Requests/IndexRequest.cs`: migrate to `IngestionPropertyList` and preserve JSON contract
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: ensure compatibility with new `IndexRequest.Properties` type
    - `tools/FileShareEmulator/Services/IndexService.cs`: construct `IngestionPropertyList`
    - `tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs`: accept/pass `IngestionPropertyList`
  - **Work Item Dependencies**: none
  - **Run / Verification Instructions**:
    - Build: `dotnet build`
    - Run host: start `IngestionServiceHost`
    - Run workbench: start `RulesWorkbench` and load a known batch id from `docs/known-batch-ids.txt`
  - **User Instructions**:
    - None

- [x] Work Item 2: Enforce canonical lowercase property names for ingestion `properties[...]` lookups - Completed
  - **Purpose**: Make property name resolution deterministic by constraining ingestion property names to lowercase and requiring rules engine path resolver to look up the lowercase key only.
  - **Acceptance Criteria**:
    - When evaluating `properties["X"]`, the rules engine resolves using `x.ToLowerInvariant()` and expects the canonical lowercase key.
    - If the canonical lowercase key does not exist, treat as missing (no match / no output).
    - All existing rules still run (after rules updated to use lowercase property names where required).
  - **Definition of Done**:
    - Rule evaluation changes implemented in shared library.
    - Unit tests cover the lowercase normalization behavior for property lookup.
    - RulesWorkbench and IngestionServiceHost behave identically.
  - **Completed summary**:
    - Canonicalized ingestion property names to lowercase in `IngestionPropertyList.Add`.
    - Updated `IngestionRulesPathResolver.ResolvePropertiesLookup` to normalize requested keys to lowercase and require exact match against canonical names.
    - Updated/added unit tests to reflect lowercase canonicalization behavior.

  - [x] Task 2.1: Update path resolution for `propertiesLookup` - Completed
    - [x] Step 1: Update `src/UKHO.Search.Infrastructure.Ingestion/Rules/Evaluation/IngestionRulesPathResolver.cs` `ResolvePropertiesLookup(...)` to:
      - normalize the lookup key via `ToLowerInvariant()`
      - match against canonical lowercase property names
    - [x] Step 2: Ensure any other property lookup code path uses the same normalization - Completed
  - [x] Task 2.2: Ensure ingestion property producers normalize names to lowercase - Completed
    - [x] Step 1: Decide normalization location (recommended: `IngestionPropertyList.Add` normalizes `Name` to lower-invariant) - Completed
    - [x] Step 2: Update tests accordingly - Completed
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Evaluation/IngestionRulesPathResolver.cs`: lowercase-only properties lookup
    - `src/UKHO.Search.Ingestion/Requests/IngestionPropertyList.cs`: normalize `Name` to lowercase at addition (if selected)
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - `dotnet test`
    - Run `RulesWorkbench`, load a batch; verify lookups for `Year`/`Week Number` succeed when rule uses lowercase keys and payload properties are normalized

- [x] Work Item 3: Make predicate string operator evaluation case-insensitive - Completed
  - **Purpose**: Ensure predicate operations against payload text behave consistently regardless of casing.
  - **Acceptance Criteria**:
    - `eq`, `contains`, `startsWith`, `endsWith`, `in` compare using `StringComparison.OrdinalIgnoreCase`.
    - Comprehensive unit test matrix for rule value casing and payload casing.
  - **Definition of Done**:
    - Operator evaluation updated.
    - Unit tests added for each operator with upper/lower/mixed combinations.
    - No regressions in existing tests.
  - **Completed summary**:
    - Confirmed operator evaluation already normalizes strings to lowercase and trims, providing case-insensitive behavior for `eq`, `contains`, `startsWith`, `endsWith`, and `in`.
    - Added comprehensive casing-matrix unit tests covering upper/lower/mixed-case combinations for each operator.

  - [x] Task 3.1: Locate and update predicate operator evaluation - Completed
    - [x] Step 1: Identify the code that evaluates leaf predicates and string operators in `src/UKHO.Search.Infrastructure.Ingestion/Rules/**` - Completed
    - [x] Step 2: Update string comparisons to ordinal ignore-case - Completed (no code change required due to existing normalization)
  - [x] Task 3.2: Add operator-specific unit test matrix - Completed
    - [x] Step 1: Add parameterized tests for each operator and casing combinations - Completed
    - [x] Step 2: Include `in` list casing coverage - Completed
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Evaluation/*`: operator comparers (exact files determined during implementation)
    - `test/UKHO.Search.Ingestion.Tests/Rules/*`: new/updated tests
  - **Work Item Dependencies**: Work Item 2 (recommended)
  - **Run / Verification Instructions**:
    - `dotnet test`

 [x] Work Item 4: Ensure template `$path:` resolution + `toInt(...)` behavior is consistent and tested - Completed
  - **Purpose**: Ensure output action templates resolve canonical lowercase property names and that evaluation is consistent in host and workbench.
  - **Acceptance Criteria**:
    - `$path:properties["..."]` resolves using lowercase-only lookup.
    - `toInt(...)` continues to parse invariant-culture with trimming.
    - Tests cover casing combinations for template keys and numeric parsing.
  - **Definition of Done**:
    - Template expansion updated if needed.
    - Unit tests added covering Year/Week Number paths and numeric parsing.
    - Workbench regression tests pass.
  - **Completed summary**:
    - Confirmed `$path:` template expansion delegates to `TemplateContext.PathResolver` (therefore uses the lowercase-normalized `properties[...]` lookup implemented in Work Item 2).
    - Added unit tests covering mixed-case `$path:properties["..."]` key resolution against canonically-lowercased ingestion properties.
    - Verified `toInt(...)` continues to trim input and parse using invariant culture; added/updated tests to cover numeric parsing and variable resolution ordering.
    - Updated existing template expander test payload construction to use `IngestionPropertyList`.

  - [x] Task 4.1: Audit template resolver implementation - Completed
    - [x] Step 1: Identify where `$path:` is expanded and how it uses the path resolver - Completed
    - [x] Step 2: Ensure it relies on updated lowercase-only resolver - Completed
  - [x] Task 4.2: Add end-to-end style tests for outputs - Completed
    - [x] Step 1: New tests asserting `majorVersion` and `minorVersion` outputs set when payload uses e.g. `Year=2026` and `Week Number=10` but stored canonically as lowercase - Completed
    - [x] Step 2: Include negative tests for missing lowercase key - Completed
  - [ ] Task 4.1: Audit template resolver implementation
    - [ ] Step 1: Identify where `$path:` is expanded and how it uses the path resolver
    - [ ] Step 2: Ensure it relies on updated lowercase-only resolver
  - [ ] Task 4.2: Add end-to-end style tests for outputs
    - [ ] Step 1: New tests asserting `majorVersion` and `minorVersion` outputs set when payload uses e.g. `Year=2026` and `Week Number=10` but stored canonically as lowercase
    - [ ] Step 2: Include negative tests for missing lowercase key
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Evaluation/*`: template handling (as identified)
    - `test/UKHO.Search.Ingestion.Tests/Rules/RulesEngineEndToEndExampleTests.cs`: extend or add new tests
    - `test/RulesWorkbench.Tests/*`: add regression tests if workbench has specific loader/evaluator surface
  - **Work Item Dependencies**: Work Item 3
  - **Run / Verification Instructions**:
    - `dotnet test`
    - Run `RulesWorkbench` and validate outputs for known batches

---

## Summary
This plan delivers the change in vertical, runnable slices:

1) introduce `IngestionPropertyList` (uniqueness + serialization compatibility),
2) enforce canonical lowercase property names and update property lookup to lowercase-only,
3) make predicate string operators case-insensitive,
4) ensure `$path:`/`toInt(...)` outputs behave consistently with comprehensive tests.

Key considerations:
- Maintain JSON compatibility for `IndexRequest` and `CanonicalDocument`.
- Keep host and workbench behavior identical by using shared engine code.
- Add a full casing test matrix across lookups, predicates, and template outputs.
