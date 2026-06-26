# Specification: UI Progress & UX (Delete Indexes + Queue Clear)

## Target output path
- `dev/work-packages/mvp/019-queue-clear/ui-progress.spec.md`

## 1. Background

The combined operation (delete Elasticsearch indexes + clear ingestion queue) can be long-running. Users need feedback that work is continuing and must not assume the UI is stuck.

## 2. Goals

- Provide clear progress indication during the operation.
- Communicate partial progress and final outcome.
- Prevent duplicate clicks and conflicting actions.

## 3. Functional Requirements

### 3.1 Progress during operation
- **FR-UI-1**: When the user clicks `Delete Indexes`, the UI MUST show an in-progress state immediately.
- **FR-UI-2**: While in progress:
  - `Delete Indexes` button is disabled.
  - Indexing and reset actions remain disabled (already present) to prevent race conditions.
- **FR-UI-3**: The UI SHOULD show stage-based progress:
  1. "Querying indexes"
  2. "Deleting indexes (n of N)"
  3. "Clearing ingestion queue"
  4. "Verifying queue is empty"

### 3.2 Status messaging
- **FR-UI-4**: On success, the UI MUST show a success message that explicitly states both:
  - indexes deleted (with counts), and
  - ingestion queue cleared and verified empty.
- **FR-UI-5**: On failure, the UI MUST show failure with which stage failed and any available exception details (safe for emulator).

### 3.3 Optional enhancements (nice-to-have)
- **NTH-1**: Show a spinner/progress bar and elapsed time.
- **NTH-2**: Show number of queue items removed (if available).

## 4. Technical Requirements

- Use existing UI component framework (Radzen) to keep consistency.
- Progress can be implemented as:
  - a simple text label updated during stages, or
  - a Radzen progress bar with indeterminate mode.
- Ensure updates are dispatched via `InvokeAsync(StateHasChanged)` when background tasks update progress.

## 5. Acceptance Criteria

- **AC-UI-1**: A user can see that the operation is actively working within 200ms of click.
- **AC-UI-2**: User can see which stage is running.
- **AC-UI-3**: Final message clearly confirms the queue is empty when success is shown.
