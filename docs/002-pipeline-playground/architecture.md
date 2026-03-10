# Architecture

## Overall Technical Approach

- **Runtime/Language**: .NET 10, C# latest
- **Primary primitive**: `System.Threading.Channels`
- **Core invariant**: strict per-key ordering, enforced by partitioning to lanes using a deterministic, stable hash of `Envelope.Key`.
- **Concurrency model**:
  - Pre-partition stages may run with bounded concurrency.
  - Post-partition, each lane is processed sequentially (single-threaded per lane) to guarantee ordering.
- **Backpressure**: all inter-node channels are **bounded** with `BoundedChannelFullMode.Wait` so upstream slows naturally.
- **Error semantics**:
  - **Message-scoped** (expected) errors: nodes set `Envelope.Status != Ok` and populate `Envelope.Error`; message is routed to error path (dead-letter) and pipeline continues.
  - **Pipeline-scoped** (fatal) errors: unexpected exceptions are caught at worker-task top-level, reported to supervisor, and output channels are faulted; supervisor cancels pipeline (FailFast default).
- **Observability**: each node emits `System.Diagnostics.Metrics` counters/histograms and logs include `Key`, `MessageId`, `NodeName`, `Attempt`.

```mermaid
flowchart LR
  SRC[SourceNode] --> VAL[ValidateNode]
  VAL --> PART[KeyPartitionNode (N lanes)]
  PART -->|lane 0..N-1| T1[Transform/Enrich per-lane]
  T1 --> MB[MicroBatchNode per-lane]
  MB --> BULK[Bulk-like Sink per-lane]
  VAL -->|failed| DLQ[DeadLetterSink]:::err
  BULK -->|failed| DLQ
classDef err fill:#ffd6d6,stroke:#cc0000,stroke-width:1px;
```

## Frontend

- Not in scope for this work package.
- The repository contains Blazor, but this specification is for a pipeline playground library and is implemented in the Domain project (`src/UKHO.Search/UKHO.Search.csproj`). Any future UI/dashboard should be hosted in a Host project under `src/Hosts/*` and consume this library.

## Backend

- **Domain (`src/UKHO.Search/UKHO.Search.csproj`)**
  - `Pipelines/Messaging`: `Envelope<T>`, `MessageContext`, `MessageStatus`
  - `Pipelines/Errors`: `PipelineErrorCategory`, `PipelineError`
  - `Pipelines/Nodes`: base node classes + standard nodes (source/validate/partition/transform/filter/batch/sinks)
  - `Pipelines/Supervision`: `PipelineSupervisor` (start/stop/cancel, completion, fatal capture)
  - `Pipelines/Metrics`: per-node instruments and helpers

- **Tests (`test/UKHO.Search.Tests`)**
  - Verifies invariants from spec:
    - deterministic per-key ordering
    - bounded backpressure behavior
    - message-scoped failure routing to dead-letter
    - fatal exception propagation and supervisor cancellation
    - retry blocking (strict ordering)
    - drain shutdown and microbatch flush
