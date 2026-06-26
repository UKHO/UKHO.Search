# Overview Specification (Queue Clear on Delete Indexes)

## Target output path
- `dev/work-packages/mvp/019-queue-clear/overview.spec.md`

## 1. Purpose

Define the functional and technical requirements for extending the FileShare Emulator UI so that when a user clicks **Delete Indexes**, the system also clears the **ingestion queue**. The operation must not complete until **all** items have been removed from the queue.

## 2. Scope

### In scope
- Update the Blazor page `tools/FileShareEmulator/Components/Pages/Indexing.razor` delete operation to:
  - delete Elasticsearch indexes, and
  - clear the ingestion queue of all items, guaranteeing the queue is empty when the overall operation completes.
- Provide user feedback/progress UI during long-running queue clear and/or index deletion.
- Failure behavior and messaging when queue clearing fails or cannot confirm emptiness.

### Out of scope
- Changing ingestion processing logic beyond enabling a reliable queue clearing operation.
- Changes to Elasticsearch index naming, mappings, or ingestion batch logic.

## 3. High-level system/components

### 3.1 Blazor UI: Indexing page
- Adds/updates a single destructive operation: `Delete Indexes`.
- Displays progress and final outcome.
- Prevents concurrent operations that could re-populate the queue while clearing (where feasible).

### 3.2 Emulator backend services
- Extend or add a service responsible for queue operations:
  - Clear all messages/items.
  - Confirm that the queue is empty.
  - Provide progress information (e.g., items removed, attempts, elapsed).

### 3.3 External dependencies
- Elasticsearch (already used by existing `Delete Indexes` flow).
- Ingestion queue implementation used by the emulator (referenced by existing emulator code).

## 4. Component/service specifications

- Ingestion Queue Clear: `dev/work-packages/mvp/019-queue-clear/queue-clear.spec.md`
- UI Progress & UX: `dev/work-packages/mvp/019-queue-clear/ui-progress.spec.md`
- Implementation Plan: `dev/work-packages/mvp/019-queue-clear/implementation-plan.md`
