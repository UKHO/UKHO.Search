# Implementation Plan

**Target output path:** `docs/004-ingestion-model-uplift/plan-ingestion-ingestion-model-uplift_v0.01.md`

**Version:** v0.01 (Draft)

**Based on:**
- `docs/004-ingestion-model-uplift/spec-overview-ingestion-model-uplift_v0.01.md`
- `docs/004-ingestion-model-uplift/spec-domain-add-item-request_v0.01.md`
- `docs/004-ingestion-model-uplift/spec-domain-update-item-request_v0.01.md`
- `docs/004-ingestion-model-uplift/spec-domain-delete-item-request_v0.01.md`
- `docs/004-ingestion-model-uplift/spec-domain-update-acl-request_v0.01.md`
- `docs/004-ingestion-model-uplift/spec-domain-ingestion-request-envelope_v0.01.md`

## Ingestion Request Model Uplift (Domain + JSON Contract)

- [x] Work Item 1: Extend `IngestionPropertyType` with `Text` and adjust JSON converters - Completed
  - **Purpose**: Introduce the new `text` wire token and ensure the ingestion property model cleanly supports a distinct string-like text type used by indexing.
  - **Acceptance Criteria**:
    - `IngestionPropertyType` includes `Text` and serializes/deserializes to/from the string token `"text"`.
    - `IngestionProperty` serialization/deserialization accepts string values for `Text`.
    - Existing ingestion model JSON tests are updated/extended to cover `Text`.
  - **Definition of Done**:
    - Code implemented (enum + converters + tests)
    - Tests passing
    - Can execute end-to-end via: `dotnet test`
  - [x] Task 1: Update enum + converters - Completed
    - [x] Step 1: Add `Text` to `IngestionPropertyType`.
    - [x] Step 2: Update `IngestionPropertyTypeJsonConverter` to map `"text"` <-> `Text`.
    - [x] Step 3: Update `IngestionPropertyJsonConverter` to treat `Text` as a string-valued type.
  - [x] Task 2: Update/extend tests - Completed
    - [x] Step 1: Extend `IngestionPropertyType_Serializes_ToLowercaseTokens` test coverage to include `Text`.
    - [x] Step 2: Add at least one round-trip test value using `Text`.
  - **Files**:
    - `src/UKHO.Search.Ingestion/Requests/IngestionPropertyType.cs`: add `Text` enum member.
    - `src/UKHO.Search.Ingestion/Requests/Serialization/IngestionPropertyTypeJsonConverter.cs`: add `"text"` mappings.
    - `src/UKHO.Search.Ingestion/Requests/Serialization/IngestionPropertyJsonConverter.cs`: accept/read/write `Text` as string.
    - `test/UKHO.Search.Ingestion.Tests/IngestionModelJsonTests.cs`: add/extend tests.
  - **Work Item Dependencies**: none
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`

  - **Implementation Summary**:
    - Added `IngestionPropertyType.Text` and mapped it to the `"text"` JSON token.
    - Updated `IngestionPropertyJsonConverter` to read/write `Text` as a string-valued type.
    - Extended ingestion JSON round-trip tests to cover `Text`.
    - Removed a legacy `DataCallback` test that no longer matches the current request model.

- [x] Work Item 2: Remove `id` from `IngestionPropertyType` and introduce first-class `Id` on request payloads - Completed
  - **Purpose**: Align the model with the uplift requirement that identifiers are first-class properties on request payloads and are not represented as typed ingestion properties.
  - **Acceptance Criteria**:
    - `IngestionPropertyType` no longer supports `Id` / `"id"`.
    - `AddItemRequest` and `UpdateItemRequest` include a mandatory `Id` string property (non-null, non-empty/whitespace).
    - `DeleteItemRequest` and `UpdateAclRequest` use the same first-class mandatory `Id` string.
    - Any `Properties` collection cannot carry an `Id` property (by name and/or legacy `id` type) per spec.
    - Unit tests updated so the ingestion test suite passes without `Id` property type.
  - **Definition of Done**:
    - Code implemented (enum removal + request model changes + validations)
    - All compilation errors fixed across solution
    - Tests passing
    - Documentation updated where required (specs are already updated in this work package)
    - Can execute end-to-end via: `dotnet test`
  - [x] Task 1: Remove `Id` from typed properties - Completed
    - [x] Step 1: Remove `Id` from `IngestionPropertyType`.
    - [x] Step 2: Update `IngestionPropertyTypeJsonConverter` to reject `"id"`.
    - [x] Step 3: Update `IngestionPropertyJsonConverter` to remove `Id` handling paths.
  - [x] Task 2: Introduce first-class `Id` properties on request payloads - Completed
    - [x] Step 1: Update the existing request payload model (currently `IngestionRequest`, to be renamed in Work Item 3) to include `Id`.
    - [x] Step 2: Ensure constructor/validation logic enforces `Id` required (non-null, non-empty/whitespace).
    - [x] Step 3: Ensure `Properties` cannot also contain a property named `Id` (case-insensitive), and cannot contain `Type=id` (legacy) during deserialization.
  - [x] Task 3: Update tests and any consuming code - Completed
    - [x] Step 1: Update ingestion JSON round-trip tests to carry the new `Id` property.
    - [x] Step 2: Search solution for usages of `IngestionPropertyType.Id`, `TryGetId`, and update accordingly.
  - **Files**:
    - `src/UKHO.Search.Ingestion/Requests/IngestionPropertyType.cs`: remove `Id`.
    - `src/UKHO.Search.Ingestion/Requests/Serialization/IngestionPropertyTypeJsonConverter.cs`: remove `"id"` mappings.
    - `src/UKHO.Search.Ingestion/Requests/Serialization/IngestionPropertyJsonConverter.cs`: remove `Id` handling.
    - `src/UKHO.Search.Ingestion/Requests/*Request*.cs`: add `Id` properties + validation.
    - `test/UKHO.Search.Ingestion.Tests/IngestionModelJsonTests.cs`: update for first-class `Id`.
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - `dotnet test`

  - **Implementation Summary**:
    - Removed `IngestionPropertyType.Id` and legacy `"id"` wire token support from converters.
    - Introduced first-class `IngestionRequest.Id` (required, non-empty) and prevented duplicate `Id` representation inside `Properties`.
    - Updated `TryGetId` to read the request `Id` property.
    - Updated ingestion JSON tests to use first-class `Id` and added a regression test that `"id"` is rejected.
    - Updated `tools/FileShareEmulator` to populate `IngestionRequest.Id` instead of creating an `Id` ingestion property.

- [x] Work Item 3: Rename payload `IngestionRequest` -> `AddItemRequest` and introduce the polymorphic `IngestionRequest` envelope - Completed
  - **Purpose**: Deliver the main uplift capability: an envelope that supports Add/Update/Delete/UpdateAcl operations with an explicit discriminator and one-of named payload properties.
  - **Acceptance Criteria**:
    - The existing payload type `IngestionRequest` is renamed to `AddItemRequest` (including extensions).
    - New payload types exist: `UpdateItemRequest`, `DeleteItemRequest`, `UpdateAclRequest`.
    - `SecurityTokens` exists on `AddItemRequest`, `UpdateItemRequest`, and `UpdateAclRequest` and is required/non-empty.
    - A new envelope type `IngestionRequest` exists with:
      - `RequestType` discriminator (`IngestionRequestType` enum)
      - one-of payload properties `AddItem`, `UpdateItem`, `DeleteItem`, `UpdateAcl`
      - null payload properties omitted from JSON
      - exactly-one-payload and matches-`RequestType` invariants validated
    - Unit tests cover round-trip JSON serialization/deserialization for each request type within the envelope.
  - **Definition of Done**:
    - Code implemented (models, converters/attributes, validation)
    - Tests passing
    - Can execute end-to-end via: `dotnet test`
  - [x] Task 1: Rename and refactor existing request payload and extensions - Completed
    - [x] Step 1: Rename `IngestionRequest` (payload) to `AddItemRequest`.
    - [x] Step 2: Rename `IngestionRequestExtensions` to target `AddItemRequest`.
    - [x] Step 3: Add required `SecurityTokens` to `AddItemRequest`.
  - [x] Task 2: Implement remaining payload request types - Completed
    - [x] Step 1: Add `UpdateItemRequest` matching `AddItemRequest` shape.
    - [x] Step 2: Add `DeleteItemRequest` with required `Id` string.
    - [x] Step 3: Add `UpdateAclRequest` with required `Id` and required non-empty `SecurityTokens`.
  - [x] Task 3: Implement the envelope and discriminator - Completed
    - [x] Step 1: Add `IngestionRequestType` enum with the required values.
    - [x] Step 2: Implement `IngestionRequest` envelope with `RequestType` and one-of payload properties.
    - [x] Step 3: Ensure JSON uses PascalCase field names and PascalCase discriminator string values (per spec).
    - [x] Step 4: Implement validation enforcing request type/payload alignment and one-of constraints.
  - [x] Task 4: Update and expand tests - Completed
    - [x] Step 1: Add round-trip tests for each request type inside the new envelope.
    - [x] Step 2: Add negative tests for invalid envelopes (mismatched payload vs `RequestType`, multiple payloads, missing payload).
    - [x] Step 3: Add tests for `SecurityTokens` required/non-empty constraints.
  - **Files**:
    - `src/UKHO.Search.Ingestion/Requests/AddItemRequest.cs`: new (renamed from existing payload).
    - `src/UKHO.Search.Ingestion/Requests/UpdateItemRequest.cs`: new.
    - `src/UKHO.Search.Ingestion/Requests/DeleteItemRequest.cs`: new.
    - `src/UKHO.Search.Ingestion/Requests/UpdateAclRequest.cs`: new.
    - `src/UKHO.Search.Ingestion/Requests/IngestionRequest.cs`: new envelope.
    - `src/UKHO.Search.Ingestion/Requests/IngestionRequestType.cs`: new.
    - `src/UKHO.Search.Ingestion/Requests/*Extensions.cs`: rename/update.
    - `test/UKHO.Search.Ingestion.Tests/*`: update and add envelope tests.
  - **Work Item Dependencies**: Work Item 2
  - **Run / Verification Instructions**:
    - `dotnet test`

  - **Implementation Summary**:
    - Renamed the prior payload request to `AddItemRequest` and introduced required, non-empty `SecurityTokens`.
    - Added `UpdateItemRequest`, `DeleteItemRequest`, and `UpdateAclRequest` payload types.
    - Implemented the new `IngestionRequest` envelope with `RequestType` + one-of named payload properties and validation.
    - Retargeted typed-property extensions to `AddItemRequest`.
    - Updated `tools/FileShareEmulator` to publish the envelope with an `AddItemRequest` payload.
    - Updated/expanded ingestion tests to cover envelope round-trips and validation failures.

## Summary / Key Considerations

- The plan is sequenced to keep the solution buildable and tests runnable after each work item.
- JSON contract changes (removing `"id"`, introducing `"text"`, introducing the envelope) are breaking changes; tests act as the primary executable contract.
- Validation and invariants should be enforced at deserialization boundaries so invalid messages fail fast and deterministically.
