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

- **`BroadcastNode<TIn>`** (spec §7.5)
  - Modes: `AllMustReceive` (strict) and `BestEffort` (optional outputs)
  - Backpressure rule: if any required output is backpressured, broadcast slows
  - Test needs:
    - strict mode blocks when any output is slow
    - best-effort mode drops to optional outputs and continues
    - completion propagation to all outputs

- **`MergeNode<TIn>`** (spec §7.6)
  - Fairness policy: round-robin or `Task.WhenAny` style
  - Ordering note: no global ordering; safe only if lanes aren’t mixed (or downstream re-partitions)
  - Test needs:
    - merges two inputs without starvation
    - completion propagation with one upstream completing early
    - fault propagation from either upstream

- **`RouteNode<TIn>`** (spec §7.7)
  - Dictionary-based routing
  - Ordering note: routing after partitioning must preserve lane membership
  - Test needs:
    - correct routing
    - routing table missing a key (expected behavior must be defined: drop? fail message? fatal?)

- **`BulkIndexNode<TDocument>`** (spec §7.10)
  - Requires batch input (`BatchEnvelope<TDocument>`) and per-item response classification
  - Requires transient/non-transient classification based on response (e.g., 429/503)
  - Requires integration test strategy and probably an emulator or test double

### 2.2 Base types described by the spec but missing in code

- **`MultiInputNodeBase<T1, T2, TOut>`** (spec §3.2)
  - Needed for merge-like nodes and consistent error/fault semantics
  - Should include fairness policy hooks and consistent completion semantics

## 3. Spec alignment gaps (implemented, but not fully matching the spec)

### 3.1 Cancellation semantics: graceful vs immediate

Spec requirements (spec §3.1):

- Nodes must support **graceful cancellation (drain mode)** and **immediate cancellation (stop now)**.

Current state:

- Nodes are cancellation-responsive and won’t deadlock when idle.
- There is no explicit “drain mode” vs “stop now” policy surfaced.

Remaining work:

- Define a cancellation policy model (e.g., `CancellationMode.Immediate`/`Drain`) and integrate into node loops.
- Add tests proving:
  - drain mode completes after draining buffered items
  - immediate mode stops quickly and completes outputs deterministically

### 3.2 Retry policy interface mismatch vs spec

Spec (spec §4.3) describes a policy shaped like:

- `ShouldRetry(envelope, error) -> bool`
- `GetDelay(attempt) -> TimeSpan`
- `MaxAttempts`

Current implementation is exception-driven (via `isTransientException`) rather than `envelope + error` classification.

Remaining work:

- Decide whether to:
  - adapt `IRetryPolicy` to match the spec signature, or
  - document the deviation and keep exception-based classification for the playground.

### 3.3 Key hashing implementation detail

Spec (spec §7.8) recommends hashing UTF-8 bytes rather than `string.GetHashCode()`.

Current state:

- Deterministic hashing exists, but currently operates over `char` values.

Remaining work:

- Update partition hashing to use UTF-8 bytes if strict adherence is required.
- Add tests verifying stable partition assignment across known inputs (including non-ASCII keys).

### 3.4 Micro-batching: missing `MaxBytes` and aggregate context

Spec (spec §7.9) lists triggers:

- `MaxItems`
- `MaxDelay`
- `MaxBytes` (optional)

And `BatchEnvelope` should carry:

- “aggregate context and metrics”

Current state:

- `MaxItems` and `MaxDelay` supported.
- No `MaxBytes` trigger.
- `BatchEnvelope<T>` contains minimal fields (id, partition id, timestamps, items) and no aggregated context.

Remaining work:

- Add optional `MaxBytes` support (requires a size estimator).
- Decide what “aggregate context” means (e.g., union of breadcrumbs, min/max timestamps, counts by status).

### 3.5 Dead-letter record content

Spec (spec §4.4) suggests dead-letter should record:

- envelope + error
- node name
- raw input snapshot if possible
- timestamps
- environment details

Current state:

- JSONL contains envelope + error + node name + timestamp.
- No raw input snapshot or environment metadata.

Remaining work:

- Extend dead-letter schema to include optional environment/build metadata.
- Consider adding a raw input snapshot mechanism (requires defining what “raw” means per stage).

### 3.6 Dead-letter concurrency beyond a single process

Current state:

- In-process concurrent appends are serialized.

Remaining work:

- If multiple processes may write to the same file, a per-process lock is insufficient.
  - Decide: either document “single writer process” as an invariant, or implement a robust cross-process strategy.

### 3.7 Metrics: queue depth across nodes

Spec intent (spec §1.5 and §7.0+ instrumentation concepts): queue depth per node.

Current state:

- `queue_depth` is meaningful only for `MicroBatchNode` (buffer count).
- Most nodes report `0` (no queue depth provider).

Remaining work:

- Define queue depth signal per node type:
  - for channel-driven nodes, queue depth is typically “reader backlog”; `Channel` does not expose it directly
  - options:
    - wrap channels with a counting decorator
    - track in-flight + in/out deltas with sampling
    - emit only in-flight (and document queue depth limitations)

## 4. Additional hardening / design decisions still open

These items are not strictly mandated by the spec, but are likely required for productionizing the playground.

- **Node naming collisions**: metrics providers are keyed by node name.
  - Decide if node names must be unique (and enforce), or key providers by unique id.

- **`StopAsync` contract**: currently mostly a no-op.
  - Decide whether nodes must implement explicit shutdown hooks.

- **Structured logging**: spec implies logging + diagnostics sinks; current logging is callback-based.
  - Decide whether to integrate `ILogger` (would stay in Domain if used as an abstraction only).

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
