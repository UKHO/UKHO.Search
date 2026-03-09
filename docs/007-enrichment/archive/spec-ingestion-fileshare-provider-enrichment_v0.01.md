# Specification: File Share Provider Enrichment (DI + Enrichers)

Version: v0.01  
Status: Draft  
Work Package: `docs/007-enrichment/`  

## 1. Summary
This document specifies changes in `UKHO.Search.Ingestion.Providers.FileShare` to:
- Register provider-owned components and enrichers via a provider DI extension method
- Introduce multiple initial enrichers (no-op) implementing `IIngestionEnricher`

## 2. Goals
- Provider project owns DI registration for provider project types (avoid external DI code needing to know provider internals)
- Enable registering multiple enrichers
- Ensure ingestion service DI setup calls the provider DI registration

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

Rationale:
- File content extraction often provides material that geo/exchange-set enrichment may build upon.

## 6. Pipeline wiring (provider usage)
- The file share ingestion pipeline must include the new core enrichment node before bulk indexing.
- The file share provider graph must supply:
  - The list of `IIngestionEnricher` instances resolved from DI
  - A dead-letter output path for enrichment failures (index-operation dead-letter)

## 7. Testing strategy
- DI tests:
  - Verify multiple `IIngestionEnricher` instances resolve from DI
  - Verify `AddIngestionServices` wires in `AddFileShareProvider`

- Provider-level tests (optional for no-op stage):
  - Validate enrichers are executed in expected order when used in the pipeline

## 8. Open questions
- Confirm required list of provider-owned registrations to move under `AddFileShareProvider` (minimum: provider factory + enrichers).
- Confirm whether enrichers should be registered as `Singleton` (expected for stateless no-op enrichers) or `Scoped`/`Transient`.
