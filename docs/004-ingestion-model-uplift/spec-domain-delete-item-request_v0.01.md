# Specification: `DeleteItemRequest`

**Target output path:** `docs/004-ingestion-model-uplift/spec-domain-delete-item-request_v0.01.md`

**Version:** v0.01 (Draft)

## 1. Overview

`DeleteItemRequest` represents an ingestion operation that deletes an item by identifier.

The request carries a single mandatory `Id` as a first-class string property.

This type must be serializable/deserializable with `System.Text.Json`.

## 2. Goals and Non-Goals

### Goals
- Provide a request payload type named `DeleteItemRequest`.
- Represent the delete target using a single `IngestionProperty` named `Id`.
- Ensure `null` properties are omitted when serialized.

### Non-Goals
- Defining delete semantics in the downstream index.

## 3. Data Contract (JSON)

### 3.1 JSON field naming
- JSON property names must be **PascalCase**.

### 3.2 Conceptual schema
- `Id`: string

### 3.3 Example JSON

```json
{
  "Id": "ABC123"
}
```

## 4. C# Model Requirements

### 4.1 Shape
- `DeleteItemRequest` must contain:
  - `Id`: `string`

### 4.2 Null omission
- Any `null` properties must be omitted in JSON output.

## 5. Validation Rules

- `Id` must not be `null`.
- `Id` must not be empty or whitespace.

## 6. Open Questions / Decisions

### 6.1 Decisions captured
- `Id` is a first-class required string property.

### 6.2 Open questions
- None.
