# Implementation Plan

## S-101 parsing refactor into `S101Parser`
- [x] Work Item 1: Introduce minimal parser abstraction and delegate S-101 catalogue enrichment - Completed
  - **Purpose**: Partition S-101-specific `catalog.xml` parsing/enrichment out of `S100BatchContentHandler` to reduce complexity while preserving existing behaviour and error handling.
  - **Acceptance Criteria**:
    - When `catalog.xml` is present, `S100BatchContentHandler` invokes `S101Parser` for S-101 enrichment (FR1).
    - S-101 parsing/enrichment logic is removed from `S100BatchContentHandler` and exists only in `S101Parser` (FR2).
    - Only S-101 catalogues are enriched (gated by product specification in the XML) (FR3).
    - Parsing errors (excluding cancellation) are logged and do not fail the overall enrichment step (NFR1).
    - Existing enrichment output on `CanonicalDocument` matches current behaviour (TR5).
  - **Definition of Done**:
    - Code implemented (`IS100Parser`, `S101Parser`, handler delegation)
    - Unit/integration tests updated and passing
    - Logging + graceful error handling preserved
    - Documentation updated (this plan + architecture)
    - Can execute end-to-end via: run test suite and local ingestion/enrichment verification steps

  - [x] Task 1: Locate current S-101 parsing logic in `S100BatchContentHandler` - Completed
    - [x] Step 1: Identify the project containing `S100BatchContentHandler` (expected: file-share ingestion provider project). - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/S100BatchContentHandler.cs`
    - [x] Step 2: Capture current behaviour boundaries (inputs: extracted batch folder + `catalog.xml`; outputs: enriched `CanonicalDocument`). - Confirmed handler selects `catalog.xml` and mutates `CanonicalDocument` (keywords/search text/polygons + `DocumentType`).
    - [x] Step 3: Identify how S-101 is detected today (product spec value, namespaces). - Based on `XC:productSpecification/XC:name` using XC 5.2 namespace.
    - [x] Step 4: Inventory enrichment mapping applied (keywords/search text/polygons) to ensure parity post-refactor. - Keywords: S-101/S101; search text: organization + comment; polygons: parse `gml:posList`.

  - [x] Task 2: Create `IS100Parser` interface (internal) - Completed
    - [x] Step 1: Add new file `IS100Parser.cs` in the same project/namespace as the handler/parsers. - Added `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/IS100Parser.cs`.
    - [x] Step 2: Define a minimal API aligned to handler needs (TR6), e.g. `bool TryEnrichFromCatalogue(string cataloguePath, CanonicalDocument document, CancellationToken ct)` or similar. - Implemented `Task<bool> TryEnrichFromCatalogueAsync(string cataloguePath, CanonicalDocument document, CancellationToken cancellationToken)`.
    - [x] Step 3: Ensure cancellation is not swallowed (rethrow `OperationCanceledException`). - Parser catches `Exception` with `when (ex is not OperationCanceledException)`.

  - [x] Task 3: Implement `S101Parser` (internal) and move enrichment logic - Completed
    - [x] Step 1: Add new file `S101Parser.cs` implementing `IS100Parser`. - Added `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/Handlers/S101Parser.cs`.
    - [x] Step 2: Move the S-101 XML load/parse code from handler into the parser. - Handler is now orchestration-only.
    - [x] Step 3: Keep existing namespace handling and mapping rules (TR5). - XC/GCO/GML namespaces preserved.
    - [x] Step 4: Implement S-101 gating based on product specification value from XML (FR3). - `S101Parser` gates on `XC:productSpecification/XC:name`.
    - [x] Step 5: Preserve error handling: catch parse exceptions (except cancellation), log, and return `false`/no-op (NFR1). - Best-effort `TryEnrich...` with warning logs.
    - [x] Step 6: Ensure the parser does not require DI; accept only required dependencies via constructor (e.g., `ILogger`) or static logging if consistent with existing style. - Constructed via `new S101Parser(...)` in handler.

  - [x] Task 4: Update `S100BatchContentHandler` to delegate to `S101Parser` - Completed
    - [x] Step 1: Remove inline S-101-specific parsing code from handler (FR2). - Removed XML parsing and polygon parsing from handler.
    - [x] Step 2: Instantiate parser directly with `new` (TR4) once per `HandleFiles` invocation or equivalent. - `new S101Parser(...)` per invocation.
    - [x] Step 3: Invoke parser when `catalog.xml` is present (FR1). - Delegation occurs after locating `catalog.xml`.
    - [x] Step 4: Keep existing flow control: handler continues processing other enrichments even if parser fails. - Handler catches non-cancellation exceptions and logs.

  - [x] Task 5: Refactor tests to assert behaviour via `CanonicalDocument` - Completed
    - [x] Step 1: Identify existing tests covering S-101 enrichment (or handler behaviour) and update them to tolerate the new partition. - Updated existing handler tests only.
    - [x] Step 2: Add/ensure test case: valid S-101 `catalog.xml` enriches expected fields (keywords/search text/polygons). - Existing test retained; added `DocumentType` assertion.
    - [x] Step 3: Add/ensure test case: non S-101 `catalog.xml` does not enrich. - Existing test retained; added `DocumentType` assertion.
    - [x] Step 4: Add/ensure test case: invalid polygon `gml:posList` does not throw; other enrichments still apply. - Existing test retained.
    - [x] Step 5: Ensure tests do not test parser internals; test the handler output or end-to-end enrichment surface (6.1). - Tests still invoke `S100BatchContentHandler`.

  - [x] Task 6: Add minimal documentation + verification instructions - Completed
    - [x] Step 1: Document intended class responsibilities and non-DI instantiation approach. - Added `docs/025-s101-parser/architecture-s101-parser_v0.01.md`.
    - [x] Step 2: Provide run/verification steps (tests + manual enrichment run path). - Verified via `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj -c Release`.

  - **Implementation Summary (chronological)**:
    - Added internal parser abstraction `IS100Parser` and new `S101Parser` containing S-101-specific enrichment extraction (keywords, search text, polygons) from a loaded `XDocument`.
    - Updated `S100BatchContentHandler` to locate and load `catalog.xml` into an `XDocument`, determine product specification (S-101 gating), set `CanonicalDocument.DocumentType`, then delegate to `S101Parser` via direct instantiation (no DI).
    - Updated tests to continue asserting handler-driven `CanonicalDocument` outcomes; added `DocumentType` assertions.
    - Build and tests passed (`UKHO.Search.Ingestion.Tests`: 183 tests).

  - **Files**:
    - `src/.../S100BatchContentHandler.cs`: Remove S-101 parsing logic, instantiate and call `S101Parser`.
    - `src/.../Parsing/IS100Parser.cs` (or similar): New internal interface.
    - `src/.../Parsing/S101Parser.cs` (or similar): New internal parser containing all S-101-specific logic.
    - `src/...Tests/...`: Update/add tests for S-101 enrichment behaviour.
    - `docs/025-s101-parser/plan-s101-parser_v0.01.md`: This plan.
    - `docs/025-s101-parser/architecture-s101-parser_v0.01.md`: Architecture note.

  - **Work Item Dependencies**:
    - None beyond existing ingestion/enrichment pipeline and current tests.

  - **Run / Verification Instructions**:
    - Run unit/integration tests for the ingestion/provider project(s) and verify all pass.
    - (Optional manual) Execute the ingestion/enrichment path against a sample extracted batch containing an S-101 `catalog.xml`; verify enriched fields are produced and logged errors do not stop processing for malformed polygon data.

  - **User Instructions**:
    - None expected (no DI wiring, no configuration changes).

---

## Summary
This plan delivers a single vertical slice refactor: introduce an internal `IS100Parser` abstraction, move all S-101 `catalog.xml` parsing/enrichment into `S101Parser`, and update `S100BatchContentHandler` to delegate via direct instantiation. Behaviour must be preserved (namespaces, mapping, error handling). Tests are refactored to validate `CanonicalDocument` outcomes rather than internal implementation details.
