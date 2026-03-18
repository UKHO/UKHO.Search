# Specification: Remove unused Elasticsearch `documentId` field

Target output path: `docs/043-remove-document-id/spec.md`

## 1. Overview

### 1.1 Purpose

This work package removes the unused `documentId` field from the Elasticsearch canonical index contract.

The field is no longer part of the intended searchable document shape, but it still appears in the explicit index mapping and in runtime mapping validation. This creates a mismatch between the current product intent and the deployed index schema.

### 1.2 Scope

This work package covers:
- Removing `documentId` from the explicit Elasticsearch index mapping.
- Removing any runtime index validation that still requires `documentId` to exist.
- Updating or adding tests so the solution no longer expects `documentId` to be present in the canonical index mapping.
- Preserving the existing Elasticsearch document `_id` behavior for indexed documents.

Out of scope:
- Renaming `CanonicalDocument.Id`.
- Changing how bulk index operations set the Elasticsearch document `_id`.
- Refactoring unrelated canonical fields or ingestion behavior.

### 1.3 Stakeholders

- Ingestion pipeline maintainers.
- Elasticsearch/indexing maintainers.
- QA and test maintainers.

### 1.4 Definitions

- `documentId`: a mapped Elasticsearch field currently declared in the canonical index mapping.
- Elasticsearch `_id`: the metadata identifier used by Elasticsearch for each stored document.
- `CanonicalDocument.Id`: the application-level identifier currently used to populate Elasticsearch `_id` during bulk indexing.

## 2. System context

### 2.1 Current state

Current evidence in the codebase shows that `documentId` still remains in the index contract:

- `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`
  - Explicitly adds `.Keyword("documentId")` to the canonical index mapping.
- `src/UKHO.Search.Infrastructure.Ingestion/Elastic/ElasticsearchBulkIndexClient.cs`
  - Runtime mapping validation still calls `EnsureHasType(fields, "documentId", "keyword")`.
- `test/UKHO.Search.Ingestion.Tests/Elastic/CanonicalIndexDefinitionTests.cs`
  - Verifies the canonical mapping shape but does not currently assert that `documentId` is absent.

Related evidence also shows that document identity is still needed, but as Elasticsearch `_id`, not as a mapped field:

- `src/UKHO.Search.Infrastructure.Ingestion/Elastic/ElasticsearchBulkIndexClient.cs`
  - Bulk indexing sets `Id = envelope.Key` on `BulkIndexOperation<CanonicalDocument>`.
- `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`
  - `CanonicalDocument` still contains `Id`, which remains valid as the domain/application identifier.

### 2.2 Proposed state

After this change:
- The canonical index mapping no longer declares a `documentId` field.
- Runtime mapping validation no longer expects `documentId` to exist.
- Tests explicitly protect against accidental reintroduction of `documentId` into the mapping.
- Document identity continues to be carried through Elasticsearch `_id` only.

### 2.3 Assumptions

- `documentId` is confirmed as obsolete and should not be indexed as a searchable/stored canonical field.
- The retained `CanonicalDocument.Id` property is still required for ingestion and for setting Elasticsearch `_id`.
- No downstream query, dashboard, or contract depends on `documentId` being present in indexed `_source`.

### 2.4 Constraints

- Elasticsearch mappings cannot remove an existing field from an already-created index in place; rollout may require index recreation or reindexing.
- The change must be minimal and must not affect unrelated canonical field mappings.

## 3. Component / service design (high level)

### 3.1 Components

Components in scope:
- `CanonicalIndexDefinition`
- `ElasticsearchBulkIndexClient`
- Canonical index mapping tests

Components explicitly not being removed/changed in behavior:
- `CanonicalDocument.Id`
- Bulk indexing document key behavior (`_id`)

### 3.2 Data flows

Current flow:
1. A `CanonicalDocument` is produced by ingestion.
2. Bulk indexing sends the document to Elasticsearch using the envelope key as Elasticsearch `_id`.
3. Index creation and validation still expect a mapped `documentId` field even though the field is no longer intended for use.

Target flow:
1. A `CanonicalDocument` is produced by ingestion.
2. Bulk indexing continues to send the document using Elasticsearch `_id`.
3. Index creation and validation only enforce the fields that are still part of the canonical searchable document.

### 3.3 Key decisions

- `documentId` SHALL be removed from the canonical Elasticsearch mapping.
- `CanonicalDocument.Id` SHALL remain unless a separate work package explicitly removes or redefines it.
- Elasticsearch `_id` behavior SHALL remain unchanged.
- Tests SHALL validate absence of `documentId` in the mapping contract.

## 4. Functional requirements

### FR1 — Remove mapped `documentId`
The system SHALL no longer create the canonical Elasticsearch index with a mapped `documentId` field.

### FR2 — Preserve document identity semantics
The system SHALL continue to index documents with the correct Elasticsearch `_id` value.

### FR3 — Remove runtime dependency on `documentId`
The system SHALL no longer fail index readiness or mapping validation because `documentId` is absent.

### FR4 — Prevent regression
The test suite SHALL detect any future reintroduction of `documentId` into the canonical mapping contract.

## 5. Non-functional requirements

- The change should be minimal and localized to indexing contract code and its tests.
- Existing behavior for other canonical mapped fields must remain unchanged.
- The implementation should remain aligned with the repository’s current ingestion and Elasticsearch conventions.

## 6. Data model

### 6.1 Canonical indexed document

The canonical indexed document model SHALL no longer include `documentId` as a mapped field in Elasticsearch.

### 6.2 Identity model

The distinction between identity forms must remain clear:
- `CanonicalDocument.Id` is an application/domain identifier.
- Elasticsearch `_id` is the persisted document identifier in the index.
- `documentId` as an indexed `_source` field is obsolete and SHALL be removed.

## 7. Interfaces & integration

### 7.1 Elasticsearch mapping interface

- `CanonicalIndexDefinition.Configure(...)` must stop declaring `documentId`.

### 7.2 Elasticsearch readiness validation

- `ElasticsearchBulkIndexClient.ValidateIndexMappingAsync(...)` must stop requiring `documentId` as an expected mapped field.

### 7.3 Backward compatibility / integration impact

- Existing indexes in the current dev environment may continue to contain the legacy `documentId` mapping until manually recreated.
- New or recreated indexes from the updated code must not include `documentId`.
- Because this is brand new, dev-only code, no versioned index migration or alias cutover is required for this work package.

## 8. Observability (logging/metrics/tracing)

- No new logging or metrics are required.
- Existing indexing and startup diagnostics should continue to report mapping mismatches accurately, excluding `documentId` from required-field checks.

## 9. Security & compliance

- No security model changes are expected.
- No new secrets, permissions, or data classifications are introduced.

## 10. Testing strategy

### 10.1 Unit tests

Update indexing tests so they validate the new contract:
- `test/UKHO.Search.Ingestion.Tests/Elastic/CanonicalIndexDefinitionTests.cs`
  - Assert that expected fields still exist.
  - Assert that `documentId` is absent from the generated mapping JSON.

### 10.2 Additional coverage

Add a focused automated test covering index validation behavior so runtime field checks no longer require `documentId`.

### 10.3 Regression expectations

- All existing indexing tests should pass after removing `documentId`.
- No tests should assert or rely on `documentId` as a canonical mapped field.

## 11. Rollout / migration

### 11.1 Code rollout

The code change is straightforward and is expected to be rolled out only in the current dev environment.

### 11.2 Index migration consideration

Because Elasticsearch does not support removing an existing field mapping in place:
- Existing dev indexes may need to be deleted and recreated manually.
- This work package does not require a versioned migration path, alias cutover, or production rollout strategy.

### 11.3 Operational validation

After deployment or recreation of the target index:
- Confirm field capabilities no longer report `documentId`.
- Confirm indexing still works and document `_id` values remain correct.

### 11.4 Dev environment cleanup

- Manual recreation of any existing dev index is outside the scope of this work item.
- This work item is limited to code and automated test changes.

## 12. Open questions

No open questions remain for this work package.

Resolved decisions:
- No external consumers, dashboards, saved queries, or downstream integrations depend on `documentId`.
- The change will be rolled out by direct index recreation only in the dev environment.
- Automated coverage will include both mapping JSON verification and a focused test for runtime mapping validation.
- Cleanup of any pre-existing dev index is a manual step outside this work item.
