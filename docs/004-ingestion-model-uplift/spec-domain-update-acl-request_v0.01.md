# Specification: `UpdateAclRequest`

**Target output path:** `docs/004-ingestion-model-uplift/spec-domain-update-acl-request_v0.01.md`

**Version:** v0.01 (Draft)

## 1. Overview

`UpdateAclRequest` represents an ingestion operation that updates the ACL (or access control representation) for an item.

Per work item scope, this request contains:
- `Id`: a first-class string property identifying the target item, and
- `SecurityTokens`: a required, non-empty array of strings.

This type must be serializable/deserializable with `System.Text.Json`.

## 2. Goals and Non-Goals

### Goals
- Provide a request payload type named `UpdateAclRequest`.
- Represent the target item via a single `Id` property.
- Carry `SecurityTokens` used by downstream components.
- Ensure `null` properties are omitted when serialized.

### Non-Goals
- Defining the ACL model, token contents, or token evaluation semantics.

## 3. Data Contract (JSON)

### 3.1 JSON field naming
- JSON property names must be **PascalCase**.

### 3.2 Conceptual schema
- `Id`: string
- `SecurityTokens`: array of strings

### 3.3 Example JSON

```json
{
  "Id": "ABC123",
  "SecurityTokens": [
    "token-a",
    "token-b"
  ]
}
```

## 4. C# Model Requirements

### 4.1 Shape
- `UpdateAclRequest` must contain:
  - `Id`: `string`
  - `SecurityTokens`: `string[]`

### 4.2 Null omission
- Any `null` properties must be omitted in JSON output.

## 5. Validation Rules

### 5.1 Id
- `Id` must not be `null`.
- `Id` must not be empty or whitespace.

### 5.2 SecurityTokens
- `SecurityTokens` is **required** and **must be non-empty**.
- `SecurityTokens` must not be `null`.
- Each token must be a non-empty string (whitespace-only tokens are invalid).

## 6. Open Questions / Decisions

### 6.1 Decisions captured
- `Id` is a first-class required string property.
- `SecurityTokens` required and must be non-empty.

### 6.2 Open questions
- None.
