# Implementation Plan

## Pipeline Playground (Key-Ordered, Channel-Based)

> Spec: `docs/002-pipeline-playground/002-pipeline-playground.spec.md`  
> Implementation target (Domain): `src/UKHO.Search/UKHO.Search.csproj`

- [x] Work Item 1: “Hello Pipeline” runnable end-to-end (synthetic source → validate → key partition → transform → sink) - Completed
  - **Purpose**: Establish the minimal executable vertical slice proving: envelope model, node lifecycle, bounded channels/backpressure, keyed partitioning, normal completion propagation, and basic logging/metrics scaffolding—implemented in the Domain project `src/UKHO.Search/UKHO.Search.csproj`.
  - **Acceptance Criteria**:
    - A developer can run a small in-process pipeline that processes N synthetic messages through the graph.
    - Messages with the same `Key` exit the sink in the same order they entered.
    - Channels are bounded and upstream blocks when sink is slowed (backpressure observable).
    - Input completion drains the pipeline and completes downstream channels cleanly.
  - **Definition of Done**:
    - Code implemented (models + nodes + supervisor skeleton) in `UKHO.Search`
    - Tests passing (unit; integration as applicable)
    - Logging & error handling added
    - Documentation updated
    - Can execute end-to-end via: `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`
    - Tests are located in the `test/UKHO.Search.Tests/UKHO.Search.Tests.csproj` project (under `test/UKHO.Search.Tests/Pipelines/*`)
  - Summary: Implemented a minimal channel-based pipeline vertical slice in `UKHO.Search` (envelopes/errors, node lifecycle/bases, bounded channels, hello nodes, and fail-fast supervisor) with integration tests proving ordering, completion drain, and observable backpressure.
  - [x] Task 1: Create core envelope + error model
    - [x] Step 1: Add `MessageStatus` enum.
    - [x] Step 2: Add `PipelineErrorCategory` enum.
    - [x] Step 3: Add `PipelineError` with spec fields.
    - [x] Step 4: Add `MessageContext` to track breadcrumbs/timings (minimal first).
    - [x] Step 5: Add `Envelope<TPayload>` with spec fields + helpers (e.g. `MarkFailed`, `MarkDropped`).
  - [x] Task 2: Define node contracts and base types
    - [x] Step 1: Add `INode` interface (`Name`, `StartAsync(ct)`, `Completion`, optional `StopAsync`).
    - [x] Step 2: Implement `NodeBase<TIn,TOut>` with read loop, completion propagation, bounded writes, and top-level try/catch.
    - [x] Step 3: Implement minimal `SourceNodeBase<TOut>` and `SinkNodeBase<TIn>`.
  - [x] Task 3: Implement bounded channel creation helpers
    - [x] Step 1: Add helper/factory for bounded channels with `BoundedChannelFullMode.Wait`.
    - [x] Step 2: Make capacity/options configurable in tests.
  - [x] Task 4: Implement minimal nodes
    - [x] Step 1: `SyntheticSourceNode<T>` to emit deterministic keys + payload ordering markers.
    - [x] Step 2: `ValidateNode<T>` (key non-empty) producing message-scoped failures (no exceptions).
    - [x] Step 3: `KeyPartitionNode<T>`: stable hash of `Key` modulo N to lane outputs.
    - [x] Step 4: `TransformNode<TIn,TOut>`: `ValueTask<TOut>` transform; expected failures mark envelope as failed.
    - [x] Step 5: `CollectingSinkNode<T>` (test helper) to assert order and completion.
  - [x] Task 5: Minimal supervisor scaffolding
    - [x] Step 1: Add `PipelineSupervisor` (FailFast default): starts nodes, monitors completions, cancels on fatal.
    - [x] Step 2: Add a small graph builder used only by tests to wire channels.
  - [x] Task 6: Tests for slice
    - [x] Step 1: Deterministic ordering test.
    - [x] Step 2: Completion propagation/drain test.
    - [x] Step 3: Backpressure test (slow sink throttles source).
  - **Files**:
    - `src/UKHO.Search/Pipelines/Messaging/Envelope.cs`: `Envelope<TPayload>`
    - `src/UKHO.Search/Pipelines/Messaging/MessageStatus.cs`
    - `src/UKHO.Search/Pipelines/Messaging/MessageContext.cs`
    - `src/UKHO.Search/Pipelines/Errors/PipelineError.cs`
    - `src/UKHO.Search/Pipelines/Errors/PipelineErrorCategory.cs`
    - `src/UKHO.Search/Pipelines/Nodes/INode.cs`
    - `src/UKHO.Search/Pipelines/Nodes/NodeBase.cs`
    - `src/UKHO.Search/Pipelines/Nodes/SourceNodeBase.cs`
    - `src/UKHO.Search/Pipelines/Nodes/SinkNodeBase.cs`
    - `src/UKHO.Search/Pipelines/Nodes/SyntheticSourceNode.cs`
    - `src/UKHO.Search/Pipelines/Nodes/ValidateNode.cs`
    - `src/UKHO.Search/Pipelines/Nodes/KeyPartitionNode.cs`
    - `src/UKHO.Search/Pipelines/Nodes/TransformNode.cs`
    - `src/UKHO.Search/Pipelines/Supervision/PipelineSupervisor.cs`
    - `test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`: test project for the pipelines capability
    - `test/UKHO.Search.Tests/Pipelines/*`: ordering/backpressure/completion tests
  - **Work Item Dependencies**: none
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

---

- [x] Work Item 2: Message-scoped failures + Dead-letter end-to-end (continue with dead-letter while pipeline runs) - Completed
  - **Purpose**: Implement explicit “error path” routing to a dead-letter sink; prove poison messages don’t crash the pipeline.
  - **Acceptance Criteria**:
    - Validation failure produces `Envelope.Status = Failed` + populated `Error`.
    - Failed envelopes are routed to `DeadLetterSinkNode<T>`.
    - Pipeline continues processing other messages after a poison message.
  - **Definition of Done**:
    - Dead-letter sink exists (local JSONL default)
    - Tests cover poison message routing + continued processing
    - Logging includes key message details
  - Summary: Added JSONL dead-letter persistence (`DeadLetterSinkNode<T>`), enhanced validation to optionally route failed envelopes to a dedicated error channel, wired the error channel into the test graph, and added a poison-message integration test proving the pipeline continues.
  - [x] Task 1: Implement `DeadLetterSinkNode<T>`
    - [x] Step 1: Persist JSONL entries containing envelope + error.
    - [x] Step 2: Never throw for a single bad message; log and continue.
    - [x] Step 3: Configurable “fatal if cannot persist” mode.
  - [x] Task 2: Add dedicated error outputs (where appropriate)
    - [x] Step 1: Enhance `ValidateNode<T>` to optionally write failed envelopes to an error writer.
    - [x] Step 2: Wire error channel to dead-letter in the graph builder.
  - [x] Task 3: Tests
    - [x] Step 1: Poison message test.
  - **Files**:
    - `src/UKHO.Search/Pipelines/Nodes/DeadLetterSinkNode.cs`
    - `test/UKHO.Search.Tests/Pipelines/DeadLetterTests.cs`
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

---

- [x] Work Item 3: Fail-fast supervision + fatal exception propagation - Completed
  - **Purpose**: Implement pipeline-scoped fatal errors: node faults output channels, supervisor cancels all nodes, unified pipeline completion.
  - **Acceptance Criteria**:
    - Unexpected exception in a node triggers supervisor cancellation.
    - Downstream observes channel completion with exception.
    - Tests validate fail-fast behavior deterministically.
  - **Definition of Done**:
    - Nodes catch exceptions at worker boundary and notify supervisor
    - Supervisor cancels all on first fatal
    - Tests validate fault propagation path
  - Summary: Added `IPipelineFatalErrorReporter` and updated `PipelineSupervisor` + base nodes to report and propagate fatal exceptions via faulted channels (fail-fast cancellation on first fatal). Added deterministic tests for fatal exception propagation using a throwing transform.
  - [x] Task 1: Add fatal error reporting contract
    - [x] Step 1: Introduce `IPipelineFatalErrorReporter` (or equivalent callback) that supervisor implements.
    - [x] Step 2: `NodeBase` reports fatal, completes outputs with exception.
  - [x] Task 2: Tests
    - [x] Step 1: Fatal error test using a throwing transform.
  - **Files**:
    - `src/UKHO.Search/Pipelines/Supervision/*`
    - `test/UKHO.Search.Tests/Pipelines/FatalErrorTests.cs`
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`

---

- [x] Work Item 4: Retry policy + strict in-order retry blocking (per-lane sequential retry) - Completed
  - **Purpose**: Add transient retry semantics that block the lane (strict in-order per key/partition) and dead-letter after exhaustion.
  - **Acceptance Criteria**:
    - Transient failures retry with backoff and increment `Attempt`.
    - While retrying, subsequent messages in the same lane do not pass.
    - After max attempts, message is dead-lettered and lane continues.
  - **Definition of Done**:
    - `IRetryPolicy` exists + one implementation
    - A node supports inline retry without dequeuing the next item
    - Tests cover retry-blocking scenario
  - Summary: Added retry abstractions (`IRetryPolicy`, `ExponentialBackoffRetryPolicy`) and implemented strict in-order retries inside a per-lane sequential `RetryingTransformNode` (retries occur before dequeuing the next message). Added integration tests proving retry blocking and dead-lettering after max attempts.
  - [x] Task 1: Add retry abstractions
    - [x] Step 1: Implement `IRetryPolicy`.
    - [x] Step 2: Implement `ExponentialBackoffRetryPolicy` (with jitter).
  - [x] Task 2: Implement inline retry in sequential worker loop
    - [x] Step 1: Add `RetryingTransformNode` or enhance `TransformNode` with optional retry policy.
    - [x] Step 2: Ensure retries happen before reading the next message.
  - [x] Task 3: Tests
    - [x] Step 1: Retry blocking test (message #3 waits for #2 to finish retrying).
  - **Files**:
    - `src/UKHO.Search/Pipelines/Retry/IRetryPolicy.cs`
    - `src/UKHO.Search/Pipelines/Retry/ExponentialBackoffRetryPolicy.cs`
    - `test/UKHO.Search.Tests/Pipelines/RetryBlockingTests.cs`
  - **Work Item Dependencies**: Work Item 1 + Work Item 2

---

- [x] Work Item 5: Micro-batching per lane + flush semantics - Completed
  - **Purpose**: Implement `MicroBatchNode<T>` producing `BatchEnvelope<T>` per lane, preserving order and flushing on completion.
  - **Acceptance Criteria**:
    - Batches emit on `MaxItems` or `MaxDelay`.
    - Items within a batch remain ordered.
    - On completion, remaining items flush before output completes.
  - **Definition of Done**:
    - `BatchEnvelope<T>` exists
    - `MicroBatchNode<T>` implemented + tested
  - Summary: Implemented per-lane micro-batching with `BatchEnvelope<T>` + `MicroBatchNode<T>` supporting size/time flush triggers and flush-on-completion semantics. Added integration tests covering completion flush and MaxDelay-based flush.
  - [x] Task 1: Add batching model
    - [x] Step 1: Implement `BatchEnvelope<T>`.
  - [x] Task 2: Implement `MicroBatchNode<T>`
    - [x] Step 1: Buffer and flush based on triggers.
    - [x] Step 2: Flush-on-completion.
  - [x] Task 3: Tests
    - [x] Step 1: Flush-on-completion test.
    - [x] Step 2: MaxDelay test.
  - **Files**:
    - `src/UKHO.Search/Pipelines/Batching/BatchEnvelope.cs`
    - `src/UKHO.Search/Pipelines/Nodes/MicroBatchNode.cs`
    - `test/UKHO.Search.Tests/Pipelines/MicroBatchTests.cs`
  - **Work Item Dependencies**: Work Item 1

---

- [x] Work Item 6: Metrics & instrumentation per node (`System.Diagnostics.Metrics`) - Completed
  - **Purpose**: Provide operational visibility required by spec (counts, durations, queue depth, in-flight).
  - **Acceptance Criteria**:
    - Nodes emit counters: in/out/failed/dropped.
    - Nodes emit processing duration histogram.
    - Queue depth + in-flight tracked (even if via local counters).
  - **Definition of Done**:
    - Metrics are present and smoke-validated via `MeterListener` in tests.
  - Summary: Added `NodeMetrics` (global `Meter` + standardized instruments) and wired instrumentation into core node types (in/out/failed/dropped counters, processing duration histogram, and in-flight/queue-depth gauges). Added a `MeterListener` smoke test validating metrics emission.
  - [x] Task 1: Implement metrics helpers
    - [x] Step 1: Add `Meter` singleton for pipeline playground.
    - [x] Step 2: Create `NodeMetrics` helper to standardize instrument names.
  - [x] Task 2: Wire metrics into base nodes
    - [x] Step 1: Increment counters at relevant points.
    - [x] Step 2: Track processing durations.
  - [x] Task 3: Tests
    - [x] Step 1: Metrics smoke test.
  - **Files**:
    - `src/UKHO.Search/Pipelines/Metrics/*`
    - `test/UKHO.Search.Tests/Pipelines/MetricsSmokeTests.cs`
  - **Work Item Dependencies**: Work Item 1

---

## Summary / Key Considerations

- Deliver features as vertical slices: a runnable synthetic pipeline first, then add dead-letter, supervisor fail-fast, retries/blocking, microbatching, and metrics.
- Preserve strict per-key order via `KeyPartitionNode` (stable hashing) + single-threaded per-lane processing.
- Use bounded channels everywhere (`BoundedChannelFullMode.Wait`) to enforce backpressure.
- Keep code within the Domain project (`UKHO.Search`) so it stays framework-agnostic and host-agnostic per Onion Architecture.
