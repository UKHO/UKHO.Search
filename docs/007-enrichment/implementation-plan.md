# Implementation Plan

Work Package folder: `docs/007-enrichment/`

This plan implements `docs/007-enrichment/spec-enrichment-complete_v0.01.md` using vertical slices. Each Work Item ends in a runnable ingestion pipeline (queue -> dispatch -> (enrichment) -> bulk index -> ack/dead-letter) with tests.

## Overall project structure / placement
- Domain contract + context payload live in `src/UKHO.Search.Ingestion/` (provider-agnostic).
- Core enrichment pipeline node lives in `src/UKHO.Search.Ingestion/` (so any provider can reuse it).
- File share provider enrichers + provider DI live in `src/UKHO.Search.Ingestion.Providers.FileShare/`.
- Central ingestion DI entrypoint remains `src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs` and calls provider registration.

---

## Core Enrichment Slice (Contract + Context + Node wired into FileShare graph)

- [x] Work Item 1: Introduce `IIngestionEnricher`, `IngestionPipelineContext`, and a core enrichment node; wire into file share ingestion graph (no-op behaviour) - Completed
  - **Purpose**: Establish a provider-agnostic enrichment contract and a runnable pipeline stage that sits between dispatch and bulk indexing without changing runtime output.
  - **Acceptance Criteria**:
    - `IIngestionEnricher` exists in `UKHO.Search.Ingestion` with `Ordinal` and `TryBuildEnrichmentAsync(IngestionRequest, CanonicalDocument, CancellationToken)`.
    - `IngestionPipelineContext` exists in namespace `UKHO.Search.Ingestion.Pipeline` and carries `Request` + `Operation`.
    - File share ingestion graph emits `Envelope<IngestionPipelineContext>` from dispatch, applies enrichment, and then continues bulk indexing with `IndexOperation`.
    - Enrichment ordering is deterministic (Ordinal asc, then type name asc) even when enrichers are no-op.
    - Cancellation is honored by the enrichment node and enricher calls.
  - **Definition of Done**:
    - Code implemented (contracts, node, provider graph wiring)
    - Unit tests passing
    - Logging & error handling added (use `ILogger`)
    - Documentation updated (this plan tracked; code docs only if needed)
    - Can execute end-to-end via: start ingestion host + send a queue message (e.g., via FileShareEmulator) and observe successful indexing/acking
  - [x] Task 1: Replace existing file-share-specific enrichment contract with core contract - Completed
    - [x] Step 1: Create `src/UKHO.Search.Ingestion/IIngestionEnricher.cs`.
    - [x] Step 2: Remove/retire `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/IFileShareIngestionEnricher.cs` usage by migrating existing references (keep file temporarily if needed for a single-PR transition, but aim to delete by Work Item 3).
  - [x] Task 2: Add core context payload - Completed
    - [x] Step 1: Create `src/UKHO.Search.Ingestion/Pipeline/IngestionPipelineContext.cs` with exactly one public type in the file.
    - [x] Step 2: Ensure it carries `IngestionRequest Request` and `IndexOperation Operation`.
  - [x] Task 3: Add core enrichment pipeline node - Completed
    - [x] Step 1: Create a node (e.g., `EnrichmentNode` / `ApplyEnrichmentNode`) that reads `Envelope<IngestionPipelineContext>` and outputs `Envelope<IndexOperation>`.
    - [x] Step 2: Execute all registered `IIngestionEnricher` for `UpsertOperation` only (mutating `CanonicalDocument`).
    - [x] Step 3: Sort enrichers by `Ordinal`, then `Type.FullName` for deterministic ties.
    - [x] Step 4: On non-recovered error, mark envelope failed with `PipelineErrorCategory.Transform` and route to index-operation dead-letter output.
    - [x] Step 5: Add `ILogger` logs at debug/info for enrichment start/finish and warning for failures.
  - [x] Task 4: Wire node into FileShare provider graph - Completed
    - [x] Step 1: Update `IngestionRequestDispatchNode` to output `Envelope<IngestionPipelineContext>` instead of `Envelope<IndexOperation>`.
    - [x] Step 2: Update `FileShareIngestionGraph` lane wiring to insert enrichment node between dispatch and microbatch.
    - [x] Step 3: Ensure enrichment failures route to existing index dead-letter lane sink (not request dead-letter).
    - [x] Step 4: Preserve envelope metadata (key, messageId, attempt, context/breadcrumbs) when mapping between payload types.
  - [x] Task 5: Add minimal tests proving the pipeline slice is runnable - Completed
    - [x] Step 1: Add unit tests in `test/UKHO.Search.Ingestion.Tests/` for:
      - deterministic ordering with two fake enrichers
      - upsert vs non-upsert behaviour (enrichment called only for upserts)
      - envelope metadata preserved when mapping to/from `IngestionPipelineContext`
  - **Summary**:
    - Added provider-agnostic enrichment contract (`IIngestionEnricher`) and context payload (`IngestionPipelineContext`) in `UKHO.Search.Ingestion`.
    - Implemented core enrichment node (`ApplyEnrichmentNode`) with deterministic ordering and transform-failure routing to index dead-letter.
    - Updated file share graph to emit `Envelope<IngestionPipelineContext>` from dispatch and apply enrichment before micro-batching.
    - Updated/added unit tests for ordering, upsert-only execution, and envelope metadata preservation.
  - **Files**:
    - `src/UKHO.Search.Ingestion/IIngestionEnricher.cs`: new provider-agnostic enricher contract.
    - `src/UKHO.Search.Ingestion/Pipeline/IngestionPipelineContext.cs`: new context payload.
    - `src/UKHO.Search.Ingestion/Pipeline/Nodes/<EnrichmentNode>.cs`: new enrichment node.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/Nodes/IngestionRequestDispatchNode.cs`: change output payload to `IngestionPipelineContext`.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/FileShareIngestionGraph.cs`: insert enrichment node and adjust channels.
    - `test/UKHO.Search.Ingestion.Tests/*`: new/updated tests for ordering + payload mapping.
  - **Work Item Dependencies**: none.
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
    - Manual (optional): run `dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj` and enqueue a message via the FileShareEmulator UI/tooling.
  - **User Instructions**: none.

---

## Resilience Slice (Retry + Transient classification + Configuration)

- [x] Work Item 2: Add configurable retry policy for transient enricher failures - Completed
  - **Purpose**: Make enrichment resilient to transient failures without letting permanently failing items reach bulk indexing.
  - **Acceptance Criteria**:
    - Retry uses exponential backoff: 5 retries (6 attempts total), base 200ms, max 5000ms, jitter 250ms.
    - Configuration keys live under `ingestion:` and default to spec values if missing.
    - Transient whitelist implemented exactly as spec.
    - `TaskCanceledException` is treated as transient only when not caused by the provided cancellation token.
    - On retry exhaustion, envelope is marked failed with `Transform` error and routed to index dead-letter.
  - **Definition of Done**:
    - Code implemented + unit tests passing
    - Logging includes attempt number, delay, and exception type
    - Documentation updated: configuration keys added to `configuration/configuration.json`
    - Can execute end-to-end via: run ingestion host with enrichers throwing transient exceptions and verify retries then success / dead-letter on exhaustion
  - [x] Task 1: Implement transient exception classification helper - Completed
    - [x] Step 1: Add a helper method (private/internal) in the enrichment node that classifies exceptions using the whitelist:
      - `TimeoutException`, `HttpRequestException`, `IOException`
      - `TaskCanceledException` only when `!cancellationToken.IsCancellationRequested`
      - Never transient: `OperationCanceledException` when `cancellationToken.IsCancellationRequested`
  - [x] Task 2: Add retry loop to enrichment node - Completed
    - [x] Step 1: Wrap each enricher invocation in retry handling.
    - [x] Step 2: Use exponential backoff and apply jitter (ms-based, consistent with existing indexing retry settings).
    - [x] Step 3: Respect `CancellationToken` during delay and invocation.
  - [x] Task 3: Add configuration plumbing - Completed
    - [x] Step 1: Update `configuration/configuration.json` for each environment block (`local`, `dev`, `iat`, `live`, etc.) adding:
      - `enrichmentRetryMaxAttempts`
      - `enrichmentRetryBaseDelayMilliseconds`
      - `enrichmentRetryMaxDelayMilliseconds`
      - `enrichmentRetryJitterMilliseconds`
    - [x] Step 2: Ensure ingestion host reads these settings and passes them into the enrichment node construction (via DI or graph factory dependencies).
  - [x] Task 4: Unit tests for retry behaviour - Completed
    - [x] Step 1: Add tests verifying:
      - retries occur only for whitelisted exceptions
      - `TaskCanceledException` is transient only when not caused by provided cancellation token
      - retry exhaustion routes to index dead-letter
      - no retry occurs for non-transient exceptions
  - **Summary**:
    - Added transient exception classification and retry loop (exponential backoff + jitter) to `ApplyEnrichmentNode`.
    - Added `ingestion:enrichmentRetry*` keys to `configuration/configuration.json` for all environments.
    - Updated pipeline construction to pass retry settings into `ApplyEnrichmentNode`.
    - Added unit tests covering transient retry, non-transient no-retry, exhaustion dead-lettering, and `TaskCanceledException` behaviour.
  - **Files**:
    - `src/UKHO.Search.Ingestion/Pipeline/Nodes/<EnrichmentNode>.cs`: add retry loop + transient classification.
    - `configuration/configuration.json`: add `enrichmentRetry*` keys in each environment.
    - `src/UKHO.Search.Infrastructure.Ingestion/Pipeline/FileShareIngestionPipelineAdapter.cs` (or provider graph dependencies): pass retry settings into graph/node creation.
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/*`: new tests for retry behaviour.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test test/UKHO.Search.Ingestion.Tests/UKHO.Search.Ingestion.Tests.csproj`
  - **User Instructions**:
    - Ensure local configuration includes the new `ingestion:enrichmentRetry*` keys (defaults should work if omitted).

---

## Provider Slice (FileShare provider DI + initial enrichers)

- [x] Work Item 3: Add file share provider DI registration and initial no-op enrichers - Completed
  - **Purpose**: Make enrichment provider-extensible and keep Infrastructure as the single ingestion DI entrypoint while allowing provider-owned registrations.
  - **Acceptance Criteria**:
    - Provider project contains `UKHO.Search.Ingestion.Providers.FileShare.Injection/InjectionExtensions` with `AddFileShareProvider(IServiceCollection)`.
    - `AddFileShareProvider` registers:
      - `IIngestionDataProviderFactory` -> `FileShareIngestionDataProviderFactory` (queue name from config)
      - `IIngestionEnricher` multi-registrations for the three enrichers
    - Enrichers are registered `Scoped`.
    - Central `AddIngestionServices` calls `services.AddFileShareProvider()`.
    - Existing pipeline remains runnable.
  - **Definition of Done**:
    - Code implemented + tests passing
    - Old `IFileShareIngestionEnricher` removed (or left only as internal/unused if a hard delete is safe)
    - Can execute end-to-end via: ingestion host starts, provider graph builds, and messages index successfully
  - [x] Task 1: Create provider DI extension - Completed
    - [x] Step 1: Add folder/namespace `src/UKHO.Search.Ingestion.Providers.FileShare/Injection`.
    - [x] Step 2: Add `InjectionExtensions` class with `AddFileShareProvider(this IServiceCollection services)`.
  - [x] Task 2: Implement initial enrichers (no-op) - Completed
    - [x] Step 1: Add `FileContentEnricher` (`Ordinal = 100`).
    - [x] Step 2: Add `ExchangeSetEnricher` (`Ordinal = 200`).
    - [x] Step 3: Add `GeoLocationEnricher` (`Ordinal = 300`).
    - [x] Step 4: Ensure each enricher is no-op but correctly accepts `(IngestionRequest, CanonicalDocument, CancellationToken)`.
  - [x] Task 3: Update central ingestion DI wiring - Completed
    - [x] Step 1: Update `src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs`:
      - remove direct `IIngestionDataProviderFactory` and enricher registrations
      - call `services.AddFileShareProvider()`
  - **Summary**:
    - Added provider DI entrypoint `AddFileShareProvider()` in `UKHO.Search.Ingestion.Providers.FileShare`.
    - Registered `FileShareIngestionDataProviderFactory` and the initial no-op enrichers as `Scoped` `IIngestionEnricher` implementations.
    - Updated central ingestion DI wiring to call `AddFileShareProvider()`.
    - Removed legacy `IFileShareIngestionEnricher` and `NoOpFileShareIngestionEnricher`.
  - **Files**:
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Injection/InjectionExtensions.cs`: provider DI entrypoint.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/FileContentEnricher.cs`: new no-op enricher.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/ExchangeSetEnricher.cs`: new no-op enricher.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Enrichment/GeoLocationEnricher.cs`: new no-op enricher.
    - `src/UKHO.Search.Infrastructure.Ingestion/Injection/InjectionExtensions.cs`: call provider DI.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/IFileShareIngestionEnricher.cs`: delete if fully migrated.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/NoOpFileShareIngestionEnricher.cs`: delete/replace with new enrichers.
  - **Work Item Dependencies**: Work Item 1 (and 2 if retry settings are passed via DI).
  - **Run / Verification Instructions**:
    - `dotnet test`
    - Manual: `dotnet run --project src/Hosts/IngestionServiceHost/IngestionServiceHost.csproj`
  - **User Instructions**:
    - None beyond existing ingestion configuration.

---

## Hardening Slice (Full test coverage for ordering, error handling, and dead-letter routing)

- [x] Work Item 4: Expand unit/integration tests for enrichment ordering, failure routing, and metadata preservation - Completed
  - **Purpose**: Lock in spec behaviour and prevent regressions.
  - **Acceptance Criteria**:
    - Unit tests cover ordering, tie-break determinism, and selective execution for upserts.
    - Unit tests cover retry behaviour exhaustively.
    - Unit tests cover error handling: transform error + index dead-letter routing; bulk indexing never sees failed items.
    - Tests verify envelope metadata preservation across payload transforms.
  - **Definition of Done**:
    - All new tests pass in CI
    - All code paths are exercised (success, transient failure -> retry -> success, failure -> dead-letter)
    - Can execute end-to-end via: `dotnet test` + optional local manual run
  - [x] Task 1: Add deterministic ordering tests - Completed
    - [x] Step 1: Two enrichers with same `Ordinal` must execute in `Type.FullName` order.
  - [x] Task 2: Add failure routing tests - Completed
    - [x] Step 1: Non-transient exceptions route to index dead-letter and are not microbatched.
    - [x] Step 2: After exhaustion of transient retries, route to index dead-letter.
  - [x] Task 3: Add context payload tests - Completed
    - [x] Step 1: Validate that `IngestionRequest` and `IndexOperation` are both available to enrichers.
  - **Summary**:
    - Added hardening tests for deterministic ordering and context payload mutation in `ApplyEnrichmentNode`.
    - Added graph-level tests ensuring enrichment failures (non-transient and transient exhaustion) route to the index dead-letter lane and do not reach bulk indexing/microbatching.
  - **Files**:
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/ApplyEnrichmentNodeTests.cs`: added context payload test.
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/FileShareIngestionGraphEnrichmentFailureRoutingTests.cs`: added graph failure routing tests.
    - `test/UKHO.Search.Ingestion.Tests/TestNodes/RecordingBulkIndexNode.cs`: added bulk index recording test node.
    - `test/UKHO.Search.Ingestion.Tests/TestEnrichers/RequestEchoEnricher.cs`: added test enricher to validate context access.
    - `test/UKHO.Search.Ingestion.Tests/TestEnrichers/AlwaysThrowingEnricher.cs`: added test enricher for failure routing.
  - **Files**:
    - `test/UKHO.Search.Ingestion.Tests/*`: new tests and test doubles.
  - **Work Item Dependencies**: Work Items 1–3.
  - **Run / Verification Instructions**:
    - `dotnet test`
  - **User Instructions**: none.

---

## Summary
This implementation introduces a provider-agnostic enrichment contract and context payload in `UKHO.Search.Ingestion`, adds a core enrichment node with deterministic ordering and resilient retry behaviour, and moves file-share-specific registrations and enrichers into the provider project via `AddFileShareProvider`. The file share ingestion graph is updated to emit `IngestionPipelineContext` so enrichers can see both the original request and the pre-index `CanonicalDocument`.
