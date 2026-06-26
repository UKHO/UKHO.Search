# Specification: Index a Specific Batch by ID (FileShare Emulator UI)

**Document type:** Single-document functional + technical specification  
**Work package:** `021-specific-batch`  
**Target path:** `dev/work-packages/mvp/021-specific-batch/spec.md`

## 1. Overview

### 1.1 Purpose
Enhance the FileShare Emulator Blazor UI page `Indexing.razor` to allow an operator/developer to submit a *specific* batch (by batch ID) to the ingestion queue. This is intended for debugging and targeted testing scenarios.

### 1.2 Scope
In scope:
- UI additions to `tools/FileShareEmulator/Components/Pages/Indexing.razor`:
  - A text box for entering a batch ID.
  - A button labeled **Index batch**.
  - Status/error messaging for the submission attempt.
- Backend interaction from the page to:
  - Validate that the batch exists.
  - Enqueue the specified batch for ingestion.

Out of scope:
- Changing ingestion pipeline behavior.
- Changing batch creation or batch discovery mechanisms beyond what is required to find a batch by ID.
- Adding new projects (emulator constraint).

### 1.3 Users and scenarios
Primary user: Developer / tester using the emulator.

Scenarios:
1. Developer has a known batch ID (from logs or previous runs) and wants to re-run ingestion for that exact batch.
2. Developer enters an invalid/non-existent batch ID and needs clear feedback.
3. Developer encounters transient queue submission errors and needs clear feedback.

### 1.4 Assumptions
- The emulator already has a concept of “batch” and can submit the next batch to the ingestion queue.
- The emulator can locate batch metadata/content locally (or via existing emulator services).
- The ingestion submission mechanism is already implemented somewhere for the “Index next batch” functionality and can be reused.

### 1.5 Constraints
- All emulator changes must remain within the existing emulator project (no new projects).
- UI is Blazor.

## 2. Functional requirements

### 2.1 UI placement and copy
Add a new section **under** the existing “Index next batch” section in `Indexing.razor`.

The section must include:
- A short description, for example:
  - “Use this for debugging. Enter a batch ID to re-submit that batch to the ingestion queue.”
- A text input labeled (or with placeholder) indicating it expects a **Batch ID**.
- A button labeled **Index batch**.

### 2.2 Button enablement rules
- The **Index batch** button is enabled **only** when the textbox contains text.
  - Whitespace-only input must be treated as empty.
- When the textbox is empty (or whitespace), the button is disabled.

### 2.3 Submission behavior
When **Index batch** is clicked:
1. Read the value from the textbox as the batch ID.
2. Validate the batch exists.
3. If it exists, submit/enqueue it to the ingestion queue using the same mechanism as “Index next batch” (or equivalent existing queue submission).

### 2.4 Success messaging
- If the batch is successfully submitted to the ingestion queue, a success message must be displayed.
- The success message must be clearly associated with this action (not the “index next batch” action).

Suggested success message:
- “Batch `{batchId}` submitted to ingestion queue.”

### 2.5 Error messaging
- If the batch ID does not correspond to a known batch (incorrect/missing), display an error message.
- If any other error occurs while submitting to the queue, display an error message.

Examples:
- Not found: “Batch `{batchId}` not found.”
- Submit failure: “Failed to submit batch `{batchId}` to ingestion queue: {errorDetails}”

### 2.6 Usability and behavior details
- Clicking **Index batch** should clear any prior success/error message for this action before starting, then set the final outcome message.
- The textbox value should remain after submission (to allow repeated clicks for debugging) unless there is an established UX pattern on the page to clear inputs.

## 3. Technical requirements

### 3.1 Blazor implementation details
- File to modify: `tools/FileShareEmulator/Components/Pages/Indexing.razor`.
- Use Blazor data binding for the textbox:
  - `@bind-Value` or `@bind` (depending on existing component usage).
- Button enablement must be driven by a computed condition (e.g., `string.IsNullOrWhiteSpace` check).

### 3.2 Batch retrieval / validation
- Implement (or reuse) a method/service that can retrieve batch details by batch ID.
- If no batch is found, the UI must show the “not found” error and **must not** attempt queue submission.

### 3.3 Queue submission
- Reuse the existing ingestion queue publishing mechanism currently used by “Index next batch”.
- Submit the chosen batch in the same payload shape as used for normal operation (so ingestion path is identical).

### 3.4 Error handling
- Catch and handle exceptions from:
  - batch lookup/retrieval
  - queue submission
- UI must display a friendly error message; exception details should not leak sensitive information.

### 3.5 Logging
- If the emulator has logging via `ILogger`, log:
  - Info on successful submission (include batch ID)
  - Warning when not found
  - Error on submission failures (include batch ID, exception)

### 3.6 Non-functional
- No noticeable page performance impact.
- No changes to hosting/deployment.

## 4. UI acceptance criteria

1. **Index batch section appears** under “Index next batch” in `Indexing.razor`.
2. **Index batch button disabled** when the textbox is empty or whitespace.
3. **Index batch button enabled** when the textbox contains non-whitespace text.
4. **When valid batch ID is entered and submitted:**
   - The batch is enqueued
   - A success message is shown
5. **When invalid batch ID is entered:**
   - No queue submission is attempted
   - “Batch not found” error is shown
6. **When queue submission fails:**
   - An error message is shown indicating submission failed

## 5. Test approach (repository-aligned)

- Prefer Playwright end-to-end tests for UI behavior if existing test infrastructure covers the emulator UI.
- Minimum verification:
  - Button enablement toggles based on input
  - Success message shown on valid submission
  - Error message shown when ID not found

## 6. Open questions / decisions

1. Batch ID format: Is it always a GUID, or can it be any string?
2. Where is the authoritative batch store for the emulator (filesystem folder, in-memory registry, etc.)?
3. Should batch lookup be case-sensitive?
4. Should we trim input before lookup (recommended: yes)?
