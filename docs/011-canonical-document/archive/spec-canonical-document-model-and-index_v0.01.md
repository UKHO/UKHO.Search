# Specification: Canonical Document Model + Search Index Mapping

Version: v0.01  
Status: Draft  
Work Package: `docs/011-canonical-document/`

## 1. Summary
Modify `CanonicalDocument` so it is fit for future search enrichment scenarios by:
- Strongly typing the `Source` payload as the ingestion request model.
- Removing currently incorrect sections.
- Introducing explicit fields for exact keywords, analyzed search text, and facets.

In parallel, configure the shared Elasticsearch index mapping/settings (in Infrastructure, not provider code) so these new fields are ingested with appropriate analysis and aggregation behaviour (English language).

## 2. Goals
- `CanonicalDocument.Source` is always an `IngestionRequest` and is serialized correctly when indexing.
- `CanonicalDocument.DocumentType` is settable by a later enricher (no placeholder injection requirement).
- Remove incorrect/unused fields from the canonical schema.
- Introduce canonical fields for:
  - `Keywords`: exact, verbatim indexed terms.
  - `SearchText`: analyzed English full-text search content.
  - `Facets`: name/value data suitable for filtering and aggregations.
- Ensure `Keywords`, `SearchText`, and `Facets` are normalized to lowercase to support case-insensitive matching.
- Provide safe, intention-revealing APIs on `CanonicalDocument` so enrichers can add/append values without providers mapping into these fields.
- Ensure the index mapping is provider-agnostic and lives in Infrastructure.

## 3. Non-goals
- Implementing enrichers that populate `DocumentType`, `Keywords`, `SearchText`, or `Facets`.
- Provider-specific schema extensions.
- Defining the query DSL, scoring rules, or UI behaviour.
- Backfill/reindex strategy for existing indices.

## 4. Background / evidence
- `CanonicalDocument` currently stores:
  - `Source` as a `JsonObject` containing an `ingestionRequest` snapshot created by `BuildIngestionRequestSnapshot()`.
  - Several other `JsonObject` sections (`Normalized`, `Descriptions`, `Search`, `Quality`, `Provenance`) which are not correct for upcoming search requirements.

- Index bootstrapping currently creates an index if it does not exist, but does not apply explicit mappings/settings (defaults/dynamic mapping).

- Elasticsearch mapping configuration will be implemented using the official .NET client mapping APIs:
  - https://www.elastic.co/docs/reference/elasticsearch/clients/dotnet/mappings

## 5. Requirements

### 5.1 Canonical schema changes
#### 5.1.1 `Source`
- `CanonicalDocument.Source` MUST be of type `IngestionRequest`.
- The canonical builder MUST populate `Source` from the existing `BuildIngestionRequestSnapshot()` flow, but the resulting stored value MUST be the ingestion request model (not an intermediate `JsonObject`).
  - Design intent: the canonical document stores the original ingestion request contract for traceability/debugging.

Serialization:
- `CanonicalDocument` MUST serialize `Source` in a stable, deterministic way suitable for indexing and debugging.
- The serialization approach MUST be compatible with:
  - `System.Text.Json` serialization of `IngestionRequest` (as used elsewhere in ingestion contracts).
  - `Elastic.Clients.Elasticsearch` document serialization when indexing.

Open design note (to resolve): whether `Source` should be mapped/indexed for search (see §11).

Decision:
- `Source` is stored for traceability/debugging but is not indexed for search.

#### 5.1.2 `DocumentType`
- Remove any dependency on a provider-supplied placeholder value.
- `CanonicalDocument.DocumentType` MUST be settable after creation (a setter is required).
- Providers MUST NOT assign a placeholder value.
- A later work package will set this value via an enricher.

#### 5.1.3 Remove incorrect sections
- The following properties MUST be removed from `CanonicalDocument`:
  - `Normalized`
  - `Descriptions`
  - `Search`
  - `Quality`
  - `Provenance`

### 5.2 `Keywords` (exact, verbatim)
- `CanonicalDocument` MUST expose a `Keywords` property representing a collection of exact terms.
- `Keywords` MUST be indexed verbatim (not analyzed, not stemmed).
- `Keywords` values MUST be normalized to lowercase before storage/indexing.
- Ingestion providers/pipeline MUST NOT attempt to map any source data into `Keywords`.
- A later work package (BasicEnricher) will populate `Keywords`.

Representation:
- `Keywords` MUST serialize as a JSON array of strings.
- Elasticsearch mapping for `keywords` MUST use the `keyword` field type.

Rationale (non-normative):
- Elasticsearch natively supports multi-valued fields by sending an array for a `keyword` field; this is efficient for exact filtering and aggregations.
- Storing keywords as a single token string and relying on tokenization would require a `text` field with an analyzer (e.g., whitespace), which is not aligned with the intent of “exact keyword values” and complicates aggregations (typically requiring a `.keyword` subfield or enabling fielddata).

Canonical API:
- `CanonicalDocument` MUST provide methods suitable for enrichers to add keywords:
  - Add a single keyword.
  - Add multiple keywords.
- `CanonicalDocument` SHOULD also provide a convenience setter for token strings (if an enricher naturally produces a token string), which MUST:
  - Split into individual tokens using a deterministic rule (TBD: whitespace-only vs whitespace+comma; default SHOULD be whitespace).
  - Normalize each token to lowercase (invariant) before storage.
  - Apply the same deduplication rules as other keyword-add methods.
- Keyword addition MUST:
- Keyword addition MUST:
  - Ignore null/empty/whitespace values.
  - Normalize to lowercase (invariant) before storage.
  - Avoid introducing duplicates after normalization.

### 5.3 `SearchText` (analyzed English full-text)
- `CanonicalDocument` MUST expose a `SearchText` property of type string.
- `SearchText` MUST be normalized to lowercase before storage/indexing.
- The ingestion pipeline MUST NOT attempt to map any source data into `SearchText`.
- A later work package will populate `SearchText` via an enricher.

Canonical API:
- `CanonicalDocument` MUST provide methods suitable for enrichers to:
- `CanonicalDocument` MUST provide methods suitable for enrichers to:
  - Set `SearchText`.
  - Append to `SearchText`.
- Append behaviour MUST be deterministic and avoid accidental word concatenation (e.g., by inserting a separator when required).
- Set/append methods MUST normalize appended content to lowercase (invariant) prior to storage.

### 5.4 `Facets` (filtering + aggregations)
- `CanonicalDocument` MUST expose a `Facets` property representing facet name/value data.
- `Facets` names and values MUST be normalized to lowercase before storage/indexing.
- The ingestion pipeline MUST NOT attempt to map any source data into `Facets`.
- A later work package will populate facets via an enricher.

Canonical API:
- `CanonicalDocument` MUST provide methods suitable for enrichers to:
  - Add a facet value by name.
  - Add multiple values for a facet name.
- Facet addition MUST:
- Facet addition MUST:
  - Ignore null/empty/whitespace names/values.
  - Normalize facet names and values to lowercase (invariant) before storage.
  - Support multiple values per facet name.

Representation (normative intent, subject to §11 confirmation):
- `Facets` SHOULD serialize as a JSON object whose properties are facet names and whose values are arrays of strings.

### 5.5 Elasticsearch index mapping/settings (Infrastructure-owned)
#### 5.5.1 Location/ownership
- The index settings/mapping for canonical search fields MUST be implemented in Infrastructure (not provider projects).
- The ingestion bootstrap/index-creation flow MUST apply these mappings/settings when creating a new index.

#### 5.5.2 Field mapping requirements
- `Keywords` MUST be mapped as an exact-match field type suitable for:
  - Exact filtering (terms queries).
  - Aggregations.

- `Keywords` MUST NOT enforce an `ignore_above` cap by default (no length-based drop from the keyword index), unless introduced in a later version of this specification.

- `SearchText` MUST be mapped as an analyzed full-text field using English analysis (stemming, stopwords, etc.).

- `Facets` MUST be mapped to support:
  - Filtering on facet name/value.
  - Aggregations for faceted navigation.
  - A dynamic set of facet names without requiring provider-specific mapping code.

- `Source` MUST be stored in the document `_source` for traceability/debugging, but MUST NOT be indexed for search.

#### 5.5.3 Proposed mapping (draft)
This section captures the selected mapping shape for this work package.

- `source`: `object` with indexing disabled (`enabled: false`)
- `keywords`: `keyword` (array of keyword values; values are lowercased before indexing; optionally also apply a lowercase normalizer for defense-in-depth)
- `searchText`: `text` with an English analyzer (built-in `english` or a custom analyzer equivalent)
- `facets`: `flattened` (selected) to avoid mapping explosion for arbitrary facet keys while retaining term-query and aggregation support; facet keys/values are lowercased before indexing

Index settings:
- Configure analysis so the `searchText` analyzer is English.
- Assumption: language is English.

Implementation mechanism:
- Use the Elastic .NET client mapping APIs (see link in §4) to define index settings and mappings as part of index creation.

## 6. Validation rules
- `Keywords` values MUST be non-empty/non-whitespace.
- `Facets` names and values MUST be non-empty/non-whitespace.
- `SearchText` append operations MUST not produce leading/trailing separator noise beyond a single deterministic separator policy.

## 7. Compatibility and versioning
- Removing fields from `CanonicalDocument` is a breaking change to the indexed document shape.
- This work package does not define any migration/backfill approach; the expectation is that index recreation/reindexing will be handled separately.

## 8. Acceptance criteria
- `CanonicalDocument` has:
  - `DocumentId`
  - `DocumentType` (settable)
  - `Source` (`IngestionRequest`)
  - `Keywords`
  - `SearchText`
  - `Facets`

- Provider canonical document builder no longer requires a `documentTypePlaceholder` configuration.

- Enrichers can populate:
  - `Keywords` (add single/multiple)
  - `SearchText` (set/append)
  - `Facets` (add name/value)

- Elasticsearch index creation applies explicit mappings/settings so:
  - `Keywords` is not analyzed/stemmed.
  - `SearchText` is analyzed using English.
  - `Facets` supports filtering and aggregations without provider-specific mapping updates.

## 9. Testing strategy
- Unit tests for `CanonicalDocument` mutation APIs:
  - Adding keywords and deduplication behaviour.
  - Setting/appending search text with deterministic separator policy.
  - Adding facet values, including multiple values per name.

- Contract/serialization tests:
  - `CanonicalDocument` round-trips through `System.Text.Json` (including `Source` as `IngestionRequest`).

- Infrastructure tests (where feasible):
  - Validate index creation request includes expected mappings/settings for `keywords`, `searchText`, and `facets`.

## 10. Implementation notes (non-normative)
- Consider mapping `Source` as a stored-but-not-indexed object (`enabled: false`) if it is not needed for search queries.
- Prefer a single place in Infrastructure that defines canonical index mappings/settings (shared across providers).

## 11. Open questions
None.
