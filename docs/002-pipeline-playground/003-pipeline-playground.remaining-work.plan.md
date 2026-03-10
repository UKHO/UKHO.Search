# Implementation Plan

> Work package: `docs/002-pipeline-playground`
> 
> Input gap review: `docs/002-pipeline-playground/002-remaining-work.md`
> 
> Implementation targets (Domain):
> - `src/UKHO.Search/UKHO.Search.csproj`
> 
> Test targets:
> - `test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

This plan covers **all remaining work** identified in `002-remaining-work.md` to bring the pipeline playground into full alignment with the spec.

## Guiding principles
- Each Work Item is a **vertical slice**: new capability + tests + documentation, keeping the solution runnable throughout.
- Prefer adding small shims/adapters over breaking changes unless unavoidable.
- Keep changes within Onion Architecture boundaries:
  - Domain pipeline code: `src/UKHO.Search/Pipelines/*`
  - Tests: `test/UKHO.Search.Tests/Pipelines/*`

---

## Slice A — Branching and fan-in/fan-out nodes

### [x] Work Item 7: Add `BroadcastNode<TIn>` (spec §7.5) - Completed
- **Purpose**: Enable fan-out to multiple downstream lanes with explicit backpressure semantics.
- **Acceptance Criteria**:
  - `BroadcastNode<TIn>` supports modes:
    - `AllMustReceive`: blocks when any required output is backpressured.
    - `BestEffort`: continues even if optional outputs are backpressured (drops/skips to those outputs).
  - Completion propagates to all outputs.
  - Faults propagate consistently with existing node semantics.
  - Unit/integration tests cover strict blocking, best-effort skipping, and completion propagation.
- **Definition of Done**:
  - Node implemented and validated with tests.
  - Metrics and error handling follow existing patterns.
  - Can execute end-to-end via: `dotnet test`.
- **Summary**:
  - Added `BroadcastNode<TIn>` with `AllMustReceive` (two-phase `WaitToWriteAsync` to enforce backpressure) and `BestEffort` optional outputs via `TryWrite`.
  - Added safe envelope duplication via `Envelope.Clone()` + `MessageContext.Clone()` to avoid sharing mutable `MessageContext` across branches.
  - Added tests covering strict backpressure, best-effort optional drop, and completion propagation.
- [x] Task 7.1: Define `BroadcastMode` and output registration model
  - [x] Step 1: Decide output contract (required outputs list + optional outputs list).
  - [x] Step 2: Ensure the chosen API fits existing node construction style in `src/UKHO.Search/Pipelines/Nodes/*`.
- [x] Task 7.2: Implement `BroadcastNode<TIn>`
  - [x] Step 1: Implement read loop from input channel.
  - [x] Step 2: For `AllMustReceive`, wait for all outputs to be writable before writing (enforces backpressure across all outputs).
  - [x] Step 3: For `BestEffort`, write required outputs normally and use `TryWrite` for optional outputs.
  - [x] Step 4: Ensure completion/fault propagation is deterministic.
- [x] Task 7.3: Add tests
  - [x] Step 1: Strict mode blocks when any output is slow.
  - [x] Step 2: Best-effort continues when optional output is slow/backpressured.
  - [x] Step 3: Completion is observed by all outputs.
- **Files**:
  - `src/UKHO.Search/Pipelines/Nodes/BroadcastMode.cs`: Broadcast mode enum.
  - `src/UKHO.Search/Pipelines/Nodes/BroadcastNode.cs`: Node implementation.
  - `test/UKHO.Search.Tests/Pipelines/Nodes/BroadcastNodeTests.cs`: Test suite.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update “Remaining work” section 2.1 to mark implemented.
- **Work Item Dependencies**: none.
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

### [x] Work Item 8: Add `MultiInputNodeBase<T1,T2,TOut>` + `MergeNode<TIn>` (spec §3.2, §7.6) - Completed
- **Purpose**: Provide consistent fan-in semantics and a merge node with fairness guarantees.
- **Acceptance Criteria**:
  - `MultiInputNodeBase<T1,T2,TOut>` exists and standardizes:
    - start/stop lifecycle
    - fault propagation
    - completion semantics (including one input completing early)
    - cancellation handling hooks
  - `MergeNode<TIn>` merges two inputs without starvation.
  - Completion propagation works when one upstream completes early.
  - Fault propagation occurs from either upstream.
  - Tests validate fairness and completion/fault behavior.
- **Definition of Done**:
  - Base type and merge node implemented.
  - Tests added and passing.
  - Can execute end-to-end via: `dotnet test`.
- **Summary**:
  - Added `MultiInputNodeBase<T1,T2,TOut>` with a simple round-robin preference to avoid starvation.
  - Added `MergeNode<TIn>` built on `MultiInputNodeBase`, merging two `Envelope<TIn>` inputs into a single output.
  - Added tests covering fairness (no starvation), completion when one upstream completes early, and fault propagation.
- [x] Task 8.1: Implement `MultiInputNodeBase<T1,T2,TOut>`
  - [x] Step 1: Review existing `NodeBase` lifecycle + fault semantics and extract common patterns.
  - [x] Step 2: Define fairness policy hook (implemented as a round-robin preference between inputs).
  - [x] Step 3: Define deterministic completion rules (output completes after both upstream completions are observed; upstream faults propagate).
- [x] Task 8.2: Implement `MergeNode<TIn>`
  - [x] Step 1: Choose fairness approach (round-robin preference with `Task.WhenAny` waiting).
  - [x] Step 2: Implement non-starving merge behavior.
  - [x] Step 3: Ensure merge behavior is documented as “no global ordering”.
- [x] Task 8.3: Add tests for base + merge
  - [x] Step 1: No starvation under asymmetric producers.
  - [x] Step 2: One input completes early; output continues from remaining input and completes correctly.
  - [x] Step 3: Fault in either input propagates.
- **Files**:
  - `src/UKHO.Search/Pipelines/Nodes/MultiInputNodeBase.cs`: Base type.
  - `src/UKHO.Search/Pipelines/Nodes/MergeNode.cs`: Merge node.
  - `test/UKHO.Search.Tests/Pipelines/MergeNodeTests.cs`: Test suite.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update sections 2.2 and 2.1.
- **Work Item Dependencies**: Work Item 7 (recommended sequencing only; not required).
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

### [x] Work Item 9: Add `RouteNode<TIn>` with defined “missing key” semantics (spec §7.7) - Completed
- **Purpose**: Allow dictionary-based routing after partitioning while preserving lane membership.
- **Acceptance Criteria**:
  - `RouteNode<TIn>` routes messages based on a key selector and routing table.
  - Missing key behavior is explicitly defined and tested (choose one):
    - (A) drop message and emit metric/log event
    - (B) dead-letter message (message-scoped failure)
    - (C) fail message (transient/non-transient) without killing the pipeline
    - (D) fatal pipeline fault
  - Completion and fault propagation match established conventions.
- **Definition of Done**:
  - Route node implemented.
  - Missing key decision recorded in docs.
  - Tests passing.
  - Can execute end-to-end via: `dotnet test`.
- **Summary**:
  - Added `RouteNode<TIn>` supporting dictionary-based routing via a route selector.
  - Decision: missing route key marks the envelope as `Failed` (`ROUTE_NOT_FOUND`) and is written to `errorOutput` when configured; otherwise the node faults.
  - Added tests for correct routing, missing route behavior, completion propagation, and the “no error output configured” failure mode.
- [x] Task 9.1: Decide routing contract and missing key behavior
  - [x] Step 1: Review existing “message-scoped failure + dead-letter” semantics.
  - [x] Step 2: Select missing-key behavior (message-scoped failure to `errorOutput`; fatal if `errorOutput` not configured).
  - [x] Step 3: Document decision in `002-remaining-work.md`.
- [x] Task 9.2: Implement `RouteNode<TIn>`
  - [x] Step 1: Implement read loop.
  - [x] Step 2: Route to correct output via dictionary lookup.
  - [x] Step 3: Implement missing-key path according to decision.
- [x] Task 9.3: Add tests
  - [x] Step 1: Correct routing for multiple keys.
  - [x] Step 2: Missing route key behavior.
  - [x] Step 3: Completion propagation.
- **Files**:
  - `src/UKHO.Search/Pipelines/Nodes/RouteNode.cs`: Route node.
  - `test/UKHO.Search.Tests/Pipelines/RouteNodeTests.cs`: Test suite.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update section 2.1.
- **Work Item Dependencies**: none.
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

---

## Slice B — Bulk indexing contract spike

### [x] Work Item 10: Spike `BulkIndexNode<TDocument>` contract + test strategy (spec §7.10) - Completed
- **Purpose**: Establish a realistic bulk indexing abstraction and verify response classification (transient vs non-transient) before committing to a concrete backend.
- **Acceptance Criteria**:
  - A minimal, internal “bulk index client” abstraction exists (test double friendly).
  - `BulkIndexNode<TDocument>` consumes `BatchEnvelope<TDocument>`.
  - Node classifies per-item results into success/transient failure/permanent failure.
  - Test strategy is implemented using a test double (no external dependency required).
  - A follow-up decision is captured on whether to add an emulator/integration test harness later.
- **Definition of Done**:
  - Node and contract exist behind an interface.
  - Unit tests verify classification rules.
  - Documentation updated.
  - Can execute end-to-end via: `dotnet test`.
- **Summary**:
  - Added `IBulkIndexClient<TDocument>` abstraction with request/response DTOs including per-item status codes.
  - Implemented `BulkIndexNode<TDocument>` consuming `BatchEnvelope<TDocument>` and classifying items into success / transient retry (`Retrying`) / permanent failure (`Failed`).
  - Transient status codes are configurable; defaults include 429 and 503.
  - Added unit tests using an in-memory test double (no external dependency).
- [x] Task 10.1: Define contract for bulk indexing
  - [x] Step 1: Defined request/response DTOs for “bulk index” with per-item status codes.
  - [x] Step 2: Defined transient status code list (default 429/503) and made it configurable.
- [x] Task 10.2: Implement `BulkIndexNode<TDocument>`
  - [x] Step 1: Consume `BatchEnvelope<TDocument>` and call bulk client.
  - [x] Step 2: Map responses into pipeline results (success continues, transient emits `Retrying` to `retryOutput`, permanent emits `Failed` to `errorOutput`).
  - [x] Step 3: Ensure consistent message-scoped failure semantics (populate `PipelineError` with `Category=BulkIndex`).
- [x] Task 10.3: Tests
  - [x] Step 1: Classification: mixed success/transient/permanent in one batch.
  - [x] Step 2: Verifies transient items are sent to `retryOutput` (when configured).
- **Files**:
  - `src/UKHO.Search/Pipelines/Nodes/BulkIndexNode.cs`: Node.
  - `src/UKHO.Search/Pipelines/Nodes/IBulkIndexClient.cs`: Abstraction.
  - `test/UKHO.Search.Tests/Pipelines/BulkIndexNodeTests.cs`: Tests.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update section 2.1.
- **Work Item Dependencies**: Work Item 12 (optional, if `BatchEnvelope` needs extension first); otherwise independent.
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

---

## Slice C — Cancellation semantics (Drain vs Immediate)

### [x] Work Item 11: Implement cancellation modes (spec §3.1) - Completed
- **Purpose**: Make node shutdown behavior explicit and testable, supporting both graceful draining and immediate stop.
- **Acceptance Criteria**:
  - A `CancellationMode` (or equivalent) exists with at least:
    - `Immediate`: stop quickly
    - `Drain`: stop accepting new input, drain buffered/in-flight work, then complete
  - Nodes that buffer (e.g., `MicroBatchNode`) honor `Drain` semantics.
  - Node loops apply the mode consistently (no deadlocks; deterministic completion).
  - Tests prove drain completes after draining buffered items and immediate stops promptly.
- **Definition of Done**:
  - Cancellation model is surfaced in node base(s) and supervision where appropriate.
  - Updated tests pass.
  - Can execute end-to-end via: `dotnet test`.
- **Summary**:
  - Introduced `CancellationMode` (`Immediate` vs `Drain`) and wired it into `NodeBase`, `SinkNodeBase`, and `MultiInputNodeBase`.
  - Updated `MicroBatchNode` to honor Drain semantics by flushing buffered items on cancellation and ensuring in-progress flushes cannot lose buffered items.
  - Added tests proving drain vs immediate behavior for both `NodeBase`-based nodes and `MicroBatchNode`.
- [x] Task 11.1: Define cancellation policy model
  - [x] Step 1: Introduced `CancellationMode` and passed it via node constructors (default: `Immediate`).
  - [x] Step 2: Policy stays within Domain; no Host/Infrastructure leakage.
- [x] Task 11.2: Implement in base types and key nodes
  - [x] Step 1: Updated `NodeBase` and `MultiInputNodeBase` to accept/apply cancellation mode.
  - [x] Step 2: Updated buffering node `MicroBatchNode` to implement drain behavior.
  - [x] Step 3: Completion propagation remains deterministic (outputs completed in all modes).
- [x] Task 11.3: Add tests
  - [x] Step 1: Drain mode drains buffered items and completes outputs.
  - [x] Step 2: Immediate mode stops quickly and completes outputs deterministically.
- **Files**:
  - `src/UKHO.Search/Pipelines/Nodes/CancellationMode.cs`: Cancellation mode enum.
  - `src/UKHO.Search/Pipelines/Nodes/NodeBase.cs`: Applies cancellation mode.
  - `src/UKHO.Search/Pipelines/Nodes/SinkNodeBase.cs`: Applies cancellation mode.
  - `src/UKHO.Search/Pipelines/Nodes/MultiInputNodeBase.cs`: Applies cancellation mode.
  - `src/UKHO.Search/Pipelines/Nodes/MicroBatchNode.cs`: Drain-mode flushing behavior.
  - `test/UKHO.Search.Tests/Pipelines/CancellationModeTests.cs`: Tests.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update section 3.1.
- **Work Item Dependencies**: Work Item 8 (recommended if `MultiInputNodeBase` should share cancellation semantics).
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

---

## Slice D — Spec alignment and hardening

### [x] Work Item 12: Micro-batching `MaxBytes` + batch context aggregation (spec §7.9) - Completed
- **Purpose**: Complete batching triggers and enrich batch-level context to support downstream observability and bulk operations.
- **Acceptance Criteria**:
  - `MicroBatchNode` supports optional `MaxBytes` trigger.
  - A size estimation strategy is defined and tested.
  - `BatchEnvelope<T>` carries aggregated context/metrics (explicitly defined fields).
  - Existing batching behavior (`MaxItems`, `MaxDelay`, flush) remains stable.
- **Definition of Done**:
  - Implementation complete with tests.
  - Backward compatibility considered (additive fields, default behaviors).
  - Can execute end-to-end via: `dotnet test`.
- **Summary**:
  - Added optional `maxBytes` support to `MicroBatchNode<T>` with a caller-provided `estimateSizeBytes` function.
  - Extended `BatchEnvelope<T>` with aggregate context: `ItemCount`, `TotalEstimatedBytes`, `MinItemTimestampUtc`, `MaxItemTimestampUtc`.
  - Added tests for `MaxBytes` flushing and aggregate context values.
- [x] Task 12.1: Define “bytes” estimation approach
  - [x] Step 1: Chose estimator API: `Func<TPayload, int> estimateSizeBytes` (payload-based estimator).
  - [x] Step 2: Default behavior: if `maxBytes` is `null`, the trigger is disabled; if `maxBytes` is set, `estimateSizeBytes` is required.
- [x] Task 12.2: Extend `BatchEnvelope<T>` aggregate context
  - [x] Step 1: Added `ItemCount` (computed), `TotalEstimatedBytes` (optional), and min/max item timestamps.
  - [x] Step 2: Aggregate context is produced in `MicroBatchNode.FlushAsync` (single batching point).
- [x] Task 12.3: Implement MaxBytes trigger
  - [x] Step 1: Track accumulated estimated bytes while buffering.
  - [x] Step 2: Flush when `bufferedBytes >= maxBytes`.
- [x] Task 12.4: Add tests
  - [x] Step 1: Flush occurs on MaxBytes.
  - [x] Step 2: Aggregated context values correct.
  - [x] Step 3: Regression coverage relies on existing micro-batching test suite.
- **Files**:
  - `src/UKHO.Search/Pipelines/Batching/BatchEnvelope.cs`: Extend envelope.
  - `src/UKHO.Search/Pipelines/Nodes/MicroBatchNode.cs`: Implement `MaxBytes` trigger and aggregate context population.
  - `test/UKHO.Search.Tests/Pipelines/MicroBatchMaxBytesTests.cs`: Tests.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update section 3.4.
- **Work Item Dependencies**: none (but Work Item 10 may consume enriched `BatchEnvelope`).
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

### [x] Work Item 13: Partition hashing over UTF-8 bytes (spec §7.8) - Completed
- **Purpose**: Align partition assignment with spec recommendation and ensure stability across non-ASCII inputs.
- **Acceptance Criteria**:
  - Partition hashing uses UTF-8 bytes rather than `char` enumeration.
  - Tests verify stable partition assignment for known inputs (including non-ASCII keys).
- **Definition of Done**:
  - Hashing updated and tested.
  - Can execute end-to-end via: `dotnet test`.
- **Summary**:
  - Updated `KeyPartitionNode` to hash over UTF-8 bytes (FNV-1a 32-bit) rather than `char` enumeration.
  - Added tests covering ASCII and non-ASCII keys and proving behavior matches a UTF-8 reference implementation.
- [x] Task 13.1: Update hashing implementation
  - [x] Step 1: Located partition hashing in `KeyPartitionNode` and updated it to hash over UTF-8 bytes.
  - [x] Step 2: Preserved determinism (stable FNV-1a 32-bit; byte-level).
- [x] Task 13.2: Tests
  - [x] Step 1: Added fixed input vectors for ASCII and non-ASCII keys.
  - [x] Step 2: Asserted partition index matches a UTF-8 FNV-1a reference implementation (and differs from char enumeration for non-ASCII).
- **Files**:
  - `src/UKHO.Search/Pipelines/Nodes/KeyPartitionNode.cs`: Partition hashing implementation.
  - `test/UKHO.Search.Tests/Pipelines/KeyPartitionHashVectorsTests.cs`: Hashing tests.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update section 3.3.
- **Work Item Dependencies**: none.
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

### [x] Work Item 14: Retry policy alignment decision + implementation (spec §4.3) - Completed
- **Purpose**: Resolve mismatch between spec’s envelope+error-driven retry policy and current exception-driven classification.
- **Acceptance Criteria**:
  - A decision is captured:
    - Option 1: adapt `IRetryPolicy` to match spec signature, or
    - Option 2: keep current approach and document deviation
  - If adapting:
    - Existing behavior remains intact (via adapter/shim).
    - Tests cover both transient and non-transient classification.
- **Definition of Done**:
  - Decision recorded and code updated accordingly.
  - Tests passing.
- **Decision**:
  - Adapt retry node API to be `envelope + error`-driven (spec-aligned), while keeping the existing exception-driven entrypoint via a shim constructor for backward compatibility.
- **Summary**:
  - Added a new `RetryingTransformNode` constructor that takes a `createError(envelope, exception)` factory.
  - Retained existing constructor (`isTransientException`) as an adapter over the new API.
  - Added unit tests covering transient retry behavior and non-transient fail-fast/dead-letter behavior using the new API.
- [x] Task 14.1: Review current retry pipeline shape
  - [x] Step 1: Confirmed `IRetryPolicy` already matches spec (`ShouldRetry(envelope, error)`, `GetDelay`, `MaxAttempts`).
  - [x] Step 2: Represented “envelope + error” via `PipelineError` and a `createError(envelope, exception)` factory.
- [x] Task 14.2: Implement chosen approach
  - [x] Step 1: Introduced shim constructor to preserve existing `isTransientException` usage.
  - [x] Step 2: Updated `RetryingTransformNode` implementation to rely on `PipelineError` and `IRetryPolicy.ShouldRetry`.
- [x] Task 14.3: Add/update tests
  - [x] Step 1: Transient errors retry with expected delay and eventually succeed.
  - [x] Step 2: Non-transient errors fail fast and are written to `errorOutput`.
- **Files**:
  - `src/UKHO.Search/Pipelines/Retry/IRetryPolicy.cs`: Retry policy contract.
  - `src/UKHO.Search/Pipelines/Retry/ExponentialBackoffRetryPolicy.cs`: Default implementation.
  - `src/UKHO.Search/Pipelines/Nodes/RetryingTransformNode.cs`: Retry node updated to use `PipelineError` factory.
  - `test/UKHO.Search.Tests/Pipelines/RetryErrorFactoryTests.cs`: New tests.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update section 3.2.
- **Work Item Dependencies**: none.
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

### [x] Work Item 15: Dead-letter record enrichment (spec §4.4) + single-writer decision (sections 3.5–3.6) - Completed
- **Purpose**: Improve diagnosability of failures and clarify or harden concurrency guarantees.
- **Acceptance Criteria**:
  - Dead-letter record includes, where feasible:
    - envelope + error
    - node name
    - timestamps
    - optional raw input snapshot
    - environment/build metadata (app version, commit, host)
  - Concurrency story is explicit:
    - either document “single writer process” as an invariant, or
    - implement cross-process safe append.
  - Tests validate schema serialization/deserialization and presence of new fields.
- **Definition of Done**:
  - Schema extended in a backward-compatible way.
  - Tests passing.
  - Documentation updated.
- **Decision**:
  - Implement cross-process safe append (exclusive writer access) rather than relying on a single-writer process invariant.
- **Summary**:
  - Introduced a versionable `DeadLetterRecord<T>` schema carrying envelope+error, timestamps, node name, optional raw snapshot, and build/environment metadata.
  - Added `IDeadLetterMetadataProvider` abstraction with a default provider and test provider.
  - Hardened append semantics by opening the dead-letter file with exclusive writer access (`FileShare.Read`) plus a retry loop to avoid write interleaving across processes.
  - Updated schema tests to assert metadata + snapshot are present when configured.
- [x] Task 15.1: Extend dead-letter schema
  - [x] Step 1: Defined new optional fields (`RawSnapshot`, `Metadata`) in `DeadLetterRecord<T>`.
  - [x] Step 2: Implemented `IDeadLetterMetadataProvider` + default provider.
- [x] Task 15.2: Raw input snapshot strategy
  - [x] Step 1: Defined “raw” as an optional string produced by a caller-provided snapshotter callback.
  - [x] Step 2: Implemented snapshot capture via `Func<Envelope<T>, string?> snapshotter`.
- [x] Task 15.3: Concurrency decision/implementation
  - [x] Step 1: Chose cross-process safe append.
  - [x] Step 2: Implemented exclusive writer access with retry on `IOException`.
- [x] Task 15.4: Tests
  - [x] Step 1: JSONL record contains new metadata.
  - [x] Step 2: Raw snapshot present when configured.
- **Files**:
  - `src/UKHO.Search/Pipelines/DeadLetter/DeadLetterRecord.cs`: Dead-letter record schema.
  - `src/UKHO.Search/Pipelines/DeadLetter/DeadLetterMetadata.cs`: Metadata DTO.
  - `src/UKHO.Search/Pipelines/DeadLetter/IDeadLetterMetadataProvider.cs`: Metadata abstraction.
  - `src/UKHO.Search/Pipelines/DeadLetter/DefaultDeadLetterMetadataProvider.cs`: Default metadata provider.
  - `src/UKHO.Search/Pipelines/Nodes/DeadLetterSinkNode.cs`: Writer updates + cross-process append hardening.
  - `test/UKHO.Search.Tests/Pipelines/TestDeadLetterMetadataProvider.cs`: Test metadata provider.
  - `test/UKHO.Search.Tests/Pipelines/DeadLetterSchemaTests.cs`: Schema assertions.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update sections 3.5–3.6.
- **Work Item Dependencies**: none.
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

### [x] Work Item 16: Queue depth metrics across nodes (spec intent §1.5 / §7 instrumentation) - Completed
- **Purpose**: Provide meaningful queue depth signals beyond micro-batching.
- **Acceptance Criteria**:
  - A consistent queue depth signal exists for channel-based nodes.
  - Metrics emitted are documented (what is and isn’t measurable).
  - Tests validate queue depth tracking does not break ordering/backpressure.
- **Definition of Done**:
  - Implementation complete with tests.
  - Can execute end-to-end via: `dotnet test`.
- **Summary**:
  - Implemented a counting channel decorator (`CountingChannel*`) that tracks queue depth as `writes - reads` and exposes it via `IQueueDepthProvider`.
  - Integrated queue depth automatically into `NodeMetrics` by wiring depth providers in `NodeBase`, `SinkNodeBase`, and `MultiInputNodeBase`.
  - Updated `BoundedChannelFactory` to consistently return depth-tracked channels and updated test helpers accordingly.
  - Added unit tests validating depth accounting and non-negative behavior.
- [x] Task 16.1: Choose queue depth implementation
  - [x] Step 1: Implemented counting channel decorator tracking `Write` - `Read`.
  - [x] Step 2: Integrated wrapper in `BoundedChannelFactory`.
- [x] Task 16.2: Integrate metrics provider
  - [x] Step 1: `NodeMetrics` already supports queue depth providers; base node types now supply them automatically.
  - [x] Step 2: Node names map deterministically via existing `node` tag usage.
- [x] Task 16.3: Tests
  - [x] Step 1: Depth increments on write, decrements on read.
  - [x] Step 2: No negative depths; returns to zero after completion.
- **Files**:
  - `src/UKHO.Search/Pipelines/Channels/IQueueDepthProvider.cs`: Queue depth provider contract.
  - `src/UKHO.Search/Pipelines/Channels/CountingChannel.cs`: Counting channel wrapper.
  - `src/UKHO.Search/Pipelines/Channels/CountingChannelReader.cs`: Reader wrapper (decrements on read; exposes depth).
  - `src/UKHO.Search/Pipelines/Channels/CountingChannelWriter.cs`: Writer wrapper (increments on write).
  - `src/UKHO.Search/Pipelines/Channels/BoundedChannelFactory.cs`: Returns depth-tracked channels.
  - `src/UKHO.Search/Pipelines/Nodes/NodeBase.cs`: Auto-wires input queue depth into `NodeMetrics`.
  - `src/UKHO.Search/Pipelines/Nodes/SinkNodeBase.cs`: Auto-wires input queue depth into `NodeMetrics`.
  - `src/UKHO.Search/Pipelines/Nodes/MultiInputNodeBase.cs`: Publishes summed queue depth across both inputs.
  - `test/UKHO.Search.Tests/Pipelines/Metrics/CountingChannelQueueDepthTests.cs`: Tests.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update section 3.7.
- **Work Item Dependencies**: none.
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

### [x] Work Item 17: Hardening decisions (naming collisions, `StopAsync`, structured logging) (section 4) - Completed
- **Purpose**: Close open design gaps needed to reduce operational risk.
- **Acceptance Criteria**:
  - Node naming collisions are handled (either enforced unique or internally uniqued).
  - `StopAsync` contract is defined and tested (even if implemented as a no-op with documented semantics).
  - Logging approach decision recorded:
    - keep callback-based logging, or
    - introduce `ILogger` as an abstraction without violating layering.
- **Definition of Done**:
  - Decisions recorded.
  - Any code changes implemented with tests.
- **Decisions**:
  - Enforce unique node names (throw early on collisions) to prevent metrics/provider ambiguity.
  - Define `PipelineSupervisor.StopAsync` to cancel the pipeline and forward `StopAsync` to all nodes.
  - Use `ILogger` abstractions in Domain (via `Microsoft.Extensions.Logging.Abstractions`) instead of `Action<string>` callbacks.
- **Summary**:
  - Added unique node-name enforcement in `PipelineSupervisor.AddNode` with a unit test.
  - Added `PipelineSupervisor.StopAsync` and unit tests proving it cancels and forwards stop requests.
  - Adopted `ILogger` abstractions in Domain and replaced `Action<string>` callbacks across pipeline nodes.
  - Updated documentation to record the decisions.
- [x] Task 17.1: Node naming collision policy
  - [x] Step 1: Chose enforcement (unique names) rather than auto-suffixing.
  - [x] Step 2: Added guardrails in supervisor (`AddNode` throws on duplicate names).
  - [x] Step 3: Added tests for collision behavior.
- [x] Task 17.2: Define `StopAsync`
  - [x] Step 1: Documented intended use in `docs/002-pipeline-playground/002-remaining-work.md` section 4.
  - [x] Step 2: Implemented supervisor-level stop orchestration (`StopAsync` forwards to nodes); base types retain safe no-op defaults.
- [x] Task 17.3: Logging decision
  - [x] Step 1: Adopted `ILogger` abstractions in Domain (no `Microsoft.Extensions.Logging` implementation dependency).
  - [x] Step 2: Replaced `Action<string>` callbacks with `ILogger` across pipeline nodes and updated docs.
- **Files**:
  - `src/UKHO.Search/Pipelines/Supervision/PipelineSupervisor.cs`: Unique name enforcement + `StopAsync` orchestration.
  - `src/UKHO.Search/UKHO.Search.csproj`: Adds `Microsoft.Extensions.Logging.Abstractions`.
  - `src/UKHO.Search/Pipelines/Nodes/*`: Replaced `Action<string>` logging callbacks with `ILogger`.
  - `test/UKHO.Search.Tests/Pipelines/NodeNamingCollisionTests.cs`: Naming collision tests.
  - `test/UKHO.Search.Tests/Pipelines/PipelineSupervisorStopAsyncTests.cs`: StopAsync orchestration tests.
  - `test/UKHO.Search.Tests/Pipelines/StopAsyncProbeNode.cs`: Test node used to assert StopAsync forwarding.
  - `docs/002-pipeline-playground/002-remaining-work.md`: Update section 4.
- **Work Item Dependencies**: none.
- **Run / Verification Instructions**:
  - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

---

## Sequencing summary
Recommended execution order (minimizes rework):
1. Work Item 7 (`BroadcastNode`)
2. Work Item 8 (`MultiInputNodeBase` + `MergeNode`)
3. Work Item 9 (`RouteNode`)
4. Work Item 12 (batching: `MaxBytes` + batch context)
5. Work Item 10 (`BulkIndexNode` spike; can move earlier if it does not need batch envelope changes)
6. Work Item 11 (cancellation modes)
7. Work Item 13 (UTF-8 partition hashing)
8. Work Item 14 (retry policy alignment)
9. Work Item 15 (dead-letter enrichment + concurrency decision)
10. Work Item 16 (queue depth metrics)
11. Work Item 17 (hardening decisions)

## Overall approach / key considerations
- Treat “missing key route” and “retry policy shape” as explicit decisions with written outcomes to avoid ambiguous behavior.
- Prefer additive changes to public-ish types like `BatchEnvelope<T>` and dead-letter schemas.
- Ensure every Work Item is validated through tests and results in a demoable capability (at minimum, a new node exercised in tests).
