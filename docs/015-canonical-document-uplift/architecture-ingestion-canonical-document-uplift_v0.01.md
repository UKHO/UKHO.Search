# Architecture

**Target output path:** `docs/015-canonical-document-uplift/architecture-ingestion-canonical-document-uplift_v0.01.md`

**Based on:** `docs/015-canonical-document-uplift/spec-canonical-document-uplift_v0.01.md`

**Version:** v0.01 (Draft)

---

## Overall Technical Approach

This change is an ingestion-pipeline domain/model uplift plus provider-specific enrichment. It follows the repository’s onion architecture:

- **Domain**: `UKHO.Search.Ingestion` owns `CanonicalDocument`.
- **Provider**: `UKHO.Search.Ingestion.Providers.FileShare` owns FileShare-specific enrichment such as `BasicEnricher`.
- **Infrastructure**: `UKHO.Search.Infrastructure.Ingestion` owns index bootstrapping/mapping (`CanonicalIndexDefinition`) and the rules engine implementation.
- **Hosts**: wiring and hosted service orchestration.

Key behaviors:

- Canonical document creation (for Add/Update) stamps:
  - `Source` as a shallow defensive copy of active payload `Properties`
  - `Timestamp` from active payload timestamp
- Enrichment runs through `ApplyEnrichmentNode`, ordering enrichers by `Ordinal`.

### Data flow (simplified)

```mermaid
flowchart LR
  A[IngestionRequest (Add/Update)] --> B[Provider CanonicalDocumentBuilder]
  B --> C[CanonicalDocument
  - Source: Properties copy
  - Timestamp]
  C --> D[ApplyEnrichmentNode]
  D --> E[BasicEnricher (ordinal 10)
  - keywords from values
  - facets from name/value]
  D --> F[Rules enricher (ordinal 50)]
  D --> G[Other provider enrichers (e.g. FileContentEnricher)]
  G --> H[IndexOperation (Upsert)]
```

---

## Frontend

No frontend/UI changes are required. The repository contains a Blazor project, but this work package is backend ingestion pipeline functionality only.

---

## Backend

### Domain model

- `CanonicalDocument` is extended with:
  - `Timestamp: DateTimeOffset`
  - `Source: IReadOnlyList<IngestionProperty>`

### Provider: FileShare

- New `BasicEnricher` (ordinal 10) reads active Add/Update payload properties and populates canonical:
  - keywords from values
  - facets from name/value

### Infrastructure

- Index mapping compatibility is verified/updated (if required) for `Timestamp`.
- Ingestion rules DSL behavior remains request-payload scoped; tests and docs are reviewed for regressions.
