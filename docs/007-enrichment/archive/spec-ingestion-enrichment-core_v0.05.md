# Specification: Core Ingestion Enrichment (IIngestionEnricher + Enrichment Node)

Version: v0.05  
Status: Draft  
Work Package: `docs/007-enrichment/`  
Supersedes: `docs/007-enrichment/archive/spec-ingestion-enrichment-core_v0.04.md`

## Change Log
- v0.05: Confirmed failure handling policy: enricher exceptions fail the item and route to index-operation dead-letter (no bulk index attempt).

## 1. Summary
This document specifies:
- The new core enrichment contract (`IIngestionEnricher`) hosted in `UKHO.Search.Ingestion`
- A core pipeline node that executes enrichment before documents are written to Elasticsearch
- A core context payload type (`IngestionPipelineContext`) that ensures enrichers can access both the original `IngestionRequest` and the `CanonicalDocument`.

## 2. Goals
- Support multiple enrichers registered via DI
- Ensure deterministic ordering of enrichers via an `Ordinal` property
- Run enrichment before the Elasticsearch bulk index write
- Keep the enricher abstraction provider-agnostic (usable by any ingestion provider)

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
- If an enricher throws, the item must NOT proceed to bulk indexing.
- The enrichment node must:
  - Mark the envelope as failed with a `Transform` error.
  - Route the failed item to the index-operation dead-letter path (same sink used for bulk index failures).

Rationale:
- Indexing partially enriched documents introduces data quality and troubleshooting issues.

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

## 8. Testing strategy
- Unit tests for the enrichment node ordering and error handling.
- Unit tests for context payload flow and envelope metadata preservation.

## 9. Open questions
- Should transient enricher failures be retried (within the enrichment node) before dead-letter, and if so what retry policy?
