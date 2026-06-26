# Component Spec: Test & Regression Updates for New Taxonomy Fields

Target path: `dev/work-packages/mvp/029-new-canonical-fields/tests-and-regressions.md`

## 1. Overview

All tests and related code must be updated to account for the uplifted `CanonicalDocument` and the updated index mapping.

This spec defines the test impact areas and required updates/new tests.

## 2. Impacted Test Areas

### 2.1 Unit tests for `CanonicalDocument`

Update or add unit tests to prove:

- Each new field supports additive set behavior.
- Duplicate values are not stored.
- Stored values are deterministically ordered.
- Null/empty/whitespace string values are ignored.

### 2.2 Rules engine and pipeline tests

The workspace includes rule-engine tests under `test/UKHO.Search.Ingestion.Tests/Rules/*` which likely depend on the canonical document shape and/or serialized payload snapshots.

Update tests to:

- Include the new fields in any expected canonical document objects (where applicable).
- Update any snapshot/payload regression baselines to include taxonomy fields only when present.
- Ensure rule evaluation behavior remains unchanged unless rules explicitly set these new fields.

### 2.3 Index mapping tests

If the repository has tests verifying mapping JSON/structure, update them to:

- Assert that each new taxonomy field exists.
- Assert the configured type is `keyword`.

## 3. New/Updated Test Scenarios (Minimum)

1. **Additive behavior**
   - Add value A, then value B to the same field → both present.

2. **De-duplication**
   - Add value A twice → only one instance present.

3. **Ordering stability**
   - Add values in non-sorted order → stored/serialized order is sorted.

4. **Index payload inclusion**
   - Given a canonical document with multiple taxonomy values, the indexing payload includes arrays with all values.

5. **Mapping validation**
   - Mapping includes the fields with correct types.

## 4. Acceptance Criteria

- All existing tests in the solution pass after updating expectations and helpers.
- Added tests cover additive set semantics for each new field.
- Mapping-related tests (if present) validate the new fields.
