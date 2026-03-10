# Specification: Canonical Document (Overview)

Version: v0.01  
Status: Draft  
Work Package: `docs/011-canonical-document/`

## 1. Purpose
Evolve the canonical document shape and the shared search index mapping to support future search scenarios (keywords, analyzed search text, and faceting), while simplifying the current canonical schema.

## 2. Scope
This work package covers:
- Updating `CanonicalDocument` to:
  - Strongly type `Source` to the ingestion request model.
  - Remove currently incorrect sections (normalized/descriptions/search/quality/provenance).
  - Introduce `Keywords`, `SearchText`, and `Facets` fields and safe mutation APIs intended for enrichers.
  - Allow `DocumentType` to be set by later enrichment stages.

- Adding shared Elasticsearch index settings/mappings (provider-agnostic) so:
  - `Keywords` is indexed verbatim (exact matching; not analyzed/stemmed).
  - `SearchText` is indexed as analyzed English full text.
  - `Facets` supports filtering and aggregations.

Out of scope (initially):
- Defining the enrichment logic that populates `DocumentType`, `Keywords`, `SearchText`, or `Facets`.
- Provider-specific field mapping beyond the canonical schema.
- Query-side behaviour (search DSL, scoring, ranking, highlighting).

## 3. High-level design
### Components
- `UKHO.Search.Ingestion` (Domain)
  - Defines `CanonicalDocument` and core ingestion request contracts.

- Provider canonical build step (current: `UKHO.Search.Ingestion.Providers.FileShare`)
  - Builds a minimal canonical document from an ingestion request.

- `UKHO.Search.Infrastructure.*` (Infrastructure)
  - Owns shared Elasticsearch index bootstrapping, settings, and mappings.

### Component specifications
- `docs/011-canonical-document/spec-canonical-document-model-and-index_v0.01.md`
