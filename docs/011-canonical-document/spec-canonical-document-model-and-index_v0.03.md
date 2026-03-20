# Specification: Canonical Document Model + Search Index Mapping

Version: `v0.03`  
Status: `Draft`  
Work Package: `docs/011-canonical-document/`  
Supersedes: `docs/011-canonical-document/spec-canonical-document-model-and-index_v0.02.md`

## 1. Summary
Uplift the canonical document schema and shared Elasticsearch index mapping to add a system-managed `Provider` field.

`Provider` records which ingestion provider created the canonical document, for example `file-share`. It is provenance metadata owned by the ingestion pipeline rather than user-authored discovery content.

## 2. Goals
- Add `CanonicalDocument.Provider` as a required immutable string field.
- Ensure canonical document construction receives provider context from the ingestion pipeline rather than from user payload data.
- Configure the shared Elasticsearch index mapping so `provider` is indexed as a `keyword`.
- Extend projection, serialization, and mapping tests to cover the new field.
- Clarify developer-facing documentation so consumers understand why `Provider` cannot be set directly.

## 3. Non-goals
- Allowing users, rules, or enrichers to mutate `Provider` after construction.
- Defining migration/backfill strategy for previously indexed documents.
- Changing provider-agnostic pipeline node abstractions into provider-specific contracts.

## 4. Background / evidence
Baseline specs introduced the provider-independent canonical model and later added `Content` as an analyzed full-text field.

This uplift adds `Provider` so canonical documents explicitly preserve provenance across dispatch, enrichment, indexing, and diagnostics.

## 5. Requirements

### 5.1 Canonical schema changes
#### 5.1.1 `Provider` (exact-match provenance)
- `CanonicalDocument` MUST expose a `Provider` property of type string.
- `Provider` MUST be required for newly created canonical documents.
- `Provider` MUST be immutable after construction.
- `Provider` MUST be set only at construction time.
- `Provider` MUST be sourced from provider context already known at queue ingress.
- The ingestion pipeline/providers MUST NOT allow user payload data to override or supply `Provider`.
- Rules and enrichers MUST NOT mutate `Provider`.

Canonical API:
- Minimal canonical document creation APIs MUST require `Provider`.
- Provider-specific builders (for example `CanonicalDocumentBuilder`) MUST require provider context and MUST fail fast when it is absent.

### 5.2 Elasticsearch index mapping/settings (Infrastructure-owned)
#### 5.2.1 Field mapping requirements (delta)
- `Provider` MUST be mapped as a `keyword` field.

#### 5.2.2 Proposed mapping (draft delta)
Add:
- `provider`: `keyword`

No change to the existing mapping intents for:
- `source`: stored in `_source` but not indexed (`enabled: false`)
- `timestamp`: `date`
- `keywords`: `keyword`
- taxonomy fields: `keyword`
- `searchText`: `text` (English analyzer)
- `content`: `text` (English analyzer)
- `geoPolygons`: `geo_shape`

## 6. Validation rules
- Canonical document creation MUST reject null, empty, or whitespace `Provider` values.
- Mapping validation MUST reject indices where `provider` is not explicitly typed as `keyword`.

## 7. Compatibility and versioning
- Adding `provider` changes the indexed document shape.
- This work package does not define any migration/backfill approach.

## 8. Acceptance criteria
- `CanonicalDocument` has (in addition to existing fields):
  - `Provider`
- `CanonicalIndexDocument` preserves `Provider`.
- Elasticsearch index creation applies explicit mappings/settings so:
  - `provider` is indexed as `keyword`
- Documentation explains that `Provider` is pipeline-owned provenance and not user-settable.

## 9. Testing strategy
Extend existing tests under `test/UKHO.Search.Ingestion.Tests/`:
- Add unit tests for canonical document/provider construction invariants.
- Extend JSON round-trip tests to include the new `Provider` property.
- Extend index mapping tests to assert `provider` is a `keyword`.
- Extend projection/bulk payload tests to assert `Provider` is preserved into the indexed document.

## 10. Implementation notes (non-normative)
- Preserve provider-agnostic node design by passing provider context as data rather than specializing generic pipeline node contracts.
- Keep `Provider` stable and exact-match oriented for filtering, diagnostics, and provenance inspection.

## 11. Change Log
- `v0.03`: Added immutable canonical `Provider` support, provider keyword index mapping, projection/test requirements, and documentation guidance about provider-owned provenance.
- `v0.02`: Added `CanonicalDocument.Content` and corresponding Infrastructure mapping and tests. Confirmed `Content` follows set-then-append semantics aligned with `SearchText`.
