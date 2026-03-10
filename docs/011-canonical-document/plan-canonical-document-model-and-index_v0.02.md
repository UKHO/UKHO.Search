# Implementation Plan

Work Package: `docs/011-canonical-document/`

Spec: `docs/011-canonical-document/spec-canonical-document-model-and-index_v0.02.md`

## Canonical document model + mapping uplift (v0.02: add `Content`)

- [x] Work Item 1: Add `Content` to `CanonicalDocument` + domain-level mutation API + tests - Completed
  - **Purpose**: Introduce a new analyzed full-text field `Content` (longer-form body text) with the same normalization + set-then-append semantics as `SearchText`, and ensure it serializes correctly.
  - **Acceptance Criteria**:
    - `CanonicalDocument.Content` exists and is serialized/deserialized via `System.Text.Json`.
    - `CanonicalDocument.SetContent(string? text)`:
      - ignores null/empty/whitespace
      - normalizes to lowercase invariant
      - sets when empty
      - appends with a single deterministic separator when non-empty
    - Existing `CanonicalDocument` behaviour remains unchanged.
  - **Definition of Done**:
    - Code implemented with repo conventions.
    - Unit tests added/updated and passing.
    - `dotnet build` and `dotnet test` pass.
    - Can execute end-to-end via: `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
  - [x] Task 1.1: Update `CanonicalDocument` - Completed
    - [x] Step 1: Add `[JsonInclude] public string Content { get; private set; } = string.Empty;`.
    - [x] Step 2: Add `SetContent(string? text)` implementing the set-then-append semantics aligned with `SetSearchText`.
    - [x] Step 3: Ensure the existing `NormalizeToken` helper is reused.
  - [x] Task 1.2: Add/extend unit tests for `Content` - Completed
    - [x] Step 1: Add new test file `test/UKHO.Search.Ingestion.Tests/Documents/CanonicalDocumentContentTests.cs`.
    - [x] Step 2: Cover:
      - initial set when empty
      - append on subsequent calls (single separator)
      - lowercase normalization
      - ignoring null/empty/whitespace
  - [x] Task 1.3: Extend JSON round-trip coverage - Completed
    - [x] Step 1: Update `test/UKHO.Search.Ingestion.Tests/Documents/CanonicalDocumentJsonRoundTripTests.cs` to assert `Content` round-trips.
  - **Files**:
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: add `Content` + `SetContent`.
    - `test/UKHO.Search.Ingestion.Tests/Documents/CanonicalDocumentContentTests.cs`: new tests.
    - `test/UKHO.Search.Ingestion.Tests/Documents/CanonicalDocumentJsonRoundTripTests.cs`: extend assertions.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet build`
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
  - **User Instructions**: None.
  - **Completed Summary**: Added `CanonicalDocument.Content` + `SetContent` with the same normalization/append semantics as `SearchText`; added `CanonicalDocumentContentTests`; extended JSON round-trip assertions.

- [x] Work Item 2: Add `content` to Infrastructure-owned Elasticsearch mappings + mapping tests - Completed
  - **Purpose**: Ensure the shared canonical index mapping includes `content` as analyzed English text (same as `searchText`).
  - **Acceptance Criteria**:
    - `CanonicalIndexDefinition` includes a `content` field mapped as `text` using an English analyzer (same approach as `searchText`).
    - Mapping tests assert presence and correct type/analyzer for `content`.
    - Bootstrap behaviour remains safe (index created with mapping only when it does not exist).
  - **Definition of Done**:
    - Mapping definition updated.
    - Mapping tests updated and passing.
    - `dotnet test` passes.
  - [x] Task 2.1: Update canonical index mapping definition - Completed
    - [x] Step 1: Update `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs` to include `content`.
    - [x] Step 2: Reuse the same analyzer configuration used for `searchText`.
  - [x] Task 2.2: Update mapping tests - Completed
    - [x] Step 1: Update `test/UKHO.Search.Ingestion.Tests/Elastic/CanonicalIndexDefinitionTests.cs` to assert `content` mapping.
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`: add `content` mapping.
    - `test/UKHO.Search.Ingestion.Tests/Elastic/CanonicalIndexDefinitionTests.cs`: extend assertions.
  - **Work Item Dependencies**:
    - Depends on Work Item 1 (field name and serialization finalised).
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **User Instructions**: None.
  - **Completed Summary**: Extended `CanonicalIndexDefinition` to map `content` as English-analyzed `text` (mirroring `searchText`); updated `CanonicalIndexDefinitionTests` to assert the new field mapping.

- [x] Work Item 3: Runnable smoke verification (local) - Completed
  - **Purpose**: Demonstrate that index creation and ingestion indexing remain runnable with the updated mapping.
  - **Acceptance Criteria**:
    - Running AppHost creates the index with the `content` mapping.
    - Ingestion indexing succeeds (no mapping errors) using existing emulator/ingestion flow.
  - **Definition of Done**:
    - Manual steps documented and repeatable.
  - [x] Task 3.1: Verify mapping contains `content` - Completed
    - [x] Step 1: Run: `dotnet run --project src/Hosts/AppHost/AppHost.csproj`.
    - [x] Step 2: Query Elasticsearch mapping (e.g., via Kibana Dev Tools or HTTP) and confirm `content` exists and is `text` with English analysis.
  - [x] Task 3.2: Index a sample document - Completed
    - [x] Step 1: Use the existing FileShareEmulator flow to enqueue/index a batch.
    - [x] Step 2: Confirm ingestion/indexing completes without errors.
  - **Files**:
    - Documentation only (this plan + updated architecture note).
  - **Work Item Dependencies**:
    - Depends on Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet run --project src/Hosts/AppHost/AppHost.csproj`
  - **User Instructions**:
    - Ensure Docker is running for Aspire/AppHost.
  - **Completed Summary**: Added repeatable manual smoke verification steps to `architecture-canonical-document-model-and-index_v0.02.md` (run AppHost, check mapping for `content`, and index a sample document via FileShareEmulator).

## Summary / key considerations
- Keep `Content` semantics aligned with `SearchText` to avoid divergent enrichment APIs.
- Ensure Infrastructure mapping remains provider-agnostic and applied only on index creation.
- Extend tests at the domain (mutation/normalization) and infra (mapping definition) layers to prevent drift.
