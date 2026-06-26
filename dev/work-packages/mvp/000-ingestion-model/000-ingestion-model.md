# Work Package: 000-ingestion-model � Ingestion Data Model

**Target output path:** `dev/work-packages/mvp/000-ingestion-model/000-ingestion-model.md`

## 1. Overview

Create a strongly typed C# ingestion data model in the `UKHO.Search` codebase that can be serialized/deserialized to JSON using `System.Text.Json` (Microsoft JSON serializer).

The model represents an ingestion request/command containing:

- A collection of **typed named properties** (name/value/type)
- A `DataCallback` URI indicating where the ingestion pipeline can fetch associated **binary data** (out-of-band)

This work package defines the functional and technical requirements for the model and its JSON wire format.

## 2. Goals and Non-Goals

### Goals

- Provide a **single, strongly typed** model for ingestion metadata.
- Support **round-trip JSON** (serialize + deserialize) with `System.Text.Json`.
- Support a dictionary-like set of properties using **typed named pairs**.
- Ensure types are constrained to the supported set: `string`, `integer`, `double`, `decimal`, `boolean`, `datetime`, `timespan`, `id`, `guid`, `uri`, `string-array`.
- Ensure null values are not emitted when serializing to JSON (omit null properties).

### Non-Goals

- Defining the ingestion API endpoint(s) that accept this model.
- Defining authorization/authentication for the `DataCallback` endpoint.
- Defining binary payload transfer protocol (beyond �fetchable via callback URI�).

## 3. JSON Contract

### 3.1 Example JSON

```json
{
  "Properties": [
    {
      "Name": "AProperty",
      "Value": "a value",
      "Type": "string"
    },
    {
      "Name": "AnotherProperty",
      "Value": 1234,
      "Type": "integer"
    }
  ],
  "DataCallback": "https://someserver/123456ID"
}
```

### 3.2 Schema (conceptual)

- `Properties`: array of typed property objects
  - `Name`: string (required)
  - `Type`: string enum (required) with allowed values:
    - `string`
    - `integer`
    - `double`
    - `decimal`
    - `boolean`
    - `datetime`
    - `timespan`
    - `id`
    - `guid`
    - `uri`
    - `string-array`
  - `Value`: JSON value (required)
    - The JSON token type depends on `Type`:
      - `string`, `id`, `datetime`, `timespan`, `guid`, `uri` => JSON string
      - `integer` => JSON number without fractional part
      - `double` => JSON number
      - `decimal` => JSON number
      - `boolean` => JSON boolean
      - `string-array` => JSON array of strings
- `DataCallback`: absolute URI string (required)

### 3.3 Date/time and duration formats

- `datetime`: must use ISO 8601 / RFC 3339 format as a JSON string (e.g. `"2026-03-05T10:15:30Z"`).
- `timespan`: must use the .NET `TimeSpan` constant format as a JSON string (e.g. `"00:15:00"`, `"2.03:00:00"`).
- `uri`: must be an absolute URI string (e.g. `"https://example.test/resource/123"`).

## 4. C# Model Requirements

### 4.1 Primary types

Define model types (names indicative):

- `IngestionRequest`
  - `IReadOnlyList<IngestionProperty> Properties` (required)
  - `Uri DataCallback` (required)

- `IngestionProperty`
  - `string Name` (required)
  - `IngestionPropertyType Type` (required)
  - `object Value` (required) with **controlled** serialization/deserialization

- `IngestionPropertyType` (enum)
  - `String`
  - `Integer`
  - `Double`
  - `Decimal`
  - `Boolean`
  - `DateTime`
  - `TimeSpan`
  - `Id`
  - `Guid`
  - `Uri`
  - `StringArray`

### 4.2 Serialization expectations (`System.Text.Json`)

- `IngestionPropertyType` must serialize as the specified lowercase strings (e.g. `"string"`, `"integer"`).
- `DataCallback` must serialize as a JSON string.
- `Value` must serialize into the appropriate JSON token type based on `Type`.
- `uri` property values must serialize as a JSON string.
- `string-array` property values must serialize as a JSON array of strings.
- Null values must not be serialized (omit null properties).

### 4.3 Deserialization expectations

- Deserialization must validate that `Value` matches `Type`.
- Invalid combinations must fail fast with a clear exception (e.g., `Type="integer"` with `Value="abc"`).
- For `Type="uri"`, deserialization must validate that the value parses as an absolute `System.Uri`.

### 4.4 Dictionary semantics

Although JSON uses an array, consumers should be able to treat `Properties` as a **dictionary-like** set:

- `Name` must be unique (case-sensitivity rule must be defined and implemented; recommended: case-insensitive uniqueness).
- Provide a helper API for retrieval:
  - `TryGetString(string name, out string? value)`
  - `TryGetInt32(string name, out int value)`
  - etc.

## 5. Validation Rules

- `Properties` cannot be null.
- Each property:
  - `Name` is required and non-empty.
  - `Type` is required.
  - `Value` is required.
- `DataCallback` is required and must be an absolute URI.
- Properties with `Type="uri"` must be absolute URIs.
- Property names must be unique (recommended: case-insensitive).

## 6. Integration Notes

- The model should live in a location that makes it accessible to any ingestion entrypoint (e.g. API/controller, background worker, queue consumer).
- Avoid coupling the model to any specific transport (HTTP, queue) beyond JSON.

## 7. Open Questions / Decisions

### 7.1 Decisions captured

1. `Properties` name uniqueness/lookups: case-insensitive.
2. `integer` CLR type: `int64` (`long`).
3. `datetime` CLR type: `DateTimeOffset`.
4. `timespan` wire format: .NET `TimeSpan` constant format (`"00:15:00"`, `"2.03:00:00"`).

### 7.2 Remaining open questions

None.
