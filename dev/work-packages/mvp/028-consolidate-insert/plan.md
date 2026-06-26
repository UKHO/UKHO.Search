# Implementation Plan

Target path: `dev/work-packages/mvp/028-consolidate-insert/plan.md`

## Project Structure / Change Scope
This work package is limited to the ingestion domain project and its tests, focused on consolidating add/update ingestion requests into a single `IndexRequest` contract and updating `CanonicalDocument` accordingly.

Expected impacted areas:
- Domain request contracts in `src/UKHO.Search.Ingestion/Requests/`
- `CanonicalDocument` in `src/UKHO.Search.Ingestion/Pipeline/Documents/`
- Pipeline nodes, provider contexts, and adapters that reference `AddItemRequest` / `UpdateItemRequest` or `CanonicalDocument.DocumentId` / `Source`
- Unit/integration tests across ingestion projects

Naming / conventions:
- Use block-scoped namespaces and Allman braces in all C# edits.
- Maintain one public type per file when renaming/removing request types.

---

## Feature Slice: Unified ingestion request (Add/Update -> Index)

- [x] Work Item 1: Introduce `IndexRequest` and replace `AddItemRequest` usage end-to-end - Completed
  - **Purpose**: Deliver a runnable ingestion flow using the new unified request type while keeping behavior equivalent.
  - **Acceptance Criteria**:
    - `IndexRequest` exists and is used as the request type throughout the ingestion pipeline.
    - `AddItemRequest` no longer exists (type and file removed or renamed).
    - All compilation succeeds and existing tests are updated to use `IndexRequest`.
  - **Definition of Done**:
    - Code compiles across solution
    - Unit/integration tests referencing add request updated and passing
    - No references remain to `AddItemRequest`
    - Can execute end-to-end via: `dotnet test` for affected test projects (or solution-level test execution)
  - [x] Task 1.1: Rename request type and file - Completed
    - [x] Step 1: Rename `AddItemRequest` type to `IndexRequest`. (Renamed and re-homed into `IndexRequest.cs`)
    - [x] Step 2: Rename file `AddItemRequest.cs` -> `IndexRequest.cs` (keeping one public type per file).
    - [x] Step 3: Update constructors/factory methods (if any) and update namespace/usings in dependents.
  - [x] Task 1.2: Update all references to use `IndexRequest` - Completed
    - [x] Step 1: Update pipeline nodes, providers, contexts, validators, mapping, and serialization code that references `AddItemRequest`.
    - [x] Step 2: Update any discriminators/enums/handlers that treat Add differently (ensure no behavior change except naming/unification).
  - [x] Task 1.3: Update tests for new request type - Completed
    - [x] Step 1: Replace usages of `AddItemRequest` in tests with `IndexRequest`.
    - [x] Step 2: Update test data builders/fixtures accordingly.
  - **Files** (indicative):
    - `src/UKHO.Search.Ingestion/Requests/AddItemRequest.cs`: rename to `IndexRequest.cs` and rename type.
    - `src/UKHO.Search.Ingestion/**`: update references.
    - `src/**Tests**/**`: update references and fixtures.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test`
  - **User Instructions**: None.

  - **Implementation notes / summary**:
    - Introduced `IndexRequest` (same fields/validation as previous add request) and updated `IngestionRequest.AddItem` to use it while keeping JSON property name `AddItem` unchanged.
    - Renamed extension methods to target `IndexRequest`.
    - Updated ingestion rules path validation and FileShare emulator to build `IndexRequest`.
    - Updated ingestion/enrichment/rules tests to reference `IndexRequest`.
    - Verified: `dotnet build` and `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`.

---

## Feature Slice: Remove update request concept

- [x] Work Item 2: Remove `UpdateItemRequest` and consolidate update pathways into `IndexRequest` - Completed
  - **Purpose**: Remove maintenance burden and branching by eliminating the update-specific request type while keeping ingestion functional.
  - **Acceptance Criteria**:
    - `UpdateItemRequest` type and file are removed.
    - All prior update code paths now accept/process `IndexRequest`.
    - Tests formerly covering update request behavior are updated and pass.
  - **Definition of Done**:
    - Code compiles and tests pass
    - No references remain to `UpdateItemRequest`
    - Any add/update branching is removed or reduced to single-path logic
    - Can execute end-to-end via: `dotnet test`
  - [x] Task 2.1: Remove type and update compilation errors - Completed
    - [x] Step 1: Delete `UpdateItemRequest.cs`.
    - [x] Step 2: Update code to replace `UpdateItemRequest` parameters/variables with `IndexRequest`.
  - [x] Task 2.2: Consolidate handlers/dispatch logic - Completed
    - [x] Step 1: Replace any “handle add” vs “handle update” handler selection with a single handler.
    - [x] Step 2: Ensure any message routing or request parsing no longer depends on operation type.
  - [x] Task 2.3: Update tests - Completed
    - [x] Step 1: Update/rename test cases that mention update.
    - [x] Step 2: Ensure coverage still asserts upsert behavior (same expected canonical output for equivalent inputs).
  - **Files** (indicative):
    - `src/UKHO.Search.Ingestion/Requests/UpdateItemRequest.cs`: remove.
    - `src/UKHO.Search.Ingestion/**`: update handlers, pipeline, providers.
    - `src/**Tests**/**`: update tests.
  - **Work Item Dependencies**:
    - Depends on Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test`
  - **User Instructions**: None.

  - **Implementation notes / summary**:
    - Removed `UpdateItemRequest` and updated `IngestionRequest.UpdateItem` to use `IndexRequest` (JSON property name remains `UpdateItem`).
    - Updated synthetic request generation and rules path validation to reflect unified payload type.
    - Updated ingestion/enrichment/rules/queue tests to use `IndexRequest` for update payloads and retained upsert semantics.
    - Verified: `dotnet build` and `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`.

---

## Feature Slice: CanonicalDocument updates (Source + Id)

- [x] Work Item 3: Update `CanonicalDocument` contract to use `IndexRequest` as `Source` and rename identifier to `Id` - Completed
  - **Purpose**: Ensure canonical documents preserve the full originating request context and adopt consistent naming (`Id`).
  - **Acceptance Criteria**:
    - `CanonicalDocument.Source` is of type `IndexRequest` (not `IReadOnlyList<IngestionProperty>`).
    - `CanonicalDocument.DocumentId` is replaced by `CanonicalDocument.Id`.
    - All code and tests compile and pass with the new API.
  - **Definition of Done**:
    - Code updated for all call sites creating/reading canonical documents
    - Tests updated to validate `Source` and `Id`
    - `dotnet build` and `dotnet test` pass
  - [x] Task 3.1: Update `CanonicalDocument` properties - Completed
    - [x] Step 1: Change `Source` property type to `IndexRequest`.
    - [x] Step 2: Rename `DocumentId` to `Id`.
    - [x] Step 3: Update any object initializers and mapping code.
  - [x] Task 3.2: Update downstream usage - Completed
    - [x] Step 1: Update any enrichers/rules/evaluators that enumerate `Source` properties to read from the unified request.
    - [x] Step 2: Ensure behaviors relying on defensive copies are updated appropriately (no unexpected mutation).
  - [x] Task 3.3: Update tests - Completed
    - [x] Step 1: Update all assertions and test data building for `CanonicalDocument.Id`.
    - [x] Step 2: Update assertions that expect `Source` to be a list.
  - **Files** (indicative):
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: update `Source` and `Id`.
    - `src/UKHO.Search.Ingestion/**`: update call sites.
    - `src/**Tests**/**`: update.
  - **Work Item Dependencies**:
    - Depends on Work Items 1 and 2.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test`
  - **User Instructions**: None.

  - **Implementation notes / summary**:
    - Updated `CanonicalDocument` to use `Id` and `Source: IndexRequest` (full originating request payload) instead of `DocumentId` and a properties list.
    - Updated `CanonicalDocumentBuilder` to populate `Source` with a shallow defensive copy of the `IndexRequest.Properties` list.
    - Updated all call sites and tests to use `Id` and `Source.Properties` / pass `IndexRequest` into `CreateMinimal`.
    - Verified: `dotnet build` and `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`.

---

## Summary / Key Considerations
- Implement sequentially to keep the solution runnable after each Work Item:
  1) rename `AddItemRequest` to `IndexRequest`,
  2) remove `UpdateItemRequest`,
  3) update `CanonicalDocument` and fix all call sites.
- Pay special attention to serialization and external message contracts if request type names are embedded in payloads.
- Expect most work to be reference updates and test refactoring; avoid behavior changes beyond consolidation.
