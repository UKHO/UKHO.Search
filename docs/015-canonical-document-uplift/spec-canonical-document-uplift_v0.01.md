# Specification: Canonical Document Uplift (Combined)

**Target output path:** `docs/015-canonical-document-uplift/spec-canonical-document-uplift_v0.01.md`

**Version:** v0.01 (Draft)

## 0. Source documents merged

This document is a merge of the following specifications (retained in the work package folder for traceability):

- `docs/015-canonical-document-uplift/spec-overview-canonical-document-uplift_v0.01.md`
- `docs/015-canonical-document-uplift/spec-domain-canonical-document-uplift_v0.01.md`
- `docs/015-canonical-document-uplift/spec-domain-basic-enricher_v0.01.md`
- `docs/015-canonical-document-uplift/spec-domain-ingestion-rules-dsl-impact_v0.01.md`

---

## 1. Overview (work package overview)

This work package uplifts the ingestion canonical document model and the FileShare provider enrichment baseline so that:

- The canonical document retains a **provider-agnostic** record of the *active ingestion properties* (AddItem/UpdateItem) for traceability.
- A new low-ordinal enricher (`BasicEnricher`) performs a deterministic “baseline” enrichment by copying ingestion properties into:
  - `CanonicalDocument.Keywords` (values only)
  - `CanonicalDocument.Facets` (name/value)
- The canonical document includes a first-class `Timestamp` for downstream consumers.

This is intended to provide consistent data availability for indexing, filtering/aggregation, and rule-based enrichment.

---

## 2. High-level components

1. **Canonical Document Model (`CanonicalDocument`)**
   - Adds a first-class `Timestamp`.
   - Changes `Source` from an `IngestionRequest` snapshot to a minimal list of `IngestionProperty` representing the active Add/Update payload.

2. **FileShare Provider Baseline Enricher (`BasicEnricher`)**
   - Runs early in enrichment ordering (ordinal 10).
   - Maps ingestion properties into canonical keywords and facets.

3. **Ingestion Rules DSL Compatibility & Documentation**
   - Validate and (if required) update ingestion rules path parsing tests and documentation to ensure this uplift does not introduce ambiguity.

4. **Tests**
   - Update existing `CanonicalDocument` tests for the new `Source`/`Timestamp` shape.
   - Add a complete set of tests for `BasicEnricher` behavior and edge cases.

---

## 3. High-level flow (ingestion → canonical → enrichment)

At a high level:

1. Ingestion receives an `IngestionRequest` with exactly one of Add/Update/Delete/UpdateAcl.
2. For Add/Update, the provider’s canonical document builder creates a `CanonicalDocument` and stamps it with:
   - `Source` = active payload’s properties
   - `Timestamp` = active payload’s timestamp
3. Enrichment is applied in ordinal order:
   - `BasicEnricher` executes early to populate baseline keywords/facets.
   - Provider-specific enrichers execute later.
   - Rule-based enricher executes according to its configured ordinal.

---

## 4. Canonical document uplift (`Source` + `Timestamp`)

### 4.1 Summary

Uplift `CanonicalDocument` so it:

- Stores the active ingestion property list rather than the entire `IngestionRequest` payload.
- Stores a first-class `Timestamp` derived from the active Add/Update payload.

This supports baseline enrichers (including `BasicEnricher`) and improves downstream traceability without coupling the canonical document to the full ingestion request shape.

---

### 4.2 Goals

1. `CanonicalDocument.Source` is of type `IReadOnlyList<IngestionProperty>` and represents the **active payload** properties from either:
   - `request.AddItem.Properties`, or
   - `request.UpdateItem.Properties`.

2. `CanonicalDocument.Timestamp` is a first-class property of type `DateTimeOffset`.

3. Existing serialization behavior (System.Text.Json) remains deterministic and compatible with indexing.

4. All existing tests that construct/round-trip/compare canonical documents are updated and remain meaningful.

---

### 4.3 Non-goals

- Changing the ingestion request contract itself.
- Changing enrichment rules semantics.
- Introducing indexing/search behavior changes beyond the canonical document shape.

---

### 4.4 Requirements

#### 4.4.1 `Source`

- `CanonicalDocument.Source` MUST be of type `IReadOnlyList<IngestionProperty>`.
- `Source` MUST contain the full set of ingestion properties from the active Add/Update payload:
  - AddItem: `request.AddItem.Properties`
  - UpdateItem: `request.UpdateItem.Properties`
- `Source` MUST NOT include request-level metadata (e.g., request type, security tokens, files) unless those are explicitly modeled as `IngestionProperty` in the active payload.

Immutability / defensive copy:

- The canonical document builder MUST create a defensive copy of the active payload properties list when setting `CanonicalDocument.Source` (do not retain a direct reference to the request object's list).
- The defensive copy MUST be a shallow copy (new list/array), reusing the existing `IngestionProperty` instances.

#### 4.4.2 `Timestamp`

- `CanonicalDocument.Timestamp` MUST be of type `DateTimeOffset`.
- For Add/Update, it MUST be set to the corresponding request timestamp:
  - AddItem: `request.AddItem.Timestamp`
  - UpdateItem: `request.UpdateItem.Timestamp`

#### 4.4.3 Canonical document creation responsibility

- The provider’s canonical document builder MUST stamp `Source` and `Timestamp` at document creation time for Add/Update operations.
- Delete/UpdateAcl operations MUST NOT create a canonical document and are out of scope.

---

### 4.5 Serialization

- `CanonicalDocument` MUST be serializable/deserializable via `System.Text.Json`.
- `Source` MUST serialize as a JSON array of ingestion property objects.
- `Timestamp` MUST serialize as an ISO-8601 datetime value (System.Text.Json default for `DateTimeOffset`).

---

### 4.6 Search index mapping compatibility

- Any mapping that explicitly handles `source` MUST remain compatible with the new `Source` type.
- If `source` is mapped as “not indexed” (for trace/debug only), this requirement remains unchanged by this uplift.

---

### 4.7 Testing requirements

#### 4.7.1 Update existing canonical document tests

- Update all tests constructing canonical documents to supply:
  - a property list for `Source`, and
  - a timestamp value.

#### 4.7.2 Add tests for new behavior

At minimum, add tests that verify:

- JSON round-trip includes `Timestamp` and a list-shaped `Source`.
- `Timestamp` is preserved through serialization.
- `Source` properties are preserved through serialization.

---

### 4.8 Decisions

1. `CanonicalDocument.Timestamp` MUST use the request-level timestamp (`AddItem.Timestamp` / `UpdateItem.Timestamp`).
2. `CanonicalDocument.Source` MUST be set using a defensive copy of the active payload properties list.
3. The defensive copy for `CanonicalDocument.Source` MUST be a shallow copy (new list/array, reuse existing `IngestionProperty` instances).

---

## 5. FileShare `BasicEnricher` (baseline keywords + facets)

### 5.1 Summary

Introduce a new FileShare provider enricher named `BasicEnricher` that provides baseline enrichment by copying ingestion properties from the active Add/Update payload into the canonical document:

- **Keywords**: all property values (value only)
- **Facets**: all property name/value pairs

`BasicEnricher` must run early (ordinal 10) and be registered with existing FileShare enrichers in DI.

---

### 5.2 Goals

1. Provide a deterministic baseline enrichment from ingestion input into canonical fields.
2. Ensure consistent behavior across AddItem and UpdateItem.
3. Ensure baseline enrichment runs early so that later enrichers/rules can rely on these values existing.

---

### 5.3 Non-goals

- Implementing provider-specific semantic mapping (that remains the job of provider enrichers and/or ingestion rules).
- Mutating request payloads.

---

### 5.4 Requirements

#### 5.4.1 Enricher identity and ordering

- The implementation MUST be a class named `BasicEnricher`.
- It MUST implement `IIngestionEnricher`.
- It MUST have `Ordinal == 10`.

#### 5.4.2 Input selection

- The enricher MUST select exactly one active payload:
  1. If `request.AddItem` is not null, use it.
  2. Else if `request.UpdateItem` is not null, use it.
  3. Else no-op.

- The enricher MUST iterate the active payload’s `Properties` collection.

#### 5.4.3 Keyword mapping (values only)

For each `IngestionProperty` in the active payload:

- The enricher MUST take the property **value** and add it to `CanonicalDocument.Keywords`.
- If the property value is multi-valued (e.g., `string[]`), each element MUST be added as a separate keyword.
- Null/empty/whitespace values MUST be ignored.
- Keyword normalization/deduplication MUST rely on canonical document APIs (e.g., `CanonicalDocument.AddKeyword` / `SetKeyword`).

Value representation:

- Values MUST be treated as strings, or their string representations.
- If a value is an array, each element of the array MUST be treated as a separate value.

Value-to-string conversion rules:

- Values SHOULD use their standard string representation.
- If an explicit formatting policy is required for determinism (e.g., invariant culture for numbers), it MUST be documented as a decision in this specification.

Decision (current):

- Ingestion property values are expected to originate from database `NVARCHAR` fields, therefore values are expected to already be strings (or `string[]`).
- If an unexpected non-string value is encountered at runtime, the enricher SHOULD fall back to `Value.ToString()` and continue (non-throwing), unless and until a stricter policy is required.

#### 5.4.4 Facet mapping (name + value)

For each `IngestionProperty` in the active payload:

- The enricher MUST add a facet entry where:
  - Facet name = ingestion property name
  - Facet value(s) = ingestion property value(s)
- Multi-valued properties MUST add multiple facet values under the same facet name.
- Null/empty/whitespace facet names or values MUST be ignored.
- Facet normalization/deduplication MUST rely on canonical document APIs (e.g., `CanonicalDocument.AddFacetValue` / `AddFacetValues`).

Value representation:

- Values MUST be treated as strings, or their string representations.
- If a value is an array, each element of the array MUST be treated as a separate facet value.

#### 5.4.5 Dependency injection registration

- `BasicEnricher` MUST be registered in the FileShare provider’s DI composition root alongside existing enrichers.
- The registration MUST ensure `BasicEnricher` is discoverable via `IEnumerable<IIngestionEnricher>`.

#### 5.4.6 Error handling

- The enricher MUST be non-throwing for missing/empty properties collections.
- Any unexpected runtime conversion errors SHOULD be treated as per-message enrichment failure (consistent with existing enrichment node retry/failure behavior).

---

### 5.5 Testing requirements

A complete set of tests MUST be added covering:

1. **Ordering:** `Ordinal` equals 10.
2. **AddItem behavior:** properties copied to keywords and facets.
3. **UpdateItem behavior:** same as AddItem.
4. **String-array behavior:** array values become multiple keywords and multiple facet values.
5. **Type coverage:** at least one non-string type (e.g., `DateTimeOffset`, numeric, `Uri`) is converted deterministically.
6. **Null/empty handling:** null/whitespace values are ignored.
7. **Deduplication:** repeated values do not produce duplicates (case-insensitive, via canonical document normalization).

---

### 5.6 Open questions

1. Do we require a deterministic formatting policy (recommended) for non-string types (numbers/datetime/timespan), or is `Value.ToString()` acceptable?

---

## 6. Ingestion rules DSL impact check

### 6.1 Summary

This work package changes the shape of `CanonicalDocument.Source` and introduces `CanonicalDocument.Timestamp`.

Although the ingestion rules DSL primarily evaluates predicates against the active Add/Update payload (not the canonical document), this uplift requires an explicit compatibility check:

- Ensure rule path parsing/resolution remains correct.
- Ensure documentation and examples do not incorrectly imply rules target `CanonicalDocument.Source`.

---

### 6.2 Goals

1. Confirm that ingestion rules predicate evaluation remains based on:
   - `request.AddItem` or `request.UpdateItem` payloads.
2. Ensure DSL path parsing and path resolution tests remain valid and cover any edge cases impacted by model changes.
3. Update `docs/ingestion-rules.md` if it contains examples or explanations that become misleading due to the canonical document uplift.

---

### 6.3 Requirements

#### 6.3.1 DSL runtime behavior

- The rules engine MUST continue to evaluate predicates against the active Add/Update payload object.
- Paths like `properties["name"]` MUST continue to resolve against the active payload’s `Properties` collection.

#### 6.3.2 Documentation

- `docs/ingestion-rules.md` MUST be reviewed for:
  - references to `CanonicalDocument.Source` (if any)
  - references to canonical document indexing of `source`
- If updates are necessary, they MUST:
  - clarify that rules operate on Add/Update payloads
  - avoid introducing specific requirements into overview-level documents

Decision (current):

- Do not add FileShare `BasicEnricher` behavior to `docs/ingestion-rules.md` unless the review identifies existing content that becomes misleading without it.

#### 6.3.3 Tests

- All existing ingestion rules DSL parsing and evaluation tests MUST be re-run and updated if they assume a specific canonical document `Source` shape.
- Add a regression test (if absent) that asserts rule evaluation payload selection:
  - AddItem preferred when present
  - UpdateItem used when AddItem absent

---

### 6.4 Open questions

1. Do we want to extend the DSL in future to resolve paths against the canonical document (post-enrichment), or keep it strictly request-payload scoped?
