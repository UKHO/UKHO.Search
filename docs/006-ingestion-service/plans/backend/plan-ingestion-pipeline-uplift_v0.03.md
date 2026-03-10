# Implementation Plan (Uplift)

**Target output path:** `docs/006-ingestion-service/plans/backend/plan-ingestion-pipeline-uplift_v0.03.md`

**Based on:** `docs/006-ingestion-service/006-ingestion-service.spec.md` (v0.01 + latest clarifications on File Share provider ownership)

**Supersedes:** `docs/006-ingestion-service/plans/backend/plan-ingestion-pipeline-uplift_v0.02.md`

## Purpose
Uplift the codebase to match the updated spec guidance that the **File Share provider owns the provider-specific ingestion processing graph**, including:

- Mapping queue input → `IngestionRequest` → (Add/Update) canonical index document / `IndexOperation` payload.
- Provider-specific validation/dispatch/enrichment stages.
- A provider-level entrypoint method that takes a queue input and starts the ingestion graph (invoked by the ingestion host).

This uplift must be **explicitly refactor-driven**:

- All **existing** code that is File Share-specific (as discussed) is **MOVED** into `src/UKHO.Search.Ingestion.Providers.FileShare`.
- All affected code paths and tests are **refactored** (not duplicated) to use the moved types.

Non-goals (unchanged):
- No enrichment/business mapping beyond the structural canonical document.
- Do not change Elasticsearch index mapping.

---

## Slice 1 — Make canonical/index operation types layer-correct (so provider can own mapping)

- [x] **Work Item 1: Move canonical/index operation contracts into an inner-layer project and refactor all consumers** - Completed
  - **Purpose**: Enable File Share provider code to build canonical/indexing payloads without taking a dependency on Infrastructure projects, preserving Onion Architecture while still allowing provider ownership.
  - **Acceptance Criteria**:
    - `CanonicalDocument` and `IndexOperation` (and subtypes) live in an inner-layer project (Domain/Services) that `UKHO.Search.Ingestion.Providers.FileShare` can reference.
    - `UKHO.Search.Infrastructure.Ingestion` and all tests compile and run using the new locations.
  - **Definition of Done**:
    - Existing types are moved (not copied): file paths and namespaces updated.
    - All references updated (Infrastructure bulk index adapter, pipeline nodes, tests).
    - `dotnet build` and `dotnet test` pass.
  - [x] Task 1.1: Relocate contracts
    - [x] Step: Choose the correct target project for the contracts:
      - Prefer `src/UKHO.Search.Ingestion` (Domain) if no Services/Infrastructure dependencies are required.
      - Otherwise use an appropriate `UKHO.Search.Services.*` project.
    - [x] Step: Move:
      - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Documents/CanonicalDocument.cs`
      - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Operations/IndexOperation.cs` and its concrete operation types
    - [x] Step: Update namespaces and ensure no new invalid dependency directions are introduced.
  - [x] Task 1.2: Refactor Infrastructure and tests to the new contract locations
    - [x] Step: Update `ElasticsearchBulkIndexClient` and any other consumers.
    - [x] Step: Update pipeline graph/node code to reference the new namespaces.
    - [x] Step: Update all test projects to reference the new namespaces and/or projects.
  - **Summary**:
    - Moved canonical/index operation contracts into `src/UKHO.Search.Ingestion/Pipeline/*` (`CanonicalDocument`, `IndexOperation`, `UpsertOperation`, `DeleteOperation`, `AclUpdateOperation`).
    - Refactored Infrastructure and tests to reference `UKHO.Search.Ingestion.Pipeline.Documents/Operations`.
    - Verified: `dotnet test`.
  - **Files**:
    - (Move) `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Documents/CanonicalDocument.cs`
    - (Move) `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Operations/*.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/ElasticsearchBulkIndexClient.cs`
    - `test/UKHO.Search.Ingestion.Tests/*`
  - **Work Item Dependencies**: none
  - **Run / Verification Instructions**:
    - `dotnet test`

---

## Slice 2 — Move FileShare-specific validation/dispatch/canonical-build into the FileShare provider project

- [x] **Work Item 2: Move FileShare-specific nodes and mapping logic into `UKHO.Search.Ingestion.Providers.FileShare` and refactor pipeline to call them** - Completed
  - **Purpose**: Align implementation with the spec’s ownership model: File Share provider owns the mapping from `IngestionRequest` (Add/Update) to canonical index document.
  - **Acceptance Criteria**:
    - The following existing code is **moved** (not duplicated) into `src/UKHO.Search.Ingestion.Providers.FileShare`:
      - `IngestionRequestValidateNode`
      - `IngestionRequestDispatchNode`
      - `CanonicalDocumentBuilder` (or the equivalent canonical-build logic)
    - Infrastructure pipeline wiring uses the moved provider code.
    - Unit tests are refactored to reference the new locations.
  - **Definition of Done**:
    - Provider project compiles without referencing Infrastructure projects.
    - All existing tests covering validation/dispatch/canonical build pass.
  - [x] Task 2.1: Relocate/refactor FileShare-specific node implementations
    - [x] Step: Move the node files into a provider-owned folder, e.g. `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/Nodes/`.
    - [x] Step: Move `CanonicalDocumentBuilder` into provider project (or merge it into the provider-owned dispatch node if appropriate).
    - [x] Step: Ensure logging/metrics conventions are preserved.
  - [x] Task 2.2: Refactor the existing pipeline wiring to call provider-owned nodes
    - [x] Step: Update `IngestionPipelineBuilder` (or introduce a provider graph adapter) so the graph uses the moved nodes.
    - [x] Step: Ensure per-key ordering, microbatching, and lane semantics remain unchanged.
  - [x] Task 2.3: Refactor tests
    - [x] Step: Update `test/UKHO.Search.Ingestion.Tests/Pipeline/IngestionRequestValidateNodeTests.cs`.
    - [x] Step: Update `test/UKHO.Search.Ingestion.Tests/Pipeline/IngestionRequestDispatchNodeTests.cs`.
    - [x] Step: Add a focused test proving an `AddItemRequest` produces a canonical document payload via the provider-owned dispatch/build path.
  - **Summary**:
    - Moved `IngestionRequestValidateNode`, `IngestionRequestDispatchNode`, and `CanonicalDocumentBuilder` into `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/*`.
    - Refactored `IngestionPipelineBuilder` to use provider-owned nodes/builder (dependency direction remains inward).
    - Updated tests and added `FileShareProviderDispatchBuildPathTests`.
    - Verified: `dotnet test`.
  - **Files**:
    - (Move) `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Nodes/IngestionRequestValidateNode.cs`
    - (Move) `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Nodes/IngestionRequestDispatchNode.cs`
    - (Move) `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/Documents/CanonicalDocumentBuilder.cs`
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/IngestionPipelineBuilder.cs`
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/*`
  - **Work Item Dependencies**: Work Item 1
  - **Run / Verification Instructions**:
    - `dotnet test`

---

## Slice 3 — Provider-owned graph entrypoint (host-invoked)

- [x] **Work Item 3: Add a FileShare provider entrypoint that starts the ingestion graph and refactor host integration** - Completed
  - **Purpose**: Implement the spec requirement that the provider owns a method that takes a queue input and starts the ingestion graph.
  - **Acceptance Criteria**:
    - `UKHO.Search.Ingestion.Providers.FileShare` exposes a single entrypoint (e.g., `FileShareIngestionGraph.StartAsync(...)` or a `BuildGraph(...)` method returning a supervisor/handle).
    - `IngestionPipelineHostedService` (or a small infrastructure adapter) uses this provider entrypoint to start/stop ingestion.
    - Existing Aspire scenario remains runnable end-to-end.
  - **Definition of Done**:
    - Host starts and stops ingestion through the provider entrypoint.
    - `dotnet test` passes.
    - `dotnet run --project src/Hosts/AppHost` + FileShareEmulator indexing still results in Elasticsearch documents.
  - [x] Task 3.1: Design the provider entrypoint API
    - [x] Step: Define an entrypoint method signature that accepts queue input without violating Onion Architecture.
      - Preferred: accept inner-layer abstractions; Infrastructure provides adapters to `IQueueClient`.
    - [x] Step: Decide whether the entrypoint returns a `PipelineSupervisor`, `Task`, or a small provider-defined handle.
  - [x] Task 3.2: Implement entrypoint and infrastructure adapter
    - [x] Step: Implement provider entrypoint in `UKHO.Search.Ingestion.Providers.FileShare`.
    - [x] Step: Implement an Infrastructure adapter that supplies queue clients, bulk index clients, dead-letter sinks, and invokes the entrypoint.
  - [x] Task 3.3: Add an integration-style test
    - [x] Step: Add a test that injects fake queue input and asserts the provider entrypoint starts a graph that produces index operations.
  - **Summary**:
    - Added provider-owned graph entrypoint `FileShareIngestionGraph.BuildAzureQueueBacked(...)` returning `FileShareIngestionGraphHandle`.
    - Added Infrastructure adapter `FileShareIngestionPipelineAdapter` and refactored `IngestionPipelineHostedService` to start ingestion via the provider entrypoint.
    - Added integration-style test `FileShareIngestionGraphEntrypointTests` (with `PassthroughBulkIndexNode`) verifying the provider entrypoint produces index operations.
    - Verified: `dotnet test`.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/*`
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/IngestionPipelineHostedService.cs`
    - `test/UKHO.Search.Ingestion.Tests/*`
  - **Work Item Dependencies**: Work Items 1–2
  - **Run / Verification Instructions**:
    - `dotnet test`
    - `dotnet run --project src/Hosts/AppHost` → use FileShareEmulator `/indexing` UI to enqueue → verify in Kibana

---

## Slice 4 — Cleanup + remove legacy Infrastructure-owned FileShare logic

- [x] **Work Item 4: Remove legacy Infrastructure-owned FileShare node implementations and consolidate docs** - Completed
  - **Purpose**: Ensure there is a single source of truth for FileShare-specific pipeline logic.
  - **Acceptance Criteria**:
    - No FileShare-specific node implementations remain in `UKHO.Search.Infrastructure.Ingestion`.
    - Documentation (spec + plan + runbook) points to correct final locations.
  - **Definition of Done**:
    - All moved files are removed from Infrastructure.
    - No dead code remains.
    - `dotnet test` passes.
  - [x] Task 4.1: Remove legacy files and update references
    - [x] Step: Delete old Infrastructure node files that were moved.
    - [x] Step: Update any remaining references.
  - [x] Task 4.2: Documentation touch-ups
    - [x] Step: Update `docs/006-ingestion-service/README.md` if it references old locations.
  - **Summary**:
    - Confirmed no FileShare-specific node implementations remain in `UKHO.Search.Infrastructure.Ingestion` (nodes/builder moved in Slice 2).
    - Refactored `IngestionPipelineBuilder.BuildAzureQueueBacked(...)` to delegate to the provider-owned entrypoint via `FileShareIngestionPipelineAdapter` to avoid duplicate graph ownership.
    - Verified docs/runbook did not reference removed locations.
    - Verified: `dotnet test`.
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/*`
    - `docs/006-ingestion-service/README.md`
  - **Work Item Dependencies**: Work Items 1–3
  - **Run / Verification Instructions**:
    - `dotnet test`

---

## Summary (approach)

This uplift is a controlled refactor to align implementation with the spec’s updated ownership model:

1. First, make canonical/index operation types available at an inner layer so provider code can legally build them.
2. Move the existing FileShare-specific validation/dispatch/canonical-build logic into the FileShare provider project.
3. Introduce a provider-owned graph entrypoint and refactor host start/stop logic to invoke it.
4. Remove legacy Infrastructure-owned FileShare logic to avoid duplication.

Key considerations:
- Preserve strict per-key ordering and lane semantics throughout the refactor.
- Prefer moving/refactoring over re-implementing; tests should move with the code where appropriate.
- Keep dependency direction inward (Onion Architecture); use interfaces/adapters where a provider needs to be invoked by Infrastructure.
