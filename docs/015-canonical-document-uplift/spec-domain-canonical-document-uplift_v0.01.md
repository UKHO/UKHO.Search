# Specification: Canonical Document Uplift (Source + Timestamp)

**Target output path:** `docs/015-canonical-document-uplift/spec-domain-canonical-document-uplift_v0.01.md`

**Version:** v0.01 (Draft)

## 0. Related documents

- Overview: `docs/015-canonical-document-uplift/spec-overview-canonical-document-uplift_v0.01.md`
- FileShare `BasicEnricher`: `docs/015-canonical-document-uplift/spec-domain-basic-enricher_v0.01.md`
- Ingestion rules guide: `docs/ingestion-rules.md`

---

## 1. Summary

Uplift `CanonicalDocument` so it:

- Stores the active ingestion property list rather than the entire `IngestionRequest` payload.
- Stores a first-class `Timestamp` derived from the active Add/Update payload.

This supports baseline enrichers (including `BasicEnricher`) and improves downstream traceability without coupling the canonical document to the full ingestion request shape.

---

## 2. Goals

1. `CanonicalDocument.Source` is of type `IReadOnlyList<IngestionProperty>` and represents the **active payload** properties from either:
   - `request.AddItem.Properties`, or
   - `request.UpdateItem.Properties`.

2. `CanonicalDocument.Timestamp` is a first-class property of type `DateTimeOffset`.

3. Existing serialization behavior (System.Text.Json) remains deterministic and compatible with indexing.

4. All existing tests that construct/round-trip/compare canonical documents are updated and remain meaningful.

---

## 3. Non-goals

- Changing the ingestion request contract itself.
- Changing enrichment rules semantics.
- Introducing indexing/search behavior changes beyond the canonical document shape.

---

## 4. Requirements

### 4.1 Canonical schema changes

#### 4.1.1 `Source`

- `CanonicalDocument.Source` MUST be of type `IReadOnlyList<IngestionProperty>`.
- `Source` MUST contain the full set of ingestion properties from the active Add/Update payload:
  - AddItem: `request.AddItem.Properties`
  - UpdateItem: `request.UpdateItem.Properties`
- `Source` MUST NOT include request-level metadata (e.g., request type, security tokens, files) unless those are explicitly modeled as `IngestionProperty` in the active payload.

Immutability / defensive copy:

- The canonical document builder MUST create a defensive copy of the active payload properties list when setting `CanonicalDocument.Source` (do not retain a direct reference to the request object's list).
- The defensive copy MUST be a shallow copy (new list/array), reusing the existing `IngestionProperty` instances.

#### 4.1.2 `Timestamp`

- `CanonicalDocument.Timestamp` MUST be of type `DateTimeOffset`.
- For Add/Update, it MUST be set to the corresponding request timestamp:
  - AddItem: `request.AddItem.Timestamp`
  - UpdateItem: `request.UpdateItem.Timestamp`

#### 4.1.3 Canonical document creation responsibility

- The provider’s canonical document builder MUST stamp `Source` and `Timestamp` at document creation time for Add/Update operations.
- Delete/UpdateAcl operations MUST NOT create a canonical document and are out of scope.

### 4.2 Serialization

- `CanonicalDocument` MUST be serializable/deserializable via `System.Text.Json`.
- `Source` MUST serialize as a JSON array of ingestion property objects.
- `Timestamp` MUST serialize as an ISO-8601 datetime value (System.Text.Json default for `DateTimeOffset`).

### 4.3 Search index mapping compatibility

- Any mapping that explicitly handles `source` MUST remain compatible with the new `Source` type.
- If `source` is mapped as “not indexed” (for trace/debug only), this requirement remains unchanged by this uplift.

---

## 5. Testing requirements

### 5.1 Update existing canonical document tests

- Update all tests constructing canonical documents to supply:
  - a property list for `Source`, and
  - a timestamp value.

### 5.2 Add tests for new behavior

At minimum, add tests that verify:

- JSON round-trip includes `Timestamp` and a list-shaped `Source`.
- `Timestamp` is preserved through serialization.
- `Source` properties are preserved through serialization.

---

## 6. Decisions

1. `CanonicalDocument.Timestamp` MUST use the request-level timestamp (`AddItem.Timestamp` / `UpdateItem.Timestamp`).
2. `CanonicalDocument.Source` MUST be set using a defensive copy of the active payload properties list.
3. The defensive copy for `CanonicalDocument.Source` MUST be a shallow copy (new list/array, reuse existing `IngestionProperty` instances).
