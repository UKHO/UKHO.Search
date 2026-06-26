# Specification: FileShare `BatchContentEnricher` (Batch Content Handlers)

Version: v0.01  
Status: Draft  
Work Package: `dev/work-packages/mvp/020-batch-enrich/`

## 1. Summary
This change refactors FileShare batch content enrichment into a handler-based model.

- Rename FileShare `FileContentEnricher` to `BatchContentEnricher`.
- Introduce a Domain interface `IBatchContentHandler` that can process a batch of files for an ingestion request.
- Provide FileShare handler implementations:
  - `S57BatchContentHandler` (no-op)
  - `S100BatchContentHandler` (moves `catalog.xml` detection here)
  - `TextExtractionBatchContentHandler` (Kreuzberg-backed text extraction)
- Ensure `BatchContentEnricher` always downloads the batch ZIP per invocation, extracts files, and executes all handlers in a best-effort manner.

This document captures functional and technical requirements in a single specification.

## 2. Goals
- Support multiple, independently deployable batch-content enrichment behaviors.
- Ensure all handlers run for all content (best effort); one handler failing must not prevent others from running.
- Ensure the batch ZIP is always downloaded on each `BatchContentEnricher` invocation.
- Keep existing behavior for Kreuzberg-based extraction, while moving it behind a handler and passing allowed extensions through.
- Move S-100 `catalog.xml` detection logic out of the enricher into the S-100 handler.
- Update/amend existing tests impacted by renames and behavioral changes.

## 3. Non-goals
- Changing ingestion contracts (`IngestionRequest`, message payload shape, etc.).
- Changing how FileShare batches are produced or stored.
- Introducing ordering guarantees among handlers.

## 4. Background / evidence
- Existing FileShare Kreuzberg extraction was specified in `dev/work-packages/mvp/014-kreuzberg-extraction/spec-fileshare-filecontentenricher_v0.01.md`.
- Current implementation uses a FileShare enrichment component that downloads a ZIP and extracts content.

## 5. Requirements

### 5.1 Rename `FileContentEnricher` to `BatchContentEnricher`
- The FileShare provider MUST rename `FileContentEnricher` to `BatchContentEnricher`.
- All call sites, DI registrations, logs, and tests MUST be updated accordingly.
- Any log categories MUST be updated to match the new type name.

### 5.2 Introduce Domain interface `IBatchContentHandler`
- A new Domain interface `IBatchContentHandler` MUST be created.
- Location: Domain layer (must follow repo Onion architecture; domain must not depend on provider/infrastructure).
- API:

  `HandleFiles(IEnumerable<string> paths, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)`

- Semantics:
  - `paths` is the set of extracted file paths on local disk for the current batch.
  - `request` is the current ingestion request (Add or Update).
  - `document` is the mutable `CanonicalDocument` to enrich.
  - The handler MAY be a no-op.
  - The handler SHOULD honor cancellation and return promptly when cancelled.

### 5.3 Handler injection into `BatchContentEnricher`
- `BatchContentEnricher` MUST accept an `IEnumerable<IBatchContentHandler>` via DI.
- The enricher MUST run all handlers for the current content.
- The enricher MUST NOT assume any execution order for handlers.

### 5.4 FileShare provider handler implementations

#### 5.4.1 `S57BatchContentHandler`
- Implement `IBatchContentHandler` in the FileShare provider project.
- `HandleFiles(...)` MUST be a no-op (for now).

#### 5.4.2 `S100BatchContentHandler`
- Implement `IBatchContentHandler` in the FileShare provider project.
- `HandleFiles(...)` MUST be a no-op except for S-100 `catalog.xml` detection.

##### `catalog.xml` detection
- The logic that detects `catalog.xml` (case-insensitive) MUST be moved from the enricher to `S100BatchContentHandler`.
- Detection MUST be case-insensitive.
- Batch ZIPs MAY contain files within nested folder paths (potentially several directories deep); `catalog.xml` MAY appear in any subdirectory.
- The handler MUST only rely on the `paths` passed in (extracted file paths) and MUST NOT perform its own FileShare download.
- If the catalog is not present, the handler MUST no-op.

> Note: The specific downstream effect of catalog detection must remain consistent with current behavior (e.g., setting keywords/content/facets/etc.). The refactor must preserve outputs.

### 5.5 `TextExtractionBatchContentHandler` (Kreuzberg extraction)
- Implement `IBatchContentHandler` as `TextExtractionBatchContentHandler`.
- This handler MUST implement Kreuzberg-backed text extraction for allowed file types.

#### 5.5.1 Allowed extensions input
- The allow list of file extensions MUST be passed to `TextExtractionBatchContentHandler`.
- The allow list MUST be parsed case-insensitively.
- Extensions MAY include or omit leading `.`; they MUST be normalized for comparison.

#### 5.5.2 Extraction behavior
- The handler MUST iterate the provided `paths` and attempt extraction only for files whose extension is in the allow list.
- For each file that yields extracted text:
  - MUST call `CanonicalDocument.SetContent(...)` once per file.
  - MUST call `CanonicalDocument.SetKeyword(...)` with the file name excluding extension.
- Per-file extraction failures MUST be best-effort:
  - If extraction fails/throws for a specific file, the handler MUST log a warning and continue with other files.

### 5.6 Zip download/extract behavior in `BatchContentEnricher`
- `BatchContentEnricher` MUST download the ZIP on every invocation (no caching/reuse between calls).
- The enricher MUST determine the batch id from `request`:
  - Prefer `request.AddItem.Id` when `AddItem` is not null.
  - Otherwise use `request.UpdateItem.Id` when `UpdateItem` is not null.
  - If neither is present, the enricher MUST no-op.
- The enricher MUST extract the ZIP contents to a unique per-invocation working directory.
- The enricher MUST extract the ZIP contents to a unique per-invocation working directory, preserving nested directory structure from the ZIP.
- Extraction MUST be safe against path traversal (“zip slip”).
- After extraction, the enricher MUST call all registered `IBatchContentHandler` instances with:
  - `paths`: the complete set of extracted file paths for the batch, including files in nested subdirectories (not just entries at the ZIP root).
  - `request`: the same ingestion request.
  - `document`: the canonical document.

### 5.7 Handler execution and fault tolerance
- `BatchContentEnricher` MUST execute all handlers even if one handler fails.
- If a handler throws:
  - The enricher MUST log the exception as an error.
  - The enricher MUST continue and invoke the remaining handlers.
- There is no specified order for handler execution; order is arbitrary.

### 5.8 Cleanup
- Temporary artifacts (downloaded ZIP and extracted directory) MUST be deleted before the enricher returns.
- Cleanup MUST run even when:
  - A handler throws.
  - Cancellation is requested.
  - ZIP extraction fails after temp resources were created.

## 6. Logging
- `BatchContentEnricher` MUST log:
  - Errors for download/unzip failures (terminal for enrichment).
  - Errors when a handler throws (non-terminal for enrichment).
- Handlers SHOULD use `ILogger<THandler>`.
- Log entries SHOULD include the batch id context.

## 7. Acceptance criteria
- The solution compiles with `FileContentEnricher` renamed to `BatchContentEnricher`.
- `IBatchContentHandler` exists in Domain and is injectable.
- FileShare provider registers and resolves `S57BatchContentHandler`, `S100BatchContentHandler`, and `TextExtractionBatchContentHandler`.
- `BatchContentEnricher` downloads the ZIP each time it is invoked.
- All handlers run for all content; one handler failing does not prevent others.
- `catalog.xml` detection is performed by `S100BatchContentHandler` and removed from the enricher.
- Kreuzberg text extraction occurs via `TextExtractionBatchContentHandler` and respects allowed extensions.
- All existing tests impacted by rename/refactor are updated and passing.

## 8. Testing strategy
- Unit tests (or existing tests updated):
  - Enricher handler execution continues after a handler exception.
  - ZIP download is invoked per enricher call.
  - `catalog.xml` detection moved to `S100BatchContentHandler` (behavior preserved).
  - Text extraction handler respects allow list and appends content/keywords.
- Integration tests (where already present):
  - End-to-end ingestion using FileShare emulator validates extraction and enrichment.

## 9. Technical considerations / decisions
- Handler ordering is intentionally unspecified; implementations must not rely on running before/after another handler.
- The handlers model enables future enrichment behaviors without modifying the enricher.

## 10. Open questions
1. What is the current observable behavior/output of `catalog.xml` detection (which fields are set on `CanonicalDocument`)?
2. Where should the allow list configuration live (existing key `ingestion:fileContentExtractionAllowedExtensions` vs new key)?
