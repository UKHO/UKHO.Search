# Architecture

**Target output path:** `docs/006-ingestion-service/architecture-ingestion-pipeline_v0.01.md`

## Overall Technical Approach
- The ingestion service is implemented as a **supervised** pipeline graph built on `System.Threading.Channels`.
- The core runtime is the existing `UKHO.Search.Pipelines` library (Domain) providing:
  - `Envelope<TPayload>` message envelope + `MessageStatus` and `PipelineError`
  - standard nodes (`ValidateNode`, `KeyPartitionNode`, `MicroBatchNode`, etc.)
  - bounded channels via `BoundedChannelFactory` and queue depth via `CountingChannel`
  - `PipelineSupervisor` (fail-fast cancellation on fatal node errors)
  - node-level metrics via `NodeMetrics` (`System.Diagnostics.Metrics`)
- Ingestion-specific integrations (Azure Queue, Elasticsearch, Azure Blob) live in Infrastructure, respecting Onion Architecture dependency direction.

### Topology
```mermaid
flowchart LR
  SRC[IngestionSourceNode\n(Azure Storage Queues)] --> VAL[IngestionRequestValidateNode]
  VAL --> PART[KeyPartitionNode\n(N lanes)]

  PART -->|lane p| MB[MicroBatchNode p]
  MB --> ES[InOrderBulkIndexNode p]

  VAL --> DLQ[BlobDeadLetterSinkNode]
  ES --> DLQ

  ES --> ACK[Ack sink\n(delete queue msg on terminal)]

  ES --> DIAG[Diagnostics sink]
```

## Frontend
- The `IngestionServiceHost` is a Blazor Server host used primarily for operational UI.
- UI concerns are limited to:
  - status/visibility into pipeline behaviour
  - viewing statistics/diagnostics
  - optional tooling pages for local development

## Backend
### Layering (Onion)
- **Hosts/UI**: `src/Hosts/IngestionServiceHost`
  - Configures DI for external clients and starts the ingestion pipeline via hosted services.
- **Infrastructure**: `src/UKHO.Search.Infrastructure.Ingestion`
  - Implements Azure Queue source, Elasticsearch indexing adapters, and Azure Blob dead-letter sink.
  - Builds and wires the ingestion pipeline graph (channels + nodes).
- **Services**: `src/UKHO.Search.Services.Ingestion`
  - Provider discovery and orchestration logic (e.g., `IIngestionProviderService`).
- **Domain**: `src/UKHO.Search` + `src/UKHO.Search.Ingestion`
  - Pipeline runtime primitives (`UKHO.Search.Pipelines.*`).
  - Ingestion request contract (`UKHO.Search.Ingestion.Requests.IngestionRequest`).

### External integrations
- Elasticsearch
  - `Elastic.Clients.Elasticsearch.ElasticsearchClient` is configured in the host via `builder.AddElasticsearchClient(ServiceNames.ElasticSearch)`.
  - Ingestion indexing must use this DI instance.
- Azure Queue Storage
  - `QueueServiceClient` is configured in the host via `builder.AddAzureQueueServiceClient(ServiceNames.Queues)`.
  - `IngestionSourceNode` uses it for queue polling, visibility management, poison routing, and delete-on-terminal.
- Azure Blob Storage
  - `BlobServiceClient` is configured in the host via `builder.AddAzureBlobServiceClient(ServiceNames.Blobs)`.
  - Dead-letter sink uses it to persist `DeadLetterRecord<T>` JSON.

### Backpressure + ordering
- All inter-node channels are bounded using `BoundedChannelFactory.Create(...)` (Wait mode).
- Per-key ordering is guaranteed by:
  - deriving `Envelope.Key` from the request `Id`, and
  - routing through `KeyPartitionNode` into N lanes, each processed sequentially.
- Retry for Elasticsearch indexing is implemented inline in the per-lane indexing node so that retries **block the lane** and do not allow later messages to pass.
