# Implementation Plan

**Target output path:** `dev/work-packages/mvp/000-ingestion-model/000-ingestion-model.plan.md`

## Work Package: 000-ingestion-model � Typed Ingestion Data Model

### Project Structure / Placement

- Implement the model in a shared location suitable for ingestion entrypoints (API/background/queue). Candidate projects:
  - `src/UKHO.Search.Ingestion` if it owns ingestion contracts.
  - `src/UKHO.Search.Services` if contracts are shared across services.

(Determine the final placement by inspecting existing ingestion DTO/contract patterns in the repo during Work Item 1.)

---

## Vertical Slice 1: �Hello Ingestion Model� (round-trip JSON + validation)

- [x] **Work Item 1: Add strongly typed ingestion request model with `System.Text.Json` round-trip** - Completed
  - **Purpose**: Establish the canonical ingestion metadata contract (typed named properties + `DataCallback`) with correct JSON behavior and validation.
  - **Summary (Completed)**:
    - Implemented `IngestionRequest`, `IngestionProperty`, `IngestionPropertyType` in `src/UKHO.Search.Ingestion`.
    - Added `System.Text.Json` converters enforcing lowercase `Type` tokens + strict `Type`/`Value` validation (including absolute `uri` and non-null `string-array` elements) and order-independent deserialization.
    - Added dictionary-style helpers via `IngestionRequestExtensions` (case-insensitive lookup).
    - Added thorough xUnit/Shouldly tests in `test/UKHO.Search.Tests` and referenced `UKHO.Search.Ingestion` from the test project.
  - **Acceptance Criteria**:
    - A strongly typed C# model exists representing the spec:
      - `IngestionRequest` with `Properties` and `DataCallback`.
      - `IngestionProperty` with `Name`, `Type`, and `Value`.
      - Supported `Type` values: `string`, `integer` (`long`), `double`, `decimal`, `boolean`, `datetime` (`DateTimeOffset`), `timespan` (TimeSpan constant string), `id` (string), `guid`, `uri` (absolute), `string-array`.
    - `System.Text.Json` can serialize and deserialize the model successfully.
    - `Type` serializes to the specified lowercase tokens.
    - `Value` serializes/deserializes to the correct JSON token types per `Type`.
    - Null values are not serialized (omit null properties).
    - Validation failures are deterministic and throw clear exceptions.
    - All tests are implemented in `test/UKHO.Search.Tests` (project: `test/UKHO.Search.Tests/UKHO.Search.Tests.csproj`) using xUnit and Shouldly.
    - Tests are extremely thorough and cover:
      - all supported data types with representative valid values
      - invalid values for each data type
      - mismatched `Type`/`Value` combinations
      - property name casing/uniqueness behavior (case-insensitive)
      - serialization settings (omit null values, casing of `Type`, and any configured serializer defaults/options)
  - **Definition of Done**:
    - Code implemented (model + converters/serialization)
    - Unit tests cover:
      - successful round-trip for each supported type
      - invalid `Type`/`Value` combinations
      - `uri` absolute validation
      - case-insensitive duplicate name behavior
      - �omit nulls� behavior
      - casing behavior for `Type` values (serialized lowercase; invalid casing on deserialize handled as specified)
      - boundary values where applicable (e.g., `long` min/max, decimal precision, empty string array)
    - `dotnet test` passes
    - Documentation updated if needed (this work package)
    - Can execute end-to-end via: `dotnet test`
  - [x] **Task 1: Locate correct project/folder for ingestion contracts** - Completed
    - [x] Step 1: Search existing ingestion DTOs/models and identify the established pattern (namespace/folder/project). - Completed (placed in `src/UKHO.Search.Ingestion` for ingestion entrypoints)
    - [x] Step 2: Choose the placement that minimizes coupling and matches repo conventions. - Completed
  - [x] **Task 2: Implement model types and enums** - Completed
    - [x] Step 1: Create `IngestionRequest`, `IngestionProperty`, `IngestionPropertyType`. - Completed
    - [x] Step 2: Ensure Allman braces, nullable annotations, and immutability where appropriate. - Completed
  - [x] **Task 3: Implement `System.Text.Json` converters** - Completed
    - [x] Step 1: Add an enum converter so `IngestionPropertyType` serializes as lowercase tokens. - Completed
    - [x] Step 2: Add a custom converter for `IngestionProperty` (or for the `Value`) to enforce `Type`/`Value` token compatibility and parse into the correct CLR type. - Completed
    - [x] Step 3: Enforce `uri` is absolute. - Completed
    - [x] Step 4: Ensure `string-array` only allows arrays of strings (no null elements). - Completed
    - [x] Step 5: Ensure null-value omission is satisfied (via `[JsonIgnore(Condition = WhenWritingNull)]` and/or default serializer options used by the host). - Completed
  - [x] **Task 4: Add dictionary-style helpers** - Completed
    - [x] Step 1: Add helper methods (e.g., `TryGetString`, `TryGetInt64`, `TryGetDateTimeOffset`, etc.). - Completed
    - [x] Step 2: Ensure lookups are `StringComparer.OrdinalIgnoreCase`. - Completed
  - [x] **Task 5: Unit tests** - Completed
    - [x] Step 1: Create a test matrix covering every supported `IngestionPropertyType` with multiple valid values. - Completed
    - [x] Step 2: For each supported type, add invalid-value tests (wrong JSON token type, unparsable strings, out-of-range numbers where applicable). - Completed
    - [x] Step 3: Add exhaustive mismatched `Type`/`Value` tests (e.g., `Type="integer"` with string value; `Type="string-array"` with non-array; boolean with number, etc.). - Completed
    - [x] Step 4: Add tests for property name uniqueness and lookup behavior using case-insensitive comparisons. - Completed
    - [x] Step 5: Add tests for serialization settings:
      - omit null properties (no `null` JSON emitted) - Completed
      - `Type` casing (serialized lowercase) and deserialization handling of casing - Completed
      - consistent behavior using the repo�s default `JsonSerializerOptions` (if applicable) - Completed (via `IngestionJsonSerializerOptions.Create()`)
    - [x] Step 6: Ensure all tests live in `test/UKHO.Search.Tests` and use xUnit + Shouldly assertions. - Completed
  - **Files** (indicative; final paths depend on repo conventions):
    - `src/UKHO.Search.Ingestion/Models/IngestionRequest.cs`: request root model
    - `src/UKHO.Search.Ingestion/Models/IngestionProperty.cs`: property model + converter
    - `src/UKHO.Search.Ingestion/Models/IngestionPropertyType.cs`: enum
    - `src/UKHO.Search.Ingestion/Serialization/IngestionJsonSerializerOptions.cs`: shared `JsonSerializerOptions` factory (if repo uses this pattern)
    - `test/UKHO.Search.Tests/.../IngestionModelJsonTests.cs`: extremely thorough unit tests (xUnit + Shouldly)
  - **Work Item Dependencies**: None
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **User Instructions**: None

---

## Summary / Key Considerations

- The highest-risk portion is the `Value` field: it is polymorphic and must be enforced by a custom JSON converter to keep strong typing and validation.
- Prefer `StringComparer.OrdinalIgnoreCase` for property name uniqueness/lookups.
- Ensure null omission is defined either via attributes on model properties and/or centralized `JsonSerializerOptions` to keep behavior consistent across hosts.
- Focus on correctness + converter enforcement + exhaustive tests to lock the contract down before any host integration.
