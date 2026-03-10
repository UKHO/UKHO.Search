# Specification: Core Ingestion Enrichment (IIngestionEnricher + Enrichment Node)

Version: v0.08  
Status: Draft  
Work Package: `docs/007-enrichment/`  
Supersedes: `docs/007-enrichment/archive/spec-ingestion-enrichment-core_v0.07.md`

## Change Log
- v0.08: Added `IOException` to the transient exception whitelist for enrichment retries.

## 1. Summary
This document specifies:
- The core enrichment contract (`IIngestionEnricher`) hosted in `UKHO.Search.Ingestion`
- A core pipeline node that executes enrichment before documents are written to Elasticsearch
- A core context payload type (`IngestionPipelineContext`) that ensures enrichers can access both the original `IngestionRequest` and the `CanonicalDocument`
- Retry behaviour for transient enricher failures, driven by configuration

## 2. Goals
- Support multiple enrichers registered via DI
- Ensure deterministic ordering of enrichers via an `Ordinal` property
- Run enrichment before the Elasticsearch bulk index write
- Keep the enricher abstraction provider-agnostic (usable by any ingestion provider)
- Allow resilience against transient enrichment failures via a configurable retry policy

## 3. Non-goals
- Define the actual enrichment outputs/fields for file content, geo-location, or exchange sets (these start as no-op)
- Change ingestion request schema

## 4. Public contract
### 4.1 Rename/move
- Rename `IFileShareIngestionEnricher` to `IIngestionEnricher`
- Move it from `UKHO.Search.Ingestion.Providers.FileShare` to `UKHO.Search.Ingestion`

### 4.2 Interface definition
`IIngestionEnricher` must declare:
- `int Ordinal { get; }`
- `Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default);`

Notes:
- Enrichers may be no-op.
- Enrichers apply enrichment by mutating the provided `CanonicalDocument` instance.

## 5. Enrichment execution model
### 5.1 Ordering
- The enrichment node must sort enrichers by ascending `Ordinal` and execute in that order.
- If multiple enrichers share the same `Ordinal`, ordering must be deterministic. Recommended tie-breakers:
  1) `Ordinal` ascending
  2) `Type.FullName` ascending

### 5.2 Error handling (confirmed)
- If an enricher throws and the failure is not retried/successfully recovered, the item must NOT proceed to bulk indexing.
- The enrichment node must:
  - Mark the envelope as failed with a `Transform` error.
  - Route the failed item to the index-operation dead-letter path.

### 5.3 Cancellation
- The node and all enricher calls must honor `CancellationToken`.

## 6. Core pipeline context payload: `IngestionPipelineContext`
### 6.1 Location + file constraints
- Namespace: `UKHO.Search.Ingestion.Pipeline`
- Implementation constraint: `IngestionPipelineContext` must be the only public type in its `.cs` file (one public type per file).

### 6.2 Requirement
`IngestionPipelineContext` carries:
- `IngestionRequest Request`
- `IndexOperation Operation`

Notes:
- The payload is provider-agnostic and can be reused by other providers.
- Envelope metadata must be preserved when mapping between `IngestionRequest`, `IngestionPipelineContext`, and `IndexOperation` stages.

## 7. Core pipeline node
### 7.1 Responsibility
Add a core node that:
- Accepts a stream of context envelopes (payload is `IngestionPipelineContext`)
- Applies all registered `IIngestionEnricher` implementations to upsert operations by calling:
  `TryBuildEnrichmentAsync(context.Request, upsert.Document, ct)`
- Outputs the `IndexOperation` (post-enrichment) for downstream batching/indexing
- Routes failures to an index-operation dead-letter output

### 7.2 Placement in pipeline
- The node must execute before the Elasticsearch bulk index node.

## 8. Retry policy for transient enricher failures (confirmed)
### 8.1 Policy
Retry transient enrichment failures using exponential backoff:
- 5 retries (total 6 attempts)
- Base delay: 200ms
- Max delay: 5000ms
- Jitter: 250ms

### 8.2 Configuration storage
Retry settings must be stored under the existing `ingestion:` section in configuration, consistent with the existing indexing retry settings pattern observed in `configuration/configuration.json`.

Add the following keys under `ingestion:` (for each environment block, e.g. `local`, `dev`, `iat`, `live`):
- `enrichmentRetryMaxAttempts`: `5`
- `enrichmentRetryBaseDelayMilliseconds`: `200`
- `enrichmentRetryMaxDelayMilliseconds`: `5000`
- `enrichmentRetryJitterMilliseconds`: `250`

Notes:
- These keys mirror the shape of existing `indexRetry*` keys.
- Defaults should match the values above if configuration is missing.

### 8.3 Transient classification (confirmed: whitelist-only)
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

## 9. Testing strategy
- Unit tests for the enrichment node ordering and error handling.
- Unit tests for retry behaviour:
  - Retries occur only for whitelisted exception types
  - `TaskCanceledException` handling differentiates between true cancellation vs timeout scenarios
  - Attempts increment and backoff is applied
  - On exhaustion, item routes to index dead-letter
- Unit tests for context payload flow and envelope metadata preservation.
