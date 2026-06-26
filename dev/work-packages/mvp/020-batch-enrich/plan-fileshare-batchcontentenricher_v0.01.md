# Implementation Plan

Work Package: `dev/work-packages/mvp/020-batch-enrich/`  
Based on: `spec-fileshare-batchcontentenricher_v0.01.md`

## Project structure / approach
- Primary runtime component: FileShare provider enrichment component (`BatchContentEnricher`).
- New Domain contract to support extensibility: `IBatchContentHandler`.
- Provider-side implementations:
  - `S57BatchContentHandler`
  - `S100BatchContentHandler`
  - `TextExtractionBatchContentHandler` (Kreuzberg)
- DI wiring: FileShare provider project registers the above handlers and injects `IEnumerable<IBatchContentHandler>` into `BatchContentEnricher`.

Key constraints:
- Onion architecture: `IBatchContentHandler` goes in Domain and must not depend on provider projects.
- One public type per file; block-scoped namespaces; Allman braces.
- Handlers must be best-effort: all handlers invoked even if one throws.
- ZIPs may contain deeply nested paths; handlers must receive the complete extracted file list.

---

## Vertical Slice 1: Introduce handler contract + wire handler execution path
- [x] Work Item 1: Add `IBatchContentHandler` Domain interface and execute injected handlers (no functional change yet) - Completed
  - **Purpose**: Establish the extensibility seam and a runnable enrichment path that executes at least one handler.
  - **Acceptance Criteria**:
    - `IBatchContentHandler` exists in a Domain project and compiles.
    - FileShare enricher is renamed to `BatchContentEnricher` and resolves `IEnumerable<IBatchContentHandler>`.
    - Enricher executes all handlers and continues after exceptions (logs error and continues).
    - Existing tests compile after rename.
  - **Definition of Done**:
    - Code implemented and builds.
    - Unit tests updated/added and passing.
    - Logging added for handler failures.
    - Documentation unchanged (spec already exists).
    - Can execute end-to-end via: running ingestion pipeline locally (existing host) and verifying no regressions.
  - [x] Task 1.1: Create `IBatchContentHandler` in Domain - Completed
    - [x] Step 1: Identify correct Domain project for ingestion contracts (likely `src/UKHO.Search.Ingestion`). - Completed
    - [x] Step 2: Add new file `IBatchContentHandler.cs` with method signature from spec. - Completed (`src/UKHO.Search.Ingestion/IBatchContentHandler.cs`)
    - [x] Step 3: Ensure references compile (namespaces: `IngestionRequest`, `CanonicalDocument`). - Completed
  - [x] Task 1.2: Rename `FileContentEnricher` to `BatchContentEnricher` - Completed
    - [x] Step 1: Locate `FileContentEnricher` implementation in FileShare provider project. - Completed
    - [x] Step 2: Rename file/class/type usages and DI wiring. - Completed (new `BatchContentEnricher` + DI updated)
    - [x] Step 3: Update log category usages (generic `ILogger<...>`). - Completed
    - [x] Step 4: Update any references in tests. - Completed (tests renamed/updated)
  - [x] Task 1.3: Inject and execute handlers - Completed
    - [x] Step 1: Add constructor dependency `IEnumerable<IBatchContentHandler>`. - Completed
    - [x] Step 2: After ZIP extraction and file enumeration, invoke each handler. - Completed
    - [x] Step 3: Wrap each handler invocation in `try/catch` to log errors and continue. - Completed
    - [x] Step 4: Ensure cancellation token is passed through. - Completed
  - **Summary**:
    - Added Domain interface `IBatchContentHandler`.
    - Renamed and moved FileShare enricher implementation to `BatchContentEnricher` and updated provider DI registration.
    - Added best-effort handler execution (logs and continues on handler exception).
    - Updated tests (`FileContentEnricherTests` -> `BatchContentEnricherTests`) and verified they pass.
  - **Files**:
    - `src/UKHO.Search.Ingestion/.../IBatchContentHandler.cs`: new domain interface.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/BatchContentEnricher.cs`: rename + handler execution.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/...`: update DI registration and call sites.
    - `tests/**`: update rename references.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - Run existing test suite(s) relevant to FileShare provider.

---

## Vertical Slice 2: Add provider handlers (S57/S100) and move `catalog.xml` detection
- [x] Work Item 2: Implement `S57BatchContentHandler` and `S100BatchContentHandler` and register in DI - Completed
  - **Purpose**: Move S-57/S-100 specific behavior behind handlers and validate multi-handler execution.
  - **Acceptance Criteria**:
    - `S57BatchContentHandler` exists and is a no-op.
    - `S100BatchContentHandler` exists; includes moved `catalog.xml` detection (case-insensitive, supports nested paths).
    - `catalog.xml` detection removed from `BatchContentEnricher` (behavior preserved).
    - Handlers are registered in FileShare provider DI and invoked.
  - **Definition of Done**:
    - Code implemented and builds.
    - Tests updated/added to verify `catalog.xml` detection via handler.
    - Existing behavior preserved (same `CanonicalDocument` mutations as before).
  - [x] Task 2.1: Create `S57BatchContentHandler` - Completed
    - [x] Step 1: Add new file implementing `IBatchContentHandler`. - Completed (`S57BatchContentHandler`)
    - [x] Step 2: Add logging (optional) but keep handler a no-op. - Completed
  - [x] Task 2.2: Create `S100BatchContentHandler` and move detection logic - Completed
    - [x] Step 1: Locate existing `catalog.xml` detection logic in current enricher. - Completed
    - [x] Step 2: Move logic verbatim (or smallest safe refactor) into `S100BatchContentHandler`. - Completed
    - [x] Step 3: Ensure detection uses `Path.GetFileName(path)` and case-insensitive compare to support nested paths. - Completed
    - [x] Step 4: Remove old detection code from the enricher. - Completed
  - [x] Task 2.3: DI wiring in provider project - Completed
    - [x] Step 1: Register both handlers in service collection as `IBatchContentHandler`. - Completed
    - [x] Step 2: Ensure multiple registrations resolve as `IEnumerable<IBatchContentHandler>`. - Completed
  - **Summary**:
    - Added no-op `S57BatchContentHandler` and `S100BatchContentHandler` (catalog.xml detection/loading) in FileShare provider.
    - Moved `catalog.xml` logic out of `BatchContentEnricher` into `S100BatchContentHandler` and registered both handlers in DI.
    - Added regression test for nested `catalog.xml` path.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/.../S57BatchContentHandler.cs`: new.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/.../S100BatchContentHandler.cs`: new.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/.../ServiceCollectionExtensions.cs` (or equivalent): register handlers.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/BatchContentEnricher.cs`: remove catalog logic.
    - `tests/**`: update/new tests.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test` (or VS Test Explorer) filtering to FileShare provider tests.

---

## Vertical Slice 3: Extract Kreuzberg text extraction into `TextExtractionBatchContentHandler`
- [x] Work Item 3: Implement `TextExtractionBatchContentHandler` and pass allowed extensions - Completed
  - **Purpose**: Maintain Kreuzberg extraction while moving it into a handler and ensuring configuration is passed correctly.
  - **Acceptance Criteria**:
    - `TextExtractionBatchContentHandler` implements Kreuzberg extraction.
    - Allowed extensions are provided to the handler (via configuration/options) and applied case-insensitively.
    - Handler appends extracted content via `CanonicalDocument.SetContent()` and adds keywords via `CanonicalDocument.SetKeyword()`.
    - Per-file extraction failure does not fail the handler.
  - **Definition of Done**:
    - Code implemented and builds.
    - Tests updated/added to verify allow-list filtering and keyword/content behavior.
  - [x] Task 3.1: Define configuration/options shape for allowed extensions - Completed
    - [x] Step 1: Reuse existing configuration key if present (`ingestion:fileContentExtractionAllowedExtensions`). - Completed
    - [x] Step 2: Add options binding or pass a parsed set into handler constructor. - Completed (DI factory parses config and passes to handler)
    - [x] Step 3: Ensure normalization to leading `.`. - Completed (handler normalizes)
  - [x] Task 3.2: Implement Kreuzberg extraction handler - Completed
    - [x] Step 1: Extract existing Kreuzberg code from enricher into handler. - Completed
    - [x] Step 2: Implement allow-list filtering. - Completed
    - [x] Step 3: Add per-file try/catch with warning logs. - Completed
  - [x] Task 3.3: Register handler in DI - Completed
    - [x] Step 1: Register `TextExtractionBatchContentHandler` as `IBatchContentHandler`. - Completed
  - **Summary**:
    - Added `TextExtractionBatchContentHandler` implementing Kreuzberg extraction and allow-list filtering.
    - Removed Kreuzberg extraction/config parsing from `BatchContentEnricher`; it now strictly downloads/extracts/enumerates and invokes handlers.
    - Updated FileShare provider DI to register `TextExtractionBatchContentHandler` and pass allowed extensions from `ingestion:fileContentExtractionAllowedExtensions`.
    - Updated enrichment tests to construct the handler directly (unit-level) and verified ingestion tests pass.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/.../TextExtractionBatchContentHandler.cs`: new.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/.../options/*` (if used): new/updated.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/.../ServiceCollectionExtensions.cs`: register.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/BatchContentEnricher.cs`: remove Kreuzberg logic.
    - `tests/**`: update/new tests.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test`

---

## Vertical Slice 4: Ensure ZIP is always downloaded per call and full file list includes nested paths
- [x] Work Item 4: Enforce per-invocation download + robust extraction/enumeration + cleanup - Completed
  - **Purpose**: Ensure correctness and determinism for per-call execution, and guarantee handlers see all files.
  - **Acceptance Criteria**:
    - `BatchContentEnricher` downloads ZIP every invocation (verified in tests).
    - Extracted file enumeration passes the complete list, including nested subdirectories.
    - Temporary artifacts are always cleaned up.
  - **Definition of Done**:
    - Code implemented and builds.
    - Tests updated/added for download-per-call and nested file passing.
  - [x] Task 4.1: Download-per-invocation verification - Completed
    - [x] Step 1: Add/adjust unit test using a mock `IFileShareZipDownloader` verifying `DownloadZipFileAsync` called once per enricher execution. - Completed
  - [x] Task 4.2: Enumerate files recursively - Completed
    - [x] Step 1: Ensure unzipping preserves directories. - Completed
    - [x] Step 2: Use `Directory.EnumerateFiles(extractRoot, "*", SearchOption.AllDirectories)` (or equivalent) to produce file list. - Completed
    - [x] Step 3: Ensure handler receives full list. - Completed
  - [x] Task 4.3: Cleanup hardening - Completed
    - [x] Step 1: Ensure cleanup in `finally` and robust against partial failures. - Completed (existing test already covers workspace cleanup)
  - **Summary**:
    - Added tests verifying ZIP download occurs once per enricher invocation (no caching) and that handlers receive a full recursive file list (nested paths included).
    - Confirmed recursive file enumeration and cleanup behavior remain intact.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/BatchContentEnricher.cs`: download/extract/enumerate/cleanup.
    - `tests/**`: new/updated tests.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test`

---

## Vertical Slice 5: Test suite updates and regression coverage
- [x] Work Item 5: Update all impacted tests and add regressions for handler fault tolerance - Completed
  - **Purpose**: Ensure confidence in refactor and protect against regressions.
  - **Acceptance Criteria**:
    - All existing tests are amended for rename/refactor and pass.
    - New tests cover: handler continues after exception; catalog detection in nested paths; allow-list filtering.
  - **Definition of Done**:
    - `dotnet test` passes for solution.
    - Any new tests follow repo conventions.
  - [x] Task 5.1: Update existing tests for renames - Completed
    - [x] Step 1: Find tests referencing `FileContentEnricher` and update. - Completed (`FileContentEnricherTests` replaced by `BatchContentEnricherTests`)
  - [x] Task 5.2: Add fault-tolerance test - Completed
    - [x] Step 1: Add a test handler that throws and another that sets a marker on the document. - Completed
    - [x] Step 2: Verify the marker is set even when earlier handler throws. - Completed
  - [x] Task 5.3: Add nested `catalog.xml` case test - Completed
    - [x] Step 1: Provide extracted path list containing `.../a/b/catalog.xml` and verify behavior. - Completed
  - **Summary**:
    - Updated enrichment tests to reflect the rename and handler-based model.
    - Added regression coverage for nested `catalog.xml` handling and handler fault tolerance (subsequent handlers still invoked after an exception).
    - Verified `UKHO.Search.Ingestion.Tests` passes.
  - **Files**:
    - `tests/**`: updated and new tests.
  - **Work Item Dependencies**: Work Items 1-4.
  - **Run / Verification Instructions**:
    - `dotnet test`

---

## Summary / key considerations
- The implementation is structured around an early introduction of the handler execution seam, then incrementally moving existing behavior into handlers.
- The bulk of risk is in preserving current `catalog.xml` behavior and Kreuzberg extraction output while refactoring; tests should capture current outcomes before/while refactoring.
- ZIP handling must remain secure (“zip slip” defense) while supporting deeply nested content and passing the full recursive file list to handlers.
