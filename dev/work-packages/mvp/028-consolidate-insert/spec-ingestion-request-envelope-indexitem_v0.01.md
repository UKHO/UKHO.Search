# Spec: IngestionRequest envelope consolidation to `IndexItem`

Target path: `dev/work-packages/mvp/028-consolidate-insert/spec-ingestion-request-envelope-indexitem_v0.01.md`

## 1. Overview

### 1.1 Purpose
`UKHO.Search.Ingestion.Requests.IngestionRequest` currently exposes **two** upsert payload properties:
- `AddItem` (`IndexRequest?`)
- `UpdateItem` (`IndexRequest?`)

This duplicates the upsert concept and forces the rest of the codebase to branch on naming rather than behavior.

This change consolidates these into a **single** upsert payload property:

```csharp
[JsonPropertyName("IndexItem")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public IndexRequest? IndexItem { get; init; }
```

All code and tests must be updated so that upsert behavior uses `IndexItem` only.

### 1.2 Non-goals
- This spec **does** consolidate `IngestionRequestType` by replacing `AddItem` and `UpdateItem` with `IndexItem`.
- This spec does **not** change the shape of `IndexRequest`.

---

## 2. Contract changes

### 2.1 `IngestionRequestType` values

#### Remove
- `AddItem`
- `UpdateItem`

#### Add
- `IndexItem`

#### Resulting request types
- `IndexItem`
- `DeleteItem`
- `UpdateAcl`

> Note: all serialization, parsing, validation and dispatch logic must be updated so `RequestType` uses `IndexItem` for all upsert/index operations.

### 2.2 `IngestionRequest` properties

#### Remove
- `AddItem`
- `UpdateItem`

#### Add
- `IndexItem`

#### Resulting envelope model (conceptual)
- `RequestType` remains.
- One and only one of:
  - `IndexItem`
  - `DeleteItem`
  - `UpdateAcl`
  is populated.

> Note: `ValidateOneOf(...)` must be updated accordingly.

### 2.3 JSON serialization

The JSON payload property name for upsert must be:
- `"IndexItem"`

All JSON tests (“golden” JSON) must be updated.

If any component previously emitted `"AddItem"` / `"UpdateItem"`, those emitters must switch to `"IndexItem"`.

---

## 3. Behavioral requirements

### 3.1 Upsert behavior
Any pipeline, provider, adapter, or enricher that previously accepted `AddItem` or `UpdateItem` must be updated to:
- treat `IndexItem` as the only upsert payload
- preserve existing upsert semantics

### 3.2 Validation behavior
When `RequestType` is `IndexItem`, the request must require:
- `IndexItem` is non-null

When `IndexItem` is set, the envelope must not contain other payloads.

---

## 4. Implementation guidance (solution-wide refactor)

### 4.1 Code updates
Update all references across the solution:
- `request.AddItem` -> `request.IndexItem`
- `request.UpdateItem` -> `request.IndexItem`

Update enum usages across the solution:
- `IngestionRequestType.AddItem` -> `IngestionRequestType.IndexItem`
- `IngestionRequestType.UpdateItem` -> `IngestionRequestType.IndexItem`

Update any branching logic that checks for “add vs update” based on payload presence to a single-path upsert.

### 4.2 Tests
Update/introduce tests to cover:
- Envelope round-trip JSON serialization/deserialization with `IndexItem`
- Envelope rejects missing payload when `RequestType` indicates upsert
- Pipeline dispatch treats an upsert request exactly as before, but via `IndexItem`

---

## 5. Acceptance criteria
- `IngestionRequest` has **only one** upsert payload property: `IndexItem`.
- No code references remain to `AddItem` or `UpdateItem` properties on `IngestionRequest`.
- `IngestionRequestType` no longer contains `AddItem` or `UpdateItem`; it contains `IndexItem`.
- All compilation succeeds.
- Existing test suites are updated and pass.

