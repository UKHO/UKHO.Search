# Specification: Ingestion Queue Clear (Emulator)

## Target output path
- `dev/work-packages/mvp/019-queue-clear/queue-clear.spec.md`

## 1. Background / Problem Statement

The emulator currently allows deleting all Elasticsearch indexes via the **Delete Indexes** button on the `Indexing` page. However, deleting indexes alone is insufficient for resetting emulator state: items may remain in the ingestion queue and will later be processed, re-creating indexes or producing inconsistent behavior.

## 2. Goals

1. When a user triggers **Delete Indexes**, the ingestion queue is cleared as part of the same operation.
2. The operation MUST NOT be considered complete until the queue has been cleared of all items.
3. No items must be left in the queue when returning success to the user.
4. The system provides determinism and safety: if the queue cannot be proven empty, the operation fails and the UI indicates the failure.

## 3. Non-goals

- Adding new projects to the solution (emulator constraint: all changes remain within the existing emulator project).
- Implementing multi-tenant queue semantics.

## 4. Functional Requirements

### 4.1 Triggering
- **FR-1**: The queue clear operation is triggered automatically when the user clicks `Delete Indexes` on the emulator UI.
- **FR-2**: The queue clear operation is required regardless of whether indexes exist.

### 4.2 Clearing semantics
- **FR-3**: The ingestion queue MUST be cleared of all items.
- **FR-4**: The system MUST verify/confirm emptiness before reporting success.
- **FR-5**: If any items remain (or emptiness cannot be confirmed), the overall operation MUST be reported as failed.

### 4.3 Concurrency / interference
- **FR-6**: While `Delete Indexes` is running, other UI operations that could index or enqueue work should be disabled (at minimum: indexing buttons already disabled during delete).
- **FR-7**: If background ingestion workers are running in the emulator, the design MUST ensure they do not re-enqueue or race the clear operation (e.g., by pausing ingestion during delete, or by repeating clear+verify until stable).

### 4.4 Error handling
- **FR-8**: If clearing fails due to connectivity/auth/transport errors, the operation fails and presents an actionable message.
- **FR-9**: Partial success MUST be explicitly communicated (e.g., indexes deleted but queue clear failed).

## 5. Technical Requirements

### 5.1 Service API (proposed)
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

### 5.2 Clearing strategy
The implementation MUST match the queue technology used by the emulator.

Acceptable strategies include:
1. **Purge/clear API** provided by queue provider (preferred).
2. **Drain loop**: receive messages in batches and delete/ack them until empty.

Verification MUST be performed after clearing:
- If provider supports accurate counts, use them.
- Otherwise, verify via repeated receive attempts until no messages are available for a defined stability window.

### 5.3 Completion guarantee
- The `Delete Indexes` UI operation is complete only after:
  1. Elasticsearch indexes delete attempt finishes (success or failure), and
  2. queue clear+verify finishes.

### 5.4 Timeouts and cancellation
- Use a cancellation token controlled by UI lifetime.
- Introduce a maximum overall duration for queue clearing (configurable) to prevent infinite loops.

### 5.5 Observability
- Log key events: start, attempts, counts, end, and failures.

## 6. Security / Safety

- Clear operation is destructive; should be available only in emulator.
- Ensure no accidental clearing of non-emulator queues (e.g., configuration validation: require explicit queue name/prefix).

## 7. Acceptance Criteria

- **AC-1**: Clicking `Delete Indexes` results in the ingestion queue being empty.
- **AC-2**: If queue cannot be emptied or emptiness cannot be verified, UI shows failure and does not show success.
- **AC-3**: Progress is visible during long operations (see `ui-progress.spec.md`).
- **AC-4**: No queue items remain after a reported success.
