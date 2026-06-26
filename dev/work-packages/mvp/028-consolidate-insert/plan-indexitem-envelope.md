# Implementation Plan

Target path: `dev/work-packages/mvp/028-consolidate-insert/plan-indexitem-envelope.md`

This plan implements `dev/work-packages/mvp/028-consolidate-insert/spec-ingestion-request-envelope-indexitem_v0.01.md` by consolidating:
- `IngestionRequestType.AddItem` + `IngestionRequestType.UpdateItem` -> `IngestionRequestType.IndexItem`
- `IngestionRequest.AddItem` + `IngestionRequest.UpdateItem` -> `IngestionRequest.IndexItem`

Scope note: this is a **solution-wide refactor** (domain + providers + infrastructure + tools + tests) to update payload names, enum values, and JSON shapes.

---

## Feature Slice: Unified upsert envelope (`IndexItem`) + unified request type

- [x] Work Item 1: Introduce `IndexItem` in `IngestionRequest` and keep solution runnable - Completed
  - **Purpose**: Establish the new unified upsert payload (`IndexItem`) end-to-end while maintaining existing upsert behavior.
  - **Acceptance Criteria**:
    - `IngestionRequest` exposes `IndexItem` (type `IndexRequest?`) with JSON property name `IndexItem`.
    - `IngestionRequest` no longer exposes `AddItem` or `UpdateItem`.
    - All compilation succeeds.
    - Tests are updated to use `IndexItem`.
  - **Definition of Done**:
    - Code compiles across solution
    - Unit/integration tests updated and passing
    - No references remain to `IngestionRequest.AddItem` or `IngestionRequest.UpdateItem`
    - Can execute end-to-end via: `dotnet test` (at least ingestion test project)
  - [x] Task 1.1: Update `IngestionRequest` contract - Completed
    - [x] Step 1: Replace ctor signature parameters `addItem`/`updateItem` with `indexItem`.
    - [x] Step 2: Replace properties `AddItem`/`UpdateItem` with `IndexItem` using:
      - `[JsonPropertyName("IndexItem")]`
      - `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]`
    - [x] Step 3: Update `ValidateOneOf(...)` to validate exactly one of `IndexItem`, `DeleteItem`, `UpdateAcl`.
    - [x] Step 4: Update error messages to reflect `IndexItem`.
  - [x] Task 1.2: Update request creators/emitters to set `IndexItem` - Completed
    - [x] Step 1: Update FileShare emulator request creation.
    - [x] Step 2: Update synthetic pipeline request generation.
    - [x] Step 3: Update any message producers/queue writers that populate `AddItem`/`UpdateItem`.
  - [x] Task 1.3: Update pipeline dispatch/building to read `IndexItem` - Completed
    - [x] Step 1: Update provider dispatch nodes and any canonical builders that pick add vs update based on payload.
    - [x] Step 2: Remove add/update branching where it is only selecting payload.
  - [x] Task 1.4: Update tests for payload property rename and JSON shape - Completed
    - [x] Step 1: Update JSON envelope tests (“golden json”) to use `IndexItem`.
    - [x] Step 2: Update pipeline/enrichment/rules tests to build `IngestionRequest` with `IndexItem`.
    - [x] Step 3: Add/adjust validation tests to ensure missing `IndexItem` fails when request type indicates upsert.
  - Summary:
    - Replaced `IngestionRequest.AddItem`/`UpdateItem` with `IndexItem` and updated constructor + validation logic.
    - Updated FileShare provider dispatch/canonical build/enrichers and queue request-id extraction to use `IndexItem`.
    - Updated FileShare emulator and all affected tests/golden JSON to emit/expect `IndexItem`.
    - Verified: `dotnet build` and `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`.
  - **Files** (indicative):
    - `src/UKHO.Search.Ingestion/Requests/IngestionRequest.cs`: replace `AddItem`/`UpdateItem` with `IndexItem`.
    - `src/**`: update all call sites.
    - `tools/**`: update request producers.
    - `test/**`: update all tests and golden JSON.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
  - **User Instructions**: None.

---

## Feature Slice: Consolidate request type enum (`IndexItem`)

- [x] Work Item 2: Replace `AddItem`/`UpdateItem` request types with `IndexItem` - Completed
  - **Purpose**: Remove the conceptual split between add/update at the envelope level and unify upsert semantics.
  - **Acceptance Criteria**:
    - `IngestionRequestType` contains `IndexItem` and does not contain `AddItem`/`UpdateItem`.
    - All request type checks, routing, and serialization use `IndexItem`.
    - Tests updated and passing.
  - **Definition of Done**:
    - Code compiles across solution
    - Tests pass
    - No references remain to `IngestionRequestType.AddItem` or `IngestionRequestType.UpdateItem`
  - [x] Task 2.1: Update `IngestionRequestType` - Completed
    - [x] Step 1: Replace enum members with: `IndexItem`, `DeleteItem`, `UpdateAcl`.
    - [x] Step 2: Update any JSON parsing/serialization assumptions/tests.
  - [x] Task 2.2: Update dispatch/routing - Completed
    - [x] Step 1: Update dispatch nodes to handle `IndexItem` (upsert) + existing delete/acl.
    - [x] Step 2: Update any switch expressions over request type.
  - [x] Task 2.3: Update tests - Completed
    - [x] Step 1: Update all tests referencing enum values.
    - [x] Step 2: Update golden JSON string values (`"RequestType":"IndexItem"`).
  - Summary:
    - Updated `IngestionRequestType` enum to `IndexItem/DeleteItem/UpdateAcl`.
    - Updated FileShare provider dispatch/validation, pipeline builder, queue source node, emulator, and tests to use `IndexItem`.
    - Updated JSON golden payloads to use `"RequestType":"IndexItem"`.
    - Verified: `dotnet build` and `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`.
  - **Files** (indicative):
    - `src/UKHO.Search.Ingestion/Requests/IngestionRequestType.cs`: consolidate enum members.
    - `src/**`: update all references.
    - `test/**`: update tests.
  - **Work Item Dependencies**:
    - Depends on Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
  - **User Instructions**: None.

---

## Feature Slice: Backwards-compatibility decision (explicit)

- [x] Work Item 3: Decide and implement compatibility for legacy JSON payloads (AddItem/UpdateItem) - Completed (Strict)
  - **Purpose**: Make the migration strategy explicit: either reject old payload names/types or accept them for a transition period.
  - **Acceptance Criteria** (choose one strategy):
    1) Strict: old payloads are rejected with clear validation errors. **(Chosen)**
    2) Compatible: old payloads are accepted and mapped into `IndexItem`.
  - **Definition of Done**:
    - Behavior is defined and tested.
    - Documentation updated with the chosen migration strategy.
  - [x] Task 3.1: Implement chosen strategy - Completed
    - [x] Step 1: If compatible, add custom JSON converter or shim model to map `AddItem`/`UpdateItem` -> `IndexItem`. (N/A)
    - [x] Step 2: If strict, ensure validation errors are actionable.
  - [x] Task 3.2: Tests - Completed
    - [x] Step 1: Add tests for legacy JSON payload behavior.
  - Summary:
    - Chosen strategy: **Strict** (fresh development; do not accept legacy payload/property names).
    - Added tests ensuring JSON with legacy `\"AddItem\"` / `\"UpdateItem\"` payload properties is rejected.
  - **Files** (indicative):
    - `src/UKHO.Search.Ingestion/Requests/*`: converters/shims if needed.
    - `test/UKHO.Search.Ingestion.Tests/*`: JSON behavior tests.
  - **Work Item Dependencies**:
    - Depends on Work Items 1 and 2.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
  - **User Instructions**:
    - If strict strategy is chosen: coordinate rollout with any external producers of ingestion messages.

---

## Summary / Key Considerations
- Deliver in vertical slices: first unify the payload property (`IndexItem`) while keeping UI/pipeline runnable; then consolidate enum; then explicitly handle compatibility.
- Pay special attention to JSON payload names/values and any external message producers.
- Ensure rule evaluation, dispatch, and upsert canonical-building logic no longer branches on add vs update.
