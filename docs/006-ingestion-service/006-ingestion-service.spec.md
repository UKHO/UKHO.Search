# Work Package: 006-ingestion-service — Ingestion Pipeline (Key-Ordered, Channel-Based)

**Target output path:** `docs/006-ingestion-service/006-ingestion-service.spec.md`

**Version:** v0.01 (Draft)

## 0. Related documents

- Pipeline runtime design (ordering/backpressure/error semantics): `docs/002-pipeline-playground/002-pipeline-playground.spec.md`
- Ingestion request model (input contract): `docs/004-ingestion-model-uplift/spec-overview-ingestion-model-uplift_v0.01.md`
- Ingestion pipeline design note (stage list and canonical document shape; enrichment deferred): `docs/006-ingestion-service/elasticsearch_ingestion_pipeline_design.md`

---

## 1. Overview

This work package implements the **ingestion pipeline** for UKHO.Search as a **composable node graph** built on `System.Threading.Channels`.

The pipeline must support:

- **Keyed ordering**: all operations for the same `Id` (document identifier) are processed in-order end-to-end.
- **Backpressure**: bounded channels between nodes ensure upstream naturally slows when downstream is saturated.
- **Bounded concurrency**: parallelism is achieved primarily via **N partitions/lanes**; each lane processes sequentially.
- **Clear error semantics**:
  - *Expected per-message failures* are represented as message status + structured error (not exceptions).
  - *Unexpected node failures* fault the node, fault downstream channels, and cancel the pipeline.
- **Operational visibility**: metrics and structured logs per node and per lane.

The pipeline consumes `UKHO.Search.Ingestion.Requests.IngestionRequest` messages (Add/Update/Delete/UpdateAcl). It produces Elasticsearch index operations.

This specification intentionally **does not define enrichment/business mapping logic** beyond structural requirements. Enrichment and canonical field mappings are future work.

---

## 2. Goals and Non-Goals

### 2.1 Goals

1. Use the existing reusable **pipeline runtime** in `UKHO.Search.Pipelines` (envelope, node base classes, supervisor) aligned to `002-pipeline-playground`.
2. Implement the **ingestion pipeline graph** for the ingestion service:
   - intake/source
   - validation
   - keyed partitioning
   - per-lane processing
   - optional micro-batching
   - Elasticsearch bulk indexing
   - dead-letter and diagnostics sinks
3. Ensure the pipeline uses the **current ingestion request contract** from `UKHO.Search.Ingestion` (work package 004).
4. Ensure the pipeline supports:
   - graceful drain shutdown
   - fail-fast cancellation on fatal node error
   - message-scoped failure propagation to dead-letter
   - retry for transient indexing failures without violating per-key ordering

### 2.2 Non-Goals

- Defining ingestion API endpoints, UI workflows, or transport mechanisms beyond the pipeline source abstraction.
- Defining the canonical search document mapping/enrichment logic (reference data, geo enrichment, semantic narrative generation).
- Defining the Elasticsearch index mapping itself.

---

## 3. High-level Components

### 3.1 Domain contracts (existing)

- `UKHO.Search.Ingestion.Requests.IngestionRequest`
- `UKHO.Search.Ingestion.Requests.IngestionRequestType`
- `UKHO.Search.Ingestion.Requests.AddItemRequest` / `UpdateItemRequest` / `DeleteItemRequest` / `UpdateAclRequest`

### 3.2 Pipeline runtime (existing)

The ingestion pipeline must be implemented using the **existing pipeline runtime library** in the Domain project `src/UKHO.Search/UKHO.Search.csproj` under the `UKHO.Search.Pipelines` namespace.

Core primitives (already implemented):

- Messaging:
  - `UKHO.Search.Pipelines.Messaging.Envelope<TPayload>`
  - `UKHO.Search.Pipelines.Messaging.MessageContext`
  - `UKHO.Search.Pipelines.Messaging.MessageStatus`
- Errors:
  - `UKHO.Search.Pipelines.Errors.PipelineError`
  - `UKHO.Search.Pipelines.Errors.PipelineErrorCategory`
- Nodes/base types:
  - `UKHO.Search.Pipelines.Nodes.INode`
  - `UKHO.Search.Pipelines.Nodes.NodeBase<TIn,TOut>`
  - `UKHO.Search.Pipelines.Nodes.SourceNodeBase<TOut>`
  - `UKHO.Search.Pipelines.Nodes.SinkNodeBase<TIn>`
  - `UKHO.Search.Pipelines.Nodes.MultiInputNodeBase<T1,T2,TOut>`
- Standard nodes:
  - `UKHO.Search.Pipelines.Nodes.ValidateNode<TPayload>`
  - `UKHO.Search.Pipelines.Nodes.TransformNode<TIn,TOut>`
  - `UKHO.Search.Pipelines.Nodes.RetryingTransformNode<TIn,TOut>`
  - `UKHO.Search.Pipelines.Nodes.KeyPartitionNode<TPayload>`
  - `UKHO.Search.Pipelines.Nodes.MicroBatchNode<TPayload>`
  - `UKHO.Search.Pipelines.Nodes.DeadLetterSinkNode<TPayload>`
  - `UKHO.Search.Pipelines.Nodes.BroadcastNode<TPayload>` / `MergeNode<TPayload>` / `RouteNode<TPayload>`
- Supervision:
  - `UKHO.Search.Pipelines.Supervision.PipelineSupervisor`
- Channels/backpressure helpers:
  - `UKHO.Search.Pipelines.Channels.BoundedChannelFactory`
  - `UKHO.Search.Pipelines.Channels.CountingChannel` (queue depth for metrics)

### 3.3 Ingestion pipeline (to be implemented)

A concrete graph assembled from **existing standard nodes** (`UKHO.Search.Pipelines.Nodes.*`) plus a small number of ingestion-specific nodes/adapters:

- `IngestionSourceNode` (monitors one Azure Storage Queue per registered `IIngestionDataProviderFactory`)
- `ValidateNode<IngestionRequest>` and/or an ingestion-specific validation node (as needed)
- `TransformNode<IngestionRequest, ...>` / `RetryingTransformNode<IngestionRequest, ...>` to dispatch/build indexing operations
- `KeyPartitionNode<T>` (N lanes) for keyed ordering
- `MicroBatchNode<T>` per lane
- An ingestion-specific Elasticsearch indexing node implemented on top of `INode` / `NodeBase` (see §6.6)
- `DeadLetterSinkNode<T>` or an ingestion-specific dead-letter sink implemented on top of `SinkNodeBase` (see §6.7)

#### v0.01 scope note (provider-specific)

In v0.01, the ingestion pipeline is wired with the **File Share ingestion provider** (`UKHO.Search.Ingestion.Providers.FileShare.FileShareIngestionDataProvider`). The graph is still assembled in a way that supports registering additional `IIngestionDataProviderFactory` instances later; in this slice only the File Share provider is expected to be registered.

### 3.4 Infrastructure integrations (existing + to be extended)

- Elasticsearch client: `Elastic.Clients.Elasticsearch.ElasticsearchClient` (already used in `UKHO.Search.Infrastructure.Ingestion`)
- Provider discovery: `UKHO.Search.Services.Ingestion.Providers.IIngestionProviderService`
- Provider factories: `UKHO.Search.Ingestion.Providers.IIngestionDataProviderFactory` + `CreateProvider()`

### 3.5 Code ownership / project boundaries (v0.01)

To keep responsibilities clear:

- The File Share provider project (`src/UKHO.Search.Ingestion.Providers.FileShare`) owns the **provider-specific ingestion processing graph**, including the parts that are expected to evolve with File Share domain knowledge:
  - reading input messages from the File Share queue (one queue per provider factory)
  - deserializing to `IngestionRequest`
  - mapping `AddItemRequest` / `UpdateItemRequest` into canonical indexing payloads (structural canonical document in v0.01)
  - any file-share-specific parsing/extraction/enrichment nodes
  - a provider-level entrypoint method that takes a queue input and starts the ingestion graph (invoked by the ingestion host)

- `src/UKHO.Search.Infrastructure.Ingestion` owns **shared infrastructure adapters and primitives** (source-agnostic) such as:
  - Azure queue client abstractions (`IQueueClient`, `IQueueClientFactory`) and message ack/visibility/poison mechanics
  - Elasticsearch bulk client adapter(s)
  - dead-letter persistence (Blob) and diagnostics sinks

This keeps File Share-specific ingestion behavior and pipeline composition with the provider, while keeping external system adapters and reusable plumbing in Infrastructure.

---

## 4. Pipeline Topology (v0.01)

### 4.1 Key definition

- The pipeline ordering key is the **document Id** from the request payload:
  - `AddItemRequest.Id`
  - `UpdateItemRequest.Id`
  - `DeleteItemRequest.Id`
  - `UpdateAclRequest.Id`

This `Id` is used as the envelope `Key`.

### 4.2 Recommended default graph

```mermaid
flowchart LR
  SRC[IngestionSourceNode] --> VAL[IngestionRequestValidateNode]
  VAL --> PART[KeyPartitionNode (N lanes)]

  PART -->|lane 0..N-1| DISPATCH[Dispatch / Build canonical op]
  DISPATCH --> MB[MicroBatchNode per-lane]
  MB --> ES[ElasticsearchBulkIndexNode per-lane]

  VAL --> ERR[DeadLetterSinkNode]:::err
  ES --> ERR2[DeadLetterSinkNode]:::err

  DISPATCH --> DIAG[DiagnosticsSinkNode]:::diag
  ES --> DIAG2[DiagnosticsSinkNode]:::diag

classDef err fill:#ffd6d6,stroke:#cc0000,stroke-width:1px;
classDef diag fill:#e8f4ff,stroke:#0066cc,stroke-width:1px;
```

### 4.3 Partition/lane model

- The pipeline uses **N lanes** to achieve concurrency.
- Each lane is processed by a **single sequential worker** to guarantee strict per-key ordering.
- N is configurable via `IConfiguration` key `ingestion:laneCount`.
- Default (v0.01): `ingestion:laneCount = 8` (configured per environment in `configuration/configuration.json`).

### 4.4 Configuration (v0.01)

All ingestion pipeline settings are supplied via `IConfiguration` from the environment-specific `ingestion` section in `configuration/configuration.json`.

- Settings are read in service code via `:`-delimited paths, e.g. `config["ingestion:laneCount"]`.

The following keys are required in v0.01:

- `ingestion:indexname`
- `ingestion:laneCount`
- `ingestion:channelCapacityPrePartition`
- `ingestion:channelCapacityPerLane`
- `ingestion:channelCapacityMicrobatchOut`
- `ingestion:documentTypePlaceholder`
- `ingestion:queueReceiveBatchSize`
- `ingestion:filesharequeuename`
- `ingestion:queueVisibilityTimeoutSeconds`
- `ingestion:queueVisibilityRenewalSeconds`
- `ingestion:queuePollingIntervalMilliseconds`
- `ingestion:queueMaxDequeueCount`
- `ingestion:poisonQueueSuffix`
- `ingestion:microbatchMaxItems`
- `ingestion:microbatchMaxDelayMilliseconds`
- `ingestion:indexRetryMaxAttempts`
- `ingestion:indexRetryBaseDelayMilliseconds`
- `ingestion:indexRetryMaxDelayMilliseconds`
- `ingestion:indexRetryJitterMilliseconds`
- `ingestion:deadletterContainer`
- `ingestion:deadletterBlobPrefix`

---

## 5. Message Envelope & Error Semantics

The ingestion pipeline adopts the envelope + error rules from `002-pipeline-playground`.

### 5.1 Envelope shape

Each message is represented as `Envelope<T>` containing:

- `MessageId` (`Guid`)
- `Key` (`string`)
- `TimestampUtc` (`DateTimeOffset`)
- `CorrelationId` (`string?`)
- `Attempt` (`int`)
- `Headers` (`IReadOnlyDictionary<string,string>`)
- `Payload` (`T`)
- `Context` (`MessageContext`)
- `Status` (`MessageStatus`: Ok/Failed/Dropped/Retrying)
- `Error` (`PipelineError?`)

### 5.2 Message-scoped failures (expected)

Examples:

- invalid/missing `Id`
- invalid JSON payload from source
- unsupported request type
- validation errors for required fields

Handling:

- Node sets `Status = Failed` and populates `Error`.
- Envelope is routed to a dead-letter path (preferred) and/or continues on a diagnostics path.
- No exception is thrown for these failures.

### 5.3 Node-scoped fatal failures (unexpected)

Examples:

- unhandled exception in a worker loop
- channel misuse / invariants broken

Handling:

- Node faults; output channels are completed with an exception.
- Supervisor cancels the entire pipeline (default policy: fail-fast).

---

## 6. Node Responsibilities (Ingestion-Specific)

### 6.1 File Share provider ingestion graph (provider-owned)

Purpose: the File Share provider owns the ingestion graph that reads from the File Share queue and produces canonical index operations.

Requirements:

- Transport: **Azure Storage Queues**.
- The service registers one or more `IIngestionDataProviderFactory` instances.
- When the service starts, **a queue must be monitored for each factory present**.
- For File Share (v0.01), the provider owns the graph that:
  - listens to `IIngestionDataProviderFactory.QueueName`
  - reads queue messages and deserializes them to `IngestionRequest`
  - maps `AddItemRequest` / `UpdateItemRequest` to a structural canonical document and emits the resulting `IndexOperation` path

Provider entrypoint:

- The File Share provider must expose a method that takes a queue input (queue client / queue reader abstraction) and starts the ingestion graph.
- The ingestion host calls this provider entrypoint from its hosted service to start/stop ingestion.

Implementation notes (current codebase):

- The provider-owned entrypoint is `UKHO.Search.Ingestion.Providers.FileShare.Pipeline.FileShareIngestionGraph.BuildAzureQueueBacked(...)`.
- Queue-backed ingestion is started via a single Infrastructure entrypoint `UKHO.Search.Infrastructure.Ingestion.Pipeline.FileShareIngestionPipelineAdapter.BuildAzureQueueBacked(...)`, which supplies Infrastructure-owned node implementations/adapters (queue source, bulk indexing, dead-letter sinks, diagnostics, ack) and invokes the provider-owned entrypoint.
- The ingestion host’s `IngestionPipelineHostedService` uses this adapter; there should be no other queue-backed graph builder entrypoints.

Error handling:

- Source transport errors are *node-scoped* (retry policy TBD) or fatal.
- Bad message deserialization is *message-scoped* → dead-letter.

Queue acknowledgement / delete semantics (v0.01):

- Azure Storage Queue processing must be treated as **at-least-once**.
- A queue message must only be deleted **after** the corresponding pipeline envelope has reached a terminal outcome:
  - successfully indexed (or successfully applied for delete/ACL update), or
  - persisted to dead-letter as terminal failed.
- While a message is in-flight, the ingestion worker must ensure the queue message does not reappear by:
  - setting an appropriate initial visibility timeout, and
  - renewing/extending visibility while processing (implementation detail; must not break keyed ordering).

Idempotency requirement:

- Indexing operations must be idempotent to tolerate retries and duplicate deliveries.
- The Elasticsearch document `_id` must be derived from the request `Id` (`Envelope.Key`) so reprocessing results in overwrite/upsert rather than duplicate documents.

Poison handling (Azure Storage Queues):

- If a message cannot be processed after `MaxDequeueCount` attempts (configurable), it must be moved to a dedicated poison queue.
- Poison queue naming: `<QueueName><poisonQueueSuffix>`.
- Poison messages must include the original body and diagnostic metadata (e.g., dequeue count, last error, timestamp).

Configuration (v0.01):

- `ingestion:queueReceiveBatchSize = 16`
- Provider queue names:
  - File share queue name: `ingestion:filesharequeuename = "file-share-queue"` (renamed from `ingestion:queuename`)
- `ingestion:queueVisibilityTimeoutSeconds = 300`
- `ingestion:queueVisibilityRenewalSeconds = 60`
- `ingestion:queuePollingIntervalMilliseconds = 1000`
- `ingestion:queueMaxDequeueCount = 5`
- `ingestion:poisonQueueSuffix = "-poison"`

### 6.2 `IngestionRequestValidateNode`

Purpose: validate the envelope and the `IngestionRequest` payload.

Requirements:

- Validate that exactly one payload (`AddItem`, `UpdateItem`, `DeleteItem`, `UpdateAcl`) is present (already enforced by the model constructor on deserialization, but validation must still be defensive).
- Validate `Id` and `SecurityTokens` rules per request type.
- On failure: create `PipelineError` with category `Validation`.

### 6.3 `IngestionRequestDispatchNode`

Purpose: convert a validated request into a **pipeline operation** suitable for indexing.

Output operation types (suggested; subject to refinement):

- `UpsertOperation` (for AddItem/UpdateItem)
- `DeleteOperation` (for DeleteItem)
- `AclUpdateOperation` (for UpdateAcl)

Requirements:

- Preserve envelope metadata and context.
- Avoid business-specific enrichment in v0.01.
- For `UpsertOperation`, the node produces a **structural canonical document** that:
  - preserves source inputs for traceability
  - matches the canonical top-level structure from `elasticsearch_ingestion_pipeline_design.md`:
    - `documentId`
    - `documentType`
    - `source`
    - `normalized`
    - `descriptions`
    - `search`
    - `facets`
    - `quality`
    - `provenance`
  - contains minimal/empty values for these sections (no enrichment logic), but must be valid JSON for indexing
  - sets `documentType` to a placeholder value in v0.01. Configuration: `ingestion:documentTypePlaceholder = "unknown"`. A later work package will derive `documentType` by inspecting/classifying the associated content.

### 6.4 Content extraction stages (structural)

The ingestion pipeline must be designed to support the following stages as optional nodes, but the detailed mapping/enrichment behaviour is deferred:

- archive unpacking (e.g., ZIP)
- file classification
- structured extraction from XML
- document text extraction (e.g., PDFs/Office) via an adapter interface

v0.01 requirement:

- define clear extension points (interfaces) for these extractors
- ensure they can be added as nodes in the per-lane chain without breaking ordering/backpressure

### 6.5 `MicroBatchNode`

Purpose: batch operations to improve Elasticsearch bulk throughput.

Requirements:

- Runs per-lane.
- Batches by size and/or time.
- Must not reorder items.
- Flushes on input completion.

Configuration (v0.01):

- `ingestion:microbatchMaxItems = 200`
- `ingestion:microbatchMaxDelayMilliseconds = 100`

### 6.6 `ElasticsearchBulkIndexNode`

Purpose: execute bulk operations against Elasticsearch.

Implementation note:

- The `UKHO.Search.Pipelines` library includes `UKHO.Search.Pipelines.Nodes.BulkIndexNode<TDocument>` and related types (`IBulkIndexClient<TDocument>`, `BulkIndexRequest<TDocument>`, `BulkIndexResponse`).
- The ingestion service must reuse these primitives where possible.

Dependency injection (existing):

- The ingestion host already configures the Elasticsearch client in `src/Hosts/IngestionServiceHost/Program.cs` via `builder.AddElasticsearchClient(ServiceNames.ElasticSearch)`.
- The ingestion pipeline indexing adapter/client must use **this configured `Elastic.Clients.Elasticsearch.ElasticsearchClient` instance** via constructor dependency injection.
- The ingestion pipeline must not create/configure its own `ElasticsearchClient`.

Ordering + retry requirement:

- This work package requires **strict in-order processing per key** (retries must block the lane).
- The ingestion pipeline must therefore implement Elasticsearch retry behaviour **inline within the per-lane indexing path** (block the lane while retrying). This is done by composing the existing pipeline primitives into a per-lane “indexing subgraph” as described below.

#### 6.6.1 Per-lane indexing subgraph (concrete build recipe)

For each partition/lane `p` (0..`ingestion:laneCount-1`), the ingestion pipeline must build the following node + channel chain.

**Channels (all bounded, all created with `BoundedChannelFactory.Create(...)` so they are `CountingChannel<T>`):**

| Channel name (suggested) | Type | Created with | Capacity setting | Producer | Consumer |
|---|---|---:|---|---|---|
| `laneDispatch[p]` | `Envelope<IndexOperation>` | `BoundedChannelFactory.Create(..., singleReader:true, singleWriter:true)` | `ingestion:channelCapacityPerLane` | `KeyPartitionNode` | `MicroBatchNode` |
| `laneBatches[p]` | `BatchEnvelope<IndexOperation>` | `BoundedChannelFactory.Create(..., singleReader:true, singleWriter:true)` | `ingestion:channelCapacityMicrobatchOut` | `MicroBatchNode` | `InOrderBulkIndexNode` |
| `laneIndexedOk[p]` (optional) | `Envelope<IndexOperation>` | `BoundedChannelFactory.Create(..., singleReader:true, singleWriter:true)` | `ingestion:channelCapacityPerLane` | `InOrderBulkIndexNode` | diagnostics/terminal sink |
| `deadLetter` (shared) | `Envelope<IndexOperation>` | `BoundedChannelFactory.Create(..., singleReader:false, singleWriter:false)` | `ingestion:channelCapacityPrePartition` | multiple nodes | dead-letter sink |

Notes:

- Every channel above is a `CountingChannel<T>`; nodes will receive `ChannelReader<T>` instances that expose queue depth via `IQueueDepthProvider`, which is consumed by `NodeBase` metrics.
- `IndexOperation` is an ingestion-specific payload type representing Add/Update/Delete/ACL operations after dispatch. It is a placeholder in v0.01.

**Nodes (per lane):**

1) `MicroBatchNode<IndexOperation>`
   - Input: `laneDispatch[p].Reader`
   - Output: `laneBatches[p].Writer`
   - Args:
     - `partitionId = p`
     - `maxItems = ingestion:microbatchMaxItems`
     - `maxDelay = TimeSpan.FromMilliseconds(ingestion:microbatchMaxDelayMilliseconds)`
   - Rationale: reuse the existing `UKHO.Search.Pipelines.Nodes.MicroBatchNode<TPayload>` implementation.

2) `InOrderBulkIndexNode` (ingestion-specific node; implemented on top of `NodeBase<BatchEnvelope<IndexOperation>, Envelope<IndexOperation>>` or `INode`)
   - Input: `laneBatches[p].Reader`
   - Outputs:
     - success → `laneIndexedOk[p].Writer` (optional; can also route to a shared diagnostics sink)
     - failures → `deadLetter.Writer`
   - Uses:
     - `Elastic.Clients.Elasticsearch.ElasticsearchClient` via an adapter implementing `UKHO.Search.Pipelines.Nodes.IBulkIndexClient<TDocument>`.
   - Retry policy:
     - Implements **inline retry** on transient failures using the config values in §6.6.2.
     - While retrying a batch, it must not read/process subsequent batches for that lane.

3) Optional: diagnostics/terminal sinks using existing `CollectingSinkNode<T>` or an ingestion-specific sink.

#### 6.6.2 Bulk indexing retry behaviour (exact)

The lane must be treated as strictly sequential. The indexing node must:

- Attempt to index the batch.
- If the bulk call fails with a transient error (timeout/network/HTTP 429/503):
  - increment `Envelope.Attempt` for each affected envelope,
  - delay using exponential backoff with jitter (`ingestion:indexRetry*` settings),
  - retry the batch **without allowing later batches to proceed**.
- If retries are exhausted:
  - mark affected envelopes as `Failed` with `PipelineErrorCategory.BulkIndex`
  - write them to the `deadLetter` channel.

Retry behaviour (must preserve ordering):

- Retries are inline within the lane worker loop.
- While retrying an item, the lane must not process subsequent items.
- Transient classification:
  - retryable: HTTP 429, 503, timeouts, connection issues
  - non-retryable: mapping errors, validation errors, most 400s (excluding 429)

Dead-letter behaviour:

- Non-transient failures or exhausted retries → mark failed and send to dead-letter.

Retry configuration (v0.01):

- `ingestion:indexRetryMaxAttempts = 5`
- `ingestion:indexRetryBaseDelayMilliseconds = 200`
- `ingestion:indexRetryMaxDelayMilliseconds = 5000`
- `ingestion:indexRetryJitterMilliseconds = 250`

### 6.7 `DeadLetterSinkNode`

Purpose: persist failed envelopes (including validation failures, poison messages, and exhausted retries) for inspection and replay.

Storage (v0.01): **Azure Blob Storage**.

Implementation note:

- The `UKHO.Search.Pipelines` library includes `UKHO.Search.Pipelines.Nodes.DeadLetterSinkNode<TPayload>` which persists to a local JSONL file.
- For this work package, the ingestion service must implement an Azure Blob-backed dead-letter sink node using the existing pipeline primitives (`SinkNodeBase<Envelope<T>>`, `DeadLetterRecord<T>`, `IDeadLetterMetadataProvider`), while keeping the same envelope/error semantics.

Dependency injection (existing):

- The ingestion host already configures Azure Blob Storage client integration in `src/Hosts/IngestionServiceHost/Program.cs` via `builder.AddAzureBlobServiceClient(ServiceNames.Blobs)`.
- The blob-backed dead-letter sink must use **this configured blob client instance** via constructor dependency injection.
- The ingestion pipeline must not create/configure its own blob client.

Requirements:

- Store dead-letter items as JSON blobs.
- Blob container name must be configurable.
- Configuration (v0.01): `ingestion:deadletterContainer = "ingestion-deadletter"`.
- Configuration (v0.01): `ingestion:deadletterBlobPrefix = "deadletter"`.
- Blob naming must be deterministic and partition-friendly (suggested pattern):
  - `<deadletterBlobPrefix>/yyyy/MM/dd/<Key>/<MessageId>.json`
- Payload must include:
  - the envelope (including `Status` and `Error`)
  - a timestamp for persistence
  - source metadata when available (e.g., Azure queue message id, dequeue count)

Reliability:

- The sink must not throw for a single malformed message body; it must attempt to persist what it can and continue.
- If the sink cannot persist to Blob Storage due to configuration or dependency failures, this is treated as a **pipeline-scoped fatal error** (fail-fast by default).

Operational configuration (optional):

- `ingestion:deadletterFatalIfCannotPersist` (bool, default: `true`): when `false`, the dead-letter sink logs and continues if Blob persistence fails (message remains un-acked so it can be retried).

---

## 7. Backpressure & Channel Configuration

- All inter-node channels are **bounded**.
- Default capacities (v0.01; configurable per environment in `configuration/configuration.json` under `ingestion`):
  - pre-partition: `ingestion:channelCapacityPrePartition = 500`
  - per-lane: `ingestion:channelCapacityPerLane = 100`
  - microbatch output: `ingestion:channelCapacityMicrobatchOut = 10`
- `BoundedChannelFullMode.Wait` must be used for backpressure.

Configuration:

- `ingestion:channelCapacityPrePartition = 500`
- `ingestion:channelCapacityPerLane = 100`
- `ingestion:channelCapacityMicrobatchOut = 10`

---

## 8. Shutdown, Completion, and Supervision

### 8.1 Supervisor

The `PipelineSupervisor`:

- starts nodes in order
- monitors `Completion` tasks
- records the first fatal exception
- triggers cancellation (fail-fast policy)
- exposes a unified pipeline completion task

### 8.2 Graceful shutdown

- When the source completes, downstream nodes must drain and then complete.
- Nodes with internal buffers (microbatch) must flush before completion.
- On shutdown/cancellation, in-flight Azure queue messages must not be deleted unless the terminal outcome has been reached; they should become visible again for retry.

---

## 9. Observability

### 9.1 Metrics

Use `System.Diagnostics.Metrics` (OpenTelemetry compatible) via the existing pipeline runtime metrics in `UKHO.Search.Pipelines.Metrics.NodeMetrics`.

Metric names emitted by the pipeline runtime:

- `ukho.pipeline.node.in`
- `ukho.pipeline.node.out`
- `ukho.pipeline.node.failed`
- `ukho.pipeline.node.dropped`
- `ukho.pipeline.node.duration_ms`
- `ukho.pipeline.node.inflight` (observable gauge)
- `ukho.pipeline.node.queue_depth` (observable gauge)

Per lane:

- lane queue depth
- lane throughput
- lane time spent blocked by retry

### 9.2 Logging

- Use `ILogger` (per repo logging standard).
- Log entries must include at minimum:
  - `NodeName`
  - `Key` (Id)
  - `MessageId`
  - `Attempt`
  - `CorrelationId` (if present)

---

## 10. Testing Strategy

The ingestion pipeline must include automated tests that cover:

1. Deterministic per-key ordering.
2. Backpressure behaviour (slow sink throttles source).
3. Retry blocking (message #3 does not pass #2 while #2 retries).
4. Poison message routing to dead-letter.
5. Fatal node exception cancels pipeline.
6. Drain shutdown flushes microbatch and completes.

Test location guidance:

- place tests in `test/UKHO.Search.Ingestion.Tests`.

---

## 11. Deliverables

1. Ingestion pipeline assembly/wiring (graph builder) used by the ingestion host, built on `UKHO.Search.Pipelines`.
2. Implementations:
   - validation
   - partitioning
   - dispatch to operations
   - microbatch
   - Elasticsearch bulk indexing
   - dead-letter sink (Azure Blob Storage)
3. Metrics and structured logging.
4. Automated tests listed in §10.

---

## 12. Open Questions (v0.01)

None.
