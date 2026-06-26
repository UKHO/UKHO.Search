# Work Package: Ingestion Rules Engine – Additional Top-Level `then` Fields

Target path: `dev/work-packages/mvp/030-rule-engine-additions/overview.md`

## 1. Overview

This work package updates the **UKHO.Search Ingestion Rules Engine DSL** so that the new `CanonicalDocument` fields can be enriched via rules using **top-level** `then` fields (peer to `keywords`, `searchText`, `content`, `facets`, and `documentType`).

A previous documentation update introduced these taxonomy fields under a nested `then.taxonomy` block. That shape is **not correct** for the intended DSL.

The goal of this work item is to:

- Define the desired JSON DSL shape for the new top-level fields.
- Update ruleset JSON validation, DTO model binding, and action application so these new fields are supported.
- Ensure all existing action semantics still apply (normalization, skipping empty values, additive set behavior).
- Provide regression and integration test coverage.
- Update end-user documentation (`wiki/Ingestion-Rules.md`) once implementation is complete.

This document is the complete specification for this work package (single-document spec).

## 2. High-level system context

The ingestion pipeline evaluates provider-scoped rules at runtime and applies matching `then` actions to produce a `CanonicalDocument`, which is then transformed into an indexing payload.

Rules are validated **fail-fast at service startup**. Therefore, the DSL shape for these new top-level fields must be:

- expressible in JSON
- unambiguous during deserialization
- validated consistently alongside existing `then` actions

The fields added to `CanonicalDocument` in the prior work package are set-like (additive, de-duplicated, deterministic ordering). The rules engine must be able to add values to these fields directly.

## 3. Functional requirements

### 3.1 New supported `then` fields

Update the ingestion rules JSON DSL so that the fields recently added to `CanonicalDocument` can be enriched from rules via **top-level** `then` fields.

Add support for the following **top-level** `then` fields:

String fields (set-like):

- `authority.add` (string/template array)
- `region.add` (string/template array)
- `fornat.add` (string/template array) // spelling is intentional and must match `CanonicalDocument`
- `category.add` (string/template array)
- `series.add` (string/template array)
- `instance.add` (string/template array)

Numeric fields (set-like):

- `majorVersion.add` (number array)
- `minorVersion.add` (number array)

Notes:

- These actions are applied to the corresponding fields on `CanonicalDocument`.
- They must have the same additive, de-duplicated “set semantics” as the existing `keywords.add` action.

### 3.2 JSON shape

The `then` object must support these fields directly (no nested `then.taxonomy` object):

```json
"then": {
  "keywords": { "add": ["..."] },
  "searchText": { "add": ["..."] },
  "content": { "add": ["..."] },
  "facets": { "add": [ { "name": "...", "value": "..." } ] },
  "documentType": { "set": "..." },

  "authority": { "add": ["..."] },
  "region": { "add": ["..."] },
  "fornat": { "add": ["..."] },
  "category": { "add": ["..."] },
  "series": { "add": ["..."] },
  "instance": { "add": ["..."] },

  "majorVersion": { "add": [1] },
  "minorVersion": { "add": [0] }
}
```

### 3.3 Templates and variables

For string fields, values must support the same templating/variable expansion rules as other string-based actions (e.g., `keywords.add`):

- literal string values
- `$val`
- `$path:<path>`

For numeric fields, values are JSON numbers. Templating does **not** apply to numeric fields.

### 3.4 Example rules

#### 3.4.1 String example

```json
{
  "id": "doc-taxonomy-region-authority",
  "if": {
    "all": [
      { "path": "properties[\"region\"]", "exists": true },
      { "path": "properties[\"authority\"]", "exists": true }
    ]
  },
  "then": {
    "region": { "add": ["$path:properties[\"region\"]"] },
    "authority": { "add": ["$path:properties[\"authority\"]"] }
  }
}
```

#### 3.4.2 Numeric example

```json
{
  "id": "doc-taxonomy-versions",
  "if": {
    "all": [
      { "path": "properties[\"product\"]", "eq": "AVCS" }
    ]
  },
  "then": {
    "majorVersion": { "add": [10] },
    "minorVersion": { "add": [1] }
  }
}
```

## 4. Validation requirements

### 4.1 Schema / JSON shape validation (startup)

At service startup, validation must enforce:

- For each string action (`authority`, `region`, `fornat`, `category`, `series`, `instance`):
  - If present, it must be an object.
  - It must contain an `add` property.
  - `add` must be a JSON array.
  - Each item in `add` must be a JSON string.

- For each numeric action (`majorVersion`, `minorVersion`):
  - If present, it must be an object.
  - It must contain an `add` property.
  - `add` must be a JSON array.
  - Each item in `add` must be a JSON number.

- These action blocks must not allow `set` (only `add`), consistent with set/additive semantics.

### 4.2 Unknown fields in `then`

Validation must remain consistent with current rules engine behavior:

- Unknown/unmapped fields under `then` should fail-fast at startup (preferred), or
- be explicitly ignored only if the current engine already intentionally ignores unknown fields.

The implementation should document which approach is used today and keep it consistent.

### 4.3 Rules file `schemaVersion`

- The top-level rules file `schemaVersion` remains `"1.0"` unless there is a concrete need to bump it.
- If a version bump is required, provide upgrade guidance and ensure mixed environments fail safely.

## 5. Action application & semantics

### 5.0 Removal of existing taxonomy-named rule engine code

The current codebase contains taxonomy-named rules DTO/action types (for example `TaxonomyStringActionDto` and `TaxonomyIntActionDto`). These are legacy/incorrect naming and (if currently wired into rules parsing) represent an incorrect conceptual model.

This work package must:

- Remove any rules DSL shape that implies a nested `taxonomy` object.
- Reshape the rules engine so the new fields are expressed and processed as **top-level** `then` properties.
- Remove (or rename to neutral names) any existing taxonomy-named DTO/action code paths so the implementation reflects the intended DSL and avoids misleading terminology.

Implementation note:

- Prefer renaming DTOs to reflect their purpose (e.g., string `add` action vs numeric `add` action) rather than keeping taxonomy-oriented names.
- Ensure public DSL JSON property names remain exactly as specified in §3.2.

### 5.1 Action-to-field mapping

| `then` field | Type | Target `CanonicalDocument` field |
|---|---|---|
| `authority.add` | string/template[] | `Authority` |
| `region.add` | string/template[] | `Region` |
| `fornat.add` | string/template[] | `Fornat` |
| `category.add` | string/template[] | `Category` |
| `series.add` | string/template[] | `Series` |
| `instance.add` | string/template[] | `Instance` |
| `majorVersion.add` | number[] | `MajorVersion` |
| `minorVersion.add` | number[] | `MinorVersion` |

### 5.2 Additive, set-like behavior

All new actions are additive and set-like:

- Applying an action must add values to the existing set.
- Duplicates must not be created.
- Ordering must remain deterministic, consistent with `CanonicalDocument` set semantics.

### 5.3 String normalization and skipping empty values

For string fields:

- Apply the same normalization as other rule-produced strings (trim + lowercase invariant).
- Skip null/empty/whitespace outputs.
- Template expansion must follow the existing rules engine template expansion behavior.

### 5.4 Numeric values

For numeric fields:

- Inputs are JSON numbers.
- Values are added to the corresponding numeric set.
- Duplicates are removed according to numeric equality.
- Ordering is ascending numeric.

### 5.5 Observability

Action application should contribute to existing action-apply summary/telemetry in the same way as other actions:

- Record which actions were applied.
- Record how many outputs were produced per action.
- Avoid logging individual values if that would violate current logging standards.

## 6. Tests & regression coverage

### 6.1 DTO binding / deserialization tests

Add tests proving a ruleset containing any of the new `then` fields:

- successfully deserializes
- validates at startup
- reaches the action applier with expected DTO values

Include both:

- string actions with templates (`$path:` and `$val`)
- numeric actions with JSON numbers

### 6.2 Validation failure tests

Cover fail-fast startup validation for:

- action blocks missing `add`
- `add` not being an array
- incorrect element types:
  - numbers in a string taxonomy action
  - strings/non-numbers in a numeric taxonomy action

### 6.3 End-to-end rules engine integration tests

Add/extend integration tests to prove:

- Rule predicates match and the new actions are applied.
- Additive behavior across multiple rules in file order.
- De-duplication across repeated adds.

### 6.4 Regression snapshot tests (index item payload)

Update payload regression tests (or add new snapshots) to include the new fields in the emitted indexing payload when rules add those values.

Ensure expectations include:

- deterministic ordering
- correct normalization for string fields
- numeric arrays emitted in the expected form for the mapping
