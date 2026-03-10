# Implementation Plan

**Target output path:** `docs/015-canonical-document-uplift/plan-ingestion-canonical-document-uplift_v0.01.md`

**Based on:** `docs/015-canonical-document-uplift/spec-canonical-document-uplift_v0.01.md`

**Version:** v0.01 (Draft)

---

## Slice 1 — Canonical document model uplift (Source + Timestamp)

- [x] Work Item 1: Uplift `CanonicalDocument` and canonical document builder to stamp `Source` + `Timestamp` - Completed
  - **Purpose**: Make the canonical document store active Add/Update ingestion properties for traceability and add a first-class timestamp derived from the request payload.
  - **Acceptance Criteria**:
    - `CanonicalDocument.Source` is `IReadOnlyList<IngestionProperty>`.
    - `CanonicalDocument.Timestamp` exists and is a `DateTimeOffset`.
    - For Add/Update, the canonical builder stamps:
      - `Source` as a **defensive shallow copy** of `AddItem.Properties` or `UpdateItem.Properties`.
      - `Timestamp` from `AddItem.Timestamp` / `UpdateItem.Timestamp`.
    - Canonical document JSON round-trip tests validate `Source` array and `Timestamp` value.
  - **Definition of Done**:
    - Code implemented (domain model + builder)
    - Unit tests updated/added and passing
    - No new warnings introduced
    - Documentation remains consistent with spec
    - Can execute end-to-end via: `dotnet test`
  - [x] Task 1.1: Update canonical document domain model - Completed
    - [x] Step 1: Change `CanonicalDocument.Source` type to `IReadOnlyList<IngestionProperty>`.
    - [x] Step 2: Add `CanonicalDocument.Timestamp` as `DateTimeOffset`.
    - [x] Step 3: Ensure construction helpers (if any) accept `Source` + `Timestamp`.
  - [x] Task 1.2: Update provider canonical builder for Add/Update - Completed
    - [x] Step 1: Update the FileShare provider canonical document builder to select active payload (`AddItem` else `UpdateItem`).
    - [x] Step 2: Implement defensive **shallow copy** of the selected payload’s `Properties` into `CanonicalDocument.Source`.
    - [x] Step 3: Stamp `CanonicalDocument.Timestamp` from the selected payload’s `Timestamp`.
  - [x] Task 1.3: Update and extend tests for canonical document uplift - Completed
    - [x] Step 1: Update all existing tests that construct `CanonicalDocument` to supply `Source` + `Timestamp`.
    - [x] Step 2: Add/extend a JSON round-trip test asserting:
      - `Source` round-trips as an array of ingestion properties
      - `Timestamp` round-trips as the same value
    - [x] Step 3: Update dispatch/builder tests to assert:
      - AddItem uses `AddItem.Properties` + `AddItem.Timestamp`
      - UpdateItem uses `UpdateItem.Properties` + `UpdateItem.Timestamp`
  - **Files**:
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: change `Source` type; add `Timestamp`.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/Documents/CanonicalDocumentBuilder.cs`: stamp `Source` + `Timestamp` (defensive shallow copy).
    - `test/UKHO.Search.Ingestion.Tests/Documents/*`: update canonical document tests.
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/IngestionRequestDispatchNodeTests.cs`: update assertions for `Source`/`Timestamp`.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Updated `CanonicalDocument` to store `Source` as `IReadOnlyList<IngestionProperty>` and added `Timestamp`.
    - Updated FileShare `CanonicalDocumentBuilder` to select active payload (AddItem preferred) and stamp defensive shallow copy + timestamp.
    - Updated/extended unit tests, including JSON round-trip and dispatch coverage for AddItem + UpdateItem; `dotnet test` passes.

---

## Slice 2 — FileShare baseline enrichment (`BasicEnricher`)

- [x] Work Item 2: Add FileShare `BasicEnricher` (ordinal 10) to copy properties into keywords + facets - Completed
  - **Purpose**: Provide deterministic baseline enrichment so downstream enrichers and rules can rely on ingestion properties being present in canonical keywords/facets.
  - **Acceptance Criteria**:
    - `BasicEnricher` exists in the FileShare provider and implements `IIngestionEnricher`.
    - `BasicEnricher.Ordinal == 10`.
    - For Add/Update, `BasicEnricher`:
      - Adds every ingestion property value (string / string representation) to canonical keywords.
      - Adds every ingestion property name/value pair to canonical facets.
      - If a value is an array, each element is treated as a separate keyword and facet value.
      - Ignores null/empty/whitespace values.
    - `BasicEnricher` is registered in DI alongside existing FileShare enrichers.
    - Full unit test coverage exists for AddItem + UpdateItem + array flattening + dedupe.
  - **Definition of Done**:
    - Code implemented (enricher + DI)
    - New/updated unit tests passing
    - Enricher participates in runtime ordering via ordinal
    - Can execute end-to-end via: `dotnet test`
  - [x] Task 2.1: Implement `BasicEnricher` - Completed
    - [x] Step 1: Create `BasicEnricher` under the FileShare provider enrichment folder.
    - [x] Step 2: Implement Add/Update payload selection and `Properties` iteration.
    - [x] Step 3: For each property:
      - [x] Step 3.1: Add keyword(s) from value(s).
      - [x] Step 3.2: Add facet value(s) under facet name = property name.
    - [x] Step 4: Ensure behavior is non-throwing for missing/empty property lists.
  - [x] Task 2.2: Register `BasicEnricher` in FileShare provider DI - Completed
    - [x] Step 1: Add DI registration so it is resolved as `IEnumerable<IIngestionEnricher>`.
  - [x] Task 2.3: Add/extend tests for `BasicEnricher` - Completed
    - [x] Step 1: Add unit tests that cover:
      - ordinal is 10
      - AddItem copies values into keywords and facets
      - UpdateItem copies values into keywords and facets
      - string-array values flatten to multiple keywords/facet values
      - null/whitespace values ignored
      - dedupe via canonical normalization
    - [x] Step 2: Ensure tests reflect that values are expected strings (NVARCHAR-origin) and only use arrays where needed for coverage.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/BasicEnricher.cs`: new enricher.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Injection/InjectionExtensions.cs`: DI registration.
    - `test/UKHO.Search.Ingestion.Tests/Enrichment/BasicEnricherTests.cs`: new tests.
  - **Work Item Dependencies**: Work Item 1 (canonical model changes must exist).
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Added FileShare `BasicEnricher` (ordinal 10) to copy active Add/Update ingestion property values into canonical `Keywords` and name/value pairs into `Facets`, including array flattening and canonical dedupe.
    - Registered `BasicEnricher` in FileShare provider DI.
    - Added unit tests covering AddItem/UpdateItem, string-array flattening, null/whitespace ignore, dedupe, and deterministic non-string conversion; `dotnet test` passes.

---

## Slice 3 — Search index mapping compatibility

- [x] Work Item 3: Validate/update canonical index mapping for new `Timestamp` and updated `Source` - Completed
  - **Purpose**: Ensure indexing remains compatible with the canonical document shape and that new fields do not cause unexpected dynamic mapping issues.
  - **Acceptance Criteria**:
    - Index mapping remains compatible with the new `Source` type.
    - If `Timestamp` must be queryable/sortable, mapping explicitly defines it as a date field.
    - Existing mapping choice to keep `source` “not indexed” remains unchanged.
  - **Definition of Done**:
    - Mapping changes (if required) implemented
    - Any mapping-related unit tests updated (if present)
    - `dotnet test` passes
  - [x] Task 3.1: Review canonical index definition - Completed
    - [x] Step 1: Confirm how property name casing is handled by the Elasticsearch client serialization.
    - [x] Step 2: Add mapping for timestamp field if required by consumers.
    - [x] Step 3: Confirm `source` remains disabled/not indexed.
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`: mapping updates as required.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **Completed Summary**:
    - Updated canonical index mapping to explicitly map `timestamp` as a date field.
    - Verified `source` remains disabled/not indexed to preserve existing trace-only behavior.

---

## Slice 4 — Ingestion rules DSL impact check + regression coverage

- [x] Work Item 4: Verify ingestion rules DSL parsing/evaluation unaffected and update tests/docs if required - Completed
  - **Purpose**: Ensure the canonical document uplift does not accidentally change or break rule evaluation semantics and that documentation remains correct.
  - **Acceptance Criteria**:
    - Rule predicates are still evaluated against the active Add/Update payload.
    - DSL path parsing/resolution tests pass.
    - A regression test exists verifying payload selection:
      - AddItem preferred when present
      - UpdateItem used when AddItem absent
    - `docs/ingestion-rules.md` is reviewed and only updated if necessary to correct misleading content (do not add BasicEnricher details proactively).
  - **Definition of Done**:
    - Tests pass
    - Any required doc corrections applied
    - `dotnet test` passes
  - [x] Task 4.1: Test review + regression additions - Completed
    - [x] Step 1: Run existing ingestion rules DSL tests and update expectations if they reference canonical document `Source` shape.
    - [x] Step 2: Add regression test for payload selection if absent.
  - [x] Task 4.2: Documentation review - Completed
    - [x] Step 1: Review `docs/ingestion-rules.md` for references to `CanonicalDocument.Source` or indexing of `source`.
    - [x] Step 2: Update only if needed to prevent misleading guidance.
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/Evaluation/*`: only if required.
    - `test/UKHO.Search.Ingestion.Tests/*`: regression test additions.
    - `docs/ingestion-rules.md`: only if required.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **Completed Summary**:
    - Added regression coverage asserting rules evaluate against the active payload (AddItem preferred when present; UpdateItem used when AddItem absent).
    - No rules DSL logic changes were required; `dotnet test` passes.
    - Note: `docs/ingestion-rules.md` is not present in this repository workspace, so no documentation update was applied.

---

## Summary / key considerations

- Implement in small, safe steps: uplift canonical model + builder first, then add baseline enrichment, then verify mapping + rules DSL.
- Preserve existing normalization behavior by relying on `CanonicalDocument` APIs for keywords/facets.
- Treat ingestion property values as strings (NVARCHAR-origin) and flatten arrays defensively.
- Keep documentation changes minimal and corrective only (especially for `docs/ingestion-rules.md`).
