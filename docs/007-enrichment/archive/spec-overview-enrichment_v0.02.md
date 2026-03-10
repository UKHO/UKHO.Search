# Specification: Ingestion Enrichment — Overview

Version: v0.02  
Status: Draft  
Work Package: `docs/007-enrichment/`  
Supersedes: `docs/007-enrichment/archive/spec-overview-enrichment_v0.01.md`

## Change Log
- v0.02: Updated referenced component spec versions (v0.02) and aligned overview wording to the selected approach for carrying request+document through the pipeline.

## 1. Purpose
Introduce a provider-extensible enrichment step into the ingestion pipeline, executed before documents are written to Elasticsearch.

## 2. Scope
This work package covers:
- A core enricher abstraction (`IIngestionEnricher`) in `UKHO.Search.Ingestion`
- Provider-side DI registration for file share ingestion enrichers
- A pipeline enrichment node that executes all registered enrichers in a defined order, before bulk indexing
- A pipeline context payload that allows enrichment to access both `IngestionRequest` and `CanonicalDocument` at the pre-index stage

Out of scope (initially):
- Real enrichment logic for file content, geo-location, and exchange-set (the initial implementations are no-op)
- Elasticsearch mapping/index template changes (unless required in a subsequent work package)

## 3. High-level design
### Components
- `UKHO.Search.Ingestion` (Domain)
  - Defines the enricher interface (`IIngestionEnricher`)
  - Defines a context payload for enrichment (request + operation/document)
  - Provides a pipeline node that applies enrichment ahead of the Elasticsearch bulk indexing node

- `UKHO.Search.Ingestion.Providers.FileShare`
  - Implements initial enrichers (file share provider specific)
  - Provides provider-owned DI registration entrypoint (`AddFileShareProvider(IServiceCollection)`)
  - Wires the enrichment node into the file share ingestion graph

- `UKHO.Search.Infrastructure.Ingestion`
  - Remains the single, central DI entrypoint for ingestion service wiring
  - Calls the file share provider registration during ingestion DI setup

### Component specifications
- `docs/007-enrichment/spec-ingestion-enrichment-core_v0.02.md`
- `docs/007-enrichment/spec-ingestion-fileshare-provider-enrichment_v0.02.md`
