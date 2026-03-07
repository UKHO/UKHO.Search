# Specification: `AddItemRequest`

**Target output path:** `docs/004-ingestion-model-uplift/spec-domain-add-item-request_v0.01.md`

**Version:** v0.01 (Draft)

## 1. Overview

`AddItemRequest` represents an ingestion operation that adds a new item to the search index (or equivalent downstream store). It is the uplifted/renamed form of the existing `IngestionRequest` request payload, extended to carry security tokens.

This type must be serializable/deserializable with `System.Text.Json`.

`AddItemRequest.Properties` supports `IngestionPropertyType.Text` (`"text"` on the wire) to represent human-readable text that may be treated differently by indexing processes than ordinary `String` values.

## 2. Goals and Non-Goals

### Goals
- Provide a request payload type named `AddItemRequest`.
- Preserve existing behaviours of the previous `IngestionRequest` payload shape regarding typed properties.
- Extend the request with `SecurityTokens`.
- Ensure `null` properties are omitted when serialized.

### Non-Goals
- Defining the security model or meaning of the tokens.
- Defining any ingestion workflow beyond the JSON contract and model shape.

## 3. Data Contract (JSON)

### 3.1 JSON field naming
- JSON property names must be **PascalCase**.

### 3.2 Conceptual schema
- `Id`: string
- `Properties`: array of `IngestionProperty`
- `SecurityTokens`: array of strings

### 3.3 Example JSON

```json
{
  "Id": "ABC123",
  "Properties": [
    {
      "Name": "Title",
      "Value": "My item",
      "Type": "string"
    }
  ],
  "SecurityTokens": [
    "token-a",
    "token-b"
  ]
}
```

## 4. C# Model Requirements

### 4.1 Shape
- `AddItemRequest` must contain:
  - `Id`: `string`
  - `Properties`: `IReadOnlyList<IngestionProperty>`
  - `SecurityTokens`: `string[]`

### 4.2 Null omission
- Any `null` properties must be omitted in JSON output, consistent with existing patterns.

## 5. Validation Rules

### 5.1 Id
- `Id` is required and must not be `null`.
- `Id` must not be empty or whitespace.

### 5.2 Properties
- `Properties` must not be `null`.
- Property `Name` values must be unique (case-insensitive uniqueness) within a single request.
- `Properties` must not contain an `IngestionProperty` whose `Type` is `id`.
- `Properties` must not contain an `IngestionProperty` whose `Name` is `"Id"` (case-insensitive), as `Id` is carried as a first-class property.
- If an `IngestionProperty` has `Type = text`, its `Value` must be a JSON string.

### 5.3 SecurityTokens
- `SecurityTokens` is **required** and **must be non-empty**.
- `SecurityTokens` must not be `null`.
- Each token must be a non-empty string (whitespace-only tokens are invalid).

## 6. Integration / Compatibility Notes

- This work package is a breaking change at the type-name level: existing usages of `IngestionRequest` are expected to move to `AddItemRequest`.
- Existing extension methods that read typed properties from `IngestionRequest` are expected to be renamed/re-targeted to `AddItemRequest` (see envelope spec for cross-cutting notes).

## 7. Open Questions / Decisions

### 7.1 Decisions captured
- `SecurityTokens` required and must be non-empty.

### 7.2 Open questions
- None.
