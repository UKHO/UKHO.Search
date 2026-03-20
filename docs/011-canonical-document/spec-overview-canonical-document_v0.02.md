# Specification: Canonical Document (Overview)

Version: `v0.02`  
Status: `Draft`  
Work Package: `docs/011-canonical-document/`  
Supersedes: `docs/011-canonical-document/spec-overview-canonical-document_v0.01.md`

## 1. Purpose
Evolve the canonical document shape and the shared search index mapping to support search, filtering, provenance, and provider-agnostic ingestion behavior.

## 2. Scope
This work package covers:
- Updating `CanonicalDocument` to:
  - strongly type `Source` to the ingestion request model
  - preserve queue/provider provenance via `Timestamp` and immutable `Provider`
  - expose search/discovery fields such as `Keywords`, `SearchText`, `Content`, taxonomy fields, and `GeoPolygons`
- Adding shared Elasticsearch index settings/mappings so:
  - provenance fields are represented consistently
  - exact-match discovery/filter fields are indexed as `keyword`
  - analyzed text fields use English analysis
  - geo coverage is indexed as `geo_shape`

Out of scope:
- provider-specific enrichment logic beyond canonical construction
- query-side ranking/scoring design
- migration or backfill planning for schema changes

## 3. High-level design
### Components
- `UKHO.Search.Ingestion` (Domain)
  - defines `CanonicalDocument` and core ingestion request contracts
- Provider canonical build step (current: `UKHO.Search.Ingestion.Providers.FileShare`)
  - builds a minimal canonical document from an ingestion request and provider context
- `UKHO.Search.Infrastructure.*` (Infrastructure)
  - owns shared Elasticsearch index bootstrapping, settings, mappings, and projection

### Component specifications
- `docs/011-canonical-document/spec-canonical-document-model-and-index_v0.03.md`
