# Specification: Provider-rooted Ingestion Startup Refactor (Uplift)

Version: v0.01  
Status: Draft  
Work Package: `docs/008-provider-refactor/`  

## Change Log
- v0.01: Initial uplift spec to refactor ingestion startup so Infrastructure owns queue lifecycle and polling, while each ingestion provider becomes the root entrypoint for processing requests from its queue.

---

## 1. Purpose
Ensure ingestion startup follows the repository Onion Architecture and provider ownership boundaries:
- **Infrastructure** is responsible for queue lifecycle (create, poll, poison queue handling) using `IIngestionDataProviderFactory.QueueName`.
- **Providers** are responsible for configuring and running their ingestion processing pipeline (validate/dispatch/enrich/batch/index/ack/dead-letter), and expose an explicit entrypoint method for processing a single `IngestionRequest`.

This uplift removes the current coupling where Infrastructure constructs a provider-specific end-to-end pipeline adapter, and instead makes the provider the root entrypoint for request processing.

---

## 2. Scope
### 2.1 In scope
- Introduce or adapt a provider contract so Infrastructure can call `ProcessIngestionRequest` on the provider instance created by `IIngestionDataProviderFactory.CreateProvider()`.
- Refactor queue polling so Infrastructure reads from each provider queue (via `QueueName`) and invokes provider processing.
- Refactor the FileShare provider so its processing pipeline is configured from its provider entrypoint (the pipeline graph currently expressed in `FileShareIngestionGraph`).
- Ensure ack/dead-letter routing remains correct (successful items acked; failures dead-lettered; poison queue handling remains Infrastructure-owned).
- Update DI registration and startup so the ingestion host starts ingestion generically for all registered providers.

### 2.2 Out of scope
- Adding new ingestion providers.
- Changing the message schema on queues.
- Changing the indexing contract (`IndexOperation`, bulk indexing semantics) beyond what is required to re-host the pipeline.
- New enrichment logic (beyond existing no-op enrichers).

---

## 3. Current state (problem statement)
- Infrastructure currently owns a provider-specific adapter (`FileShareIngestionPipelineAdapter`) in `UKHO.Search.Infrastructure.Ingestion`.
- The ingestion pipeline composition is not aligned with the desired ownership model:
  - Infrastructure should start and manage queue polling based on `IIngestionDataProviderFactory.QueueName`.
  - Providers should be the entrypoint for processing requests taken from their queue.
- The FileShare provider already contains the processing graph (`FileShareIngestionGraph`), but the startup / adapter boundary needs to change so:
  - Infrastructure reads requests from the queue.
  - Infrastructure calls `ProcessIngestionRequest` on the provider.
  - The provider-owned pipeline handles dispatch/enrichment/indexing/ack/dead-letter.

---

## 4. Target design
### 4.1 Roles and responsibilities
#### Infrastructure (queue host)
Infrastructure is responsible for:
- Enumerating providers via `IIngestionProviderService.GetAllProviders()`.
- For each provider factory:
  - Reading `QueueName` and ensuring the queue and poison queue exist.
  - Polling the queue, applying poison rules (max dequeue count), renewing visibility, and creating a message acker.
  - Deserializing message text to `IngestionRequest` via the provider.
  - Calling the provider entrypoint `ProcessIngestionRequest`.

Infrastructure must remain the only layer managing Azure Storage queue clients.

#### Provider (request processing root)
Each provider is responsible for:
- Defining the provider-specific ingestion pipeline composition.
- Owning the request-processing entrypoint called by Infrastructure.
- Producing the correct operational outcomes:
  - Successful requests eventually lead to ack (delete the queue message).
  - Failures route to the appropriate dead-letter path.

### 4.2 Provider entrypoint contract
A provider instance created by `IIngestionDataProviderFactory.CreateProvider()` must expose:
- `DeserializeIngestionRequestAsync(string messageText, CancellationToken ct)` (already exists).
- `ProcessIngestionRequestAsync(Envelope<IngestionRequest> envelope, CancellationToken ct)`.

Processing semantics:
- `ProcessIngestionRequestAsync(...)` must return as soon as the request is accepted/enqueued into the provider’s internal pipeline.
- Backpressure is allowed: if the provider cannot accept new work (e.g., bounded channels are full), the method may await until it can enqueue or until `CancellationToken` is cancelled.
- The method must not await the full downstream completion (bulk index + ack/dead-letter) as part of its contract.

This can be achieved by either:
1) Extending `IIngestionDataProvider` with a new method:
   - `ValueTask ProcessIngestionRequestAsync(Envelope<IngestionRequest> envelope, CancellationToken cancellationToken = default);`

or
2) Introducing a new interface (preferred if separation is required):
   - `IIngestionRequestProcessor` with `ProcessIngestionRequestAsync(...)`, implemented by the provider returned from the factory.

Decision criteria:
- If all providers are expected to both deserialize and process, extending `IIngestionDataProvider` is simplest.
- If some providers may share deserialization but vary processing strategy, a separate interface keeps responsibilities explicit.

### 4.3 Provider-owned processing pipeline
The provider processing entrypoint must configure and own the pipeline that was previously built “around” the queue source.

Refactor approach:
- The provider creates and starts its processing graph once and keeps it long-lived for the lifetime of the provider instance.
- `ProcessIngestionRequestAsync(...)` becomes the ingress point into the graph.

Lifecycle notes:
- The provider should start the graph during provider initialization/startup and dispose/stop it during host shutdown.
- The graph should be able to drain in-flight work on shutdown (consistent with existing pipeline cancellation modes).

Constraints:
- The provider graph must not create or manage queues.
- The provider graph must respect cancellation and propagate failures to dead-letter.

### 4.4 Startup wiring
#### Replace provider-specific adapter
`FileShareIngestionPipelineAdapter` should be removed or relocated so Infrastructure no longer depends on a provider-specific pipeline adapter.

Target:
- Infrastructure contains a generic “queue host” service/node that:
  - polls provider queues,
  - calls provider `ProcessIngestionRequestAsync(...)`.

Provider-specific code (FileShare) should not live in Infrastructure.

---

## 5. Functional requirements
- FR1: For each registered provider factory, ingestion host must poll `QueueName` and process messages.
- FR2: Ingestion host must create queues if missing (queue + poison queue).
- FR3: When `maxDequeueCount` is exceeded, message must be moved to poison queue.
- FR4: Successfully processed requests must be acked.
- FR5: Failed requests must be routed to the existing dead-letter mechanisms (request vs index-operation dead-letter as appropriate).

---

## 6. Non-functional requirements
- NFR1: Concurrency must remain at least equivalent to the current lane-based pipeline design.
- NFR2: Provider processing must be deterministic and testable (explicit entrypoint).
- NFR3: Architecture boundaries must be maintained:
  - Infrastructure owns queue clients and polling.
  - Provider owns pipeline composition.
- NFR4: Logging must remain structured (`ILogger`) and include provider name, queue name, message id, and request key where available.

---

## 7. Implementation notes / refactor strategy
### 7.1 Incremental migration
To minimize risk, refactor in these phases:
1) Introduce the provider processing entrypoint contract and implement it in FileShare provider using the existing `FileShareIngestionGraph` internals.
2) Update Infrastructure queue poller to call provider processing instead of emitting requests into a provider-specific adapter.
3) Remove or relocate `FileShareIngestionPipelineAdapter`.
4) Add integration tests / end-to-end verification via `IngestionServiceHost` + FileShareEmulator.

### 7.2 Testing strategy
- Unit tests:
  - Provider `ProcessIngestionRequestAsync` input validation and routing.
  - Correct invocation for upsert vs non-upsert.
- Graph-level tests:
  - Failures route to index dead-letter and never reach bulk indexing.
- Integration tests (optional):
  - Start ingestion host, enqueue a message, validate ack + indexing path.

---

## 8. Decisions
- D1: `ProcessIngestionRequestAsync(...)` returns once the request is accepted/enqueued into the provider’s internal pipeline (it does not wait for downstream completion).
- D2: Each provider instance owns a long-lived processing graph (not a per-request graph).
- D3: The provider processing entrypoint accepts `Envelope<IngestionRequest>` to preserve queue acker and message metadata through processing.
- D4: `ProcessIngestionRequestAsync(...)` is added directly to `IIngestionDataProvider` (no separate `IIngestionRequestProcessor` interface).

## 9. Open questions
- None.

---

## 10. Acceptance criteria
- AC1: Infrastructure ensures queues exist and polls each provider queue using `IIngestionDataProviderFactory.QueueName`.
- AC2: For each received queue message, Infrastructure deserializes using provider and calls `ProcessIngestionRequestAsync` on that provider.
- AC3: Provider owns the processing graph configuration and is the entrypoint for request processing.
- AC4: Provider does not create/manage queues.
- AC5: Existing ingestion host remains runnable and messages are indexed/acked as before.
- AC6: `ProcessIngestionRequestAsync(...)` returns after acceptance/enqueue (not after indexing/ack/dead-letter completion).
- AC7: Provider processing pipeline is long-lived per provider instance and is started once during ingestion host startup.
- AC8: Provider processing entrypoint accepts `Envelope<IngestionRequest>` and preserves envelope context (including queue acker) through to ack/dead-letter.
