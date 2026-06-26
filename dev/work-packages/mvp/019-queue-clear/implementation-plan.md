# Implementation Plan: Delete Indexes must also clear ingestion queue

## Target output path
- `dev/work-packages/mvp/019-queue-clear/implementation-plan.md`

## 1. Approach

1. Identify how the emulator interacts with the ingestion queue today (service/client used, queue name, how messages are enqueued/dequeued).
2. Introduce/extend an emulator service responsible for queue operations:
   - clear/drain all messages
   - verify emptiness
   - return progress metrics
3. Update `tools/FileShareEmulator/Components/Pages/Indexing.razor` to:
   - execute delete indexes
   - clear and verify the queue
   - provide stage-based progress UI

## 2. Work breakdown

### 2.1 Discovery
- Search emulator code for:
  - queue client creation
  - any existing queue admin/purge methods
  - message receiver/processor implementations

### 2.2 Backend changes
- Add `QueueClearResult` model.
- Add `IngestionQueueService` (or add methods on existing queue-related service if already present).
- Implementation details depend on queue technology:
  - If Azure Storage Queues: use `ClearMessagesAsync()` plus verification.
  - If Service Bus: use receiver loop with `ReceiveMessagesAsync` + `CompleteMessageAsync` until empty.

### 2.3 UI changes
- Track progress state fields:
  - `_deleteStage` (string)
  - `_deletedIndexesCount`, `_totalIndexesCount`
  - `_queueRemovedCount` (optional)
- Show progress text/spinner when `_isDeletingIndexes`.
- Ensure final message distinguishes:
  - index deletion result
  - queue clear result

### 2.4 Validation
- Add a test approach:
  - Prefer an end-to-end emulator run with a seeded queue and verify it is empty after `Delete Indexes`.
  - If test harness isn’t available, add an internal diagnostics endpoint/service method to query queue status for manual verification.

## 3. Risks / Decisions

- Queue providers differ in whether they expose accurate counts; verification must be designed accordingly.
- Race conditions if ingestion background worker is running during clear; may require pausing ingestion while deleting/clearing.

## 4. Definition of Done

- Specs implemented.
- UI shows progress.
- Operation reports success only when the queue is verified empty.
- Manual verification steps documented.
