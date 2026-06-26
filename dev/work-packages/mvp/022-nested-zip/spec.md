# Specification: Nested ZIP extraction in `BatchContentEnricher`

**Document type:** Single spec (functional + technical)

**Target path:** `dev/work-packages/mvp/022-nested-zip/spec.md`

## 1. Overview

### 1.1 Purpose

Ensure the ingestion enrichment pipeline does not miss files when a downloaded batch ZIP contains additional ZIP files (nested archives). `BatchContentEnricher` must unpack nested ZIPs prior to calling `IBatchContentHandler` implementations so handlers receive complete, fully-expanded file lists.

### 1.2 Scope

In scope:

- Extend `BatchContentEnricher` so that after initial batch extraction, it recursively discovers and extracts any nested ZIP files.
- Extract each nested ZIP in-place (same directory as the ZIP file) into a subdirectory named after the ZIP file (without the `.zip` extension).
- Continue extracting until there are no remaining ZIP files to extract (within the extracted tree).
- Add automated tests validating nested ZIP extraction and handler invocation behavior.

Out of scope:

- Changes to download mechanisms, batch naming conventions, or handler business logic.
- Support for archive formats other than `.zip`.
- Changes to `IBatchContentHandler` APIs.

### 1.3 Background / Problem Statement

`BatchContentEnricher` currently unzips downloaded batches and then calls `IBatchContentHandler` implementations with file lists. If the unzipped batch contains nested ZIP files, their contents are not available to handlers. This can cause downstream enrichment to miss files.

### 1.4 High-level Approach

- After extracting the outer batch ZIP, traverse the extracted directory tree and find `.zip` files.
- For each `.zip` file found:
  - Extract it into a folder named `{ZipFileNameWithoutExtension}` located alongside the ZIP.
  - Optionally delete the ZIP after successful extraction (decision documented below).
- Repeat traversal/extraction until no more ZIP files exist (or no new ones are found).
- After expansion completes, compute the file list(s) passed to handlers based on the fully-expanded tree.

## 2. Functional Requirements

### 2.1 Nested ZIP Extraction

**FR-1**: When processing a downloaded batch, `BatchContentEnricher` MUST extract nested ZIP files found within the extracted batch directory.

**FR-2**: Each nested ZIP MUST be extracted into a directory created in the same path as the ZIP file, named after the ZIP file without extension.

Example:

- Nested zip: `.../batch/ENC/abc.zip`
- Extraction directory: `.../batch/ENC/abc/`

**FR-3**: Extraction MUST be recursive: if a nested ZIP contains another ZIP, that ZIP MUST also be extracted following the same rules.

**FR-4**: `IBatchContentHandler` implementations MUST be invoked only after nested ZIP extraction completes, and they MUST receive file lists that include files from nested archives.

### 2.2 Safety and Idempotency

**FR-5**: If the target extraction directory already exists for a nested ZIP (e.g., due to prior extraction), the enricher MUST NOT silently corrupt/overwrite existing output. Preferred behavior:

- If the directory exists and appears to already contain extracted content, skip extraction for that ZIP.
- If the directory exists but is empty, it may be reused.

(Exact detection logic is a technical decision; see §4.)

**FR-6**: If a nested ZIP is invalid/corrupt, the batch enrichment MUST fail with a clear exception that identifies:

- Which ZIP failed.
- The reason (where available).

### 2.3 Limits / Constraints

**FR-7**: The implementation MUST prevent infinite expansion loops (e.g., repeated discovery of the same ZIP). The mechanism MAY be:

- Tracking extracted ZIP full paths.
- A maximum recursion depth / iteration count.

### 2.4 Non-Functional Requirements

**NFR-1**: Nested ZIP extraction SHOULD be efficient for typical batch sizes (assume tens to hundreds of files, and small nesting depth).

**NFR-2**: Logging SHOULD include:

- Count of nested ZIPs extracted.
- ZIP paths extracted.
- Total time spent extracting nested content.

(Use `ILogger` if available in the component.)

## 3. Acceptance Criteria

### 3.1 Core behavior

- **AC-1**: Given a batch ZIP that contains a nested ZIP with files, after processing, those nested files appear in the file list passed to handlers.
- **AC-2**: Given a batch ZIP with multiple nested ZIPs at different directory levels, all are extracted and contents included.
- **AC-3**: Given a nested ZIP within a nested ZIP (2+ levels), all levels are extracted.

### 3.2 Output layout

- **AC-4**: Nested ZIP `X.zip` extracted into sibling directory `X/`.

### 3.3 Robustness

- **AC-5**: Corrupt nested ZIP causes enrichment to fail with a message containing the nested ZIP path.

### 3.4 Tests

- **AC-6**: Automated tests exist and cover at least:
  - Single nested ZIP extraction.
  - Double nesting.
  - Handler invoked with expanded file list.

## 4. Technical Specification

### 4.1 Target Components

- `BatchContentEnricher` (existing): extend to perform recursive ZIP expansion.
- Handlers: `IBatchContentHandler` (existing): no API changes.
- Test project(s): add/extend unit/integration tests for nested ZIP extraction.

### 4.2 Proposed Algorithm (Iterative Expansion)

1. After outer extraction, set `rootExtractedPath`.
2. Initialize `HashSet<string> extractedZips`.
3. Loop:
   - Find all `*.zip` files under `rootExtractedPath` (recursive).
   - For each zip path not in `extractedZips`:
     - Determine destination directory as `Path.Combine(Path.GetDirectoryName(zip), Path.GetFileNameWithoutExtension(zip))`.
     - Ensure destination directory exists.
     - Extract the zip to destination.
     - Add zip path to `extractedZips`.
   - Stop when no new zips were extracted.
4. Produce final file list for handlers by enumerating all non-zip files under `rootExtractedPath` (or preserve existing behavior if different).

### 4.3 Destination Directory Exists

Recommended decision:

- If destination directory exists and is non-empty, skip extraction for that ZIP and log a warning.
- If destination directory exists and is empty, proceed.

Rationale: avoids overwriting content if rerun, and reduces risk of partial re-extraction.

### 4.4 Delete ZIPs after extraction (Decision)

Default: **do not delete** ZIP files.

Rationale:

- Keeps original input for troubleshooting.
- Avoids breaking existing behavior if other code expects the ZIP to remain.

If storage pressure becomes an issue, revisit with a configuration option.

### 4.5 Error Handling

- Wrap extraction failures with context:
  - Throw an exception containing `zipPath`.
- Avoid partially extracted outputs where feasible; extraction to destination directory is already isolated.

### 4.6 Logging

If `BatchContentEnricher` already accepts an `ILogger` (or can via DI), log:

- `Information`: start/end nested extraction; count of nested ZIPs.
- `Debug`: each extracted zip path.
- `Warning`: zip skipped due destination existing & non-empty.

### 4.7 Testing Strategy

#### 4.7.1 Test type

Prefer a **fast filesystem-based unit/integration test** that:

- Creates a temp directory.
- Creates ZIP archives programmatically using `System.IO.Compression.ZipArchive`.
- Runs the enricher.
- Asserts that the handler receives expected file paths.

#### 4.7.2 Test cases

- **TC-1 Single nested ZIP**
  - Outer zip contains `nested.zip` and maybe a regular file.
  - `nested.zip` contains `a.txt`.
  - Expect handler file list includes `a.txt`.

- **TC-2 Double nesting**
  - Outer zip contains `nested1.zip`.
  - `nested1.zip` contains `nested2.zip`.
  - `nested2.zip` contains `b.txt`.
  - Expect handler file list includes `b.txt`.

- **TC-3 Extraction directory naming**
  - Nested zip `myfiles.zip` extracted to `myfiles/`.
  - Assert directory exists and contains extracted files.

#### 4.7.3 Handler assertion approach

Use a test handler (fake implementation of `IBatchContentHandler`) to capture the file list passed by `BatchContentEnricher`.

### 4.8 Open Questions / Decisions

1. Should there be a maximum nested extraction depth / iteration count? Recommended: yes (e.g., 25 iterations) as a safety net; configurable if needed.
2. Should we exclude extracting ZIPs larger than some size or use streaming extraction? Not required for this work item.

## 5. Delivery Items

- Updated `BatchContentEnricher` implementation
- New/updated tests validating nested ZIP extraction
- This spec document: `dev/work-packages/mvp/022-nested-zip/spec.md`
