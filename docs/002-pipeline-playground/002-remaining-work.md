# Remaining Work (Spec Gap Review)

> Work package: `docs/002-pipeline-playground`  
> Spec: `docs/002-pipeline-playground/002-pipeline-playground.spec.md`  
> Implementation target (Domain): `src/UKHO.Search/UKHO.Search.csproj`

This document records all **known remaining work** to bring the current implementation into full alignment with the spec, and to capture follow-on work that is explicitly described but not yet implemented.

## 1. Current scope delivered (for context)

Work Items 1–6 in `docs/002-pipeline-playground/002-pipeline-playground.plan.md` are implemented and tested:

- Key-ordered channel-based pipeline playground (source → validate → partition → transform → sink)
- Message-scoped failures + dead-letter sink
- Fail-fast supervision + fault propagation
- Retry policy + strict in-order retry blocking (per lane)
- Micro-batching per lane + flush semantics
- Metrics & instrumentation (`System.Diagnostics.Metrics`)

Key implementations live under:

- `src/UKHO.Search/Pipelines/Messaging/*`
- `src/UKHO.Search/Pipelines/Nodes/*`
- `src/UKHO.Search/Pipelines/Supervision/*`
- `src/UKHO.Search/Pipelines/Retry/*`
- `src/UKHO.Search/Pipelines/Batching/*`
- `src/UKHO.Search/Pipelines/Metrics/*`

Tests live under:

- `test/UKHO.Search.Tests/Pipelines/*`

## 2. Remaining spec work (not implemented)

### 2.1 Node types described by the spec but missing in code

These node types are explicitly described but do not currently exist as implementations.

- **`BroadcastNode<TIn>`** (spec §7.5) - **Implemented**
  - Implementation: `src/UKHO.Search/Pipelines/Nodes/BroadcastNode.cs`, `src/UKHO.Search/Pipelines/Nodes/BroadcastMode.cs`
  - Tests: `test/UKHO.Search.Tests/Pipelines/BroadcastNodeTests.cs`

- **`MergeNode<TIn>`** (spec §7.6)
  - **Implemented**
  - Implementation: `src/UKHO.Search/Pipelines/Nodes/MultiInputNodeBase.cs`, `src/UKHO.Search/Pipelines/Nodes/MergeNode.cs`
  - Tests: `test/UKHO.Search.Tests/Pipelines/MergeNodeTests.cs`

- **`RouteNode<TIn>`** (spec §7.7)
  - **Implemented**
  - Missing route key behavior: mark message as `Failed` (`ROUTE_NOT_FOUND`) and emit to `errorOutput` when configured; otherwise fault the node/pipeline.
  - Implementation: `src/UKHO.Search/Pipelines/Nodes/RouteNode.cs`
  - Tests: `test/UKHO.Search.Tests/Pipelines/RouteNodeTests.cs`

- **`BulkIndexNode<TDocument>`** (spec §7.10)
  - **Implemented (spike)**
  - Contract: `IBulkIndexClient<TDocument>` + request/response DTOs with per-item status codes.
  - Transient classification: configurable status code set (defaults include 429/503).
  - Test strategy: in-memory test double (no external dependency).
  - Implementation: `src/UKHO.Search/Pipelines/Nodes/BulkIndexNode.cs`, `src/UKHO.Search/Pipelines/Nodes/IBulkIndexClient.cs`
  - Tests: `test/UKHO.Search.Tests/Pipelines/BulkIndexNodeTests.cs`

### 2.2 Base types described by the spec but missing in code

- **`MultiInputNodeBase<T1, T2, TOut>`** (spec §3.2)
  - **Implemented** (used by `MergeNode<TIn>`)
  - Implementation: `src/UKHO.Search/Pipelines/Nodes/MultiInputNodeBase.cs`

## 3. Spec alignment gaps (implemented, but not fully matching the spec)

### 3.1 Cancellation semantics: graceful vs immediate

Spec requirements (spec §3.1):

- Nodes must support **graceful cancellation (drain mode)** and **immediate cancellation (stop now)**.

Current state:

- `CancellationMode` is implemented and applied consistently in `NodeBase`, `MultiInputNodeBase`, and buffering nodes.
- `MicroBatchNode` supports drain semantics (flushes buffered items on cancellation).

Implementation:

- `src/UKHO.Search/Pipelines/Nodes/CancellationMode.cs`
- `src/UKHO.Search/Pipelines/Nodes/NodeBase.cs`
- `src/UKHO.Search/Pipelines/Nodes/MultiInputNodeBase.cs`
- `src/UKHO.Search/Pipelines/Nodes/MicroBatchNode.cs`
- Tests: `test/UKHO.Search.Tests/Pipelines/CancellationModeTests.cs`

### 3.2 Retry policy interface mismatch vs spec

Spec (spec §4.3) describes a policy shaped like:

- `ShouldRetry(envelope, error) -> bool`
- `GetDelay(attempt) -> TimeSpan`
- `MaxAttempts`

Current state:

- `IRetryPolicy` matches the spec shape.
- `RetryingTransformNode` now supports an `envelope + error` contract via a `createError(envelope, exception)` factory.
- The previous exception-based transient classification is retained as a shim constructor for backward compatibility.

Implementation:

- `src/UKHO.Search/Pipelines/Retry/IRetryPolicy.cs`
- `src/UKHO.Search/Pipelines/Nodes/RetryingTransformNode.cs`
- Tests: `test/UKHO.Search.Tests/Pipelines/RetryErrorFactoryTests.cs`

### 3.3 Key hashing implementation detail

Spec (spec §7.8) recommends hashing UTF-8 bytes rather than `string.GetHashCode()`.

Current state:

- Deterministic hashing exists and now operates over UTF-8 bytes (spec-aligned).

Implementation:

- `src/UKHO.Search/Pipelines/Nodes/KeyPartitionNode.cs`
- Tests: `test/UKHO.Search.Tests/Pipelines/KeyPartitionHashVectorsTests.cs`

### 3.4 Micro-batching: missing `MaxBytes` and aggregate context

Spec (spec §7.9) lists triggers:

- `MaxItems`
- `MaxDelay`
- `MaxBytes` (optional)

And `BatchEnvelope` should carry:

- “aggregate context and metrics”

Current state:

- `MaxItems` and `MaxDelay` supported.
- `MaxBytes` supported (optional) via a caller-provided size estimator.
- `BatchEnvelope<T>` carries aggregate context (item count, total estimated bytes, min/max item timestamps).

Implementation:

- `src/UKHO.Search/Pipelines/Nodes/MicroBatchNode.cs`
- `src/UKHO.Search/Pipelines/Batching/BatchEnvelope.cs`
- `test/UKHO.Search.Tests/Pipelines/MicroBatchMaxBytesTests.cs`

### 3.5 Dead-letter record content

Spec (spec §4.4) suggests dead-letter should record:

- envelope + error
- node name
- raw input snapshot if possible
- timestamps
- environment details

Current state:

- JSONL contains envelope + error + node name + timestamp.
- JSONL now also contains:
  - optional raw input snapshot (configurable via a snapshotter callback)
  - environment/build metadata (app version, commit id, host name)

Implementation:

- `src/UKHO.Search/Pipelines/DeadLetter/DeadLetterRecord.cs`
- `src/UKHO.Search/Pipelines/DeadLetter/DeadLetterMetadata.cs`
- `src/UKHO.Search/Pipelines/DeadLetter/IDeadLetterMetadataProvider.cs`
- `src/UKHO.Search/Pipelines/DeadLetter/DefaultDeadLetterMetadataProvider.cs`
- `src/UKHO.Search/Pipelines/Nodes/DeadLetterSinkNode.cs`
- Tests: `test/UKHO.Search.Tests/Pipelines/DeadLetterSchemaTests.cs`

### 3.6 Dead-letter concurrency beyond a single process

Current state:

- In-process concurrent appends are serialized.
- Cross-process safety is enforced by acquiring exclusive writer access to the file (writer opens the file with `FileShare.Read` + retry loop).

### 3.7 Metrics: queue depth across nodes

Spec intent (spec §1.5 and §7.0+ instrumentation concepts): queue depth per node.

Current state:

- `queue_depth` is now meaningful for channel-driven nodes created via `BoundedChannelFactory`.
- Channels are wrapped with a counting decorator that tracks `Write - Read` and exposes `IQueueDepthProvider` on the reader.
- Base node types (`NodeBase`, `SinkNodeBase`, `MultiInputNodeBase`) automatically register queue depth providers with `NodeMetrics`.

Implementation:

- `src/UKHO.Search/Pipelines/Channels/IQueueDepthProvider.cs`
- `src/UKHO.Search/Pipelines/Channels/CountingChannel.cs`
- `src/UKHO.Search/Pipelines/Channels/CountingChannelReader.cs`
- `src/UKHO.Search/Pipelines/Channels/CountingChannelWriter.cs`
- `src/UKHO.Search/Pipelines/Channels/BoundedChannelFactory.cs`
- `src/UKHO.Search/Pipelines/Nodes/NodeBase.cs`
- `src/UKHO.Search/Pipelines/Nodes/SinkNodeBase.cs`
- `src/UKHO.Search/Pipelines/Nodes/MultiInputNodeBase.cs`
- Tests: `test/UKHO.Search.Tests/Pipelines/Metrics/CountingChannelQueueDepthTests.cs`

## 4. Additional hardening / design decisions still open

These items are not strictly mandated by the spec, but are likely required for productionizing the playground.

- **Node naming collisions**: node names are enforced as unique by `PipelineSupervisor.AddNode`.

- **`StopAsync` contract**: `PipelineSupervisor.StopAsync` is defined to cancel the pipeline and forward `StopAsync` to all nodes (nodes may override to perform cleanup).

- **Structured logging**: decision is to use the `ILogger` abstractions (via `Microsoft.Extensions.Logging.Abstractions`) in Domain and replace `Action<string>` callbacks across pipeline nodes.

## 5. Suggested next work items (proposed)

If continuing the numbered plan approach, suggested follow-on work items:

- Work Item 7: `BroadcastNode<T>` + tests (spec §7.5)
- Work Item 8: `MergeNode<T>` + `MultiInputNodeBase<T1,T2,TOut>` + tests (spec §3.2, §7.6)
- Work Item 9: `RouteNode<T>` + tests (spec §7.7)
- Work Item 10: `BulkIndexNode<TDocument>` spike + contract design + test strategy (spec §7.10)
- Work Item 11: Cancellation mode (Drain vs Immediate) + tests (spec §3.1)
- Work Item 12: Optional `MaxBytes` batching + batch context aggregation (spec §7.9)

## 6. Where to start next time

- Read `docs/002-pipeline-playground/002-pipeline-playground.spec.md` sections:
  - §3 (node lifecycle and base types)
  - §4 (error propagation)
  - §7.5–§7.10 (remaining node shapes)
- Review current tests under `test/UKHO.Search.Tests/Pipelines/*` for existing semantics.
- Use `docs/002-pipeline-playground/002-pipeline-playground.plan.md` as the authoritative record of completed work items.
