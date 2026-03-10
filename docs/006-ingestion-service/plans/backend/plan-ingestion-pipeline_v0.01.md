# Implementation Plan

**Target output path:** `docs/006-ingestion-service/plans/backend/plan-ingestion-pipeline_v0.01.md`

**Based on:** `docs/006-ingestion-service/006-ingestion-service.spec.md` (v0.01)

**Architecture reference (must read alongside this plan):** `docs/006-ingestion-service/architecture-ingestion-pipeline_v0.01.md`

## Baseline (current implemented)
- `UKHO.Search.Pipelines` runtime exists (envelopes, nodes, partitioning, micro-batching, channels, metrics, supervisor).
- `src/Hosts/IngestionServiceHost/Program.cs` already configures external clients used by the ingestion pipeline:
  - Elasticsearch via `builder.AddElasticsearchClient(ServiceNames.ElasticSearch)`
  - Azure Queue Storage via `builder.AddAzureQueueServiceClient(ServiceNames.Queues)`
  - Azure Blob Storage via `builder.AddAzureBlobServiceClient(ServiceNames.Blobs)`
- Ingestion provider factories and bootstrap services are registered via `builder.Services.AddIngestionServices()`.
- Environment configuration contains the required `ingestion:*` keys in `configuration/configuration.json`.

## Delta (planned changes)
Implement the ingestion pipeline end-to-end using `UKHO.Search.Pipelines` primitives:
- Azure Queue-backed `IngestionSourceNode` (one queue per provider factory).
- Validation + dispatch to an ingestion indexing operation type.
- Keyed partitioning and per-lane sequential processing.
- Per-lane micro-batching.
- Elasticsearch bulk indexing with **inline retry that blocks the lane**.
- Blob-backed dead-letter sink (persist `DeadLetterRecord<TPayload>`).
- Diagnostics sinks.
- Supervisor + hosted service integration.
- Automated tests described in the spec.

## Carry-over / Deferred
- Enrichment/business mapping logic beyond structural canonical document requirements.
- Concrete document text extraction implementation (extension point only; e.g., Kreuzberg to be added later).

---

## Slice 1 — Runnable pipeline skeleton (in-process) to validate wiring + ordering
- [x] **Work Item 1: Ingestion pipeline can run end-to-end in-process with synthetic source and stub sinks** - Completed
  - **Purpose**: Establish a runnable pipeline host, verify partitioning/ordering/backpressure mechanics before adding Azure Queue/Elasticsearch/Blob.
  - **Acceptance Criteria**:
    - A pipeline graph can be started/stopped via a hosted service.
    - Messages flow through `Validate` → `KeyPartitionNode` → per-lane `MicroBatchNode` → stub index sink.
    - Per-key ordering is demonstrably preserved in the stub sink.
  - **Definition of Done**:
    - Pipeline wiring code exists in Infrastructure and is started by the host.
    - Unit tests cover per-key ordering for the stub index path.
    - Structured logs contain `NodeName`, `Key`, `MessageId`, `Attempt`.
    - Can execute end-to-end via: `dotnet run --project src/Hosts/IngestionServiceHost`
  - [x] Task 1.1: Define ingestion pipeline contracts and operation payloads (minimal) - Completed
    - [x] Step: Create an `IndexOperation` model (and subtypes or discriminator) representing Upsert/Delete/ACL-update.
    - [x] Step: Create a minimal `CanonicalDocument` model that matches the structural top-level shape required by `elasticsearch_ingestion_pipeline_design.md` (empty/minimal payloads allowed).
    - [x] Step: Ensure the document `_id` is derived from `Envelope.Key`.
    - **Task 1.1 Summary**: Added `IndexOperation` + concrete operation types and a minimal `CanonicalDocument` model under `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/`.
  - [x] Task 1.2: Create a pipeline graph builder (infrastructure) - Completed
    - [x] Step: Implement an `IngestionPipelineBuilder` in `UKHO.Search.Infrastructure.Ingestion` that:
      - Creates all bounded channels using `BoundedChannelFactory.Create(...)`.
      - Wraps the channels as `CountingChannel<T>` and passes `Reader`/`Writer` to nodes.
      - Adds nodes to `PipelineSupervisor` with unique names.
    - [x] Step: Wire per-lane channels exactly as described in the spec (§6.6.1).
    - **Task 1.2 Summary**: Implemented `IngestionPipelineBuilder` + `IngestionPipelineGraph` and created per-lane `CountingChannel` dispatch/microbatch channels wired through `KeyPartitionNode` and `MicroBatchNode` into a stub sink.
  - [x] Task 1.3: Add a hosted service to run the pipeline - Completed
    - [x] Step: Implement `IngestionPipelineHostedService : IHostedService` that creates the supervisor + nodes at startup and stops on shutdown.
    - [x] Step: Ensure cancellation uses the configured policy (fail-fast on node fatal error) and supports drain mode where required.
    - **Task 1.3 Summary**: Added `IngestionPipelineHostedService` and registered it via `AddIngestionServices()` so the synthetic pipeline graph starts/stops with the host.
  - [x] Task 1.4: Add stub source + stub indexing sink - Completed
    - [x] Step: Use `UKHO.Search.Pipelines.Nodes.SyntheticSourceNode<T>` (or a small ingestion-specific source node for now) to emit a small stream of synthetic `Envelope<IngestionRequest>`.
    - [x] Step: Implement a stub sink (`CollectingSinkNode<T>` or ingestion-specific) that records processed keys/order per lane.
    - **Task 1.4 Summary**: Wired a synthetic source and a per-lane batch-collecting stub index sink; added an automated ordering test.
  - **Files** (indicative):
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/IngestionPipelineBuilder.cs`: graph construction, channels, node creation.
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/IngestionPipelineHostedService.cs`: start/stop supervisor.
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Operations/IndexOperation.cs`: operation types.
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: structural document shape.
  - **Work Item Dependencies**: none
  - **Run / Verification Instructions**:
    - `dotnet run --project src/Hosts/IngestionServiceHost`
    - Inspect logs for pipeline start/stop and per-node processing.
    - `dotnet test`
  - **Work Item 1 Summary**:
    - Added an in-process ingestion pipeline skeleton (`Validate` → `KeyPartitionNode` → per-lane `MicroBatchNode` → stub sink) started via hosted service.
    - Added per-key ordering test in `test/UKHO.Search.Ingestion.Tests`.

---

## Slice 2 — Automated tests for runtime requirements (ordering/backpressure/cancellation/drain)
- [x] **Work Item 2: Automated tests cover pipeline runtime requirements for ingestion graph** - Completed
  - **Purpose**: Lock down ordering/backpressure/retry-blocking/fatal cancellation/drain behaviour as specified in §10.
  - **Acceptance Criteria**:
    - Tests exist in `test/UKHO.Search.Ingestion.Tests` and run in CI/local.
    - Covers: ordering, backpressure, retry blocking, fatal cancels pipeline, drain flushes microbatch.
  - **Definition of Done**:
    - Tests pass locally with `dotnet test`.
    - No external dependencies required (use fakes/stubs).
  - [x] Task 2.1: Create test project - Completed
    - [x] Step: Add `test/UKHO.Search.Ingestion.Tests` (xUnit) referencing the necessary projects.
    - [x] Step: Include deterministic time control where needed (fake clock or controllable delay).
    - **Task 2.1 Summary**: Reused existing `test/UKHO.Search.Ingestion.Tests` and added deterministic coordination helpers (blocking/signal-based test nodes) to avoid timing-based flakiness.
  - [x] Task 2.2: Implement tests - Completed
    - [x] Step: Deterministic per-key ordering across `KeyPartitionNode` + per-lane sequential chain.
    - [x] Step: Backpressure test using small channel capacities + slow sink.
    - [x] Step: Retry blocking test using a stub indexer that fails transiently for message #2 and succeeds later; assert message #3 doesn’t pass while #2 retries.
    - [x] Step: Fatal node exception cancels pipeline: create a node that throws and assert supervisor cancels.
    - [x] Step: Drain shutdown: cancel with drain mode and assert microbatch flush.
    - **Task 2.2 Summary**: Added tests covering ordering, backpressure, retry-blocking, fatal cancellation, and drain-flush semantics.
  - **Files**:
    - `test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`: test project.
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/*Tests.cs`: test suite.
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **Work Item 2 Summary**:
    - Added pipeline runtime behavior tests under `test/UKHO.Search.Ingestion.Tests/Pipeline/`.
    - Added deterministic test helpers under `test/UKHO.Search.Ingestion.Tests/TestNodes/`.

---

## Slice 3 — Azure Queue-backed `IngestionSourceNode` with ack + poison handling
- [x] **Work Item 3: Azure Queue source reads provider queues, emits envelopes, and only deletes on terminal outcome** - Completed
  - **Purpose**: Implement real ingestion transport (Azure Storage Queues) with at-least-once semantics.
  - **Acceptance Criteria**:
    - One queue is monitored per registered `IIngestionDataProviderFactory`.
    - Visibility timeout is set and renewed while processing.
    - Message is deleted only when the pipeline reaches a terminal state (success or persisted dead-letter).
    - After `ingestion:queueMaxDequeueCount`, message is moved to poison queue `<queueName><poisonQueueSuffix>`.
  - **Definition of Done**:
    - Source node runs under hosted service and emits envelopes into pre-partition channel.
    - Unit/integration tests cover poison routing behaviour (using Azurite or fakes).
  - [x] Task 3.1: Implement `IngestionSourceNode` - Completed
    - [x] Step: Create `IngestionSourceNode : SourceNodeBase<Envelope<IngestionRequest>>` (or `INode`) that:
      - Enumerates providers from `IIngestionProviderService`.
      - Creates a polling loop per provider queue.
      - Uses `QueueServiceClient` from DI.
    - [x] Step: For each received queue message:
      - Use the factory provider to deserialize into `IngestionRequest`.
      - Create `Envelope<IngestionRequest>` with `Key` = request `Id`.
      - Attach queue metadata to `Envelope.Headers` (queue name, message id, dequeue count, pop receipt if appropriate).
    - [x] Step: Implement visibility renewal loop while the message is in-flight.
    - **Task 3.1 Summary**: Added `IngestionSourceNode` that polls one Azure Storage Queue per provider, deserializes to `IngestionRequest`, and emits `Envelope<IngestionRequest>` with queue metadata.
  - [x] Task 3.2: Implement ack semantics without breaking ordering - Completed
    - [x] Step: Define a small internal abstraction (e.g., `IQueueMessageAcker`) that can:
      - Delete the message
      - Update visibility
      - Move to poison queue
    - [x] Step: Store an ack token in `Envelope.Context` (or `Headers`) that can be invoked by a terminal sink.
    - [x] Step: Add a terminal node (success + dead-letter) that invokes the ack handler.
    - **Task 3.2 Summary**: Added `IQueueMessageAcker` + `QueueMessageAcker` and a terminal `AckSinkNode<T>`; extended `MessageContext` with a small item bag to carry the ack token.
  - [x] Task 3.3: Implement poison queue move - Completed
    - [x] Step: If `DequeueCount > ingestion:queueMaxDequeueCount`, move to poison queue and delete original.
    - [x] Step: Poison message body includes original body + metadata + last error.
    - **Task 3.3 Summary**: Added poison routing in `IngestionSourceNode` (writes JSON poison message, sends to poison queue, deletes original).
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs`: queue polling + envelope creation.
    - `src/UKHO.Search.Infrastructure.Ingestion/Queue/QueueMessageAcker.cs`: delete/renew/poison operations.
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Terminal/AckSinkNode.cs`: invokes ack on terminal.
    - `src/UKHO.Search.Infrastructure.Ingestion/Queue/*`: queue client abstractions + Azure adapter.
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Nodes/BatchFlattenNode.cs`: stub per-lane index stage feeding ack.
    - `src/UKHO.Search/Pipelines/Messaging/MessageContext.cs`: added item bag for ack tokens.
  - **Work Item Dependencies**: Work Items 1–2
  - **Run / Verification Instructions**:
    - Start Azurite / queue emulator per repo standard.
    - `dotnet run --project src/Hosts/IngestionServiceHost`
    - Enqueue a test message onto provider queue and observe processing + deletion.
  - **Work Item 3 Summary**:
    - Implemented an Azure Queue-backed ingestion source with per-provider polling, visibility renewal, poison routing, and terminal ack deletion.
    - Added faked-queue tests verifying poison routing and that deletion occurs only after terminal ack.

---

## Slice 4 — Validation and dispatch to canonical indexing operations
- [x] **Work Item 4: Validate `IngestionRequest` and dispatch to `IndexOperation` + canonical document** - Completed
  - **Purpose**: Convert queue-derived ingestion requests into the canonical indexing operation payload.
  - **Acceptance Criteria**:
    - Invalid payloads are marked `Failed` with `PipelineErrorCategory.Validation` and routed to dead-letter.
    - Supported request types are dispatched to `UpsertOperation`/`DeleteOperation`/`AclUpdateOperation`.
    - Upserts create a structurally valid canonical document with placeholder `documentType` (`ingestion:documentTypePlaceholder`).
  - **Definition of Done**:
    - Nodes exist and are wired pre-partition and/or per-lane as appropriate.
    - Unit tests cover validation and dispatch.
  - [x] Task 4.1: Implement validation node - Completed
    - [x] Step: Create `IngestionRequestValidateNode : NodeBase<Envelope<IngestionRequest>, Envelope<IngestionRequest>>`.
    - [x] Step: Implement checks from the spec (exactly one payload present, Id/security token rules).
    - [x] Step: On failure set `Envelope.MarkFailed(...)` and emit to the shared dead-letter channel.
    - **Task 4.1 Summary**: Added `IngestionRequestValidateNode` with defensive one-of + Id/security token validation and dead-letter routing.
  - [x] Task 4.2: Implement dispatch/canonical build node - Completed
    - [x] Step: Create `IngestionRequestDispatchNode : TransformNode<IngestionRequest, IndexOperation>` (or a custom node) to map to operations.
    - [x] Step: Canonical doc builder produces the top-level shape and preserves source fields for traceability.
    - **Task 4.2 Summary**: Added `IngestionRequestDispatchNode` and `CanonicalDocumentBuilder` to map requests to `IndexOperation` types and build minimal canonical documents with a traceability snapshot.
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Nodes/IngestionRequestValidateNode.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Nodes/IngestionRequestDispatchNode.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Documents/CanonicalDocumentBuilder.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Terminal/DeadLetterPersistAndAckSinkNode.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Channels/RefCountedChannelWriter.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Channels/RefCountedCompletion.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/IngestionPipelineBuilder.cs`
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/IngestionRequestValidateNodeTests.cs`
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/IngestionRequestDispatchNodeTests.cs`
  - **Work Item Dependencies**: Work Items 1–3
  - **Run / Verification Instructions**:
    - `dotnet test`
    - Enqueue invalid/valid ingestion requests and confirm routing.
  - **Work Item 4 Summary**:
    - Added validation + dispatch stages and wired them into both the synthetic and Azure Queue-backed graphs.
    - Switched per-lane microbatching/stub-index stages to operate on `IndexOperation`.
    - Added unit tests covering validation and dispatch behavior.

---

## Slice 5 — Elasticsearch bulk indexing with inline retry (lane-blocking)
- [x] **Work Item 5: Implement `InOrderBulkIndexNode` using DI-configured `ElasticsearchClient`** - Completed
  - **Purpose**: Execute indexing operations against Elasticsearch with correct retry + error semantics.
  - **Acceptance Criteria**:
    - Uses the DI `Elastic.Clients.Elasticsearch.ElasticsearchClient` configured by the host.
    - Uses Elasticsearch Bulk API; maps per-item failures to `PipelineErrorCategory.BulkIndex`.
    - Inline retry on transient failures blocks the lane.
    - Non-transient or exhausted retry failures go to dead-letter.
  - **Definition of Done**:
    - Bulk indexing works against local Elasticsearch (Aspire resource/emulator).
    - Tests cover transient/non-transient classification and retry blocking.
  - [x] Task 5.1: Implement an Elasticsearch bulk client adapter - Completed
    - [x] Step: Implement `IBulkIndexClient<IndexOperation>` over `ElasticsearchClient` for the required index.
    - [x] Step: Use `ingestion:indexname` and `_id` derived from `Envelope.Key`.
    - **Task 5.1 Summary**: Added `ElasticsearchBulkIndexClient` to issue Elasticsearch Bulk API requests for `IndexOperation` items (Upsert/Delete/AclUpdate) using `ingestion:indexname` and `_id` from `Envelope.Key`.
  - [x] Task 5.2: Implement lane-blocking retry logic - Completed
    - [x] Step: Implement retry delay calculation using `ingestion:indexRetry*` settings.
    - [x] Step: Ensure while retrying, no later batch is processed for that lane.
    - **Task 5.2 Summary**: Implemented `InOrderBulkIndexNode` with inline transient retry (HTTP 429/503 and transient exceptions), exponential backoff + jitter, and lane-blocking semantics.
  - [x] Task 5.3: Wire node into per-lane chain - Completed
    - [x] Step: Replace stub sink from Slice 1 with `InOrderBulkIndexNode` and route successes/failed to sinks.
    - **Task 5.3 Summary**: Replaced the Azure Queue-backed stub index stage with `InOrderBulkIndexNode`, routing successes to `AckSinkNode` and failures to a shared `DeadLetterPersistAndAckSinkNode<IndexOperation>`.
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/ElasticsearchBulkIndexClient.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Nodes/InOrderBulkIndexNode.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/IngestionPipelineBuilder.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs`
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/InOrderBulkIndexNodeTests.cs`
  - **Work Item Dependencies**: Work Items 1–4
  - **Run / Verification Instructions**:
    - Start the Aspire stack (AppHost) and run `IngestionServiceHost`.
    - Enqueue requests and confirm documents appear in Elasticsearch index.
  - **Work Item 5 Summary**:
    - Added Elasticsearch bulk indexing via `ElasticsearchBulkIndexClient` and `InOrderBulkIndexNode` with lane-blocking inline retry.
    - Updated the Azure Queue-backed pipeline graph to index via Elasticsearch and dead-letter index failures.
    - Added tests covering transient retries, non-transient failures, and lane-blocking behaviour.

---

## Slice 6 — Blob-backed dead-letter sink + diagnostics sinks
- [x] **Work Item 6: Dead-letter failures to Azure Blob Storage and emit diagnostics stream** - Completed
  - **Purpose**: Provide operational visibility and replay capability.
  - **Acceptance Criteria**:
    - Failed envelopes are persisted to Blob as `DeadLetterRecord<T>` JSON.
    - Blob naming matches: `<deadletterBlobPrefix>/yyyy/MM/dd/<Key>/<MessageId>.json`.
    - Blob client is obtained via DI (host configured).
    - Diagnostics sinks log structured envelope state at key points.
  - **Definition of Done**:
    - Dead-letter blobs are written to configured container/prefix.
    - If persistence fails and environment config sets fatal mode, pipeline fails fast.
    - Tests cover dead-letter persistence (using Azurite blob emulator or fake).
  - [x] Task 6.1: Implement blob dead-letter sink - Completed
    - [x] Step: Create `BlobDeadLetterSinkNode<T> : SinkNodeBase<Envelope<T>>`. - Completed
    - [x] Step: Use DI `BlobServiceClient` and config `ingestion:deadletterContainer` + `ingestion:deadletterBlobPrefix`. - Completed
    - [x] Step: Serialize `DeadLetterRecord<T>` and upload as a blob. - Completed
  - [x] Task 6.2: Wire shared `deadLetter` channel - Completed
    - [x] Step: Ensure validation failures, dispatch failures, and bulk failures are written to the shared `deadLetter` channel. - Completed
    - [x] Step: Add blob sink node reading from `deadLetter.Reader`. - Completed
  - [x] Task 6.3: Diagnostics sinks - Completed
    - [x] Step: Add `BroadcastNode` / `RouteNode` to tee diagnostics from dispatch and bulk index. - Completed
    - [x] Step: Implement a simple diagnostics sink that logs envelope summaries. - Completed
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/DeadLetter/BlobDeadLetterSinkNode.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Diagnostics/DiagnosticsSinkNode.cs`
  - **Work Item Dependencies**: Work Items 1–5
  - **Run / Verification Instructions**:
    - Run host with Azurite blob enabled.
    - Produce a known failure and confirm a blob is created at expected path.

  - **Work Item 6 Summary**:
    - Replaced file-based dead-letter persistence with Azure Blob persistence via `BlobDeadLetterSinkNode<TPayload>` for both request and index-operation dead-letter paths.
    - Added best-effort diagnostics tee + sink (`BroadcastNode` → `DiagnosticsSinkNode`) to log envelope summaries after dispatch and after successful bulk indexing.
    - Updated ingestion tests' blob recording transport helpers to align with current Azure SDK pipeline abstractions; tests pass (`dotnet test`).

---

## Slice 7 — Operationalization: metrics, shutdown semantics, and documentation updates
- [x] **Work Item 7: Ensure metrics/logging/shutdown semantics match spec and update docs** - Completed
  - **Purpose**: Make the pipeline operable in real environments.
  - **Acceptance Criteria**:
    - Metrics are emitted per node (already via `NodeMetrics`) and queue depth gauges reflect `CountingChannel` depths.
    - Shutdown drains where required (microbatch flush).
    - Spec references are up to date with final code locations.
  - **Definition of Done**:
    - Verified graceful stop does not delete in-flight queue messages unless terminal.
    - Documentation updated for final node names and configuration.
  - [x] Task 7.1: Verify metrics - Completed
    - [x] Step: Ensure all nodes are using readers that implement `IQueueDepthProvider` (CountingChannelReader). - Completed
    - [x] Step: Add any additional domain-specific metrics (lane throughput / retry-block time) if needed. - Completed (no additional metrics required for v0.01)
  - [x] Task 7.2: Shutdown semantics - Completed
    - [x] Step: Configure cancellation modes where appropriate (drain microbatch on shutdown). - Completed
  - [x] Task 7.3: Documentation - Completed
    - [x] Step: Update `docs/006-ingestion-service/006-ingestion-service.spec.md` if implementation differs. - Completed
    - [x] Step: Add an operator README under `docs/006-ingestion-service/` describing how to run locally (Azurite + Elasticsearch) and how to inspect dead-letter. - Completed
  - **Files**:
    - `docs/006-ingestion-service/README.md`: local runbook.
  - **Work Item Dependencies**: Work Items 1–6
  - **Run / Verification Instructions**:
    - `dotnet run --project src/Hosts/IngestionServiceHost`
    - Validate metrics via OpenTelemetry collector/Aspire dashboard (per repo defaults).

  - **Work Item 7 Summary**:
    - Confirmed all inter-node channels are created via `BoundedChannelFactory.Create(...)` (CountingChannels), preserving queue-depth metrics support.
    - Ensured microbatch shutdown remains in drain mode.
    - Added `docs/006-ingestion-service/README.md` local runbook and updated the spec to document optional dead-letter fatal/continue configuration.

---

## Summary (overall approach)
Implement the ingestion pipeline as a supervised, channel-based node graph built from `UKHO.Search.Pipelines` primitives, delivered incrementally as runnable vertical slices. Start with an in-process pipeline to validate ordering/backpressure, then add Azure Queue transport with correct ack/poison handling, dispatch to canonical operations, Elasticsearch bulk indexing with lane-blocking retry, and Blob-based dead-letter persistence. Automated tests lock down key behaviours throughout.
