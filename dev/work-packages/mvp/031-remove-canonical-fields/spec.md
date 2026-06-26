# Specification: Remove unused `CanonicalDocument` fields (`DocumentType`, `Facets`)

Target output path: `dev/work-packages/mvp/031-remove-canonical-fields/spec.md`

## 1. Overview

### 1.1 Purpose

This work package uplifts the `CanonicalDocument` model by removing unused fields and associated mutators and ensuring the rest of the system (index mappings, rule engine, and tests) no longer depends on them.

### 1.2 Goals

- Remove the `DocumentType` property and any associated mutator methods from `CanonicalDocument`.
- Remove the `Facets` property and any associated mutator methods from `CanonicalDocument`.
- Remove all usages of these fields across ingestion, indexing, querying, and any UI components.
- Remove these fields from index mapping and any search document serialization/deserialization.
- Remove these fields from the rules DSL / rule engine and any rule authoring or evaluation paths.
- Update all unit/integration/e2e tests to reflect the new model.

### 1.3 Non-goals

- Introducing new fields to replace `DocumentType` or `Facets`.
- Refactoring unrelated `CanonicalDocument` fields or pipelines.
- Changing the underlying search platform beyond what is necessary to remove these fields.

### 1.4 Assumptions

- `DocumentType` and `Facets` are confirmed as no longer used by product requirements.
- Any external dependencies (e.g., downstream consumers of indexed documents) can tolerate removal (or a compatible migration strategy exists).

### 1.5 Dependencies

- Search index schema/mapping definitions.
- Rule engine schema and evaluation logic.
- Any ingestion providers that currently populate these properties.
- Tests that validate these properties.

### 1.6 Stakeholders

- Search ingestion and pipeline maintainers.
- Index/search query maintainers.
- Rule engine maintainers.
- QA / test maintainers.

### 1.7 Risks and considerations

- **Breaking change risk**: removing fields may break downstream assumptions, saved queries, or rule sets.
- **Index compatibility**: index schema changes may require reindexing or index versioning.

### 1.8 Technical decisions to confirm

- Whether index schema changes require creating a new index version vs. in-place update.
- Whether rule definitions referencing removed fields should:
  1) Fail validation (recommended), or
  2) Be ignored at runtime.

## 2. System / Component scope

### 2.1 Components in scope

- `CanonicalDocument` domain model (and related mutators).
- Rule engine schema/DSL and evaluation.
- Search indexing model and mappings.
- Any serialization contracts crossing boundaries where these fields appear.
- Tests covering ingestion, mapping, and rule behavior.

### 2.2 Components out of scope

- Any new features unrelated to removing `DocumentType` and `Facets`.

### 2.3 High-level change summary

- Remove two model fields (`DocumentType`, `Facets`) end-to-end from:
  - Domain model
  - Rules
  - Search index schema
  - Mapping code
  - Tests

## 3. Functional requirements (high-level)

### 3.1 Model behavior

- The system SHALL no longer expose `DocumentType` on `CanonicalDocument`.
- The system SHALL no longer expose `Facets` on `CanonicalDocument`.
- The system SHALL continue to ingest, enrich, and index documents without requiring these fields.

### 3.2 Indexing behavior

- Indexed documents SHALL no longer include fields for `DocumentType` or `Facets`.
- Any mapping code SHALL not attempt to read or write these fields.

### 3.3 Rule engine behavior

- The rule engine SHALL not support referencing `DocumentType` or `Facets` in rule conditions or actions.
- Rule schema validation SHALL reject rules that reference removed fields (decision to confirm).

## 4. Technical requirements

### 4.1 Code changes (expected)

- `CanonicalDocument`:
  - Remove property `DocumentType`.
  - Remove property `Facets`.
  - Remove any `SetDocumentType(...)`, `SetFacets(...)`, or similar mutator methods.

- Index mapping:
  - Remove the corresponding mapping fields.
  - Remove any field normalization/transform logic for these fields.

- Rule engine:
  - Remove schema elements or path resolvers for these fields.
  - Update validation to treat these as invalid/unrecognized.

- Tests:
  - Update/replace tests that assert on `DocumentType` and/or `Facets`.
  - Ensure coverage remains for:
    - Successful ingestion and indexing
    - Rule validation failures for unknown fields (if applicable)

### 4.2 Search index migration

- Define approach:
  1) Create new index version without fields and reindex; or
  2) Modify existing index mapping and reindex.

- Confirm whether existing documents in index require cleanup.

### 4.3 Observability

- Ensure logs/telemetry do not assume these fields exist.
- If rule validation errors occur due to removed fields, error messages SHOULD be actionable.

## 5. Acceptance criteria

1. `CanonicalDocument` compiles with `DocumentType` and `Facets` removed.
2. No rule definitions or evaluation code references `DocumentType` or `Facets`.
3. No index mapping/serialization includes `DocumentType` or `Facets`.
4. All tests in the solution pass.
5. Any rules/configurations attempting to reference removed fields fail in a documented and consistent manner.

## 6. Validation / Test plan

- Run full test suite.
- Add/update tests for rule validation behavior when referencing removed fields.
- Validate indexed documents do not contain removed fields.

## 7. Open questions (to resolve before implementation)

1. Are there any externally published contracts (API/search result DTOs) that currently expose `DocumentType` or `Facets`?
2. Do we need an index versioning strategy, or can we update in place?

