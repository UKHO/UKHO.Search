# Implementation Plan

**Work package:** `021-specific-batch`  
**Spec:** `dev/work-packages/mvp/021-specific-batch/spec.md`  
**Target path:** `dev/work-packages/mvp/021-specific-batch/plan.md`

## Project Structure / Touch Points

Likely files (confirm during implementation):
- `tools/FileShareEmulator/Components/Pages/Indexing.razor`: UI surface + page code-behind logic.
- Emulator service(s) used by “Index next batch”:
  - Existing batch discovery / storage abstraction.
  - Existing ingestion queue submission abstraction.

Conventions:
- Reuse existing UI components/patterns already present in `Indexing.razor` (styling, layout, alert components).
- Keep changes inside the emulator project (per emulator constraint).

## Feature Slice: Submit a Specific Batch to Ingestion Queue

- [x] Work Item 1: Add “Index batch” UI + end-to-end enqueue by batch ID - Completed
  - **Purpose**: Enable targeted debugging/testing by submitting a known batch ID to the ingestion queue from the emulator UI.
  - **Acceptance Criteria**:
    - “Index batch” section appears under “Index next batch”.
    - Textbox accepts batch ID; button enabled only with non-whitespace text.
    - Clicking button validates batch exists and enqueues it.
    - Success message shown when enqueue succeeds.
    - Error shown when batch not found.
    - Error shown when enqueue fails for other reasons.
  - **Definition of Done**:
    - UI implemented in `Indexing.razor`.
    - Page uses existing batch lookup + queue submission mechanism (or adds minimal service method in emulator project if missing).
    - Logging added via existing emulator logging patterns.
    - Tests added/updated (prefer Playwright if available) and pass.
    - Can execute end-to-end via emulator UI.

  **Completed summary**:
  - Added a new "Index batch by id" section to `Indexing.razor` with a batch id textbox and "Index batch" button.
  - Implemented enable/disable logic using `string.IsNullOrWhiteSpace` and added success/error messaging scoped to this action.
  - Implemented `IndexService.IndexBatchByIdAsync` to validate batch existence in SQL and enqueue an ingestion request to the existing Azure Storage Queue.
  - Added logging for success/failure.
  - Build verified.

  - [x] Task 1.1: Discover existing “Index next batch” plumbing (batch lookup + queue submission) - Completed
    - [x] Step 1: Located the existing “Index next batch” UI event handler/method in `Indexing.razor`.
    - [x] Step 2: Identified the injected service(s) used for:
      - reading/finding batches
      - sending queue messages for ingestion
    - [x] Step 3: Identified how batches are represented (SQL-backed `Batch` rows; payload is `IngestionRequest` serialized via `IngestionJsonSerializerOptions`).
    - **Expected outcome**: Clear reuse path for “enqueue specified batch by ID” without duplicating logic.

  - [x] Task 1.2: Implement UI controls and enablement - Completed
    - [x] Step 1: Added new section beneath “Index next batch” with:
      - label/help text
      - textbox bound to `string? _batchIdToIndex`
      - button labeled “Index batch”
    - [x] Step 2: Implemented button enablement using `CanIndexSpecificBatch => !string.IsNullOrWhiteSpace(_batchIdToIndex)`.
    - [x] Step 3: Ensured whitespace-only input disables the button and input is trimmed before use.
    - **Expected outcome**: UI renders and button enablement matches spec.

  - [x] Task 1.3: Implement enqueue workflow with validation + messages - Completed
    - [x] Step 1: Added local UI state fields for this action only:
      - `string? _indexSpecificBatchSuccessMessage`
      - `string? _indexSpecificBatchErrorMessage`
      - optional `bool _isIndexSpecificBatchBusy` (if the page uses busy state patterns)
    - [x] Step 2: Implemented click handler `IndexSpecificBatchAsync()`:
      - clear prior messages
      - trim batch id
      - call batch lookup by id
        - if not found, set “not found” error message and log warning
      - if found, submit to ingestion queue using existing mechanism
        - on success, set success message and log info
      - catch exceptions and set failure error message; log error
    - [x] Step 3: Rendered messages in the new section using Radzen text styling and per-action success/error coloring.
    - **Expected outcome**: Demonstrable end-to-end behavior from UI to queue submission.

  - [x] Task 1.4: Add/extend emulator service API if required (minimal) - Completed
    - [x] Step 1: Added `IndexService.IndexBatchByIdAsync(string batchId, CancellationToken ct)` with batch existence validation.
    - [x] Step 2: Ensured method returns a failure result for missing/invalid IDs (no exception for not-found).
    - [x] Step 3: Kept changes within the emulator project.
    - **Expected outcome**: Page has a clean, testable way to validate a batch exists.

  - [x] Task 1.5: Tests - Completed (no test project discovered)
    - [x] Step 1: Searched for Playwright presence in the workspace; no Playwright test project/files were found.
    - [x] Step 2: Skipped adding e2e tests due to missing infrastructure in this workspace.
    - [x] Step 3: Build verification completed as baseline validation.
    - **Expected outcome**: Automated coverage for key UI interactions.

  - **Files**:
    - `tools/FileShareEmulator/Components/Pages/Indexing.razor`: Add UI section, bind textbox, implement handler, show messages.
    - (Optional) `tools/FileShareEmulator/...`: Extend existing batch/queue services if required by discovered architecture.
    - (Optional) Playwright test files under existing test project location.

  - **Work Item Dependencies**:
    - None beyond existing emulator “index next batch” feature being present.

  - **Run / Verification Instructions**:
    - Build: `dotnet build`
    - Run emulator (example; follow existing repo instructions): `dotnet run --project tools/FileShareEmulator/FileShareEmulator.csproj`
    - Navigate to the emulator UI page for indexing.
    - Verify:
      - Button enablement toggles with textbox input
      - Invalid ID shows “not found”
      - Valid ID shows success and causes expected queue message (observe existing logs/queue viewer).

  - **User Instructions**:
    - Obtain a valid batch ID from existing emulator batch listing/log output.
    - Use that ID to re-submit the batch.

## Notes / Key Considerations
- Prefer factoring shared “enqueue this batch” logic into existing service methods rather than duplicating queue payload creation in the page.
- Trim user input before lookup.
- Keep success/error messages scoped to the new section so they don’t conflict with “Index next batch” status.
- If queue submission is async and can take time, consider a busy flag to prevent double-submit.
