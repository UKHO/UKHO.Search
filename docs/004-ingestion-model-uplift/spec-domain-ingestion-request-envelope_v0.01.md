# Specification: `IngestionRequest` Envelope + `IngestionRequestType`

**Target output path:** `docs/004-ingestion-model-uplift/spec-domain-ingestion-request-envelope_v0.01.md`

**Version:** v0.01 (Draft)

## 1. Overview

This specification defines a new `IngestionRequest` type that acts as a message envelope for four ingestion operations:
- Add item (`AddItemRequest`)
- Update item (`UpdateItemRequest`)
- Delete item (`DeleteItemRequest`)
- Update ACL (`UpdateAclRequest`)

The envelope must support polymorphic serialization/deserialization to/from JSON using `System.Text.Json` without relying on external type metadata.

Polymorphism is represented via:
- a discriminator enum `IngestionRequestType` stored in the `RequestType` JSON field, and
- a “one-of” set of named payload properties (`AddItem`, `UpdateItem`, `DeleteItem`, `UpdateAcl`) where only the matching payload is present.

## 2. Goals and Non-Goals

### Goals
- Provide a stable JSON contract for ingestion messages that can represent multiple operations.
- Ensure the envelope round-trips with `System.Text.Json`.
- Ensure `null` properties are omitted from JSON output.
- Ensure consumers can determine operation type via `RequestType` and then read the corresponding payload property.

### Non-Goals
- Defining transport (queue vs HTTP) or message headers.
- Defining retry semantics, correlation IDs, or operational metadata.

## 3. Data Contract (JSON)

### 3.1 JSON field naming
- JSON property names must be **PascalCase**.

### 3.2 Discriminator
- Field: `RequestType`
- JSON values (strings, PascalCase):
  - `"AddItem"`
  - `"UpdateItem"`
  - `"DeleteItem"`
  - `"UpdateAcl"`

### 3.3 Payload properties (one-of)
Exactly one of the following payload properties must be present:
- `AddItem`: `AddItemRequest`
- `UpdateItem`: `UpdateItemRequest`
- `DeleteItem`: `DeleteItemRequest`
- `UpdateAcl`: `UpdateAclRequest`

All non-selected payload properties must be `null` (and therefore omitted from JSON output).

### 3.4 Example JSON

#### Add item
```json
{
  "RequestType": "AddItem",
  "AddItem": {
    "Id": "ABC123",
    "Properties": [
      { "Name": "Title", "Value": "My item", "Type": "string" }
    ],
    "SecurityTokens": [ "token-a" ]
  }
}
```

#### Delete item
```json
{
  "RequestType": "DeleteItem",
  "DeleteItem": {
    "Id": "ABC123"
  }
}
```

## 4. C# Model Requirements

### 4.1 Envelope shape
- `IngestionRequest` must contain:
  - `RequestType`: `IngestionRequestType`
  - `AddItem`: `AddItemRequest?`
  - `UpdateItem`: `UpdateItemRequest?`
  - `DeleteItem`: `DeleteItemRequest?`
  - `UpdateAcl`: `UpdateAclRequest?`

### 4.2 Enum
- `IngestionRequestType` must include the four values:
  - `AddItem`
  - `UpdateItem`
  - `DeleteItem`
  - `UpdateAcl`

### 4.3 Serialization rules
- `RequestType` must serialize as a JSON string with the PascalCase values listed above (not numeric).
- Any `null` payload properties must be omitted from JSON output.

### 4.4 Deserialization rules
- Deserialization must be able to reconstruct the correct envelope with payload.
- Consumers must be able to read `RequestType` and then read the corresponding payload property.

## 5. Validation Rules

### 5.1 One-of invariants
- `RequestType` is required.
- Exactly one payload property must be non-null.
- The non-null payload property must match `RequestType`:
  - `RequestType = AddItem` => `AddItem` must be non-null; others must be null
  - `RequestType = UpdateItem` => `UpdateItem` must be non-null; others must be null
  - `RequestType = DeleteItem` => `DeleteItem` must be non-null; others must be null
  - `RequestType = UpdateAcl` => `UpdateAcl` must be non-null; others must be null

### 5.2 Empty JSON / missing payload
- If `RequestType` is present but the matching payload property is missing or null, the message is invalid.

### 5.3 Unexpected payload
- If a payload property is present that does not match `RequestType`, the message is invalid.

## 6. Cross-cutting Notes

### 6.1 Rename impact
- The existing `IngestionRequest` payload type is renamed to `AddItemRequest`.
- Any existing extension methods targeting the old type name must be renamed/re-targeted to `AddItemRequest`.

### 6.2 Id as first-class property
- `Id` is represented as a first-class required string property in `AddItemRequest`, `UpdateItemRequest`, `DeleteItemRequest`, and `UpdateAclRequest`.
- `IngestionPropertyType` must no longer support `id`; `Properties` arrays must not contain an `IngestionProperty` of type `id`.

### 6.3 Human-readable text property type
- `IngestionPropertyType` supports `Text` (`"text"` on the wire) as a distinct string-like type to denote values that should be treated as human-readable text by indexing processes.

### 6.4 Null omission consistency
- This envelope must follow the repository’s existing JSON practice: omit null properties.

## 7. Open Questions / Decisions

### 7.1 Decisions captured
- Envelope polymorphism uses `RequestType` + one-of named payload properties.
- JSON casing is PascalCase for both field names and discriminator values.

### 7.2 Open questions
- None.
