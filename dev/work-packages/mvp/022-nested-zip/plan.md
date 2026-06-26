# Implementation Plan

**Target path:** `dev/work-packages/mvp/022-nested-zip/plan.md`

## Nested ZIP extraction (FileShare ingestion)

- [x] Work Item 1: Implement recursive nested ZIP extraction in `BatchContentEnricher` - Completed
  - **Purpose**: Ensure all files inside nested ZIPs are extracted before `IBatchContentHandler` is invoked, preventing missed enrichment.
  - **Acceptance Criteria**:
    - Nested ZIPs within the extracted batch directory are discovered and extracted in-place into a sibling folder named after the ZIP (without extension).
    - Extraction continues until no additional ZIPs are found (supports multi-level nesting).
    - `IBatchContentHandler` receives lists that include files originating from nested ZIPs.
    - Corrupt nested ZIP causes enrichment to fail with an error containing the ZIP path.
    - Logging (if `ILogger` is available) reports extraction actions and totals.
  - **Definition of Done**:
    - Code implemented with safe/idempotent behavior for already-extracted ZIPs.
    - Unit/integration tests added and passing.
    - Solution builds successfully.
    - Can execute end-to-end via existing ingestion/enrichment entrypoint with a nested-zip batch.
  - **Summary**:
    - Added `ExpandNestedZips` to iteratively discover and extract nested `*.zip` files under the extracted batch directory before handler invocation.
    - Extraction is in-place to a sibling folder named after the ZIP without extension, with loop safety (max iterations + extracted ZIP tracking) and idempotent destination handling (skip if destination exists & non-empty).
    - Added contextual logging and error wrapping for nested ZIP failures.
    - Verified via `dotnet build` and existing ingestion test suite.
  - [x] Task 1: Locate existing extraction + handler invocation flow - Completed
    - [x] Step 1: Identify `BatchContentEnricher` implementation and where it extracts the outer ZIP. (Located `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/BatchContentEnricher.cs`.)
    - [x] Step 2: Identify how it enumerates files and passes them to `IBatchContentHandler`. (Enumerates all files under `unzipped` and passes list to each handler.)
    - [x] Step 3: Identify logging and error-handling patterns used in ingestion/enrichment. (Uses `ILogger`, logs unzip/download errors, suppresses handler failures.)
  - [x] Task 2: Add nested ZIP expansion helper - Completed
    - [x] Step 1: Implement an internal/private method (e.g., `ExpandNestedZipsAsync` or `ExpandNestedZips`) that:
      - Enumerates `*.zip` under the extracted root.
      - Extracts each ZIP to `Path.Combine(Path.GetDirectoryName(zip), Path.GetFileNameWithoutExtension(zip))`.
      - Tracks extracted ZIP paths to prevent loops.
      - Uses a max-iteration safeguard (e.g., 25 passes) to avoid infinite expansion.
    - [x] Step 2: Ensure destination directory exists behavior:
      - If directory exists and is non-empty => skip extraction and log warning.
      - If directory exists and empty => proceed.
    - [x] Step 3: Wrap extraction exceptions with ZIP path context.
  - [x] Task 3: Integrate nested ZIP expansion into enrichment flow - Completed
    - [x] Step 1: Invoke nested expansion after outer extraction and before file enumeration/handler calls.
    - [x] Step 2: Ensure final file enumeration includes nested extracted files and (optionally) excludes `.zip` files if current handler expectations require non-zip files only. (Preserved current enumeration of all files; nested files now included.)
    - [x] Step 3: Add/adjust logging around nested extraction start/end and totals.
  - **Files** (expected, confirm in repo):
    - `src/**/BatchContentEnricher*.cs`: add nested ZIP expansion.
    - Potentially `src/**/Zip*.cs` or shared extraction utility if one exists: reuse rather than duplicate.
  - **Work Item Dependencies**: None (assumes `BatchContentEnricher` already exists and outer extraction already works).
  - **Run / Verification Instructions**:
    - Run tests: `dotnet test` (or targeted test project once identified).
    - (Optional manual) Run ingestion worker/host with a sample batch containing nested ZIPs and verify enriched output contains nested files.
  - **User Instructions**:
    - Provide a sample nested-zip batch in the existing local input location used by FileShare ingestion (if applicable) for manual verification.

- [x] Work Item 2: Add automated tests for nested ZIP expansion and handler invocation - Completed
  - **Purpose**: Prevent regressions and verify handlers receive complete file lists.
  - **Acceptance Criteria**:
    - Tests create ZIPs programmatically and validate extraction outputs and handler file list.
    - Coverage includes at least:
      - Single level nesting.
      - Multi-level nesting (ZIP inside ZIP).
      - Directory naming rule (`X.zip` -> `X/`).
      - Corrupt nested ZIP failure message references ZIP path.
  - **Definition of Done**:
    - Tests are deterministic, isolated (temp folders), and clean up after themselves.
    - Tests pass on CI and locally.
  - **Summary**:
    - Added coverage to `test/UKHO.Search.Ingestion.Tests/Enrichment/BatchContentEnricherTests.cs` for:
      - Single nested ZIP extraction (handler receives nested file; asserts directory naming `nested.zip` -> `nested/`).
      - Multi-level nested ZIP extraction (zip within zip; handler receives deepest file).
      - Corrupt nested ZIP handling (throws and message includes nested ZIP path).
    - Verified via `dotnet test` (UKHO.Search.Ingestion.Tests).
  - [x] Task 1: Identify the existing test project(s) for ingestion/enrichment - Completed
    - [x] Step 1: Find the closest existing test project for `BatchContentEnricher`. (Used `UKHO.Search.Ingestion.Tests`.)
    - [x] Step 2: Follow existing test patterns (xUnit/NUnit, assertions, temp filesystem helpers). (xUnit + Shouldly + in-memory zip creation.)
  - [x] Task 2: Implement a test handler to capture file lists - Completed
    - [x] Step 1: Add a fake `IBatchContentHandler` that records the paths it was called with. (Reused existing `RecordingHandler`.)
    - [x] Step 2: Use it in `BatchContentEnricher` setup/DI in tests.
  - [x] Task 3: Implement nested ZIP tests - Completed
    - [x] Step 1: Build an outer ZIP in temp storage that includes a nested ZIP file.
    - [x] Step 2: Invoke `BatchContentEnricher` and assert:
      - Extraction folders created correctly.
      - Handler received file list containing nested files.
    - [x] Step 3: Repeat for double nesting.
    - [x] Step 4: Create a corrupt ZIP (write random bytes with `.zip` extension) and assert the thrown exception includes the path.
  - **Files** (expected, confirm in repo):
    - `test/**/BatchContentEnricherTests.cs`: new/updated tests.
    - Any shared test helpers (temp directories, zip creation helpers) in existing test utility locations.
  - **Work Item Dependencies**:
    - Depends on Work Item 1 (implementation to test).
  - **Run / Verification Instructions**:
    - `dotnet test`

## Summary / Key Considerations

- Implement nested ZIP expansion as an iterative "discover + extract" loop to handle arbitrary nesting depth.
- Extract in-place into sibling folders named after ZIPs to preserve directory context.
- Avoid overwriting existing extraction outputs; skip with warning where appropriate.
- Tests should use `System.IO.Compression.ZipArchive` and temp directories to validate structure and handler invocation.
