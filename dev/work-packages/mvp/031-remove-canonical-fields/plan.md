# Implementation Plan

Target output path: `dev/work-packages/mvp/031-remove-canonical-fields/plan.md`

## Project structure / touchpoints (expected)

This change is cross-cutting but small in surface area: remove `CanonicalDocument.DocumentType` and `CanonicalDocument.Facets` and eradicate references in:

- Domain model: `CanonicalDocument`
- Any mutator methods on `CanonicalDocument`
- Index mapping (search index field definitions and mapping code)
- Rule engine (schema / path resolver / validation)
- Tests (unit + integration)

> Note: No new components/services are introduced; this is a model/schema uplift.

## Vertical slice: “Remove unused canonical fields end-to-end”

- [x] Work Item 1: Remove `DocumentType` end-to-end (model → mapping → rules → tests) - Completed
  - **Purpose**: Deliver a runnable system without `DocumentType` anywhere (compile + tests green) while staying narrowly scoped.
  - **Acceptance Criteria**:
    - `CanonicalDocument` no longer contains `DocumentType` or related mutators.
    - Index mapping no longer defines/emits a `DocumentType` field.
    - Rule engine no longer supports `DocumentType` references; validation rejects it.
    - No test asserts on `DocumentType`; all tests pass.
  - **Definition of Done**:
    - Code implemented and compiles across solution
    - Unit/integration tests updated and passing
    - Logging/error messages (if any) remain actionable
    - Documentation updated (this work package)
    - Can execute end-to-end via: `dotnet test` (and any repo standard build/test command)
  - [x] Task 1.1: Remove `DocumentType` from `CanonicalDocument` - Completed
    - [x] Step 1: Locate `CanonicalDocument` definition (likely `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`).
    - [x] Step 2: Remove `DocumentType` property.
    - [x] Step 3: Remove any `SetDocumentType(...)` / `WithDocumentType(...)` / mutators and any internal storage.
    - [x] Step 4: Update constructors/builders/serialization attributes so compilation succeeds.
  - [x] Task 1.2: Remove `DocumentType` from index mapping - Completed
    - [x] Step 1: Locate index schema or mapping definitions that include `DocumentType`.
    - [x] Step 2: Remove field definition and any analyzers/normalization.
    - [x] Step 3: Remove mapping code that reads from `CanonicalDocument.DocumentType`.
    - [x] Step 4: Update any fixtures / sample documents used for indexing tests.
  - [x] Task 1.3: Remove `DocumentType` support from rule engine - Completed
    - [x] Step 1: Locate rule schema/path resolver list of allowed fields.
    - [x] Step 2: Remove `DocumentType` entry.
    - [x] Step 3: Remove documentType action applier/validator behaviour; treat as unknown/incorrect field (no special handling).
    - [x] Step 4: Update rule engine tests/fixtures.
  - [x] Task 1.4: Update tests and verify end-to-end - Completed
    - [x] Step 1: Update unit tests for `CanonicalDocument`.
    - [x] Step 2: Update indexing tests (schema/mapping snapshots or assertions).
    - [x] Step 3: Update rule validation/integration tests.
    - [x] Step 4: Run `dotnet test` and ensure green.
  - **Files** (to confirm during implementation):
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: remove property/mutators.
    - `src/**/Index*` / `src/**/Mapping*`: remove index field + mapping.
    - `src/**/Rules*` / `src/**/RuleEngine*`: remove schema/path + update validation.
    - `tests/**`: update fixtures and assertions.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **User Instructions**:
    - Index schema/versioning is managed externally; ensure the deployed schema aligns with the updated mapping expectations.

  - **Implementation summary**:
    - Removed `CanonicalDocument.DocumentType`.
    - Removed `documentType` field from canonical index definition + bootstrap mapping validation.
    - Removed documentType support from ingestion rules: action applier/validator and updated integration tests.
    - Updated FileShare provider S-100 handler and related tests to not set/assert `DocumentType`.
    - Deleted obsolete rules validation test `DocumentTypeSetValidationTests`.
    - Verified: `dotnet build` + `dotnet test` (UKHO.Search.Ingestion.Tests) passing.

- [x] Work Item 2: Remove `Facets` end-to-end (model → mapping → rules → tests) - Completed
  - **Purpose**: Deliver a runnable system without `Facets` anywhere (compile + tests green).
  - **Acceptance Criteria**:
    - `CanonicalDocument` no longer contains `Facets` or related mutators.
    - Index mapping no longer defines/emits a `Facets` field.
    - Rule engine no longer supports `Facets` references; validation rejects it.
    - No test asserts on `Facets`; all tests pass.
  - **Definition of Done**:
    - Code implemented and compiles across solution
    - Unit/integration tests updated and passing
    - Logging/error messages (if any) remain actionable
    - Documentation updated (this work package)
    - Can execute end-to-end via: `dotnet test`
  - [x] Task 2.1: Remove `Facets` from `CanonicalDocument` - Completed
    - [x] Step 1: Remove `Facets` property.
    - [x] Step 2: Remove any `AddFacet(...)`, `SetFacets(...)`, `WithFacets(...)` mutators.
    - [x] Step 3: Remove any normalization logic that lowercases facets (if present).
  - [x] Task 2.2: Remove `Facets` from index mapping - Completed
    - [x] Step 1: Remove `Facets` schema field(s).
    - [x] Step 2: Remove mapping code sourcing facets.
    - [x] Step 3: Update associated tests.
  - [x] Task 2.3: Remove `Facets` support from rule engine - Completed
    - [x] Step 1: Remove rule schema/path resolver entries for `Facets`.
    - [x] Step 2: Remove facets action applier/validator behaviour; treat as unknown/incorrect field (no special handling).
    - [x] Step 3: Update rule engine tests.
  - [x] Task 2.4: Update tests and verify end-to-end - Completed
    - [x] Step 1: Update any ingestion/provider tests that expect facets.
    - [x] Step 2: Run `dotnet test` and ensure green.
  - **Files** (to confirm during implementation):
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`
    - `src/**/Index*` / `src/**/Mapping*`
    - `src/**/Rules*` / `src/**/RuleEngine*`
    - `tests/**`
  - **Work Item Dependencies**:
    - Depends on Work Item 1 only in the sense of sequencing; can be done first, but doing `DocumentType` removal first typically reduces surface area.
  - **Run / Verification Instructions**:
    - `dotnet test`

  - **Implementation summary**:
    - Removed `CanonicalDocument.Facets` and facet mutator methods.
    - Removed `facets` field and `facets_as_keyword` dynamic template from canonical index definition.
    - Removed facets support from ingestion rules: action applier/validator and updated integration/end-to-end test fixtures.
    - Updated ingestion/enrichment unit tests and JSON roundtrip tests to stop using facets.
    - Updated index mapping tests to no longer assert facets are present.
    - Verified: `dotnet build` + `dotnet test` (UKHO.Search.Ingestion.Tests) passing.

- [x] Work Item 3: Remove any remaining references and harden validation - Completed
  - **Purpose**: Ensure there are no stragglers (docs, samples, UI, telemetry), and that rule validation error messages remain clear.
  - **Acceptance Criteria**:
    - No references to `DocumentType` or `Facets` remain in the repository (excluding historic docs/commit history).
    - Rule validation produces deterministic errors for unsupported fields.
  - **Definition of Done**:
    - Full repo build/test passes
    - Search and grep verification completed (without relying on `rg`)
    - Plan/spec updated if implementation discoveries require it
  - [x] Task 3.1: Repository-wide cleanup - Completed
    - [x] Step 1: Search for `DocumentType` and `Facets` usage across `src/` and `tests/`.
    - [x] Step 2: Remove/replace remaining references.
  - [x] Task 3.2: Validation ergonomics - Completed
    - [x] Step 1: Ensure errors mention the offending path and indicate it is unsupported.
    - [x] Step 2: Add/update tests for these messages.
  - **Work Item Dependencies**: Work Items 1 and 2.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test`

  - **Implementation summary**:
    - Cleaned up remaining references in repository docs to removed `facets.add` and `documentType.set` actions.
    - Updated ingestion pipeline design docs to remove `documentType` and `facets` from example canonical document shape.
    - Verified: `dotnet build` and `dotnet test` (UKHO.Search.Ingestion.Tests) passing.

---

## Open questions

1. For the rule engine: should references to `DocumentType`/`Facets` be rejected at **ruleset validation time** only, or also guarded at runtime evaluation (belt-and-braces)?
   - Answer: Neither — treat them the same as any other unknown/incorrect field; no special treatment needed.

2. Are there any **user-facing** flows (e.g., Blazor UI filters/facets panels) that still depend on `Facets`, even if backend ingestion no longer sets it?
   - Answer: 2 (No / not applicable)

## Summary

Implement this uplift in two small end-to-end slices (remove `DocumentType`, then remove `Facets`), followed by a cleanup/hardening pass. Each slice keeps the solution runnable and test-green by updating the model, index mapping, rule engine validation, and tests together.
