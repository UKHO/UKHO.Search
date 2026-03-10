# Architecture

Work Package: `docs/011-canonical-document/`

Related spec: `docs/011-canonical-document/spec-canonical-document-model-and-index_v0.01.md`

## Overall Technical Approach
- Update the canonical search document (`CanonicalDocument`) to:
  - Store the ingestion request contract (`IngestionRequest`) as `Source` for traceability/debugging.
  - Remove currently incorrect canonical sections.
  - Add explicit enrichment-owned fields:
    - `Keywords`: exact terms (stored as a multi-valued keyword array).
    - `SearchText`: analyzed English text for full-text search.
    - `Facets`: key/value facet data (stored as an object shape compatible with an Elasticsearch `flattened` field).
  - Enforce canonical normalization rules at write time (lowercase invariant for keywords/search text/facets).

- Move index mapping/settings responsibility into Infrastructure:
  - Bootstrap creates the index (if missing) with explicit mappings/settings.
  - Mappings are provider-agnostic and apply to all canonical documents.

```mermaid
flowchart LR
  subgraph Providers[Providers]
    P1[FileShare provider]
  end

  subgraph Ingestion[Ingestion pipeline]
    IR[IngestionRequest] -->|build minimal canonical| CD[CanonicalDocument]
    CD -->|enrich later (out of scope)| ENR[Enrichers]
    ENR -->|bulk index| ES[(Elasticsearch index)]
  end

  subgraph Infra[Infrastructure]
    BS[BootstrapService] -->|create index + mapping| ES
  end

  P1 --> IR
```

## Frontend
- Not applicable for this work package.

## Backend
### Domain (Canonical document)
- Project: `src/UKHO.Search.Ingestion/UKHO.Search.Ingestion.csproj`
- Key responsibilities:
  - Define the canonical schema used for indexing.
  - Provide safe mutation APIs for enrichment stages.
  - Apply normalization rules at the point data enters the canonical document.

Canonical field intent:
- `source`:
  - Stored for traceability/debugging.
  - Not indexed for search.
- `keywords`:
  - Multi-valued exact tokens.
  - Lowercased.
  - Intended for exact filters and aggregations.
- `searchText`:
  - Lowercased.
  - Indexed as analyzed English text.
- `facets`:
  - Lowercased facet keys and values.
  - Indexed as a single `flattened` field for flexible filtering and aggregations without mapping explosion.

### Provider canonical build step
- Project (current): `src/UKHO.Search.Ingestion.Providers.FileShare/UKHO.Search.Ingestion.Providers.FileShare.csproj`
- Responsibility:
  - Build the minimal `CanonicalDocument` from an `IngestionRequest`.
  - Providers do not populate enrichment-owned fields (`Keywords`, `SearchText`, `Facets`, `DocumentType`).

### Infrastructure (index mapping/settings)
- Project (expected): `src/UKHO.Search.Infrastructure.Ingestion/UKHO.Search.Infrastructure.Ingestion.csproj`
- Responsibility:
  - Own canonical index settings/mappings and apply them on index creation.

Elasticsearch mapping intent:
- `source`: `object` with indexing disabled (`enabled: false`)
- `keywords`: `keyword` (multi-valued)
- `searchText`: `text` with English analyzer
- `facets`: `flattened`

Notes:
- Lowercasing is applied before indexing, so mapping does not need to rely on analyzers/normalizers for case-folding (though a keyword normalizer may be added defensively for `keywords`).
