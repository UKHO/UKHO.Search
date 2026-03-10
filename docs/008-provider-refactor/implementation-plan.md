# Implementation Plan

Work Package folder: `docs/008-provider-refactor/`

This plan implements `docs/008-provider-refactor/spec-provider-refactor-uplift_v0.01.md` using vertical slices. Each Work Item ends in a runnable ingestion host with queue polling owned by Infrastructure and request processing owned by the provider.

## Overall project structure / placement
- Domain contract updates live in `src/UKHO.Search.Ingestion/`.
- FileShare provider pipeline composition + provider entrypoint live in `src/UKHO.Search.Ingestion.Providers.FileShare/`.
- Queue polling, queue lifecycle, poison handling, and host wiring remain in `src/UKHO.Search.Infrastructure.Ingestion/`.
- Host remains the single DI entrypoint; Infrastructure wires queue host + provider registrations.

---

## Slice 1 — Provider entrypoint contract + FileShare provider enqueuing (no queue-flow switch yet)

- [x] Work Item 1: Add `ProcessIngestionRequestAsync` to `IIngestionDataProvider` and implement in FileShare provider (enqueue-only semantics) - Completed
  - **Purpose**: Introduce the provider-rooted processing entrypoint contract (accept/enqueue only) while keeping the system runnable with existing flow.
  - **Acceptance Criteria**:
    - `IIngestionDataProvider` includes `ValueTask ProcessIngestionRequestAsync(Envelope<IngestionRequest> envelope, CancellationToken cancellationToken = default)`.
    - FileShare provider implements the method and returns once the request is accepted/enqueued.
    - Envelope context (including queue acker) is preserved (not copied/stripped) when passing into provider processing.
    - Unit tests cover:
      - method returns after enqueue
      - cancellation/backpressure behavior (bounded channel full)
  - **Definition of Done**:
    - Code implemented + unit tests passing
    - Logging added for enqueue/acceptance (provider name, key, message id)
    - Documentation updated (this plan)
    - Can execute end-to-end via existing ingestion host startup (no behavior change expected yet)
  - [x] Task 1: Extend domain contract - Completed
    - [x] Step 1: Update `src/UKHO.Search.Ingestion/Providers/IIngestionDataProvider.cs` to add `ProcessIngestionRequestAsync(...)`. - Completed
    - [x] Step 2: Update any existing implementations to compile. - Completed
  - [x] Task 2: Provider-side enqueue plumbing - Completed
    - [x] Step 1: Add a provider-owned ingress channel abstraction (bounded channel) for `Envelope<IngestionRequest>`. - Completed
    - [x] Step 2: Implement `ProcessIngestionRequestAsync(...)` by writing the provided envelope into the ingress channel and returning when accepted. - Completed
  - [x] Task 3: Tests - Completed
    - [x] Step 1: Add unit tests in `test/UKHO.Search.Ingestion.Tests/` covering enqueue-only semantics, backpressure, and cancellation. - Completed
  - **Summary**:
    - Added `ProcessIngestionRequestAsync(...)` to `IIngestionDataProvider`.
    - Implemented enqueue-only semantics in `FileShareIngestionDataProvider` using a bounded ingress channel and structured logging.
    - Added unit tests verifying enqueue-only behavior, backpressure, and cancellation.
  - **Files**:
    - `src/UKHO.Search.Ingestion/Providers/IIngestionDataProvider.cs`: add `ProcessIngestionRequestAsync(...)`.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/FileShareIngestionDataProvider.cs`: implement enqueue-only entrypoint.
    - `test/UKHO.Search.Ingestion.Tests/*`: new/updated tests.
  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
    - Optional: `dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`
  - **User Instructions**: none.

---

## Slice 2 — Provider-owned long-lived processing graph with ingress (queue source removed from provider graph)

- [x] Work Item 2: Refactor FileShare processing graph to be provider-owned with an ingress channel (no queue polling inside provider) - Completed
  - **Purpose**: Make the provider the root for request processing by owning a long-lived processing graph whose ingress is `ProcessIngestionRequestAsync(...)`.
  - **Acceptance Criteria**:
    - FileShare processing graph no longer creates/uses queue clients or queue source nodes.
    - Provider creates and starts the graph once (long-lived per provider instance).
    - Graph ingress accepts `Envelope<IngestionRequest>` and routes to:
      - validate dead-letter (request-level)
      - index dead-letter (transform/index-level)
      - ack sink on success
    - Existing enrichment node remains in the correct location (between dispatch and microbatch).
    - Unit/graph tests validate:
      - successful request reaches bulk indexing and ack (using test doubles)
      - enrichment failure routes to index dead-letter and never reaches bulk index
  - **Definition of Done**:
    - Code implemented + tests passing
    - Logging maintained
    - Provider graph is runnable in tests without Azure queues
  - [x] Task 1: Introduce provider processing graph builder - Completed
    - [x] Step 1: Create a new provider graph entrypoint (e.g., `FileShareIngestionProcessingGraph`) that takes an ingress `ChannelReader<Envelope<IngestionRequest>>`. - Completed
    - [x] Step 2: Move/reuse nodes from `FileShareIngestionGraph` but remove queue source creation. - Completed
  - [x] Task 2: Wire provider entrypoint to the graph - Completed
    - [x] Step 1: Start the graph during provider initialization (lazy-start allowed on first call if required). - Completed
    - [x] Step 2: Implement `ProcessIngestionRequestAsync(...)` to enqueue into the graph ingress channel. - Completed
  - [x] Task 3: Update factories/dependencies - Completed
    - [x] Step 1: Ensure factories for bulk index, ack, dead-letter sinks remain Infrastructure-owned and are supplied to the provider graph via dependencies. - Completed
    - [x] Step 2: Ensure provider graph dependencies include `IServiceScopeFactory` for scoped enrichers. - Completed
  - [x] Task 4: Tests - Completed
    - [x] Step 1: Add/extend graph-level tests proving end-to-end processing from ingress -> dispatch/enrich -> bulk -> ack. - Completed
  - **Summary**:
    - Introduced `FileShareIngestionProcessingGraph` with an explicit ingress `ChannelReader<Envelope<IngestionRequest>>` and no queue source.
    - Updated `FileShareIngestionDataProvider` to lazily start the processing graph once per provider instance and enqueue via `ProcessIngestionRequestAsync(...)`.
    - Moved FileShare provider factory registration into Infrastructure to supply Infrastructure-owned sink/bulk-index factories, while keeping enrichers registered in the provider.
    - Updated graph-level tests to drive the new ingress-based graph and verify success + enrichment failure routing.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/*`: refactor graph to remove source node and accept ingress.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/FileShareIngestionDataProvider.cs`: own/start graph; route ingress.
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/*`: new/updated graph tests.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **User Instructions**: none.

---

## Slice 3 — Infrastructure queue host calls provider processing (remove provider-specific adapter)

- [ ] Work Item 3: Update Infrastructure queue polling to call `ProcessIngestionRequestAsync` and remove `FileShareIngestionPipelineAdapter`
  - **Purpose**: Complete the ownership inversion: Infrastructure reads from queues and providers process requests.
  - **Acceptance Criteria**:
    - Infrastructure queue poller creates queues and reads messages using `IIngestionDataProviderFactory.QueueName`.
    - For each message, Infrastructure:
      - deserializes via provider
      - builds an `Envelope<IngestionRequest>` with queue acker in context
      - calls `provider.ProcessIngestionRequestAsync(envelope, ct)` and continues polling
    - Provider-specific adapter (`FileShareIngestionPipelineAdapter`) is removed from Infrastructure.
    - Ingestion host remains runnable end-to-end.
  - **Definition of Done**:
    - Code implemented + unit/integration tests passing
    - Ingestion host can run locally and process queued messages
  - [x] Task 1: Infrastructure queue host wiring - Completed
    - [x] Step 1: Update `src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs` to call `ProcessIngestionRequestAsync(...)` instead of writing to an output channel. - Completed
    - [x] Step 2: Ensure cancellation and poison queue behaviors remain unchanged. - Completed
  - [x] Task 2: Remove adapter-based pipeline startup - Completed
    - [x] Step 1: Remove `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/FileShareIngestionPipelineAdapter.cs`. - Completed
    - [x] Step 2: Update any host startup wiring to no longer build the provider-specific graph via the adapter. - Completed
  - [x] Task 3: Provider factory updates - Completed
    - [x] Step 1: Update the FileShare provider factory to create providers that own the long-lived processing graph and required dependencies. - Completed
  - [ ] Task 4: Integration verification
    - [ ] Step 1: Manual: run ingestion host + enqueue a message; confirm indexing + ack.
  - **Summary (so far)**:
    - Updated `IngestionSourceNode` to poll provider queues and call provider `ProcessIngestionRequestAsync(...)` directly.
    - Removed `FileShareIngestionPipelineAdapter` and updated the ingestion hosted service to run the queue host.
    - Updated unit tests to validate envelope headers/context are passed into provider processing and ack happens via the queue acker.
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs`: call provider processing.
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/FileShareIngestionPipelineAdapter.cs`: remove.
    - `src/Hosts/IngestionServiceHost/*`: adjust startup if needed.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/*`: provider factory/provider lifetime updates.
  - **Work Item Dependencies**: Work Items 1–2.
  - **Run / Verification Instructions**:
    - `dotnet test`
    - `dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`
  - **User Instructions**:
    - Enqueue a test message to the FileShare provider queue and verify it is acked.

---

## Slice 4 — Hardening and regression coverage

- [x] Work Item 4: Harden provider-rooted ingestion with lifecycle, shutdown drain, and regression tests - Completed
  - **Purpose**: Ensure the new ownership model is reliable and prevents regressions.
  - **Acceptance Criteria**:
    - Provider long-lived graph starts once and stops cleanly on shutdown.
    - Envelope context (including queue acker) flows to ack/dead-letter consistently.
    - Tests cover:
      - transient enrichment retry behavior in the full provider-rooted flow
      - failure routing for validation vs transform vs bulk index
  - **Definition of Done**:
    - Tests passing in CI
    - Documentation updated
  - [x] Task 1: Provider lifecycle management - Completed
    - [x] Step 1: Ensure provider graph drains in-flight work on shutdown. - Completed
    - [x] Step 2: Add logs/metrics around start/stop. - Completed
  - [x] Task 2: Regression tests - Completed
    - [x] Step 1: Extend graph-level tests for lifecycle and failure modes. - Completed
  - **Summary**:
    - Implemented provider shutdown draining by completing the ingress channel and waiting for graph completion, with a cancel fallback.
    - Ensured providers are disposed when the queue poller shuts down so provider-owned graphs stop cleanly.
    - Split transform vs bulk-index dead-letter channels to prevent premature completion when multiple writers share a dead-letter path.
    - Added regression tests covering transient enrichment retry success, validation failure routing, bulk index failure routing, and provider drain-on-dispose.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/*`: lifecycle.
    - `test/UKHO.Search.Ingestion.Tests/*`: additional tests.
  - **Work Item Dependencies**: Work Item 3.
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **User Instructions**: none.
