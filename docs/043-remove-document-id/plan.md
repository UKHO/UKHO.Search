# Implementation Plan

Target output path: `docs/043-remove-document-id/plan.md`

## Project structure / touchpoints (expected)

This work package is intentionally narrow in scope. The expected implementation touchpoints are:

- Elasticsearch canonical index mapping
- Elasticsearch startup / readiness validation
- Index mapping tests
- Focused runtime validation test coverage
- Work package documentation only (no automated dev index cleanup)

Primary files expected to change:

- `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`
- `src/UKHO.Search.Infrastructure.Ingestion/Elastic/ElasticsearchBulkIndexClient.cs`
- `test/UKHO.Search.Ingestion.Tests/Elastic/CanonicalIndexDefinitionTests.cs`
- `test/UKHO.Search.Ingestion.Tests/Elastic/*`
- `docs/043-remove-document-id/spec.md`
- `docs/043-remove-document-id/plan.md`

> Note: This work item must preserve `CanonicalDocument.Id` and Elasticsearch `_id` behavior. It only removes the obsolete mapped `documentId` field from the canonical index contract.

## Vertical slice: “Remove obsolete `documentId` from canonical indexing contract”

- [x] Work Item 1: Remove `documentId` from mapping, validation, and tests end-to-end - Completed
  - **Purpose**: Deliver a runnable ingestion/indexing path where canonical documents continue to index correctly using Elasticsearch `_id`, while the obsolete mapped `documentId` field is fully removed from mapping and validation.
  - **Acceptance Criteria**:
    - `CanonicalIndexDefinition` no longer declares a mapped `documentId` field.
    - `ElasticsearchBulkIndexClient.ValidateIndexMappingAsync(...)` no longer requires `documentId` to exist.
    - `CanonicalDocument.Id` remains unchanged.
    - Elasticsearch bulk indexing continues to use the document key as Elasticsearch `_id`.
    - Mapping tests assert that `documentId` is absent.
    - A focused automated test covers runtime validation behavior without requiring `documentId`.
    - Existing dev index cleanup remains a manual step and is not automated by this work item.
  - **Definition of Done**:
    - Code implemented in mapping and validation layers only
    - Unit/integration-style tests updated and passing
    - No unrelated canonical fields or ingestion behaviors changed
    - Documentation updated in this work package
    - Can execute end-to-end via: `dotnet test`

  - [x] Task 1.1: Remove `documentId` from canonical index definition - Completed
    - [x] Step 1: Update `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs` to remove `.Keyword("documentId")`.
    - [x] Step 2: Review remaining field mappings to ensure no other field definitions are changed unintentionally.
    - [x] Step 3: Confirm the canonical mapping still reflects the intended searchable document shape.

  - [x] Task 1.2: Remove runtime dependency on `documentId` - Completed
    - [x] Step 1: Update `src/UKHO.Search.Infrastructure.Ingestion/Elastic/ElasticsearchBulkIndexClient.cs` to remove the `EnsureHasType(fields, "documentId", "keyword")` requirement.
    - [x] Step 2: Verify the remaining required-field checks still match the explicit mapping.
    - [x] Step 3: Confirm there is no other startup or bootstrap validation path that still assumes `documentId` exists.

  - [x] Task 1.3: Preserve document identity behavior - Completed
    - [x] Step 1: Confirm `CanonicalDocument.Id` remains unchanged in `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`.
    - [x] Step 2: Confirm bulk indexing continues to set Elasticsearch `_id` from `envelope.Key`.
    - [x] Step 3: Ensure no code path attempts to reintroduce `documentId` as an indexed `_source` field.

  - [x] Task 1.4: Add regression coverage for mapping output - Completed
    - [x] Step 1: Update `test/UKHO.Search.Ingestion.Tests/Elastic/CanonicalIndexDefinitionTests.cs` to assert expected fields still exist.
    - [x] Step 2: Add an explicit assertion that `documentId` is absent from the generated mapping JSON.
    - [x] Step 3: Keep test intent focused on mapping contract regression rather than unrelated serialization behavior.

  - [x] Task 1.5: Add focused runtime validation test coverage - Completed
    - [x] Step 1: Identify the best existing test seam for `ElasticsearchBulkIndexClient` mapping validation behavior.
    - [x] Step 2: Add or update a test showing index validation succeeds without `documentId` being present.
    - [x] Step 3: Keep the test narrowly scoped to the runtime mapping validation contract.

  - [x] Task 1.6: Verify runnable end-to-end outcome - Completed
    - [x] Step 1: Run targeted ingestion/indexing tests first.
    - [x] Step 2: Run the relevant test project(s) to confirm all updated tests pass.
    - [x] Step 3: Record any manual dev note that existing indexes may need recreation outside the work item.

  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`: remove explicit `documentId` mapping.
    - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/ElasticsearchBulkIndexClient.cs`: remove required `documentId` field validation.
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: no functional change expected; confirm identity behavior remains intact.
    - `test/UKHO.Search.Ingestion.Tests/Elastic/CanonicalIndexDefinitionTests.cs`: assert `documentId` is absent while expected fields remain present.
    - `test/UKHO.Search.Ingestion.Tests/Elastic/*`: add/update focused runtime validation coverage.
    - `docs/043-remove-document-id/spec.md`: source specification.
    - `docs/043-remove-document-id/plan.md`: execution plan.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **User Instructions**:
    - If a local dev Elasticsearch index already exists with the old mapping, recreate it manually after the code change.
    - No automated index cleanup is included in this work item.

  - **Implementation summary**:
    - Removed the explicit `documentId` field from `CanonicalIndexDefinition` while leaving the remaining canonical mapping intact.
    - Removed the runtime requirement for `documentId` from `ElasticsearchBulkIndexClient` by extracting the expected field validation into an internal helper and validating only the still-supported mapped fields.
    - Aligned `BootstrapService` index mapping validation with `CanonicalIndexDefinition` by reusing the shared expected-field validation and removing the obsolete `documentId` requirement there as well.
    - Preserved document identity behavior: `CanonicalDocument.Id` was not changed and bulk indexing still uses `envelope.Key` as Elasticsearch `_id`.
    - Updated `CanonicalIndexDefinitionTests` to assert `documentId` is absent and added `ElasticsearchBulkIndexClientMappingValidationTests` to verify runtime field validation succeeds without `documentId`.
    - Verification: `dotnet build` succeeded; targeted Elastic tests passed. Running the full `UKHO.Search.Ingestion.Tests` project exposed 17 pre-existing unrelated rules/bootstrap failures caused by missing `IConfiguration` wiring.

## Optional follow-up slice (only if implementation reveals hidden coupling)

- [x] Work Item 2: Repository-wide cleanup of stray `documentId` assumptions - Completed
  - **Purpose**: Provide a controlled fallback slice if implementation uncovers additional non-documentation references beyond the known mapping and validation touchpoints.
  - **Acceptance Criteria**:
    - Any newly discovered code/test references to the mapped `documentId` field are removed or updated.
    - No runtime path still expects `documentId` as an indexed field.
  - **Definition of Done**:
    - Additional references addressed
    - Tests remain green
    - Documentation updated if scope expands
  - [x] Task 2.1: Search for remaining code/test references - Completed
    - [x] Step 1: Search `src/` and `test/` for `documentId` usage related to the index contract.
    - [x] Step 2: Exclude historical docs unless they are part of active implementation guidance that must stay current.
  - [x] Task 2.2: Remove or align remaining references - Completed
    - [x] Step 1: Update any discovered tests, fixtures, or helper code.
    - [x] Step 2: Re-run affected tests.
  - **Files**:
    - `src/**`
    - `test/**`
  - **Work Item Dependencies**: Depends on Work Item 1 and should only be used if new implementation evidence expands scope.
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **User Instructions**:
    - None beyond standard dev index recreation if required.

  - **Implementation summary**:
    - Searched active `src/` and `test/` files for exact `documentId` usage related to the index contract.
    - Confirmed there are no remaining exact `documentId` references in active implementation or test code; the previously surfaced matches were unrelated `Id` usages or historical documentation.
    - No additional code or test changes were required for this follow-up slice.
    - Verification: `dotnet build` succeeded and the focused Elastic regression tests passed.

---

## Summary

Implement this work as a single small vertical slice focused on the canonical Elasticsearch contract: remove `documentId` from the explicit mapping, remove the corresponding runtime validation requirement, preserve existing Elasticsearch `_id` behavior, and add regression coverage for both mapping output and validation. Keep the change narrowly scoped, leave dev index recreation manual, and avoid unrelated canonical document refactoring.
