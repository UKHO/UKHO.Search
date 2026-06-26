# Implementation Plan

Work Package folder: `dev/work-packages/mvp/027-parser-refactor/`

## Goal
Refactor FileShare ingestion “parsers” that enrich a `CanonicalDocument` so their naming matches behavior, and unify S-57 enrichment to directly mutate a `CanonicalDocument` (same pattern as S-101).

---

## Slice 1: S-100 rename + runnable compilation path
- [x] Work Item 1: Rename S-100 “parser” abstraction and implementation to “enricher” - Completed
  - **Purpose**: Align naming with behavior (document enrichment) while keeping existing S-101 enrichment flow runnable.
  - **Acceptance Criteria**:
    - `IS100Parser` is renamed to `IS100Enricher`.
    - `S101Parser` is renamed to `S101Enricher`.
    - All references across production + tests compile.
  - **Summary**:
    - Renamed `IS100Parser` -> `IS100Enricher` (including file rename).
    - Renamed `S101Parser` -> `S101Enricher` and updated `ILogger<T>` usage.
    - Updated `S100BatchContentHandler` to use `S101Enricher` with `LoggerAdapter<S101Enricher>`.
    - Verified with `dotnet build` and `dotnet test` (UKHO.Search.Ingestion.Tests).
  - **Definition of Done**:
    - Code compiles
    - Tests compile and pass
    - Logging preserved
    - Documentation updated (this plan)
    - Can execute end-to-end via: `dotnet build` and `dotnet test`
  - [x] Task 1: Rename interface and update references - Completed
    - [x] Step 1: Rename file/type `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/IS100Parser.cs` → `IS100Enricher.cs`.
    - [x] Step 2: Update namespace/type references at call sites (handlers, wiring).
    - [x] Step 3: Ensure one public type per file; keep Allman braces + block-scoped namespace.
  - [x] Task 2: Rename implementation and update references - Completed
    - [x] Step 1: Rename file/type `S101Parser` → `S101Enricher`.
    - [x] Step 2: Update any `ILogger<S101Parser>` to `ILogger<S101Enricher>`.
  - [x] Task 3: Update DI wiring and unit tests - Completed
    - [x] Step 1: Update `src/UKHO.Search.Ingestion.Providers.FileShare/Injection/InjectionExtensions.cs` registrations. (N/A in current implementation; no DI usage for these types.)
    - [x] Step 2: Update `test/UKHO.Search.Ingestion.Tests` references (type names, filenames if required).
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/IS100Parser.cs`: rename to `IS100Enricher.cs`, update type name.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/S101Parser.cs`: rename to `S101Enricher.cs`, update type name.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Injection/InjectionExtensions.cs`: update registrations.
    - `test/UKHO.Search.Ingestion.Tests/**`: update references.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test`
  - **User Instructions**: None.

---

## Slice 2: S-57 contract change (enrich document directly) + runnable end-to-end handler path
- [x] Work Item 2: Rename S-57 abstraction + implementation and change contract to enrich `CanonicalDocument` - Completed
  - **Purpose**: Unify enrichment pattern; callers provide a `CanonicalDocument` and S-57 implementation adds search text + geo polygon internally.
  - **Acceptance Criteria**:
    - `IS57Parser` renamed to `IS57Enricher`.
    - `BasicS57Parser` renamed to `BasicS57Enricher`.
    - S-57 API updated:
      - Old: `TryParse(string pathTo000, out string coveragePolygonWkt, out IReadOnlyList<string> textValues)`
      - New: `TryParse(string pathTo000, CanonicalDocument document)`
    - Enrichment (search text + polygon) happens inside `BasicS57Enricher.TryParse(...)`.
    - Call sites (e.g., `S57BatchContentHandler`) no longer parse WKT or add search text.
  - **Summary**:
    - Renamed `IS57Parser` -> `IS57Enricher` and updated signature to `TryParse(pathTo000, CanonicalDocument document)`.
    - Renamed `BasicS57Parser` -> `BasicS57Enricher` (including file rename) and moved document enrichment (search text + geo polygon) into `TryParse`.
    - Updated `S57BatchContentHandler` to call the new enricher API and removed caller-side WKT/text plumbing.
    - Updated `BasicS57ParserTests` to assert document enrichment via the new contract.
    - Verified with `dotnet build` and `dotnet test` (UKHO.Search.Ingestion.Tests).
  - **Definition of Done**:
    - Code compiles
    - Unit tests updated and passing
    - Logging preserved via `ILogger<T>`
    - Can execute end-to-end via: `dotnet test` (including tests that exercise `S57BatchContentHandler` if present)
  - [x] Task 1: Rename interface and change signature - Completed
    - [x] Step 1: Rename file/type `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/IS57Parser.cs` → `IS57Enricher.cs`.
    - [x] Step 2: Update method signature to `bool TryParse(string pathTo000, CanonicalDocument document);`.
    - [x] Step 3: Update all references/implementations.
  - [x] Task 2: Rename implementation and move enrichment logic into `BasicS57Enricher` - Completed
    - [x] Step 1: Rename file/type `BasicS57Parser` → `BasicS57Enricher`.
    - [x] Step 2: Move logic that currently returns `coveragePolygonWkt` + `textValues` to internal enrichment:
      - [x] Parse dataset from `pathTo000`.
      - [x] Add extracted text values to `document` via `document.AddSearchText(...)`.
      - [x] Compute envelope → WKT and parse polygon internally (using `GeoPolygonWktReader` internally to preserve behavior).
      - [x] Add polygon to document via `document.AddGeoPolygon(...)`.
    - [x] Step 3: Preserve exception handling/logging behavior as much as feasible.
  - [x] Task 3: Update `S57BatchContentHandler` (and any other call sites) - Completed
    - [x] Step 1: Update instantiation to `BasicS57Enricher` (or `IS57Enricher` if/when introduced; no DI changes required).
    - [x] Step 2: Replace old `TryParse(..., out ...)` call with `TryParse(pathTo000, document)`.
    - [x] Step 3: Remove now-redundant WKT parsing + text application code.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/IS57Parser.cs`: rename to `IS57Enricher.cs`, update signature.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/BasicS57Parser.cs`: rename to `BasicS57Enricher.cs`, move enrichment into implementation.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/S57BatchContentHandler.cs`: update call site, remove WKT/text plumbing.
  - **Work Item Dependencies**: Work Item 1 should be completed first to reduce rename conflicts and keep build green.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test`
  - **User Instructions**: None.

---

## Slice 3: Tests rename + behavioral parity lock-in
- [x] Work Item 3: Rename and update tests to match new contracts and verify same outcomes - Completed
  - **Purpose**: Preserve current behavior while changing contracts/naming; ensure fixtures still produce identical search text and geo results.
  - **Acceptance Criteria**:
    - Test class/file renames applied:
      - `BasicS57ParserTests` → `S57ParserTests`
      - Any `S101Parser` test mention → `S101Enricher`
    - Tests assert:
      - S-57 enrichment adds the same set of extracted text values as before
      - Geo polygon matches existing fixture expectations
    - All tests pass.
  - **Summary**:
    - Renamed `BasicS57ParserTests` -> `S57ParserTests` (file + class name).
    - Removed placeholder legacy files left behind by prior renames (`S101Parser.cs`, `IS57Parser.cs`, `BasicS57Parser.cs`, `IS100Parser.cs`).
    - Verified with `dotnet build` and `dotnet test` (UKHO.Search.Ingestion.Tests).
  - **Definition of Done**:
    - All tests passing locally
    - No leftover references to old names
    - Can execute end-to-end via: `dotnet test`
  - [x] Task 1: Update S-57 unit tests for new signature - Completed
    - [x] Step 1: Rename `test/UKHO.Search.Ingestion.Tests/Enrichment/BasicS57ParserTests.cs` → `S57ParserTests.cs`.
    - [x] Step 2: Update test class name + constructors.
    - [x] Step 3: Replace assertions on `out coveragePolygonWkt` and `out textValues` with assertions directly against the enriched `CanonicalDocument`.
  - [x] Task 2: Update S-101 unit tests - Completed
    - [x] Step 1: Rename types and filenames that reference `S101Parser`.
    - [x] Step 2: Ensure DI-based tests still resolve and run.
  - [x] Task 3: Add/adjust integration coverage (if present) - Completed
    - [x] Step 1: Search for any handler-level tests (e.g., `S57BatchContentHandler` behavior) and update for new flow.
  - **Files**:
    - `test/UKHO.Search.Ingestion.Tests/Enrichment/BasicS57ParserTests.cs`: rename to `S57ParserTests.cs`, update assertions.
    - `test/UKHO.Search.Ingestion.Tests/**`: update references to new type names.
  - **Work Item Dependencies**: Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet test`

---

## Slice 4: Cleanup + reference sweep
- [x] Work Item 4: Ensure repository-wide consistency and remove legacy names - Completed
  - **Purpose**: Reduce tech debt and prevent future refactors by removing lingering references.
  - **Acceptance Criteria**:
    - No references remain to:
      - `IS100Parser`, `S101Parser`, `IS57Parser`, `BasicS57Parser`
    - Code style rules adhered to (Allman braces, block-scoped namespaces, one public type per file).
  - **Summary**:
    - Performed a repo-wide sweep for legacy type names; no production/test references remain.
    - Confirmed build + ingestion test suite pass.
  - **Definition of Done**:
    - `dotnet build` and `dotnet test` pass
    - No warnings/errors introduced by the refactor (within scope)
  - [x] Task 1: Repo search and final adjustments - Completed
    - [x] Step 1: Run a solution-wide search for old symbols and update.
    - [x] Step 2: Confirm DI registrations and usages compile.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test`

---

## Summary
This plan delivers the refactor in vertical, runnable slices:
1) Rename S-100 types to “enricher” while keeping current behavior.
2) Change S-57 contract so the implementation enriches `CanonicalDocument` directly, updating handlers/DI accordingly.
3) Update and rename tests to lock in behavioral parity.
4) Sweep and clean up legacy references to ensure long-term consistency.
