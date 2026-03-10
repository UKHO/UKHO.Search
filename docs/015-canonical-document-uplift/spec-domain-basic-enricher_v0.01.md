# Specification: FileShare `BasicEnricher` (Baseline Keywords + Facets)

**Target output path:** `docs/015-canonical-document-uplift/spec-domain-basic-enricher_v0.01.md`

**Version:** v0.01 (Draft)

## 0. Related documents

- Overview: `docs/015-canonical-document-uplift/spec-overview-canonical-document-uplift_v0.01.md`
- Canonical document uplift: `docs/015-canonical-document-uplift/spec-domain-canonical-document-uplift_v0.01.md`

---

## 1. Summary

Introduce a new FileShare provider enricher named `BasicEnricher` that provides baseline enrichment by copying ingestion properties from the active Add/Update payload into the canonical document:

- **Keywords**: all property values (value only)
- **Facets**: all property name/value pairs

`BasicEnricher` must run early (ordinal 10) and be registered with existing FileShare enrichers in DI.

---

## 2. Goals

1. Provide a deterministic baseline enrichment from ingestion input into canonical fields.
2. Ensure consistent behavior across AddItem and UpdateItem.
3. Ensure baseline enrichment runs early so that later enrichers/rules can rely on these values existing.

---

## 3. Non-goals

- Implementing provider-specific semantic mapping (that remains the job of provider enrichers and/or ingestion rules).
- Mutating request payloads.

---

## 4. Requirements

### 4.1 Enricher identity and ordering

- The implementation MUST be a class named `BasicEnricher`.
- It MUST implement `IIngestionEnricher`.
- It MUST have `Ordinal == 10`.

### 4.2 Input selection

- The enricher MUST select exactly one active payload:
  1. If `request.AddItem` is not null, use it.
  2. Else if `request.UpdateItem` is not null, use it.
  3. Else no-op.

- The enricher MUST iterate the active payload’s `Properties` collection.

### 4.3 Keyword mapping (values only)

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

### 4.4 Facet mapping (name + value)

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

### 4.5 Dependency injection registration

- `BasicEnricher` MUST be registered in the FileShare provider’s DI composition root alongside existing enrichers.
- The registration MUST ensure `BasicEnricher` is discoverable via `IEnumerable<IIngestionEnricher>`.

### 4.6 Error handling

- The enricher MUST be non-throwing for missing/empty properties collections.
- Any unexpected runtime conversion errors SHOULD be treated as per-message enrichment failure (consistent with existing enrichment node retry/failure behavior).

---

## 5. Testing requirements

A complete set of tests MUST be added covering:

1. **Ordering:** `Ordinal` equals 10.
2. **AddItem behavior:** properties copied to keywords and facets.
3. **UpdateItem behavior:** same as AddItem.
4. **String-array behavior:** array values become multiple keywords and multiple facet values.
5. **Type coverage:** at least one non-string type (e.g., `DateTimeOffset`, numeric, `Uri`) is converted deterministically.
6. **Null/empty handling:** null/whitespace values are ignored.
7. **Deduplication:** repeated values do not produce duplicates (case-insensitive, via canonical document normalization).

---

## 6. Open questions

1. Do we require a deterministic formatting policy (recommended) for non-string types (numbers/datetime/timespan), or is `Value.ToString()` acceptable?
