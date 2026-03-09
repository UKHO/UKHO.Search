# Specification: File Share Provider Enrichment (DI + Enrichers)

Version: v0.05  
Status: Draft  
Work Package: `docs/007-enrichment/`  
Supersedes: `docs/007-enrichment/archive/spec-ingestion-fileshare-provider-enrichment_v0.04.md`

## Change Log
- v0.05: Updated initial enricher ordering to: `FileContentEnricher` → `ExchangeSetEnricher` → `GeoLocationEnricher`.

## 1. Summary
This document specifies changes in `UKHO.Search.Ingestion.Providers.FileShare` to:
- Register provider-owned components and enrichers via a provider DI extension method
- Introduce multiple initial enrichers (no-op) implementing `IIngestionEnricher`
- Adapt the provider pipeline graph so that enrichment can access both the original `IngestionRequest` and the `CanonicalDocument` before bulk indexing

## 2. Goals
- Provider project owns DI registration for provider project types
- Enable registering multiple enrichers
- Ensure ingestion service DI setup calls the provider DI registration
- Ensure file share pipeline routes enrichment failures to the existing index-operation dead-letter sink

## 3. DI design
### 3.1 Provider DI namespace
Add a new namespace/folder in the provider project:
- `UKHO.Search.Ingestion.Providers.FileShare.Injection`

### 3.2 Provider DI entrypoint
Add a new DI extension class:
- `InjectionExtensions`

It must expose:
- `IServiceCollection AddFileShareProvider(this IServiceCollection services)`

### 3.3 Registrations
`AddFileShareProvider` must register provider-owned items, including:
- File share ingestion provider factory (`IIngestionDataProviderFactory` backed by `FileShareIngestionDataProviderFactory`)
- All file share enrichers as multi-registrations of `IIngestionEnricher`

### 3.4 Ingestion service integration
- The ingestion service DI setup (central `AddIngestionServices` entrypoint) must call `services.AddFileShareProvider()`.

## 4. Enrichers
### 4.1 Initial enrichers
Create the following enrichers in `UKHO.Search.Ingestion.Providers.FileShare`:
- `FileContentEnricher`
- `ExchangeSetEnricher`
- `GeoLocationEnricher`

All must:
- Implement `IIngestionEnricher`
- Provide an `Ordinal` value
- Implement `TryBuildEnrichmentAsync(...)` as no-op initially

### 4.2 Ordinal assignment
Ordinal values must enforce a stable execution order.

Confirmed initial ordering:
1) `FileContentEnricher` (Ordinal: 100)
2) `ExchangeSetEnricher` (Ordinal: 200)
3) `GeoLocationEnricher` (Ordinal: 300)

## 5. Pipeline wiring (provider usage)
### 5.1 Context production
- The dispatch step must emit an `IngestionPipelineContext` payload that includes:
  - The original `IngestionRequest`
  - The derived `IndexOperation` (including the `CanonicalDocument` for upserts)

### 5.2 Enrichment
- Insert the core enrichment node before bulk indexing.
- Input: `IngestionPipelineContext` stream.
- Output: `IndexOperation` stream for microbatch + bulk indexing.

### 5.3 Failure routing
- Enrichment failures must be routed to index-operation dead-letter, without sending the failed item to bulk indexing.

## 6. Open questions
- Confirm expected lifetimes for enrichers (default: singleton for stateless enrichers).
