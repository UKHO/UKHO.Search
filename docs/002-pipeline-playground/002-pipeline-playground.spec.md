# Pipeline Playground Specification (Key-Ordered, Channel-Based)  
**Target runtime:** .NET 10 (or latest available), C# latest  
**Primary building block:** `System.Threading.Channels`  
**Goal:** Provide a *node-graph* playground that developers can rapidly assemble and evolve into a production ingestion pipeline (e.g., Elasticsearch indexing), with **keyed ordering**, **backpressure**, **bounded concurrency**, and **well-defined error propagation**.

---

## 1. Design Goals

1. **Keyed ordering**
   - Messages with the same **Key** must be processed *in order* across all downstream stages that require ordering.
   - Different keys may be processed concurrently.

2. **Composable node graph**
   - Nodes are small, focused units: source, transform, filter, route, partition, batch, sink.
   - Graph can be changed quickly as ingestion “shape” evolves.

3. **Backpressure**
   - Use **bounded channels** between nodes to prevent unbounded memory growth.
   - When downstream is slow, upstream naturally slows.

4. **Clear error semantics**
   - Errors are either **message-scoped** (poison message, mapping error) or **pipeline-scoped** (node crash, broken dependency).
   - Error handling must be explicit, observable, and testable.
   - “Fail fast” must be supported, but also “continue with dead-letter”.

5. **Operational visibility**
   - Every node emits metrics: throughput, queue depth, latency, error counts.
   - Every message carries trace/correlation metadata.

---

## 2. Core Concepts

### 2.1 Message Envelope

All items flowing between nodes are wrapped in a single envelope type so:
- keyed ordering is intrinsic,
- metadata is consistent,
- error propagation is standardized.

**Envelope fields:**

- `MessageId` (`Guid`): unique ID per message
- `Key` (`string`): ordering key (document id / entity id / partition key)
- `TimestampUtc` (`DateTimeOffset`): creation time
- `CorrelationId` (`string?`): optional external correlation id
- `Attempt` (`int`): current attempt number (for retries)
- `Headers` (`IReadOnlyDictionary<string,string>`): optional metadata
- `Payload` (`TPayload`): the actual item for that stage
- `Context` (`MessageContext`): mutable tracking context (timings, breadcrumbs)
- `Status` (`MessageStatus`): Ok / Failed / Dropped / Retrying
- `Error` (`PipelineError?`): if not Ok, details of error

**MessageStatus enum:**
- `Ok`
- `Failed` (terminal failure, should go to dead-letter or be dropped)
- `Dropped` (filtered intentionally)
- `Retrying` (will be re-enqueued by retry policy)

**PipelineError fields:**
- `Category` (enum): Validation, Transform, Dependency, Timeout, BulkIndex, Unknown
- `Code` (`string`): stable code (e.g., `ES_MAPPING_CONFLICT`)
- `Message` (`string`): user-readable message
- `ExceptionType` (`string?`): optional, for diagnostics
- `ExceptionMessage` (`string?`)
- `StackTrace` (`string?`) – *optional*; only in dev / with opt-in
- `IsTransient` (`bool`)
- `OccurredAtUtc` (`DateTimeOffset`)
- `NodeName` (`string`)
- `Details` (`IReadOnlyDictionary<string,string>`): structured details (index name, field, etc.)

> **Rule:** Nodes should *not* throw exceptions for expected per-message failures. They should mark the envelope as `Failed` and populate `Error`. Exceptions are reserved for truly unexpected node/pipeline failures (bugs, invariants broken, fatal runtime failures).

---

## 3. Node Model

### 3.1 Node Lifecycle & Contracts

A node is a long-running component that reads from one or more `ChannelReader<T>` inputs and writes to one or more `ChannelWriter<T>` outputs.

**All nodes must implement:**
- `Name` (string)
- `StartAsync(ct)` to start internal workers
- `Completion` (Task) that completes when node stops
- `StopAsync()` optional (graceful stop trigger)

**Completion semantics:**
- If input completes and all messages are drained, node completes output channels and completes successfully.
- If node experiences a **fatal node exception**, node:
  1) Records fatal error to pipeline supervisor,
  2) Completes output channels with an exception,
  3) Transitions to completed state (faulted).

**Cancellation semantics:**
- Cancellation requests should stop new work, finish in-flight work if possible, and then complete.
- Nodes must support both:
  - **Graceful cancellation** (drain mode)
  - **Immediate cancellation** (stop now)

### 3.2 Node Base Types

To speed development, define the following standard base types:

#### `NodeBase<TIn, TOut>`
- One input, one output.
- Handles:
  - reading loop,
  - completion propagation,
  - bounded output write respecting backpressure,
  - metrics scaffolding,
  - centralized try/catch around *worker loops*.

#### `MultiInputNodeBase<T1, T2, TOut>`
- Two inputs, one output (e.g., merge).
- Requires fairness policy.

#### `SinkNodeBase<TIn>`
- One input, no output.

#### `SourceNodeBase<TOut>`
- No input, one output.

---

## 4. Error Handling & Propagation (Critical)

### 4.1 Error Classes

#### A) Message-scoped errors (expected)
Examples:
- Validation fails
- Cannot parse document
- Mapping conflict for a specific document
- External enrichment failed for one message

**Handling rule:**
- Node sets `envelope.Status = Failed` and fills `envelope.Error`.
- Node forwards the envelope to:
  - the primary output (if downstream should decide), or
  - a dedicated error output (recommended pattern),
  - or a dead-letter sink directly (if terminal at that stage).

**No exception is thrown** for these conditions.

#### B) Node-scoped fatal errors (unexpected)
Examples:
- Null reference / invariants broken
- Out-of-memory
- Channel write/read misuse
- Serializer bug causing crash
- Unhandled exception in worker loop

**Handling rule:**
- Node catches exceptions *at the top of each worker task*.
- Node reports the fatal error to `PipelineSupervisor`.
- Node completes output channels with the exception (so downstream stops quickly).
- The pipeline supervisor triggers cancellation to all nodes.

### 4.2 Propagation Paths

There are **three** propagation paths you must implement:

1) **Normal completion propagation**
   - Upstream completes channel → node drains → node completes downstream channel.

2) **Fault propagation**
   - Node faults → node completes its output channels with exception → downstream faults during read/write.
   - Supervisor observes first fatal error and cancels entire pipeline.

3) **Message failure propagation**
   - Envelope marked `Failed` flows along *data path* or *error path*.
   - Pipeline continues.

### 4.3 Retry Policy (Message-scoped)

Retries apply only to **transient** message failures (dependency timeout, HTTP 429, ES 429, etc.).

**Retry policy interface:**
- `ShouldRetry(envelope, error) -> bool`
- `GetDelay(attempt) -> TimeSpan` (e.g., exponential backoff with jitter)
- `MaxAttempts`

**Retry mechanism options:**
- Simple: node re-enqueues into its own input (requires input writer)
- Better: a dedicated `RetryNode` that receives failed envelopes and schedules reintroduction to a chosen stage

**Keyed ordering requirement with retries:**
- If a message for a given Key is retried, it must re-enter the same key-partition lane to preserve ordering.
- For “in-order” semantics:
  - either block subsequent messages of the same key until retry succeeds/fails,
  - or allow subsequent messages but enforce idempotency at sink.  
**This spec requires strict in-order processing per key**, therefore:
- **Block subsequent messages for the same key** while a prior message is retrying.

This implies the `KeyPartitionNode` must support **per-key sequencing** and **in-flight tracking** (details below).

### 4.4 Dead-letter Policy (Message-scoped)

Dead-letter is used when:
- non-transient error,
- retry attempts exhausted,
- or “fatal to message” (validation, mapping conflict, etc.)

Dead-letter should record:
- envelope + error,
- node name,
- raw input snapshot if possible,
- timestamps,
- environment details.

---

## 5. Keyed Ordering Design (Mandatory)

### 5.1 Key Partitioning Node

Introduce `KeyPartitionNode<T>` that:
- reads `Envelope<T>`
- uses `Key` to assign to one of **N partitions** (lanes)
- writes to `N` output channels, one per partition

**Partition algorithm:**
- Stable hash of `Key` modulo `N` (e.g., `xxHash32` or `HashCode.Combine` with stable string hash)
- Must be deterministic across process lifetime.

**Ordering guarantee:**
- All messages with the same `Key` always go to the same partition.
- Within a partition, messages are processed in FIFO order.

### 5.2 Per-Partition Worker Pipeline

After partitioning, each partition feeds a chain of nodes (or a “subgraph”) that processes sequentially *or with controlled parallelism that still preserves per-key order*.

**Simplest and strongest guarantee:**
- Process each partition with **single-threaded sequential** execution.  
This guarantees per-key order trivially, and allows concurrency by increasing partitions.

**Optional (advanced):**
- Within a partition, allow parallelism across different keys while still preserving order per key (complex: requires per-key queues + scheduler).  
This spec recommends the **single-threaded per-partition** model initially for reliability.

### 5.3 Retry Blocking

Because strict per-key order is required:
- A message that enters retry state must remain “in flight” for its partition.
- Subsequent messages in that partition must not pass it.

**Implementation approach (recommended):**
- Do **not** retry inside a stage that would “skip” the item.
- Instead, implement retries in the same sequential worker loop:
  - If transient failure, `await Task.Delay(delay)` and try again, **without dequeuing the next item**.

This keeps ordering simple and deterministic.

---

## 6. Channel Topology & Backpressure

### 6.1 Bounded Channels Between Nodes

All inter-node channels should be bounded. Recommended default capacities:
- pre-partition: 1,000–10,000 (depending on item size)
- per-partition: 100–1,000
- batch output: small (e.g., 10–50 batches)

**Channel options:**
- `BoundedChannelFullMode.Wait` (apply backpressure)
- SingleReader/SingleWriter flags where applicable for performance

### 6.2 Shutdown Semantics

Nodes must:
- complete outputs when inputs complete and all in-flight items are processed
- flush any internal buffers (e.g., microbatch) before completing output

---

## 7. Node Types (Detailed Specifications)

### 7.1 SourceNode<TOut>

**Purpose:** produce envelopes from external source.

**Inputs:** none  
**Outputs:** `ChannelWriter<Envelope<TOut>>`

**Responsibilities:**
- Create envelopes with Key, MessageId, TimestampUtc
- If source provides natural ordering per key, preserve it
- Support backpressure: `await output.WriteAsync(...)`

**Error handling:**
- If external source fails transiently, retry with backoff (source-scoped)
- If fatal, fault node and stop pipeline

**Variants:**
- `HttpIngestSourceNode`
- `QueueSourceNode`
- `FileWatcherSourceNode`
- `SyntheticSourceNode` (for testing)

---

### 7.2 TransformNode<TIn, TOut>

**Purpose:** map payload `TIn` → `TOut`.

**Inputs:** `Envelope<TIn>`  
**Outputs:** `Envelope<TOut>`

**Transform function:** `ValueTask<TOut> TransformAsync(TIn input, MessageContext ctx, CancellationToken ct)`

**Rules:**
- Must preserve envelope metadata (MessageId, Key, etc.)
- Update context breadcrumbs: `ctx.AddStep(Name, start, end)`
- On expected failure: mark envelope failed and include error

**Error handling:**
- Expected transform errors: produce failed envelope
- Unexpected exceptions: fatal node error (fault pipeline)

---

### 7.3 FilterNode<TIn>

**Purpose:** drop messages based on predicate.

**Inputs:** `Envelope<TIn>`  
**Outputs:** `Envelope<TIn>` (only those that pass)

**Predicate:** `ValueTask<bool> ShouldKeepAsync(Envelope<TIn> env, ct)`

**Semantics:**
- If predicate returns false: mark `Status=Dropped`, optionally send to diagnostics sink, and do not forward to main output.

**Error handling:**
- Predicate exceptions: message-scoped if known; otherwise fatal (configurable)

---

### 7.4 ValidateNode<TIn>

**Purpose:** validate payload and/or envelope.

**Inputs:** `Envelope<TIn>`  
**Outputs:** `Envelope<TIn>` and optional `ErrorOutput`

**Validation function:** returns `ValidationResult` with structured errors.

**Semantics:**
- On failure, set `Status=Failed` with `Category=Validation`
- Route to error output (recommended) and/or main output if downstream handles.

---

### 7.5 BroadcastNode<TIn>

**Purpose:** duplicate stream to multiple outputs (e.g., diagnostics + main).

**Inputs:** `Envelope<TIn>`  
**Outputs:** `k` outputs of same type

**Semantics:**
- Writes to all outputs.
- If any output is backpressured, broadcast slows (unless configured to “best effort” for non-critical outputs).

**Modes:**
- `AllMustReceive` (default): strict
- `BestEffort` for optional sinks (drop if slow)

---

### 7.6 MergeNode<TIn>

**Purpose:** merge multiple input streams into one output.

**Inputs:** `ChannelReader<Envelope<TIn>>[]`  
**Outputs:** `ChannelWriter<Envelope<TIn>>`

**Fairness policy:**
- round-robin attempt
- or `Task.WhenAny` read strategy with bounded buffering

**Ordering note:**
- Merge does **not** preserve any global ordering.
- Keyed ordering is preserved as long as each key stays in one lane; therefore merging should not mix lanes unless downstream re-partitions.

---

### 7.7 RouteNode<TIn>

**Purpose:** route messages to different outputs based on rules.

**Inputs:** `Envelope<TIn>`  
**Outputs:** `Dictionary<RouteKey, ChannelWriter<Envelope<TIn>>>`

**Routing function:** `RouteKey GetRoute(Envelope<TIn> env)`

**Ordering note:**
- Routing *before* partitioning is fine.
- Routing *after* partitioning must preserve per-key lane membership (i.e., route within lane or re-partition by same key).

---

### 7.8 KeyPartitionNode<TIn> (Required)

**Purpose:** enforce keyed ordering by partitioning to lanes.

**Inputs:** `Envelope<TIn>`  
**Outputs:** `ChannelWriter<Envelope<TIn>>[N]`

**Hash strategy:**
- stable hash of Key (avoid `string.GetHashCode()` because it is randomized by process)
- recommended: implement a stable hash (FNV-1a, xxHash) over UTF-8 bytes

**Semantics:**
- Always sends a given Key to the same lane.
- Lane channels are bounded.

**Error handling:**
- Missing/empty key is validation failure → dead-letter or error output.

---

### 7.9 MicroBatchNode<TIn> (Time/Size Batching)

**Purpose:** micro-batch “drip” messages into batches for efficiency.

**Inputs:** `Envelope<TIn>`  
**Outputs:** `BatchEnvelope<TIn>` where batch contains many envelopes

**Batch triggers (configurable):**
- `MaxItems` (e.g., 100–1000)
- `MaxBytes` (optional; requires size estimator)
- `MaxDelay` (e.g., 50–250ms)
- Flush on input completion

**Ordering requirement:**
- Must not reorder items within a lane.  
Therefore microbatch runs **per partition lane**.

**BatchEnvelope fields:**
- `BatchId` (Guid)
- `PartitionId` (int)
- `Items` (`IReadOnlyList<Envelope<TIn>>`)
- `CreatedUtc`, `FlushedUtc`
- aggregate context and metrics

**Error handling:**
- If internal buffer flush fails (write downstream), that’s a node-scoped fatal error
- Per-item errors should be handled before batching when possible

---

### 7.10 BulkIndexNode<TDocument> (Elasticsearch Sink)

**Purpose:** write documents to Elasticsearch efficiently.

**Inputs:** `BatchEnvelope<TDocument>` (recommended)  
**Outputs:** optional `IndexResultEnvelope<TDocument>` and error/dead-letter outputs

**Core behaviors:**
- Convert each item to a bulk operation (index/update/delete)
- Call Elasticsearch Bulk API
- Parse response item-by-item
- For each failed item:
  - classify transient vs non-transient
  - apply retry or dead-letter policy

**Ordering requirement:**
- Runs per partition lane, sequentially.
- Retries happen inline (block subsequent items for that lane).

**Retry classification:**
- transient: HTTP 429, 503, timeouts, connection issues
- non-transient: mapping exceptions, validation errors, 400-level request issues (except 429)

**Dead-letter payload should include:**
- document id (Key)
- index name
- error type/reason from Elasticsearch response
- original serialized document (if allowed)

**Exception handling:**
- Unexpected exceptions from client call (transport) are transient until max attempts, then fatal if repeated *and* policy says so.
- If Elasticsearch is down entirely, you can choose:
  - keep retrying indefinitely (dev mode), or
  - fail pipeline after `MaxDowntime`.

---

### 7.11 DeadLetterSinkNode<T>

**Purpose:** store failed envelopes.

**Inputs:** `Envelope<T>` (failed)  
**Outputs:** none

**Storage options:**
- local JSONL file (default for dev)
- SQLite
- blob storage (future)
- structured logging (Serilog)

**Requirements:**
- must never throw for a single bad message; log and continue
- if the sink cannot persist, that is pipeline-scoped fatal (configurable)

---

### 7.12 DiagnosticsSinkNode<T>

**Purpose:** capture operational telemetry and samples for devs.

**Inputs:** `Envelope<T>` (Ok/Failed/Dropped)  
**Outputs:** none

**Examples:**
- log every Nth message
- publish to in-memory dashboard
- update metrics counters

---

## 8. Pipeline Supervisor

### 8.1 Supervisor Responsibilities

- start nodes in correct order
- monitor node `Completion`
- capture first fatal exception
- trigger cancellation to all nodes on fatal error
- expose a unified `PipelineCompletion` task

### 8.2 Supervisor Error Policy

Configurable modes:

1) `FailFast` (default)
   - any node fatal exception stops entire pipeline

2) `IsolateNode`
   - only stop affected subgraph (advanced; not required initially)

### 8.3 Observability Hooks

Supervisor should collect:
- node state changes (starting/running/completed/faulted)
- pipeline start/stop times
- fatal error details

---

## 9. Metrics & Instrumentation

Use `System.Diagnostics.Metrics` and OpenTelemetry-compatible instruments.

Per node:
- `messages_in_total`
- `messages_out_total`
- `messages_failed_total`
- `messages_dropped_total`
- `processing_duration_ms` (histogram)
- `queue_depth` (observable gauge)
- `in_flight` (gauge)

Per partition lane:
- lane queue depth
- lane throughput
- lane “blocked by retry” time

---

## 10. Recommended Default Graph (Key-Ordered Ingestion)

A typical starting graph:

```mermaid
flowchart LR
  SRC[SourceNode] --> VAL[ValidateNode]
  VAL --> PART[KeyPartitionNode (N lanes)]
  PART -->|lane 0..N-1| T1[Transform/Enrich per-lane]
  T1 --> MB[MicroBatchNode per-lane]
  MB --> BULK[BulkIndexNode per-lane]
  VAL --> ERR[DeadLetterSink]:::err
  BULK --> ERR2[DeadLetterSink]:::err
classDef err fill:#ffd6d6,stroke:#cc0000,stroke-width:1px;
```

**Notes:**
- `Transform/Enrich` is per-lane to preserve ordering.
- Microbatch is per-lane.
- Bulk index is per-lane.
- Dead-letter is global.

---

## 11. Implementation Notes (Performance)

1. Prefer `ValueTask` for hot-path transforms.
2. Mark channels `SingleReader/SingleWriter` whenever true.
3. Avoid allocations:
   - reuse buffers for microbatch lists (ArrayPool)
   - avoid LINQ in hot path
4. Use stable hashing for keys.
5. Keep envelope immutable where possible; use `Context` for mutable metadata.
6. Use `ConfigureAwait(false)` in library code.

---

## 12. Testing Strategy

1. **Deterministic ordering test**
   - Generate messages with same Key and ensure output preserves order.

2. **Backpressure test**
   - Make sink slow; ensure source throttles without memory growth.

3. **Retry blocking test**
   - Force transient error on message #2 for a key; ensure message #3 waits.

4. **Poison message test**
   - Validation fails; ensure it goes to dead-letter and pipeline continues.

5. **Fatal error test**
   - Throw unexpected exception in node; ensure supervisor cancels all nodes.

6. **Drain shutdown test**
   - Complete source; ensure pipeline flushes microbatch and exits cleanly.

---

## 13. Deliverables (What to Build)

A developer implementing this spec should produce:

1. **Core library**
   - Envelope, error types, context
   - base node classes
   - standard nodes listed above
   - supervisor
   - metrics

2. **Sample app**
   - a minimal pipeline with:
     - synthetic source
     - validation
     - key partition (N=8)
     - transform
     - microbatch (MaxItems=200, MaxDelay=100ms)
     - dummy sink or Elasticsearch sink
   - CLI options to tune N, capacities, delays

3. **Dev UX**
   - a simple local dashboard endpoint (optional)
   - logs that include Key, MessageId, NodeName, Attempt

---

## 14. Exception Handling Rules (Summary)

**Rule 1:** Expected failures are represented as `Envelope.Status != Ok` and `Envelope.Error != null`.  
**Rule 2:** Worker loops must catch exceptions at the top-level; any uncaught exception is fatal.  
**Rule 3:** Fatal node exceptions propagate by faulting output channels and notifying supervisor.  
**Rule 4:** Message retries must not violate per-key order; therefore retries block the lane.  
**Rule 5:** Dead-letter must be reliable and must not crash on a single bad record (unless configured).

---

## 15. Glossary

- **Lane / Partition:** A single ordered stream for a subset of keys.
- **Backpressure:** When downstream is slow, bounded channels block upstream writes.
- **Microbatch:** Small time/size-based batching to reduce sink overhead.
- **Dead-letter:** Persistent storage of failed messages for inspection/replay.
