# Implementation Plan (Uplift)

**Target output path:** `docs/006-ingestion-service/plans/backend/plan-ingestion-pipeline-uplift_v0.02.md`

**Based on:** `docs/006-ingestion-service/006-ingestion-service.spec.md` (v0.01 + clarifications)

**Related existing plan:** `docs/006-ingestion-service/plans/backend/plan-ingestion-pipeline_v0.01.md`

## Purpose
Uplift the implementation to match the clarified requirements added to the spec:

1. **Queue lifecycle**: one queue per `IIngestionDataProviderFactory`, and both provider + poison queues are **created if not exists at startup**.
2. **Project boundaries**: keep queue/client wiring in `UKHO.Search.Infrastructure.Ingestion`, and ensure any **file-share-specific parsing/extraction/enrichment pipeline nodes** live in `UKHO.Search.Ingestion.Providers.FileShare`.

This uplift includes **refactoring/moving existing code** so the solution structure matches the updated spec guidance (i.e., don’t just add new types in the “right” place—move existing source-specific code that currently lives elsewhere).

This plan is intentionally incremental: each Work Item produces a runnable end-to-end flow using the Aspire stack + FileShareEmulator.

---

## Slice A — Queue create-if-not-exists at startup (provider + poison)

- [x] **Work Item 1: Ingestion source ensures provider and poison queues exist at startup** - Completed
  - **Purpose**: Align runtime behavior with spec §6.1 and remove reliance on upstream producers creating queues.
  - **Acceptance Criteria**:
    - For each registered `IIngestionDataProviderFactory`, the ingestion service ensures:
      - `factory.QueueName` exists (create-if-not-exists)
      - `<factory.QueueName><ingestion:poisonQueueSuffix>` exists (create-if-not-exists)
    - Queue creation happens once per process start, before polling begins.
    - Works when only `IngestionServiceHost` is started (i.e., even if FileShareEmulator is not running yet).
  - **Definition of Done**:
    - `IQueueClient` supports a create-if-not-exists operation.
    - `AzureQueueClient` implements create-if-not-exists via the Azure SDK.
    - `IngestionSourceNode` calls create-if-not-exists for provider + poison queue before entering the polling loop.
    - Unit tests cover queue creation calls and poison suffix handling.
    - `dotnet test` passes.
  - [x] Task 1.1: Extend queue abstraction with create-if-not-exists - Completed
    - [x] Step: Add `ValueTask CreateIfNotExistsAsync(CancellationToken cancellationToken)` to `src/UKHO.Search.Infrastructure.Ingestion/Queue/IQueueClient.cs`.
    - [x] Step: Implement in `src/UKHO.Search.Infrastructure.Ingestion/Queue/AzureQueueClient.cs` using underlying `QueueClient.CreateIfNotExistsAsync`.
    - [x] Step: Update any fakes used in tests (`test/UKHO.Search.Ingestion.Tests/TestQueues/FakeQueueClient.cs`) to implement the new member and record calls.
  - [x] Task 1.2: Ensure queues exist during provider poller startup - Completed
    - [x] Step: In `src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs`, before the poll loop:
      - call `queue.CreateIfNotExistsAsync(...)`
      - call `poisonQueue.CreateIfNotExistsAsync(...)`
    - [x] Step: Add log entries indicating queue ensure actions (include `ProviderName`, `QueueName`, `PoisonQueueName`).
  - [x] Task 1.3: Add/adjust tests - Completed
    - [x] Step: Update `test/UKHO.Search.Ingestion.Tests/Queue/IngestionSourceNodeQueueTests.cs` to assert create-if-not-exists is called for provider + poison queues.
    - [x] Step: Add a test that validates poison queue naming uses `ingestion:poisonQueueSuffix` (default `-poison`).
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Queue/IQueueClient.cs`: add `CreateIfNotExistsAsync`.
    - `src/UKHO.Search.Infrastructure.Ingestion/Queue/AzureQueueClient.cs`: implement `CreateIfNotExistsAsync`.
    - `src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs`: ensure queues exist before polling.
    - `test/UKHO.Search.Ingestion.Tests/TestQueues/FakeQueueClient.cs`: implement new method + record calls.
    - `test/UKHO.Search.Ingestion.Tests/Queue/IngestionSourceNodeQueueTests.cs`: new assertions.
  - **Work Item Dependencies**: none
  - **Run / Verification Instructions**:
    - `dotnet test`
    - `dotnet run --project src/Hosts/AppHost` → confirm `IngestionServiceHost` starts without queue-not-found errors.

  - **Work Item 1 Summary**:
    - Added `CreateIfNotExistsAsync` to `IQueueClient` and implemented it in `AzureQueueClient`.
    - Updated `IngestionSourceNode` to ensure both provider and poison queues exist before polling.
    - Added structured logging when ensuring provider/poison queues exist.
    - Updated test fakes and added tests verifying startup creation + poison queue suffix naming.

---

## Slice B — Prepare FileShare provider project for source-specific pipeline nodes

- [x] **Work Item 2: Establish FileShare-specific pipeline node extension points and project layout** - Completed
  - **Purpose**: Align code organization with spec §3.5 so future file-share parsing/extraction/enrichment nodes live in `UKHO.Search.Ingestion.Providers.FileShare`.
  - **Acceptance Criteria**:
    - FileShare provider project contains a dedicated location for file-share-specific pipeline nodes.
    - Existing code that is **file-share-source-specific** is refactored/moved out of `UKHO.Search.Infrastructure.Ingestion` into `UKHO.Search.Ingestion.Providers.FileShare` (or behind an interface) so that Infrastructure does not “own” file-share semantics.
    - At least one minimal “no-op” file-share-specific node/interface exists to demonstrate where future enrichment will go.
    - Infrastructure code does not take dependencies on FileShare-specific types/details beyond stable abstractions.
  - **Definition of Done**:
    - Existing code is moved/refactored in accordance with the spec’s project boundary guidance without breaking build.
    - New types are added under `src/UKHO.Search.Ingestion.Providers.FileShare` without breaking build.
    - `dotnet build` and `dotnet test` pass.
  - [x] Task 2.1: Audit and move/refactor existing FileShare-specific logic - Completed
    - [x] Step: Identify existing types in `src/UKHO.Search.Infrastructure.Ingestion` that are expected to become file-share-specific over time (e.g., canonical mapping/enrichment logic tied to file-share domain knowledge).
    - [x] Step: Move those types into `src/UKHO.Search.Ingestion.Providers.FileShare` **or** extract a stable abstraction (interface) in an inner-layer project and keep only the implementation in the FileShare provider project.
    - [x] Step: Update DI registrations and consuming nodes to depend on abstractions, not concrete FileShare types.
    - [x] Step: Update/relocate tests so they continue to validate behavior after the refactor.
  - [x] Task 2.2: Create provider-local extension points for future extraction/enrichment - Completed
    - [x] Step: Add a small interface (e.g., `IFileShareContentEnricher` or `IFileShareBatchParser`) and a default no-op implementation in `src/UKHO.Search.Ingestion.Providers.FileShare`.
  - [x] Task 2.3: (Optional) Wire the no-op extension point into the per-lane pipeline - Completed (not required for v0.02)
    - [x] Step: If enrichment needs to run in-process in the pipeline, introduce a node boundary that can call the interface (node remains in Infrastructure; implementation remains in provider project).
    - [x] Step: Ensure this step preserves ordering/backpressure semantics.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/*`: new interfaces / no-op implementations.
    - `src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs`: updated DI registration after moving/refactoring.
    - (Expected) moved/refactored files currently under `src/UKHO.Search.Infrastructure.Ingestion/*` that are file-share-specific.
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - `dotnet test`

  - **Work Item 2 Summary**:
    - Audited `src/UKHO.Search.Infrastructure.Ingestion` for file-share-source-specific nodes; none required moving in v0.02 (current pipeline nodes remain source-agnostic).
    - Added a provider-local extension point `IFileShareIngestionEnricher` with a default `NoOpFileShareIngestionEnricher` under `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/`.
    - Registered the default no-op implementation via `src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs`.

---

## Summary (approach)

- First, uplift the queue abstraction so `IngestionSourceNode` can ensure queues exist at startup, matching the clarified spec requirement.
- Next, add explicit “home” structures in the FileShare provider project for file-share-specific pipeline evolution (parsing/extraction/enrichment), without moving existing infrastructure wiring.
- Keep each work item independently runnable via the Aspire `AppHost` + FileShareEmulator.
