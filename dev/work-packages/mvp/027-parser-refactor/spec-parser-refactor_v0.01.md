# Specification: Parser Refactor (Enricher naming + S-57 document enrichment)

- Work Package: `027-parser-refactor`
- Path: `dev/work-packages/mvp/027-parser-refactor/spec-parser-refactor_v0.01.md`
- Version: `v0.01`
- Status: Draft
- Owner: (TBD)
- Last updated: 2026-03-12

## 1. Overview

This work package refactors the FileShare ingestion parsing components to align naming with actual behavior (enrichment of a `CanonicalDocument`) and to unify the enrichment pattern across S-100 (S-101) and S-57 handlers.

Currently, S-100 handling enriches a `CanonicalDocument` directly via `S101Parser.TryEnrichFromCatalogue(...)`, while S-57 handling parses low-level outputs (WKT + text values) and applies them to `CanonicalDocument` externally (e.g., in `S57BatchContentHandler`).

This change:
- Renames “parser” abstractions/classes that actually *enrich* a `CanonicalDocument`.
- Updates the S-57 contract so that the S-57 implementation enriches a `CanonicalDocument` directly (same pattern as S-101).
- Renames and updates tests to reflect the new naming and responsibilities.

## 2. Scope

### 2.1 In scope

- Rename `IS100Parser` to `IS100Enricher`.
- Rename `S101Parser` to `S101Enricher`.
- Rename `IS57Parser` to `IS57Enricher`.
- Rename `BasicS57Parser` to `S57Parser`.
- Change the S-57 enrichment contract from returning parse artifacts to enriching a `CanonicalDocument`:
  - From: `bool TryParse(string pathTo000, out string coveragePolygonWkt, out IReadOnlyList<string> textValues);`
  - To: `bool TryParse(string pathTo000, CanonicalDocument document);`
- Move logic that applies S-57 parse results to `CanonicalDocument` (search text + geo polygon) into `S57Parser` so callers only pass the `CanonicalDocument`.
- Update all tests to reflect naming and signature changes.
  - Ensure test classes and their `.cs` filenames are renamed consistently.

### 2.2 Out of scope

- Any functional change to the S-57 geometric computation algorithm or text extraction behavior beyond moving where the enrichment is applied.
- Changes to DI registration patterns unless required to compile after renames/refactor.
- Additional product-spec support beyond existing S-101 logic.

## 3. Components & Interfaces

### 3.1 FileShare enrichment handlers (provider project)

Target area (current): `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/`

#### 3.1.1 S-100 (S-101)

- `IS100Parser` → `IS100Enricher`
- `S101Parser` → `S101Enricher`

`IS100Enricher` retains a document-enrichment semantic:
- `bool TryEnrichFromCatalogue(XDocument catalogueXml, CanonicalDocument document);`

#### 3.1.2 S-57

- `IS57Parser` → `IS57Enricher`
- `BasicS57Parser` → `S57Parser`

`IS57Enricher` becomes responsible for enriching `CanonicalDocument`:
- `bool TryParse(string pathTo000, CanonicalDocument document);`

### 3.2 Call sites

Call sites that currently:
- Instantiate `BasicS57Parser`, call the old `TryParse(...)` method, then apply:
  - `document.AddSearchText(...)`
  - `document.AddGeoPolygon(...)`

…will be updated to instead:
- Instantiate the renamed `S57Parser` (or resolve via `IS57Enricher` if/when DI is introduced), and
- Call `TryParse(pathTo000, document)`.

### 3.3 Tests

Test projects expected to be impacted:
- `test/UKHO.Search.Ingestion.Tests`

Test class and filename updates are required to match new production names:
- `BasicS57ParserTests` (`BasicS57ParserTests.cs`) → `S57ParserTests` (`S57ParserTests.cs`)
- Any `S101Parser` test mentions → `S101Enricher`.

## 4. Functional Requirements

### FR-1: Rename S-100 parser abstraction and implementation

- The codebase must compile with `IS100Enricher` and `S101Enricher` names replacing their existing “parser” names.
- All references (including tests) must be updated accordingly.

### FR-2: Rename S-57 parser abstraction and implementation

- The codebase must compile with `IS57Enricher` and `S57Parser` names replacing their existing names.
- All references (including tests) must be updated accordingly.

### FR-3: S-57 enrichment applies directly to `CanonicalDocument`

- `IS57Enricher` must expose `bool TryParse(string pathTo000, CanonicalDocument document);`.
- `S57Parser.TryParse(...)` must:
  - Load and parse S-57 dataset from `pathTo000`.
  - Enrich the provided `document` with:
    - Extracted text metadata (same values currently returned in `textValues`).
    - Coverage polygon (computed from dataset envelope) added as geo polygon.
  - Follow existing normalization rules used by `CanonicalDocument` (e.g., lowercasing/keyword normalization occurs within `CanonicalDocument` methods where applicable).
  - Return `true` on successful enrichment, `false` otherwise.

### FR-4: Update S-57 batch enrichment wiring

- Callers (e.g., `S57BatchContentHandler`) must not need to handle:
  - WKT parsing
  - Adding search text values

Those responsibilities must be in `S57Parser`.

### FR-5: Update and rename tests

- All unit tests in scope must compile and pass.
- Test class names and filenames must match refactored production naming.

## 5. Non-Functional Requirements

### NFR-1: Backwards-compatibility

- No requirement for source-level backwards compatibility for external consumers; these types are currently internal and used within the provider/test codebase.

### NFR-2: Logging

- Existing logging behavior must be preserved (warnings/debug logs) where possible.
- Logging should remain via `ILogger<T>`.

### NFR-3: Coding standards

- Follow repository coding standards (Allman braces, block-scoped namespaces, one type per file).

## 6. Technical Requirements / Design Notes

### TR-1: File/Type rename consistency

- File names should match type names (e.g., `S101Enricher.cs`, `IS100Enricher.cs`, `IS57Enricher.cs`, `S57Parser.cs`).

### TR-2: CanonicalDocument enrichment location

- S-57 `CanonicalDocument` mutation must be performed inside the S-57 implementation (analogous to S-101 behavior) to keep enrichment logic co-located with parsing.

### TR-3: Geo polygon parsing approach

- The existing approach currently converts an envelope to WKT and then parses WKT to a polygon (via `GeoPolygonWktReader`).
- Post-refactor, the *caller* must no longer do WKT parsing. Implementation may:
  1) Continue generating WKT and call `GeoPolygonWktReader` internally, or
  2) Construct the `GeoPolygon` directly without WKT.

Either is acceptable provided behavior matches existing tests/fixtures.

## 7. Acceptance Criteria

1. The solution builds successfully.
2. All impacted tests pass.
3. Types are renamed as specified:
   - `IS100Parser` → `IS100Enricher`
   - `S101Parser` → `S101Enricher`
   - `IS57Parser` → `IS57Enricher`
   - `BasicS57Parser` → `S57Parser`
4. The S-57 API is updated:
   - Old: `TryParse(pathTo000, out coveragePolygonWkt, out textValues)`
   - New: `TryParse(pathTo000, CanonicalDocument document)`
5. `S57BatchContentHandler` (and any other call sites) no longer manually:
   - add extracted text values to `CanonicalDocument`
   - parse WKT into geo polygons
6. Corresponding tests are renamed and verify the same fixture outcomes.

## 8. Risks & Open Questions

- Risk: Moving WKT parsing into `S57Parser` could change exception handling/logging behavior; ensure equivalence.
- Risk: Any additional call sites (outside ingestion provider) may exist; ensure all references are updated.

Open question:
- Should the method name remain `TryParse` or become `TryEnrich` for S-57 for consistency? (This spec retains `TryParse` as requested.)
