# Specification: Core Ingestion Enrichment (IIngestionEnricher + Enrichment Node)

Version: v0.01  
Status: Draft  
Work Package: `docs/007-enrichment/`  

## 1. Summary
This document specifies:
- The new core enrichment contract (`IIngestionEnricher`) hosted in `UKHO.Search.Ingestion`
- A core pipeline node that executes enrichment before documents are written to Elasticsearch

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
- Enrichers apply enrichment by mutating the provided `CanonicalDocument` instance (e.g., adding/updating JSON within its existing `JsonObject` properties).

## 5. Enrichment execution model
### 5.1 Ordering
- The enrichment node must sort enrichers by ascending `Ordinal` and execute in that order.
- If multiple enrichers share the same `Ordinal`, ordering must be deterministic. Recommended tie-breakers:
  1) `Ordinal` ascending
  2) `Type.FullName` ascending

### 5.2 Error handling
- If an enricher throws, the message must NOT proceed to bulk indexing.
- The enrichment node must route the failed item to the index dead-letter path (same sink used for bulk index failures), recording an appropriate pipeline error with category `Transform`.

Rationale:
- Enrichment occurs before indexing; indexing a partially enriched document is undesirable.

### 5.3 Cancellation
- The node and all enricher calls must honor `CancellationToken`.

## 6. Core pipeline node
### 6.1 Responsibility
Add a core node that:
- Accepts a stream of operations destined for indexing
- Applies all registered `IIngestionEnricher` implementations to upsert operations
- Passes non-upsert operations through unchanged

### 6.2 Placement in pipeline
- The node must execute before the Elasticsearch bulk index node.

### 6.3 Inputs/Outputs
- Input: operations that will be sent to bulk index.
- Output: enriched operations.
- Dead-letter output: failed operations.

## 7. Critical design dependency (request + document availability)
The enricher signature requires BOTH:
- `IngestionRequest request`
- `CanonicalDocument document`

However, the current ingestion pipeline converts `IngestionRequest` into `IndexOperation` prior to bulk indexing.

To enable enrichment in a pre-index node, the pipeline must ensure the original `IngestionRequest` is available alongside the `CanonicalDocument` at enrichment time.

### 7.1 Proposed approach
- Extend `UpsertOperation` (in `UKHO.Search.Ingestion`) to carry the originating `IngestionRequest` used to construct the `CanonicalDocument`.
  - The enrichment node can then pass `UpsertOperation.Request` + `UpsertOperation.Document` to `IIngestionEnricher`.

### 7.2 Alternatives (deferred)
- Perform enrichment in the dispatch node (provider-specific), rather than a core node.
- Introduce a new operation/context type that carries both request + operation.

## 8. Testing strategy
- Unit tests for the enrichment node:
  - Executes enrichers in ordinal order
  - Deterministic behavior on tie ordinals
  - Routes to dead-letter on enricher exception
  - Pass-through behavior when there are no enrichers registered
  - Pass-through behavior for non-upsert operations

## 9. Open questions
- Should enricher exceptions be considered transient in any cases (e.g., external dependency timeouts), and should retry be attempted before dead-letter?
- Should enrichment be applied to `AclUpdateOperation` or other operation types in future?
