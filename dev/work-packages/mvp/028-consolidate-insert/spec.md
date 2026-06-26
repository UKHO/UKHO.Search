# Specification: Consolidate Insert (Add/Update) into Single Index Request

Target path: `dev/work-packages/mvp/028-consolidate-insert/spec.md`

## 1. Overview

### 1.1 Purpose
This change simplifies ingestion by removing the distinction between *adding* new items and *updating* existing items. A single request contract will represent “index this document” regardless of its prior existence.

### 1.2 Goals
- Replace the current add/update split in ingestion requests with a single request type.
- Ensure ingestion pipeline and document model use the unified request type.
- Update tests to reflect the new, simplified model.

### 1.3 Non-goals
- Changing indexing semantics beyond the request/model consolidation.
- Changing search schema, analyzers, or query behavior.
- Introducing new ingestion providers or altering provider wiring.

### 1.4 Background / Problem Statement
The ingestion domain currently distinguishes between `AddItemRequest` and `UpdateItemRequest`. Downstream components largely treat both similarly (they describe a document and its content/metadata to be indexed). Maintaining two parallel request types creates:
- duplicate code and branching logic,
- additional test surface,
- confusion about when to use add vs update in practice.

The platform will move to a single, idempotent request representing “index this document”, allowing the indexing layer to upsert as needed.

### 1.5 Key Design Decision
- A new request type named `IndexRequest` will replace `AddItemRequest`.
- `UpdateItemRequest` will be removed.
- `CanonicalDocument.Source` will change from a list of `IngestionProperty` to the full `IndexRequest` that produced the canonical document.
- `CanonicalDocument.DocumentId` will be renamed to `Id`.

## 2. Functional Requirements

### 2.1 Unified Ingestion Request
- The system shall support a single ingestion request for indexing documents named `IndexRequest`.
- The system shall treat `IndexRequest` as an *upsert* instruction (index regardless of whether the document already exists).

### 2.2 Removal of Update Concept
- The system shall remove `UpdateItemRequest` from the ingestion domain.
- Any pipeline nodes, provider components, or adapters that previously branched on add vs update shall be simplified to accept and process `IndexRequest` only.

### 2.3 Canonical Document Source
- `CanonicalDocument.Source` shall store the originating `IndexRequest` instead of a property list.
- The canonical document shall retain access to the original request data needed for enrichment and indexing output generation.

### 2.4 Canonical Document Identifier
- `CanonicalDocument` shall expose the document identifier as `Id`.
- `DocumentId` shall be removed.

### 2.5 Test Coverage
- All unit/integration tests referencing add/update request types shall be updated to use `IndexRequest`.
- Tests shall validate:
  - the unified request is correctly parsed/constructed,
  - the pipeline still produces expected canonical documents,
  - `CanonicalDocument.Source` provides the expected request context,
  - `CanonicalDocument.Id` is used consistently.

## 3. Technical Requirements

### 3.1 API / Contract Changes

#### 3.1.1 Rename
- Rename `AddItemRequest` to `IndexRequest`.
  - File name should be renamed accordingly.
  - Namespace should remain consistent with existing ingestion request types.

#### 3.1.2 Remove
- Remove `UpdateItemRequest`.
  - Delete the type and any references.
  - Replace usage patterns with `IndexRequest`.

#### 3.1.3 Request Shape
- `IndexRequest` should preserve the fields necessary for both prior add and update operations.
- No additive fields are required by this change unless a gap is discovered during refactoring.

### 3.2 Model Changes: `CanonicalDocument`

#### 3.2.1 `Source` type change
- Change `CanonicalDocument.Source` from:
  - `IReadOnlyList<IngestionProperty>`
  - to `IndexRequest`

Notes:
- Callers that previously supplied a property list must now supply the `IndexRequest` instance.
- Any logic that relied on `Source` being enumerable must be adjusted to read properties from the request.

#### 3.2.2 `DocumentId` rename
- Change:
  - `public string DocumentId { get; init; } = string.Empty;`
  - to:
  - `public string Id { get; init; } = string.Empty;`

Notes:
- Update any JSON (de)serialization attributes/usages if present.
- Update any mapping code that uses `DocumentId`.

### 3.3 Compatibility and Migration Considerations
- This is a breaking change for any consumers referencing the old request types or `CanonicalDocument.DocumentId`.
- If there are external integrations (e.g., message contracts sent over queues), assess whether message payload shape and type names are part of a public contract.
  - If type names are serialized into messages, introduce a migration strategy (e.g., versioned message envelope). If messages only serialize fields, renaming the CLR type may not affect runtime payloads.

### 3.4 Implementation Notes (Constraints / Repo Standards)
- Maintain Onion Architecture dependency direction.
- Keep one public type per file.
- Use block-scoped namespaces and Allman braces.
- Update tests in place; do not change behavior beyond the consolidation.

## 4. Acceptance Criteria
- `AddItemRequest` no longer exists; `IndexRequest` exists and is used throughout ingestion.
- `UpdateItemRequest` no longer exists; no code references it.
- `CanonicalDocument.Source` is of type `IndexRequest` and is populated wherever canonical documents are created.
- `CanonicalDocument.DocumentId` no longer exists; `CanonicalDocument.Id` is used consistently.
- All existing tests pass after updates.

## 5. Out of Scope / Risks / Open Questions

### 5.1 Risks
- Breaking changes in serialized message payloads if type names are embedded.
- Hidden code paths branching on add vs update semantics.

### 5.2 Open Question
1. Should `IndexRequest` be treated as fully idempotent (upsert) at the index writer, or do any existing components still require explicit “add fail if exists” semantics?
