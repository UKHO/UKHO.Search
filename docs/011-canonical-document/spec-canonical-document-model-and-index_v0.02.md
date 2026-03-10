# Specification: Canonical Document Model + Search Index Mapping

Version: v0.02  
Status: Draft  
Work Package: `docs/011-canonical-document/`  
Supersedes: `docs/011-canonical-document/archive/spec-canonical-document-model-and-index_v0.01.md`

## 1. Summary
Uplift the canonical document schema and shared Elasticsearch index mapping to add a new analyzed full-text field `Content`.

`Content` is intended to hold longer-form, user-visible or document-body text distinct from `SearchText` (which remains the primary analyzed, curated search string produced by enrichers).

## 2. Goals
- Add `CanonicalDocument.Content` as an analyzed, English full-text field.
- Provide safe, intention-revealing APIs on `CanonicalDocument` so enrichers can set/append content deterministically.
- Configure the shared Elasticsearch index mapping/settings (Infrastructure-owned) so `content` is ingested with the same analysis behaviour as `searchText` (English language).
- Extend/amend unit and serialization tests to cover the new field and its mutation APIs.

## 3. Non-goals
- Implementing enrichers that populate `Content`.
- Defining query DSL/scoring/UI behaviour for `Content`.
- Backfill/reindex strategy for existing indices.

## 4. Background / evidence
Baseline spec (v0.01) introduced canonical enrichment fields (`Keywords`, `SearchText`, `Facets`) and an Infrastructure-owned Elasticsearch mapping.

This uplift adds `Content` with equivalent mapping/analysis to `SearchText`.

## 5. Requirements

### 5.1 Canonical schema changes
#### 5.1.1 `Content` (analyzed English full-text)
- `CanonicalDocument` MUST expose a `Content` property of type string.
- `Content` MUST be normalized to lowercase before storage/indexing (invariant) to maintain case-insensitive behaviour aligned with other canonical enrichment fields.
- The ingestion pipeline/providers MUST NOT attempt to map any source data into `Content`.
- A later work package MAY populate `Content` via an enricher.

Canonical API:
- `CanonicalDocument` MUST provide a method suitable for enrichers to set content (e.g., `SetContent(string? text)`).
- `SetContent` MUST follow the same set-then-append semantics as the existing `SetSearchText` implementation:
  - If `Content` is empty, the normalized value becomes the full content.
  - If `Content` is non-empty, the normalized value MUST be appended with a single separator to avoid accidental word concatenation.
- `SetContent` MUST normalize appended content to lowercase (invariant) prior to storage.

Validation:
- Setting/appending MUST ignore null/empty/whitespace values.

### 5.2 Elasticsearch index mapping/settings (Infrastructure-owned)
#### 5.2.1 Field mapping requirements (delta)
- `Content` MUST be mapped as an analyzed full-text field using English analysis (same approach as `searchText`).

#### 5.2.2 Proposed mapping (draft delta)
Add:
- `content`: `text` with an English analyzer (built-in `english` or a custom analyzer equivalent)

No change to the existing mapping intents for:
- `source`: stored in `_source` but not indexed (`enabled: false`)
- `keywords`: `keyword`
- `searchText`: `text` (English analyzer)
- `facets`: `flattened`

## 6. Validation rules
- `SetContent` MUST not produce leading/trailing separator noise beyond a single deterministic separator policy.

## 7. Compatibility and versioning
- Adding `content` changes the indexed document shape.
- This work package does not define any migration/backfill approach; expectation remains that index recreation/reindexing will be handled separately.

## 8. Acceptance criteria
- `CanonicalDocument` has (in addition to existing fields):
  - `Content`
- Enrichers can populate:
  - `Content` (set-then-append semantics, aligned with `SearchText`)
- Elasticsearch index creation applies explicit mappings/settings so:
  - `content` is analyzed using English.

## 9. Testing strategy
Extend existing tests under `test/UKHO.Search.Ingestion.Tests/Documents/`:
- Add unit tests for `CanonicalDocument` content mutation APIs:
  - Setting content when empty.
  - Appending content deterministically (separator policy).
  - Lowercasing/normalization.
  - Ignoring null/empty/whitespace.
- Extend JSON round-trip tests to include the new `Content` property.

## 10. Implementation notes (non-normative)
- Prefer reusing the same analyzer configuration used for `searchText` to keep analysis consistent.
- If `Content` is expected to be materially larger than `SearchText`, consider future guardrails (length caps, truncation strategy) in a later version.

## 11. Change Log
- v0.02: Added `CanonicalDocument.Content` and corresponding Infrastructure mapping and tests. Confirmed `Content` follows set-then-append semantics aligned with `SearchText`.
