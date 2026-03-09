# Specification: Ingestion Enrichment (Complete)

Version: v0.01  
Status: Draft  
Work Package: `docs/007-enrichment/`  

## Change Log
- v0.01: Consolidated the following specs into a single document:
  - `docs/007-enrichment/spec-overview-enrichment_v0.10.md`
  - `docs/007-enrichment/spec-ingestion-enrichment-core_v0.08.md`
  - `docs/007-enrichment/spec-ingestion-fileshare-provider-enrichment_v0.06.md`

---

## 1. Purpose
Introduce a provider-extensible enrichment step into the ingestion pipeline, executed before documents are written to Elasticsearch.

## 2. Scope
This work package covers:
- A core enricher abstraction (`IIngestionEnricher`) in `UKHO.Search.Ingestion`
- Provider-side DI registration for file share ingestion enrichers
- A pipeline enrichment node that executes all registered enrichers in a defined order, before bulk indexing
- A context payload (`IngestionPipelineContext`) in `UKHO.Search.Ingestion.Pipeline` that allows enrichment to access both `IngestionRequest` and `CanonicalDocument` at the pre-index stage
- A configurable retry policy for transient enrichment failures, stored in configuration under the existing `ingestion:` section

Out of scope (initially):
- Real enrichment logic for file content, geo-location, and exchange-set (the initial implementations are no-op)
- Elasticsearch mapping/index template changes (unless required in a subsequent work package)

## 3. High-level design
### Components
- `UKHO.Search.Ingestion` (Domain)
  - Defines the enricher interface (`IIngestionEnricher`)
  - Defines `IngestionPipelineContext` (request + operation) in `UKHO.Search.Ingestion.Pipeline`
  - Provides a pipeline node that applies enrichment ahead of the Elasticsearch bulk indexing node, with configurable retry for transient enricher failures

- `UKHO.Search.Ingestion.Providers.FileShare`
  - Implements initial enrichers (file share provider specific)
  - Provides provider-owned DI registration entrypoint (`AddFileShareProvider(IServiceCollection)`)
  - Wires the enrichment node into the file share ingestion graph

- `UKHO.Search.Infrastructure.Ingestion`
  - Remains the single, central DI entrypoint for ingestion service wiring
  - Calls the file share provider registration during ingestion DI setup

---

## 4. Core ingestion enrichment (contract + pipeline node)

### 4.1 Summary
This section specifies:
- The core enrichment contract (`IIngestionEnricher`) hosted in `UKHO.Search.Ingestion`
- A core pipeline node that executes enrichment before documents are written to Elasticsearch
- A core context payload type (`IngestionPipelineContext`) that ensures enrichers can access both the original `IngestionRequest` and the `CanonicalDocument`
- Retry behaviour for transient enricher failures, driven by configuration

### 4.2 Goals
- Support multiple enrichers registered via DI
- Ensure deterministic ordering of enrichers via an `Ordinal` property
- Run enrichment before the Elasticsearch bulk index write
- Keep the enricher abstraction provider-agnostic (usable by any ingestion provider)
- Allow resilience against transient enrichment failures via a configurable retry policy

### 4.3 Non-goals
- Define the actual enrichment outputs/fields for file content, geo-location, or exchange sets (these start as no-op)
- Change ingestion request schema

### 4.4 Public contract
#### 4.4.1 Rename/move
- Rename `IFileShareIngestionEnricher` to `IIngestionEnricher`
- Move it from `UKHO.Search.Ingestion.Providers.FileShare` to `UKHO.Search.Ingestion`

#### 4.4.2 Interface definition
`IIngestionEnricher` must declare:
- `int Ordinal { get; }`
- `Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default);`

Notes:
- Enrichers may be no-op.
- Enrichers apply enrichment by mutating the provided `CanonicalDocument` instance.

### 4.5 Enrichment execution model
#### 4.5.1 Ordering
- The enrichment node must sort enrichers by ascending `Ordinal` and execute in that order.
- If multiple enrichers share the same `Ordinal`, ordering must be deterministic. Recommended tie-breakers:
  1) `Ordinal` ascending
  2) `Type.FullName` ascending

#### 4.5.2 Error handling (confirmed)
- If an enricher throws and the failure is not retried/successfully recovered, the item must NOT proceed to bulk indexing.
- The enrichment node must:
  - Mark the envelope as failed with a `Transform` error.
  - Route the failed item to the index-operation dead-letter path.

#### 4.5.3 Cancellation
- The node and all enricher calls must honor `CancellationToken`.

### 4.6 Core pipeline context payload: `IngestionPipelineContext`
#### 4.6.1 Location + file constraints
- Namespace: `UKHO.Search.Ingestion.Pipeline`
- Implementation constraint: `IngestionPipelineContext` must be the only public type in its `.cs` file (one public type per file).

#### 4.6.2 Requirement
`IngestionPipelineContext` carries:
- `IngestionRequest Request`
- `IndexOperation Operation`

Notes:
- The payload is provider-agnostic and can be reused by other providers.
- Envelope metadata must be preserved when mapping between `IngestionRequest`, `IngestionPipelineContext`, and `IndexOperation` stages.

### 4.7 Core pipeline node
#### 4.7.1 Responsibility
Add a core node that:
- Accepts a stream of context envelopes (payload is `IngestionPipelineContext`)
- Applies all registered `IIngestionEnricher` implementations to upsert operations by calling:
  `TryBuildEnrichmentAsync(context.Request, upsert.Document, ct)`
- Outputs the `IndexOperation` (post-enrichment) for downstream batching/indexing
- Routes failures to an index-operation dead-letter output

#### 4.7.2 Placement in pipeline
- The node must execute before the Elasticsearch bulk index node.

### 4.8 Retry policy for transient enricher failures (confirmed)
#### 4.8.1 Policy
Retry transient enrichment failures using exponential backoff:
- 5 retries (total 6 attempts)
- Base delay: 200ms
- Max delay: 5000ms
- Jitter: 250ms

#### 4.8.2 Configuration storage
Retry settings must be stored under the existing `ingestion:` section in configuration, consistent with the existing indexing retry settings pattern observed in `configuration/configuration.json`.

Add the following keys under `ingestion:` (for each environment block, e.g. `local`, `dev`, `iat`, `live`):
- `enrichmentRetryMaxAttempts`: `5`
- `enrichmentRetryBaseDelayMilliseconds`: `200`
- `enrichmentRetryMaxDelayMilliseconds`: `5000`
- `enrichmentRetryJitterMilliseconds`: `250`

Notes:
- These keys mirror the shape of existing `indexRetry*` keys.
- Defaults should match the values above if configuration is missing.

#### 4.8.3 Transient classification (confirmed: whitelist-only)
Only a known set of exception types are considered transient for the purpose of retrying enrichment.

Initial transient exception whitelist:
- `TimeoutException`
- `HttpRequestException`
- `IOException`
- `TaskCanceledException` only when **not** caused by the provided `CancellationToken` being cancelled

Non-transient by default:
- Any other exception type not on the whitelist
- `OperationCanceledException` when `cancellationToken.IsCancellationRequested == true`

Notes:
- The whitelist can be extended in future, but changes must be reflected in specs/tests.

### 4.9 Testing strategy
- Unit tests for the enrichment node ordering and error handling.
- Unit tests for retry behaviour:
  - Retries occur only for whitelisted exception types
  - `TaskCanceledException` handling differentiates between true cancellation vs timeout scenarios
  - Attempts increment and backoff is applied
  - On exhaustion, item routes to index dead-letter
- Unit tests for context payload flow and envelope metadata preservation.

---

## 5. File share provider enrichment (DI + enrichers)

### 5.1 Summary
This section specifies changes in `UKHO.Search.Ingestion.Providers.FileShare` to:
- Register provider-owned components and enrichers via a provider DI extension method
- Introduce multiple initial enrichers (no-op) implementing `IIngestionEnricher`
- Adapt the provider pipeline graph so that enrichment can access both the original `IngestionRequest` and the `CanonicalDocument` before bulk indexing

### 5.2 Goals
- Provider project owns DI registration for provider project types
- Enable registering multiple enrichers
- Ensure ingestion service DI setup calls the provider DI registration
- Ensure file share pipeline routes enrichment failures to the existing index-operation dead-letter sink

### 5.3 DI design
#### 5.3.1 Provider DI namespace
Add a new namespace/folder in the provider project:
- `UKHO.Search.Ingestion.Providers.FileShare.Injection`

#### 5.3.2 Provider DI entrypoint
Add a new DI extension class:
- `InjectionExtensions`

It must expose:
- `IServiceCollection AddFileShareProvider(this IServiceCollection services)`

#### 5.3.3 Registrations
`AddFileShareProvider` must register provider-owned items, including:
- File share ingestion provider factory (`IIngestionDataProviderFactory` backed by `FileShareIngestionDataProviderFactory`)
- All file share enrichers as multi-registrations of `IIngestionEnricher`

#### 5.3.4 Lifetime
- Enrichers must be registered as `Scoped`.

Rationale:
- Allows future enrichers to take scoped dependencies safely (e.g., per-message services), while still supporting stateless enrichers.

#### 5.3.5 Ingestion service integration
- The ingestion service DI setup (central `AddIngestionServices` entrypoint) must call `services.AddFileShareProvider()`.

### 5.4 Enrichers
#### 5.4.1 Initial enrichers
Create the following enrichers in `UKHO.Search.Ingestion.Providers.FileShare`:
- `FileContentEnricher`
- `ExchangeSetEnricher`
- `GeoLocationEnricher`

All must:
- Implement `IIngestionEnricher`
- Provide an `Ordinal` value
- Implement `TryBuildEnrichmentAsync(...)` as no-op initially

#### 5.4.2 Ordinal assignment
Confirmed initial ordering:
1) `FileContentEnricher` (Ordinal: 100)
2) `ExchangeSetEnricher` (Ordinal: 200)
3) `GeoLocationEnricher` (Ordinal: 300)

### 5.5 Pipeline wiring (provider usage)
#### 5.5.1 Context production
- The dispatch step must emit an `IngestionPipelineContext` payload that includes:
  - The original `IngestionRequest`
  - The derived `IndexOperation` (including the `CanonicalDocument` for upserts)

#### 5.5.2 Enrichment
- Insert the core enrichment node before bulk indexing.
- Input: `IngestionPipelineContext` stream.
- Output: `IndexOperation` stream for microbatch + bulk indexing.

#### 5.5.3 Failure routing
- Enrichment failures must be routed to index-operation dead-letter, without sending the failed item to bulk indexing.

### 5.6 Open questions
- None.
