# Component Spec: `CanonicalDocument` Universal Discovery Taxonomy Fields

Target path: `dev/work-packages/mvp/029-new-canonical-fields/canonical-document-taxonomy-fields.md`

## 1. Overview

Introduce a set of new taxonomy-related fields to `CanonicalDocument`. These fields are all multi-valued and must follow the same semantics as the existing `Keywords` property.

## 2. Functional Requirements

### 2.1 New fields

Add the following fields to `CanonicalDocument`:

| Property name | CLR type (conceptual) | Cardinality | Notes |
|---|---:|---:|---|
| `Authority` | `string` | multi | Exact-match taxonomy value. |
| `Region` | `string` | multi | Geographic taxonomy value. |
| `Fornat` | `string` | multi | Taxonomy value. Name intentionally follows provided spelling. |
| `MajorVersion` | `int` | multi | Version taxonomy value. |
| `MinorVersion` | `int` | multi | Version taxonomy value. |
| `Category` | `string` | multi | Product/category taxonomy value. |
| `Series` | `string` | multi | Series taxonomy value. |
| `Instance` | `string` | multi | Instance taxonomy value. |

### 2.2 Set semantics (additive behavior)

All new taxonomy fields must behave as sets:

- Setting a value must **add** it to the existing set of values for that field.
- Adding the same value multiple times must not create duplicates.
- Values must be stored in a deterministic order (sorted) to ensure stable serialization and stable tests.

### 2.3 Ordering and equality rules

- Ordering must be deterministic:
  - For string fields: ordinal sorting (consistent with existing `Keywords` behavior).
  - For numeric fields: ascending numeric sort.
- Equality / de-duplication:
  - For string fields: case handling must follow the existing `Keywords` behavior in the codebase (the implementation should match the established approach to avoid inconsistent behavior).

### 2.4 Null/empty handling

- Null, empty, or whitespace-only values must not be added to a set.
- Numeric fields:
  - If the source has no value, do not add.
  - If the source provides invalid numeric values, handling must align with existing ingestion validation strategy:
    - Fail fast only for invalid schema/operators/path syntax.
    - For missing runtime values, the rule does not match (no derived output).

## 3. Technical Requirements

### 3.1 `CanonicalDocument` API shape

The implementation must align with the current `CanonicalDocument` pattern used for `Keywords`:

- The public property should expose an immutable/read-only view appropriate to the existing patterns.
- Mutating operations should be done through explicit methods (e.g., `SetXxx`, `AddXxx`, or similar), consistent with existing `CanonicalDocument` style.
- Adding a value must not replace the existing collection.

### 3.2 Serialization / indexing payload

- The ingestion-to-indexing transformation must include the new taxonomy fields.
- The serialized indexing payload must emit the full set of values for each field.
- Types in JSON must match mapping expectations:
  - String sets as JSON arrays of strings.
  - Numeric version sets as JSON arrays of numbers or strings depending on mapping guidance (see mapping spec).

### 3.3 Backwards compatibility

- Existing documents without these fields must remain indexable.
- Existing tests and payload snapshots must be updated to include the new fields only where applicable.

## 4. Acceptance Criteria

- `CanonicalDocument` exposes all new fields and provides a supported way to add values.
- Values are stored without duplicates and with deterministic ordering.
- Ingestion/indexing payload includes these fields.
- All existing tests are updated and passing.
- New tests exist proving:
  1. Additive behavior for each field (adding does not replace).
  2. De-duplication.
  3. Deterministic ordering.
