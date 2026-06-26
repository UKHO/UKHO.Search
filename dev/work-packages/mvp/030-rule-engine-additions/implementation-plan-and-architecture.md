# Implementation Plan

Target path: `dev/work-packages/mvp/030-rule-engine-additions/implementation-plan-and-architecture.md`

## Rules Engine DSL: Additional top-level `then` fields

- [x] Work Item 1: End-to-end rules enrichment using the new top-level `then` fields - Completed
  - **Purpose**: Deliver a runnable ingestion rules pipeline where a ruleset containing the new top-level `then` fields loads at startup and enriches a `CanonicalDocument` during ingestion.
  - **Acceptance Criteria**:
    - Rulesets containing any of the new `then` fields (`authority`, `region`, `fornat`, `category`, `series`, `instance`, `majorVersion`, `minorVersion`) load successfully.
    - When a rule matches, the corresponding `CanonicalDocument` fields are enriched additively and deterministically.
    - Existing actions (`keywords`, `searchText`, `content`, `facets`, `documentType`) remain unchanged.
    - No nested `then.taxonomy` shape exists in model binding or application.
  - **Definition of Done**:
    - Code implemented (DTO binding, validation, applier updates)
    - Tests passing (unit + integration)
    - Logging & error handling aligned with existing patterns
    - Documentation remains consistent with the implemented DSL
    - Can execute end-to-end via:
      - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
      - (Optional) `dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`
  - [x] Task 1: Reshape rules model binding for `then` and remove taxonomy-named DTOs - Completed
    - [x] Step 1: Inspect `ThenDto` and how `keywords`, `searchText`, etc. are represented/bound.
    - [x] Step 2: Introduce neutral action DTOs (e.g., `StringAddActionDto`, `IntAddActionDto`) or equivalent, consistent with existing `*.add` patterns.
    - [x] Step 3: Update `ThenDto` to include the new fields as top-level properties using the neutral action DTOs.
    - [x] Step 4: Remove/rename taxonomy-named DTOs (`TaxonomyStringActionDto`, `TaxonomyIntActionDto`) and update all references.
    - [x] Step 5: Ensure JSON property names align exactly to the DSL (`authority`, `region`, `fornat`, `category`, `series`, `instance`, `majorVersion`, `minorVersion`).
  - [x] Task 2: Implement action application for each new top-level field - Completed
    - [x] Step 1: Update `IngestionRulesActionApplier` to apply `authority.add`, `region.add`, etc.
    - [x] Step 2: For string fields, route through existing template expansion + string normalization (matching `keywords.add`).
    - [x] Step 3: For numeric fields, accept JSON numbers and add to the corresponding `CanonicalDocument` sets.
    - [x] Step 4: Extend `ActionApplySummary` so these actions are captured similarly to existing actions.
  - [x] Task 3: Startup validation updates for the new fields (shape/type) - Completed
    - [x] Step 1: Identify the existing ruleset validation entry point.
    - [x] Step 2: Add validation rules for each new field:
      - must be an object with `add`
      - `add` must be an array
      - element type must be string for string fields, number for numeric fields
      - disallow `set`
    - [x] Step 3: Ensure unknown `then` fields behavior (fail-fast vs ignore) remains unchanged.
  - [x] Task 4: Add end-to-end integration tests for enrichment - Completed
    - [x] Step 1: Add a ruleset JSON sample that uses at least one string field (e.g., `region.add` with `$path:`) and one numeric field (e.g., `majorVersion.add`).
    - [x] Step 2: Execute rules engine against a minimal `IngestionRequest` and assert the enriched `CanonicalDocument` fields.
    - [x] Step 3: Add coverage for multiple rules in file order to prove additive behavior.
  - [x] Task 5: Add regression tests for determinism and de-duplication - Completed
    - [x] Step 1: Add tests where multiple rules add duplicate values.
    - [x] Step 2: Assert de-duplication and deterministic ordering in canonical and payload outputs.
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Model/ThenDto.cs`: add top-level fields.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Model/StringAddActionDto.cs`: new neutral DTO (if required).
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Model/IntAddActionDto.cs`: new neutral DTO (if required).
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Model/TaxonomyStringActionDto.cs`: remove or rename.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Model/TaxonomyIntActionDto.cs`: remove or rename.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Actions/IngestionRulesActionApplier.cs`: apply actions.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Actions/ActionApplySummary.cs`: include action results.
    - `test/UKHO.Search.Ingestion.Tests/Rules/RulesEngineSlice4ActionsIntegrationTests.cs`: extend action integration coverage.
    - `test/UKHO.Search.Ingestion.Tests/Rules/RulesEngineEndToEndExampleTests.cs`: add/extend example.

  - **Completed Summary**:
    - Removed taxonomy terminology from rules model/action applier by renaming DTOs and counters:
      - `TaxonomyStringActionDto` -> `StringAddActionDto` (same JSON shape, `add: string[]`)
      - `TaxonomyIntActionDto` -> `IntAddActionDto` and changed `Add` to `int[]` to match numeric JSON inputs
      - `ActionApplySummary.TaxonomyValuesAdded` -> `AdditionalFieldValuesAdded`
      - `IngestionRulesActionApplier.ApplyTaxonomy` -> `ApplyAdditionalFields` and updated numeric handling to consume `int[]` without templating
    - Updated integration test to use JSON numbers for numeric fields and renamed the test class away from taxonomy naming.
    - Verified with: `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj -c Release`.

- [x] Work Item 2: Documentation update (confirm DSL and naming) - Completed
  - **Purpose**: Ensure external rule authors have correct guidance that matches the implemented DSL.
  - **Acceptance Criteria**:
    - `wiki/Ingestion-Rules.md` documents the new fields as top-level `then` actions.
    - No documentation refers to “taxonomy fields” or uses a nested `taxonomy` block.
  - **Definition of Done**:
    - Documentation updated
    - Can verify doc examples by running relevant rules engine tests
  - [x] Task 1: Update `wiki/Ingestion-Rules.md` - Completed
    - [x] Step 1: Ensure the action list + examples show only top-level fields.
    - [x] Step 2: Ensure terminology is “top-level fields” / “additional fields”, not taxonomy.
  - **Files**:
    - `wiki/Ingestion-Rules.md`: update actions section/examples.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`

  - **Completed Summary**:
    - Updated `wiki/Ingestion-Rules.md` to document the new fields as top-level `then` actions and removed taxonomy terminology.
    - Clarified that numeric fields require JSON numbers and do not support templating.
    - Verified with: `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj -c Release`.

- [x] Work Item 3: Index payload regression coverage for the new fields - Completed
  - **Purpose**: Ensure enrichments for the new fields flow through to the indexing payload deterministically.
  - **Acceptance Criteria**:
    - When rules run, emitted index item payload includes the new fields.
    - Regression snapshots updated/added and stable.
  - **Definition of Done**:
    - Regression tests updated/added
    - All tests passing
    - Can execute end-to-end via: `dotnet test`
  - [x] Task 1: Extend payload regression suite - Completed
    - [x] Step 1: Update `RulesEngineIndexItemPayloadRegressionTests` to include a scenario where rules set the new fields.
    - [x] Step 2: Assert serialized payload matches expected JSON (ordering/normalization).
  - [x] Task 2: Validate mapping expectations - Completed
    - [x] Step 1: Ensure emitted JSON types match mapping expectations (string arrays for string fields; numeric arrays for version fields).
  - **Files**:
    - `test/UKHO.Search.Ingestion.Tests/Rules/RulesEngineIndexItemPayloadRegressionTests.cs`: extend/add snapshot.
    - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`: only if mapping alignment changes are required.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`

  - **Completed Summary**:
    - Added regression coverage ensuring rules populate the new fields on the enriched `CanonicalDocument` used as the indexing payload source.
    - Updated: `test/UKHO.Search.Ingestion.Tests/Rules/RulesEngineIndexItemPayloadRegressionTests.cs`.
    - Verified with: `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj -c Release`.

---

# Architecture

Target path: `dev/work-packages/mvp/030-rule-engine-additions/implementation-plan-and-architecture.md`

## Overall Technical Approach

- Extend the ingestion rules engine `then` DTO to support additional **top-level** action blocks corresponding to the new `CanonicalDocument` fields.
- Remove/rename taxonomy-oriented DTO names and any taxonomy-oriented rules DSL concepts.
- Preserve fail-fast startup validation for invalid JSON shapes/types.
- Reuse existing string-action infrastructure for template expansion + normalization.
- Apply actions via the existing action applier so end-to-end enrichment behavior is preserved.

```mermaid
flowchart LR
  RulesJson[ingestion-rules.json] --> Catalog[Rules Catalog + Validation]
  Catalog --> Engine[Rules Engine]
  Request[IngestionRequest\n(AddItem/UpdateItem)] --> Engine
  Engine --> Applier[Action Applier]
  Applier --> Canonical[CanonicalDocument\n(+ new fields)]
  Canonical --> Payload[Index Item Payload]
```

## Frontend

- Not applicable. This work package affects the ingestion rules engine and does not introduce or change any Blazor UI.

## Backend

- Rules DSL binding lives under `src/UKHO.Search.Infrastructure.Ingestion/Rules/Model/*`.
- Validation occurs during ruleset loading at startup.
- Predicates are evaluated against the active `IngestionRequest` payload.
- Actions are applied by `IngestionRulesActionApplier`, enriching `CanonicalDocument` using additive set semantics.
- Regression coverage resides in `test/UKHO.Search.Ingestion.Tests` (action integration + payload regression suites).
