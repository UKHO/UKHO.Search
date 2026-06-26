# Combined Specification: Queue Clear on Delete Indexes

## Target output path
- `dev/work-packages/mvp/019-queue-clear/queue-clear.combined.spec.md`

---

## 1. Overview Specification (Queue Clear on Delete Indexes)

### 1.1 Purpose

Define the functional and technical requirements for extending the FileShare Emulator UI so that the existing destructive workflow is split into two explicit actions:

1. **Clear Queue** (clears the ingestion queue)
2. **Delete Indexes** (deletes Elasticsearch indexes)

Each operation must not complete until it has fully satisfied its respective completion criteria. In particular, the **Clear Queue** operation must not complete until **all** items have been removed from the ingestion queue.

### 1.2 Scope

#### In scope
- Update the Blazor page `tools/FileShareEmulator/Components/Pages/Indexing.razor` to provide two separate buttons/operations:
  - **Clear Queue**: clears the ingestion queue of all items, guaranteeing the queue is empty when the operation completes.
  - **Delete Indexes**: deletes Elasticsearch indexes.
- Provide user feedback/progress UI during long-running queue clear and/or index deletion.
- Failure behavior and messaging when queue clearing fails or cannot confirm emptiness.

#### Out of scope
- Changing ingestion processing logic beyond enabling a reliable queue clearing operation.
- Changes to Elasticsearch index naming, mappings, or ingestion batch logic.

### 1.3 High-level system/components

#### 1.3.1 Blazor UI: Indexing page
- Adds/updates two destructive operations:
  - `Clear Queue`
  - `Delete Indexes`
- Displays progress and final outcome for each operation.
- Prevents concurrent operations that could re-populate the queue while clearing (where feasible).

#### 1.3.2 Emulator backend services
- Extend or add a service responsible for queue operations:
  - Clear all messages/items.
  - Confirm that the queue is empty.
  - Provide progress information (e.g., items removed, attempts, elapsed).

#### 1.3.3 External dependencies
- Elasticsearch (already used by existing `Delete Indexes` flow).
- Ingestion queue implementation used by the emulator (referenced by existing emulator code).

---

## 2. Specification: Ingestion Queue Clear (Emulator)

### 2.1 Background / Problem Statement

The emulator currently allows deleting all Elasticsearch indexes via the **Delete Indexes** button on the `Indexing` page. However, deleting indexes alone is insufficient for resetting emulator state: items may remain in the ingestion queue and will later be processed, re-creating indexes or producing inconsistent behavior.

To make the user intent explicit (and reduce accidental destructive actions), the UI will be changed to provide a separate **Clear Queue** operation.

### 2.2 Goals

1. When a user triggers **Clear Queue**, the ingestion queue is cleared as part of that operation.
2. The **Clear Queue** operation MUST NOT be considered complete until the queue has been cleared of all items.
3. No items must be left in the queue when returning success for **Clear Queue**.
4. The system provides determinism and safety: if the queue cannot be proven empty, the **Clear Queue** operation fails and the UI indicates the failure.

### 2.3 Non-goals

- Adding new projects to the solution (emulator constraint: all changes remain within the existing emulator project).
- Implementing multi-tenant queue semantics.

### 2.4 Functional Requirements

#### 2.4.1 Triggering
- **FR-1**: The queue clear operation is triggered when the user clicks `Clear Queue` on the emulator UI.
- **FR-2**: The queue clear operation is independent of Elasticsearch index state.

#### 2.4.2 Clearing semantics
- **FR-3**: The ingestion queue MUST be cleared of all items.
- **FR-3a**: The ingestion poison/dead-letter queue MUST also be cleared of all items.
- **FR-4**: The system MUST verify/confirm emptiness before reporting success.
- **FR-5**: If any items remain (or emptiness cannot be confirmed), the overall operation MUST be reported as failed.

#### 2.4.3 Concurrency / interference
- **FR-6**: While `Clear Queue` is running, other UI operations that could index or enqueue work should be disabled.
- **FR-7**: While `Delete Indexes` is running, other UI operations that could index or enqueue work should be disabled.
- **FR-8**: If background ingestion workers are running in the emulator, the design MUST ensure they do not re-enqueue or race the clear operation (e.g., by pausing ingestion during `Clear Queue`, or by repeating clear+verify until stable).

#### 2.4.4 Error handling
- **FR-9**: If clearing fails due to connectivity/auth/transport errors, the operation fails and presents an actionable message.
- **FR-10**: **Clear Queue** and **Delete Indexes** MUST report success/failure independently.

### 2.5 Technical Requirements

#### 2.5.1 Service API (proposed)

- Create or extend a queue service in the emulator, e.g. `IngestionQueueService`, with operations:
  - `Task<QueueClearResult> ClearAllAsync(CancellationToken ct)`
  - `Task<QueueStatus> GetStatusAsync(CancellationToken ct)` (or equivalent to check remaining count)

`QueueClearResult` should include:
- `bool Succeeded`
- `int? ItemsRemoved` (if available)
- `int Attempts`
- `TimeSpan Duration`
- `string? FailureReason`

`QueueStatus` should include:
- `int? ApproximateMessageCount` (or a boolean `IsEmpty` when count accuracy is not available)

#### 2.5.2 Clearing strategy

The implementation MUST match the queue technology used by the emulator.

Acceptable strategies include:
1. **Purge/clear API** provided by queue provider (preferred).
2. **Drain loop**: receive messages in batches and delete/ack them until empty.

Verification MUST be performed after clearing:
- If provider supports accurate counts, use them.
- Otherwise, verify via repeated receive attempts until no messages are available for a defined stability window.

#### 2.5.3 Completion guarantee

- The `Clear Queue` operation is complete only after queue clear+verify finishes.
- The `Delete Indexes` operation is complete only after the Elasticsearch indexes delete attempt finishes.

#### 2.5.4 Timeouts and cancellation

- Use a cancellation token controlled by UI lifetime.
- Introduce a maximum overall duration for queue clearing (configurable) to prevent infinite loops.

**Implementation note (current)**:
- The emulator implementation uses a linked cancellation token from the Blazor component lifetime, plus a maximum clear duration and a separate verification timeout.
- Emptiness verification uses a stability window (multiple consecutive empty receives) to reduce race conditions and visibility-delay issues.

#### 2.5.5 Observability

- Log key events: start, attempts, counts, end, and failures.

### 2.6 Security / Safety

- Clear operation is destructive; should be available only in emulator.
- Ensure no accidental clearing of non-emulator queues (e.g., configuration validation: require explicit queue name/prefix).

**Poison/dead-letter queue naming**:
- The ingestion pipeline uses a poison queue suffix (configuration key `ingestion:poisonQueueSuffix`, default `-poison`).
- `Clear Queue` must clear both `file-share-queue` and `file-share-queue{suffix}`.

### 2.7 Acceptance Criteria

- **AC-1**: Clicking `Clear Queue` results in the ingestion queue being empty.
- **AC-2**: If queue cannot be emptied or emptiness cannot be verified, UI shows failure for `Clear Queue` and does not show success.
- **AC-3**: Progress is visible during long operations (see UI progress section).
- **AC-4**: No queue items remain after a reported `Clear Queue` success.

---

## 3. Specification: UI Progress & UX (Delete Indexes + Queue Clear)

### 3.1 Background

The destructive operations (delete Elasticsearch indexes and clearing the ingestion queue) can be long-running. Users need feedback that work is continuing and must not assume the UI is stuck.

### 3.2 Goals

- Provide clear progress indication during the operation.
- Communicate partial progress and final outcome.
- Prevent duplicate clicks and conflicting actions.

### 3.3 Functional Requirements

#### 3.3.1 Progress during operation

- **FR-UI-1**: When the user clicks `Clear Queue` or `Delete Indexes`, the UI MUST show an in-progress state immediately for the initiated operation.
- **FR-UI-2**: While `Clear Queue` is in progress, the `Clear Queue` button is disabled.
- **FR-UI-3**: While `Delete Indexes` is in progress, the `Delete Indexes` button is disabled.
- **FR-UI-4**: While either destructive operation is in progress, indexing and reset actions remain disabled to prevent race conditions.
- **FR-UI-5**: The UI SHOULD show stage-based progress.
  - For `Delete Indexes`:
    1. "Querying indexes"
    2. "Deleting indexes (n of N)"
  - For `Clear Queue`:
    1. "Clearing ingestion queue"
    2. "Verifying queue is empty"

#### 3.3.2 Status messaging

- **FR-UI-6**: On `Delete Indexes` success, the UI MUST show a success message with index counts (where available).
- **FR-UI-7**: On `Clear Queue` success, the UI MUST show a success message that explicitly states the ingestion queue was cleared and verified empty.
- **FR-UI-8**: On failure, the UI MUST show failure with which stage failed and any available exception details (safe for emulator).

#### 3.3.3 Optional enhancements (nice-to-have)

- **NTH-1**: Show a spinner/progress bar and elapsed time.
- **NTH-2**: Show number of queue items removed (if available).

### 3.4 Technical Requirements

- Use existing UI component framework (Radzen) to keep consistency.
- Progress can be implemented as:
  - a simple text label updated during stages, or
  - a Radzen progress bar with indeterminate mode.
- Ensure updates are dispatched via `InvokeAsync(StateHasChanged)` when background tasks update progress.

### 3.5 Acceptance Criteria

- **AC-UI-1**: A user can see that the operation is actively working within 200ms of click.
- **AC-UI-2**: User can see which stage is running.
- **AC-UI-3**: Final message clearly confirms the queue is empty when success is shown.

---

## 4. Implementation Plan: Delete Indexes must also clear ingestion queue

### 4.1 Approach

1. Identify how the emulator interacts with the ingestion queue today (service/client used, queue name, how messages are enqueued/dequeued).
2. Introduce/extend an emulator service responsible for queue operations:
   - clear/drain all messages
   - verify emptiness
   - return progress metrics
3. Update `tools/FileShareEmulator/Components/Pages/Indexing.razor` to provide two independent destructive actions:
   - `Clear Queue`: clear and verify the queue
   - `Delete Indexes`: delete Elasticsearch indexes
   - provide stage-based progress UI per operation

### 4.2 Work breakdown

#### 4.2.1 Discovery
- Search emulator code for:
  - queue client creation
  - any existing queue admin/purge methods
  - message receiver/processor implementations

#### 4.2.2 Backend changes
- Add `QueueClearResult` model.
- Add `IngestionQueueService` (or add methods on existing queue-related service if already present).
- Implementation details depend on queue technology:
  - If Azure Storage Queues: use `ClearMessagesAsync()` plus verification.
  - If Service Bus: use receiver loop with `ReceiveMessagesAsync` + `CompleteMessageAsync` until empty.

#### 4.2.3 UI changes
- Track progress state fields:
  - `_deleteStage` (string)
  - `_deletedIndexesCount`, `_totalIndexesCount`
  - `_queueRemovedCount` (optional)
- Show progress text/spinner when `_isDeletingIndexes`.
- Ensure final message distinguishes:
  - index deletion result
  - queue clear result

#### 4.2.4 Validation
- Add a test approach:
  - Prefer an end-to-end emulator run with a seeded queue and verify it is empty after `Delete Indexes`.
  - If test harness isn’t available, add an internal diagnostics endpoint/service method to query queue status for manual verification.

### 4.3 Risks / Decisions

- Queue providers differ in whether they expose accurate counts; verification must be designed accordingly.
- Race conditions if ingestion background worker is running during clear; may require pausing ingestion while deleting/clearing.

### 4.4 Definition of Done

- Specs implemented.
- UI shows progress.
- Operation reports success only when the queue is verified empty.
- Manual verification steps documented.
