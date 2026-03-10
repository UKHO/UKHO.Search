# Specification: FileShare `FileContentEnricher` (Kreuzberg Extraction)

Version: v0.01  
Status: Draft  
Work Package: `docs/014-kreuzberg-extraction/`

## 1. Summary
Implement `FileContentEnricher` in `UKHO.Search.Ingestion.Providers.FileShare` to:
- Download a batch ZIP from FileShareEmulator using the existing `IFileShareReadOnlyClient` integration.
- Extract textual content from files inside the ZIP using Kreuzberg.
- Populate extracted text into `CanonicalDocument` via `CanonicalDocument.SetContent`.
- Ensure all temporary artifacts (ZIP file and extracted contents) are deleted before the enricher finishes, regardless of success or failure.

## 2. Goals
- Enricher can download the ZIP for the current ingestion request batch id.
- Enricher extracts text from as many ZIP entries as possible (best-effort per file).
- Failures to extract from a given file do not fail the whole enrichment.
- Temporary disk usage is bounded to a per-request temp folder and cleaned up deterministically.

## 3. Non-goals
- Implementing OCR for images (unless Kreuzberg already performs this and is enabled intentionally).
- Persisting extracted files beyond enrichment execution.
- Changing the ingestion request contract.

## 4. Background / evidence
- `FileContentEnricher` exists as a stub: `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/FileContentEnricher.cs`.
- `IFileShareReadOnlyClient.DownloadZipFileAsync(string batchId, CancellationToken)` returns `Task<IResult<Stream>>`.
  - Example `IResult` handling exists in `tools/FileShareImageBuilder/ContentImporter.cs`.
- Kreuzberg .NET API reference:
  - https://github.com/kreuzberg-dev/kreuzberg
  - https://docs.kreuzberg.dev/reference/api-csharp/

## 5. Requirements

### 5.1 Resolve batch id
- The enricher MUST determine the batch id from the ingestion request:
  - Prefer `request.AddItem.Id` when `AddItem` is not null.
  - Otherwise use `request.UpdateItem.Id` when `UpdateItem` is not null.
- If both `AddItem` and `UpdateItem` are null, enrichment MUST no-op.

### 5.2 Download batch ZIP
- The enricher MUST call `IFileShareReadOnlyClient.DownloadZipFileAsync(batchId, cancellationToken)`.
- The enricher MUST handle `IResult<Stream>` as follows:
  - If unsuccessful, enrichment MUST throw (and log the failure).
  - If successful, the returned stream MUST be disposed.

### 5.3 Copy ZIP to temporary storage
- The enricher MUST create a unique temporary working directory per invocation.
- The ZIP stream MUST be copied to a temporary ZIP file on disk.

### 5.4 Unzip to temporary directory
- The ZIP file MUST be extracted to a temporary directory under the working directory.
- Extraction MUST be safe against path traversal (“zip slip”): entries must not be allowed to write outside the working directory.
- If the ZIP is corrupt or extraction fails, the enricher MUST throw.

### 5.5 Extract text using Kreuzberg
- The enricher MUST read a semicolon-delimited allow list of file extensions from injected `IConfiguration`.
  - Configuration key: `ingestion:fileContentExtractionAllowedExtensions`.
  - Example value: `.pdf;.docx;.txt;.html`.
  - Parsing MUST be case-insensitive.
  - Values MAY include or omit the leading `.`; the enricher MUST normalize to a leading `.` for comparison.
- If the allow list is missing or empty, the enricher MUST no-op (extract nothing) and log a warning.
- The enricher MUST only attempt Kreuzberg extraction for files whose extension is present in the allow list.
- For each extracted file, the enricher SHOULD attempt text extraction using Kreuzberg.
- If Kreuzberg returns no text for a file, the enricher SHOULD skip that file.
- If Kreuzberg throws or extraction fails for a file, the enricher SHOULD skip that file and continue.

### 5.6 Populate `CanonicalDocument`
- For each file that yields extracted text, the enricher MUST call `CanonicalDocument.SetContent(...)` to populate extracted text.
- `CanonicalDocument.SetContent(...)` appends naturally; the enricher MUST call it once per extracted-text file (in a deterministic file iteration order).
- For each extracted-text file, the enricher MUST call `CanonicalDocument.SetKeyword(...)` with the file name (excluding extension).

### 5.7 Cleanup (mandatory)
- The enricher MUST delete:
  - The temporary ZIP file.
  - The extracted (unzipped) working directory contents.
- Cleanup MUST run even when:
  - Download fails after creating the working directory.
  - ZIP extraction fails.
  - Kreuzberg extraction fails.
  - Cancellation is requested.

## 6. Error handling and logging
- Use `ILogger<FileContentEnricher>` for diagnostics (download failures, unzip failures, Kreuzberg extraction failures) with batch id context.
- Errors should be logged at:
  - Warning: per-file extraction failures.
  - Error: download failures or inability to unzip/read ZIP (these are terminal for enrichment).

## 7. Acceptance criteria
- Given a request with a valid batch id and a downloadable ZIP, extracted text from files in the ZIP is visible in `CanonicalDocument` content.
- Given extracted files, `CanonicalDocument` contains keywords for each processed file name (excluding extension).
- Given a ZIP containing mixed file types, extraction proceeds best-effort (some may be skipped) without failing the entire ingestion.
- Temporary files/folders are removed on both success and failure paths.

## 8. Testing strategy
- Unit tests:
  - Batch id selection logic (`AddItem` vs `UpdateItem`).
  - Cleanup is attempted even when download/unzip/extraction fails.
- Integration tests (preferred where feasible):
  - Against FileShareEmulator to validate end-to-end download + extraction.

## 9. Implementation notes (non-normative)
- The temp working directory should be created under `Path.GetTempPath()` using a unique name containing the batch id.
- ZIP extraction can use `System.IO.Compression.ZipArchive` so each entry can be validated for safe paths before extraction.

## 10. Open questions
None.
