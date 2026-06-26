# Work Package: Uplift `CanonicalDocument` with Universal Discovery Taxonomy Fields

Target path: `dev/work-packages/mvp/029-new-canonical-fields/overview.md`

## 1. Overview

This work package uplifts the ingestion domain model `CanonicalDocument` and the search index mapping to support a set of new multi-valued, set-based fields intended to represent a "universal discovery taxonomy".

The uplift includes:
- Adding new taxonomy fields to the canonical document model.
- Ensuring set semantics for these fields (additive; no replacement of existing values).
- Updating index mapping to store and search these fields efficiently.
- Updating all related code paths and tests that create, mutate, serialize, or index `CanonicalDocument`.

This document is the overview specification for the work package and references the component-level specification document(s) below.

### 1.1 In-scope components

- `CanonicalDocument` domain model
- Ingestion pipeline nodes/components that populate or transform canonical fields
- Serialization/deserialization and indexing payload generation
- Elasticsearch/OpenSearch index mapping and any schema/mapping deployment assets
- Unit/integration/end-to-end tests covering rules evaluation, payload generation, and mapping correctness

### 1.2 Out of scope

- Any changes to UI/Blazor components (unless they directly depend on the canonical fields)
- Changes to external upstream data sources unless required to populate the new fields

## 2. High-level system context

The ingestion pipeline produces a canonical representation (`CanonicalDocument`) that is transformed into an indexing payload and submitted to the search index. The backend search experience depends on the index mapping being aligned with the canonical fields that are emitted.

The new taxonomy fields must behave similarly to existing set-based fields (notably `Keywords`) to ensure consistent additive updates, deterministic ordering for stable payloads/tests, and suitable mapping types for exact-match/aggregation use cases.

## 3. Components

### 3.1 `CanonicalDocument` Taxonomy Field Uplift

Reference: `dev/work-packages/mvp/029-new-canonical-fields/canonical-document-taxonomy-fields.md`

This component defines:
- The new fields to be introduced.
- Their set semantics and ordering.
- Expected indexing representation.

### 3.2 Index Mapping Update

Reference: `dev/work-packages/mvp/029-new-canonical-fields/index-mapping-update.md`

This component defines:
- Changes required to the Elasticsearch/OpenSearch mapping.
- Expected field types and compatibility considerations.

### 3.3 Test & Regression Updates

Reference: `dev/work-packages/mvp/029-new-canonical-fields/tests-and-regressions.md`

This component defines:
- Required updates to existing tests.
- New tests to prove additive multi-value behavior and mapping alignment.
