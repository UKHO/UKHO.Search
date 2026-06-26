# Component Spec: Index Mapping Update for New Taxonomy Fields

Target path: `dev/work-packages/mvp/029-new-canonical-fields/index-mapping-update.md`

## 1. Overview

Update the search index mapping to include new taxonomy fields emitted by `CanonicalDocument`.

The new fields are multi-valued. In Elasticsearch/OpenSearch, arrays are represented by repeating the same field name in JSON arrays; mapping does not change specifically for arrays, but field types must be correct.

## 2. Mapping Requirements

### 2.1 New fields and types

Add the following fields to the index mapping:

| Field name | Canonical type | Index type |
|---|---|---|
| `authority` | string[] | `keyword` |
| `region` | string[] | `keyword` |
| `format` | string[] | `keyword` |
| `majorVersion` | int[] | `keyword` |
| `minorVersion` | int[] | `keyword` |
| `category` | string[] | `keyword` |
| `series` | string[] | `keyword` |
| `instance` | string[] | `keyword` |

Notes:
- Field casing in the index (e.g., camelCase vs PascalCase) must follow the repository’s existing mapping convention.
- `keyword` is used for exact matching, filtering, and aggregations.

### 2.2 Version field representation

`MajorVersion` and `MinorVersion` are specified as `int` but mapped as `keyword`.

Implementation must choose one consistent representation.

**Decision (Work Package 029): Index as numbers in JSON** (e.g., `[1, 2]`) while mapping remains `keyword`.

Decision criteria:
- Prefer matching existing conventions for numeric-as-keyword fields in the current mapping.
- Ensure the mapping does not reject documents and that aggregations/filters behave as expected.

This decision must be reflected consistently in:
- The canonical-to-index payload transform.
- Tests asserting payload shape.

### 2.3 Backward compatible mapping change strategy

- The mapping update must be forward-compatible for new documents.
- Existing documents that do not include these fields must continue to be indexed and queried.

If the repository includes:
- templates / composable index templates,
- index creation scripts,
- schema validation tests,

then those assets must be updated alongside the code.

## 3. Acceptance Criteria

- Index mapping contains all new taxonomy fields with the types listed above.
- Documents can be indexed with zero, one, or multiple values for each taxonomy field.
- Mapping deployment assets and schema tests (if present) are updated and passing.
