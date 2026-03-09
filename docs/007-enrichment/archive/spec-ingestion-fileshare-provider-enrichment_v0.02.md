# Specification: File Share Provider Enrichment (DI + Enrichers)

Version: v0.02  
Status: Draft  
Work Package: `docs/007-enrichment/`  
Supersedes: `docs/007-enrichment/archive/spec-ingestion-fileshare-provider-enrichment_v0.01.md`

## Change Log
- v0.02: Updated pipeline wiring section to reflect use of a context payload flowing into the core enrichment node (option 3).

## 1. Summary
This document specifies changes in `UKHO.Search.Ingestion.Providers.FileShare` to:
- Register provider-owned components and enrichers via a provider DI extension method
- Introduce multiple initial enrichers (no-op) implementing `IIngestionEnricher`
- Adapt the provider pipeline graph so that enrichment can access both the original `IngestionRequest` and the `CanonicalDocument` before bulk indexing

## 2. Goals
- Provider project owns DI registration for provider project types (avoid external DI code needing to know provider internals)
- Enable registering multiple enrichers
- Ensure ingestion service DI setup calls the provider DI registration
- Ensure file share pipeline routes enrichment failures to the existing index-operation dead-letter sink

## 3. Non-goals
- Implement real enrichment logic in this work package (enrichers are initially no-op)

## 4. DI design
### 4.1 Provider DI namespace
Add a new namespace/folder in the provider project:
- `UKHO.Search.Ingestion.Providers.FileShare.Injection`

### 4.2 Provider DI entrypoint
Add a new DI extension class:
- `InjectionExtensions`

It must expose:
- `IServiceCollection AddFileShareProvider(this IServiceCollection services)`

### 4.3 Registrations
`AddFileShareProvider` must register provider-owned items, including:
- File share ingestion provider factory used by ingestion (`IIngestionDataProviderFactory` backed by `FileShareIngestionDataProviderFactory`)
- All file share enrichers as multi-registrations of `IIngestionEnricher`

Notes:
- The queue name configuration currently used by file share provider factory must remain configurable via `IConfiguration`.

### 4.4 Ingestion service integration
- The ingestion service DI setup (current central `AddIngestionServices` entrypoint) must call `services.AddFileShareProvider()`.

## 5. Enrichers
### 5.1 Initial enrichers
Create the following enrichers in `UKHO.Search.Ingestion.Providers.FileShare`:
- `FileContentEnricher`
- `GeoLocationEnricher`
- `ExchangeSetEnricher`

All must:
- Implement `IIngestionEnricher`
- Provide an `Ordinal` value
- Implement `TryBuildEnrichmentAsync(...)` as no-op initially (return completed task)

### 5.2 Ordinal assignment
Ordinal values must enforce a stable execution order.

Proposed initial ordering (can be adjusted):
1) `FileContentEnricher` (Ordinal: 100)
2) `GeoLocationEnricher` (Ordinal: 200)
3) `ExchangeSetEnricher` (Ordinal: 300)

## 6. Pipeline wiring (provider usage)
### 6.1 Context production
- The file share dispatch step must emit a context payload that includes both:
  - The original `IngestionRequest`
  - The derived `IndexOperation` (including the `CanonicalDocument` for upserts)

### 6.2 Enrichment
- Insert the core enrichment node before the bulk indexing stage.
- The enrichment node input must be the context payload stream.
- The node outputs enriched `IndexOperation` items for downstream microbatching and bulk indexing.

### 6.3 Failure routing
- Enrichment failures must be routed to the index-operation dead-letter pipeline (same sink used for bulk index failures), without sending the failed item to bulk indexing.

## 7. Testing strategy
- DI tests:
  - Verify multiple `IIngestionEnricher` instances resolve from DI
  - Verify `AddIngestionServices` wires in `AddFileShareProvider`

- Provider graph tests (optional for no-op stage):
  - Verify the enrichment node is placed before bulk indexing
  - Verify an enricher exception routes the operation to index dead-letter

## 8. Open questions
- Confirm required list of provider-owned registrations to move under `AddFileShareProvider` (minimum: provider factory + enrichers).
- Confirm expected lifetimes for enrichers (default: singleton for stateless enrichers).
