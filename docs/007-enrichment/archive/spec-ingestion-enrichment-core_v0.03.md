# Specification: Core Ingestion Enrichment (IIngestionEnricher + Enrichment Node)

Version: v0.03  
Status: Draft  
Work Package: `docs/007-enrichment/`  
Supersedes: `docs/007-enrichment/archive/spec-ingestion-enrichment-core_v0.02.md`

## Change Log
- v0.03: Confirmed the context payload name as `IngestionPipelineContext`.

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
- Enrichers apply enrichment by mutating the provided `CanonicalDocument` instance (e.g., adding/updating JSON within its existing `JsonObject` properties).

## 5. Enrichment execution model
### 5.1 Ordering
- The enrichment node must sort enrichers by ascending `Ordinal` and execute in that order.
- If multiple enrichers share the same `Ordinal`, ordering must be deterministic. Recommended tie-breakers:
  1) `Ordinal` ascending
  2) `Type.FullName` ascending

### 5.2 Error handling
- If an enricher throws, the item must NOT proceed to bulk indexing.
- The enrichment node must route the failed item to the index dead-letter path (same sink used for bulk index failures), recording an appropriate pipeline error with category `Transform`.

### 5.3 Cancellation
- The node and all enricher calls must honor `CancellationToken`.

## 6. Core pipeline context payload: `IngestionPipelineContext`
### 6.1 Problem
The enrichment signature requires BOTH:
- `IngestionRequest request`
- `CanonicalDocument document`

At the pre-index stage, the pipeline is handling index operations (e.g., upsert/delete/acl update). The original request must be preserved to avoid re-parsing from JSON snapshots.

### 6.2 Requirement
Introduce a core payload type that carries:
- The originating `IngestionRequest`
- The resulting `IndexOperation` (which may include an upsert holding `CanonicalDocument`)

### 6.3 Proposed type
Create a new public record in `UKHO.Search.Ingestion`:
- `IngestionPipelineContext`
  - `IngestionRequest Request`
  - `IndexOperation Operation`

Notes:
- The payload is provider-agnostic and can be reused by other providers.
- The payload must preserve all existing envelope metadata (key/message id/attempt/context) as it flows through.

## 7. Core pipeline node
### 7.1 Responsibility
Add a core node that:
- Accepts a stream of context envelopes (payload is `IngestionPipelineContext`)
- Applies all registered `IIngestionEnricher` implementations to upsert operations by calling:
  `TryBuildEnrichmentAsync(context.Request, upsert.Document, ct)`
- Outputs the original `IndexOperation` (post-enrichment) for downstream batching/indexing
- Routes failures to an index-operation dead-letter output

### 7.2 Placement in pipeline
- The node must execute before the Elasticsearch bulk index node.

### 7.3 Pass-through semantics
- Non-upsert operations (delete, acl update) are passed through unchanged.
- When no enrichers are registered, the node behaves as a simple unwrap/forward.

## 8. Testing strategy
- Unit tests for the enrichment node:
  - Executes enrichers in ordinal order
  - Deterministic behavior on tie ordinals
  - Routes to dead-letter on enricher exception
  - Pass-through behavior when there are no enrichers registered
  - Pass-through behavior for non-upsert operations

- Unit tests for the context payload flow:
  - Dispatch produces `IngestionPipelineContext` containing both request + operation
  - Enrichment outputs the operation with unchanged envelope metadata

## 9. Open questions
- Confirm the namespace and folder location for `IngestionPipelineContext` in `UKHO.Search.Ingestion`.
- Should enricher exceptions be considered transient in any cases (e.g., external dependency timeouts), and should retry be attempted before dead-letter?
