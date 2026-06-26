# Next-Gen Consolidation Discussion: Consolidating Search and Developer UI into a React + shadcn/ui Application

Date: 2026-06-26

## Purpose

This report reviews the current UI and API shape in the repository to support early planning for a new, separate React application built on shadcn/ui. The proposed UI would eventually replace the existing Blazor/Razor developer surfaces and the current Workbench, while also becoming the future end-user search application.

This discussion assumes React as the application framework and shadcn/ui as the primary component baseline for the consolidated browser experience. That narrows frontend planning around component composition, theming, and app-shell conventions, but it does not change the main conclusion that backend API extraction and contract definition remain the gating work.

The review deliberately does not propose a final design. Its purpose is to expose the issues that must be narrowed into a specification and implementation plan.

## Executive summary

The repository already contains useful search, ingestion, rule, provider, and emulator logic, but much of the usable UI behavior is embedded inside server-side Blazor hosts rather than exposed through stable HTTP APIs. A React application built on shadcn/ui cannot simply replace Razor components without first creating or formalizing backend API contracts.

The biggest issues are:

1. The current search UI is a Blazor Server host, not a query API. `QueryServiceHost` contains useful query services and host-local DTOs, but there are no mapped search endpoints for a browser SPA.
2. RulesWorkbench is deeply server-side and partially file-share-specific. Rule authoring, validation, save-back, evaluation, batch loading, checker workflows, and business-unit scanning are invoked directly from Blazor components and services.
3. File-share batch-to-ingestion-payload reconstruction is duplicated in multiple places: FileShareEmulator, RulesWorkbench, and the retained Studio provider code all query the same tables and build similar ingestion payloads.
4. The retained Studio API is the closest existing developer API, but it is intentionally detached from the active AppHost and solution after prior cleanup work. It cannot be treated as active product surface without deciding whether to revive, rename, or selectively reuse it.
5. Workbench is mostly UI-shell machinery and dummy module composition. It contains many concepts that a React and shadcn/ui app should probably not preserve unless there is a clear product need: module assembly loading, command contribution registries, menu/status/toolbar contribution models, custom splitters, and tab management.
6. Authentication is currently optimized for multiple server-rendered browser hosts using cookie-backed OIDC through a shared Keycloak client. A React app plus APIs will need a deliberate SPA/API authentication and CORS model.
7. The repo has no current React/Node app. There is historical Theia/Studio material in docs and retained source, but the active workspace has no package.json-managed React frontend project, no Tailwind setup, and no shadcn/ui component baseline.
8. Third-party .NET integrations that only need to format and submit provider queue messages do not currently have a clean, standalone contracts assembly. They should not need to reference Studio, provider implementation projects, host projects, rules tooling, ingestion pipeline internals, or Search service APIs just to create a valid ingestion queue message.
9. Query rules already shape search meaning through normalization, typed extraction, rule evaluation, residual defaults, and Elasticsearch mapping, but there is no solid developer workflow for seeing why a query rule matched, why another rule did not match, safely editing draft query rules, comparing current versus draft output, or running a regression corpus before saving.
10. There is a major opportunity to introduce an ingestion input journal: once a provider has normalized its source data into the ingestion request that is about to enter the pipeline, the ingestion service can shadow that input for provider-neutral tooling, rule debugging, ingestion-owned repair, replay safety, and dead-letter traceability.
11. The new developer UI should make the whole dead-letter-driven repair loop a primary journey: inspect a dead letter, open the associated journaled ingestion input, run rules/debug processing against that exact input, fix rules or configuration, validate the fix, and then perform guarded repair replay only when it is safe.

Important scope boundary: FileShareEmulator is a local-development-only project and is not in scope for migration into the new React and shadcn/ui application. It should be left as-is. The configuration emulator is also out of scope for this work and should be treated as future externalized infrastructure, not as a candidate for React plus shadcn/ui consolidation or repository refactoring.

There are also clear opportunities:

1. `UKHO.Search.Services.Query` already has a query planning and execution service that could sit behind a proper `/search` API.
2. `StudioServiceHost` already contains provider discovery, rule discovery, ingestion operations, operation status, and SSE endpoints. Some of this may be reusable as an internal developer API after architectural review.
3. `IRuleConfigurationWriter`, `IProviderRulesReader`, and the ingestion rules engine provide useful rule-management primitives that can be exposed behind a provider-neutral API.
4. The ingestion boundary can become the source of truth for developer tooling and ingestion repair: shadowed, provider-normalized ingestion inputs would avoid File Share SQL reconstruction, give the React developer view a provider-neutral substrate, and let ingestion repair post-acceptance failures without asking the provider to resend data.
5. The dead-letter-to-rules-to-repair flow can become the central developer workflow for ingestion failures, giving the React plus shadcn/ui application a clear operational purpose beyond replacing scattered Blazor pages.
6. The current Workbench modules are mostly placeholders, so deleting the complex Workbench shell may have a lower functional blast radius than its size suggests.
7. A React plus shadcn/ui direction gives the frontend a clear primitive and component baseline, but it still requires an explicit plan for design tokens, theming, Tailwind configuration, and copied-component ownership inside the repository.
8. A queue-message-only .NET contracts package can make remote producer integrations safer and easier without expanding the public surface of the Search service or exposing provider/runtime internals.
9. A dedicated query-rule diagnostics workbench can make search-quality tuning explainable: developers should be able to inspect the full query interpretation trace, edit draft rules, compare plan and result deltas, and run representative query suites before promoting rule changes.

## Frontend stack direction

The future UI direction should be treated as React plus shadcn/ui, not merely React as a rendering library. shadcn/ui should provide the primary component baseline, with application-owned workflow components composed from shadcn/ui primitives rather than a port of Radzen, Bootstrap, PrimeReact, or Workbench shell widgets.

The frontend foundation still needs explicit specification: Node package setup, TypeScript, routing, Tailwind CSS, shadcn/ui initialization, design tokens, copied-component governance, API-client generation or typed fetch patterns, Monaco or comparable editor integration, linting, formatting, and a focused frontend test strategy.

## Current UI surface inventory

### Active or active-looking browser hosts

| Surface | Project | Technology | Current role | React plus shadcn/ui lift implication |
| --- | --- | --- | --- | --- |
| Query UI | `src/Hosts/QueryServiceHost` | Blazor Server, Radzen, Monaco interop | Current query/search workspace with raw query, generated plan editor, diagnostics, results, facets, and result explanation UI | Needs a real HTTP query API. Current behavior is mostly scoped Blazor state and server-side services. |
| Ingestion UI | `src/Hosts/IngestionServiceHost` | Blazor Server, Radzen | Hosts ingestion service and a statistics page; also starts ingestion pipeline background services | Needs separation between ingestion runtime host and any browser-facing controls. Current host is both service runtime and UI. |
| FileShareEmulator | `tools/FileShareEmulator` | Blazor Server, Radzen, one minimal API | Local-development-only emulator UI for statistics, indexing, downloads, queue clearing, deleting indexes, and batch-file streaming | Out of scope for React migration. Leave the project and UI as-is; only shared/duplicated backend logic remains a valid architecture concern. |
| RulesWorkbench | `tools/RulesWorkbench` | Blazor Server, Bootstrap, Monaco interop | Rule browsing/editing/saving, rule evaluation, checker, business-unit scans | Needs API extraction. It is currently a UI host with direct App Configuration, SQL, blob, and rules-engine dependencies. |
| Configuration emulator explorer | `configuration/UKHO.Aspire.Configuration.Emulator` | Blazor Server plus API endpoints | App Configuration emulator with an explorer mounted at `/_explorer` and Azure App Configuration-compatible key endpoints | Out of scope. It is expected to move out of this solution and should not be included in future React plus shadcn/ui work or refactoring plans for this effort. |
| Workbench | `src/Workbench/server/WorkbenchHost` plus modules | Blazor Server, Radzen, custom shell abstractions | Desktop-like shell, module discovery, dummy tools, menus, tabs, output panel, splitter layout | Mostly should be removed rather than ported. Preserve only real workflows, not shell mechanics, unless explicitly required. |

### Legacy, retained, or detached surfaces

| Surface | Project/path | Status observed | Planning note |
| --- | --- | --- | --- |
| StudioServiceHost | `src/Studio/StudioServiceHost` | Source and tests exist, but the project is not in `Search.slnx` and not registered in active `AppHost` | It is retained for later refactoring per cleanup docs. Treat as candidate code, not current active architecture. |
| UKHO.Search.Studio contracts | `src/Studio/UKHO.Search.Studio` | Source and tests exist but not active in solution | Useful abstractions may be reusable after review. Current relationship to the future React plus shadcn/ui application is undecided. |
| FileShare Studio provider | `src/Providers/UKHO.Search.Studio.Providers.FileShare` | Source and tests exist but not active in solution | Provides provider-specific ingestion API behavior; has direct SQL/queue coupling to file-share emulator data. |
| Old Workbench hosts | `src/Workbench/server/WorkbenchHost-old`, `src/Workbench/server/OldWorkbenchHost`, samples | Present as source | Should be excluded from any future active UI plan unless there is a missing behavior only present there. |
| Radzen source/demos | `src/Workbench/radzen-blazor` | Vendored or local Radzen code/demos in workspace | Likely removable when Blazor UI is removed, subject to dependency checks. |

## Active orchestration findings

`AppHost` currently starts these browser/API-relevant resources in service mode:

1. `IngestionServiceHost`
2. `QueryServiceHost`
3. `FileShareEmulator`
4. `RulesWorkbench`
5. `WorkbenchHost`
6. configuration emulator support through `AddConfigurationEmulator(...)`

`StudioServiceHost` is not part of the active Aspire graph. Existing documentation under `dev/work-packages/mvp/078-cleanup` explicitly says Studio/Theia and `StudioServiceHost` were removed from active Aspire and solution participation while retaining source for later refactor.

FileShareEmulator and the configuration emulator appear in the current local developer orchestration, but that does not make them target surfaces for this React plus shadcn/ui lift. FileShareEmulator should remain a local-dev tool, and the configuration emulator should be treated as out-of-scope infrastructure that will eventually leave the solution.

Planning consequence: a new React plus shadcn/ui app cannot assume there is already one active backend-for-frontend. There is an active service set, plus a detached candidate Studio API. One early architecture decision must be whether to:

1. add APIs to the existing active service hosts;
2. revive/refactor `StudioServiceHost` as the developer API host;
3. create a new API host/BFF for the React application;
4. split end-user search APIs from developer/tooling APIs.

## Query/search UI review

### What exists

`QueryServiceHost` is an interactive Blazor Server application. It registers:

1. shared service defaults;
2. App Configuration;
3. Elasticsearch client;
4. query infrastructure via `AddQueryServices()`;
5. Radzen components;
6. Keycloak browser-host authentication;
7. `IQueryUiSearchClient` implemented by `QueryUiSearchClient`;
8. scoped `QueryUiState`.

The underlying query pipeline is promising:

1. `UKHO.Search.Services.Query.Execution.QuerySearchService` plans raw query text through `IQueryPlanService` and executes through `IQueryPlanExecutor`.
2. `QueryUiSearchClient` adapts that service into a host-local `QueryResponse` with generated plan JSON, Elasticsearch request JSON, hits, warnings, timings, and edited-plan state.
3. `QueryUiState` coordinates raw query execution, edited-plan execution, validation, selected facets, selected hit, and result explanation UI state.

### Key issues for React

1. There is no HTTP search API in the active query host. The only mapped endpoints found in host code are authentication lifecycle endpoints and default health/service endpoints.
2. Query request/response models are host-local under `src/Hosts/QueryServiceHost/Models`. They are not durable public API contracts.
3. Facet selections are accepted by `QueryUiSearchClient`, but the real query path logs that facet selections are not yet translated. The real response currently projects `Facets = Array.Empty<FacetGroup>()`.
4. The generated-plan editor flow is UI-specific but valuable for developers. A React developer view would need explicit endpoints for generating a plan, executing an edited plan, and returning diagnostics.
5. Result explanation is UI state rather than backend capability. The current `Hit` model contains raw hit data and matched fields, but no clear API contract for explain/detail behavior.
6. The current query diagnostics show matched rule ids, applied filters, boosts, sorts, generated plan JSON, and Elasticsearch request JSON, but they do not expose per-rule predicate traces, no-match reasons, before/after plan deltas, or current-versus-draft rule comparisons.
7. The current search UI is developer-workspace flavored, not a designed end-user search product. It should not be assumed to represent the eventual real search UI.

### Likely API gaps

The future UI likely needs at least these query-side APIs:

1. execute raw search;
2. generate or return query plan;
3. execute supplied query plan for developer diagnostics;
4. return final search engine request/diagnostics where allowed;
5. return facets and apply facet/filter selections;
6. return result detail/explain data;
7. describe supported query features and available filters/sorts;
8. list, fetch, validate, and save query rules;
9. evaluate current or draft query rules against a raw query and return a structured transformation trace;
10. compare current and draft query rules for one query or a representative query suite;
11. expose health/readiness for UI startup and environment diagnostics.

### Query-rule diagnostics workbench gap

Query rules are different from ingestion rules. They are global search-interpretation rules over normalized user text and extracted query signals. A matched query rule can mutate canonical query intent, emit concept signals, emit sort hints, emit filters, emit boosts, and consume tokens or phrases so residual defaults do not duplicate already-accounted-for meaning.

The current query-side code already supports this runtime shape through `QueryPlanService`, `ConfigurationQueryRuleEngine`, `QueryRulesValidator`, and the flat `rules:query:*` configuration namespace. The current Blazor UI exposes only a compact view of the outcome: generated plan JSON, matched rule ids, high-level applied filters/boosts/sorts, and final Elasticsearch request JSON. That is useful for experts, but it is not enough for a developer trying to tune search quality safely.

A React developer search workbench should provide a first-class query-rule lab. Given a raw query, it should show the full interpretation pipeline: raw input, normalized text, tokens, typed extracted signals, seed model, each rule's predicate evaluation, each matched rule's action outputs, consumed tokens and phrases, residual text, default contributions, final query plan, Elasticsearch request JSON, and returned results. It should also show rules that did not match and explain the resolved path values or predicate reason that caused the no-match.

The workbench should support draft editing without immediately saving to App Configuration. A developer should be able to edit a rule, validate it with the backend validator, run the same query through current and draft rule sets, and compare the resulting model, filters, boosts, sorts, residual defaults, Elasticsearch request, result count, top result order, matched fields, and warnings. For serious search-quality work, it should also support named query suites so a draft rule can be checked against representative searches before promotion.

This capability should be backed by API contracts rather than inferred in the browser from final `QueryPlan` JSON. The rule engine or a dedicated query-rule diagnostics service needs to emit a structured trace that records predicate resolution, match/no-match state, action application, and field-level deltas. The React app should present that trace; it should not try to recreate rule semantics locally.

## Ingestion service UI review

`IngestionServiceHost` hosts the actual ingestion service runtime and also maps Blazor UI components. It wires configuration, Elasticsearch, queues, blobs, ingestion services, a file-share read-only client, and Keycloak browser authentication.

The main risk is mixing runtime service responsibilities with browser UI responsibilities. If all UI moves into React, this host may still need to exist as an ingestion runtime, but its browser pages and browser-host auth may become unnecessary.

API gaps depend on desired developer features. If the React developer view replaces existing ingestion pages, it may need APIs for:

1. ingestion runtime status;
2. pipeline mode and configuration summary;
3. index statistics and health;
4. queue status and dead-letter status;
5. recent ingestion activity or operation logs;
6. controlled reprocessing actions, if these are intentionally exposed outside FileShareEmulator's local-dev-only workflow.
7. ingestion input journal discovery, outcome history, supersession status, and repair eligibility.

## Remote ingestion queue contract assembly requirement

There is a separate integration requirement that should not be confused with provider authoring or developer API design. Some third-party or remote .NET projects need to create valid ingestion queue messages and submit them to a provider ingestion queue. Those projects are queue producers, not Search service hosts and not provider implementations. They should know only the shape of the queue message they must put on the provider queue.

The right output is a small .NET contracts assembly, tentatively `UKHO.Search.Ingestion.Contracts`, containing the ingestion queue message wire contract and dependency-light helpers. It should not reference Studio, Blazor, React, rules workbench code, provider implementations, Elasticsearch, Azure SDKs, Aspire, App Configuration, SQL, queue clients, logging abstractions, or ingestion pipeline runtime code. A remote producer should be able to reference the package, construct an `IngestionRequest`, serialize it with the package-provided JSON options, and hand the resulting JSON to whatever queue client or deployment-specific submission path it owns.

The essential type set is deliberately small:

| Type | Required role in a remote queue producer |
| --- | --- |
| `IngestionRequest` | The top-level queue message envelope. It must contain exactly one supported operation payload. |
| `IngestionRequestType` | The operation discriminator: `IndexItem`, `DeleteItem`, or `UpdateAcl`. |
| `IndexRequest` | The index/upsert payload, including document id, metadata properties, security tokens, source timestamp, and file metadata. |
| `DeleteItemRequest` | The delete payload, carrying the document id to remove. |
| `UpdateAclRequest` | The ACL update payload, carrying the document id and replacement security tokens. |
| `IngestionProperty` | One provider-normalized metadata property that rules and canonical mapping can consume. |
| `IngestionPropertyType` | The supported wire value types for metadata properties: string, text, integer, double, decimal, boolean, datetime, timespan, guid, uri, and string-array. |
| `IngestionPropertyList` | The property collection that enforces case-insensitive uniqueness and normalized property names. |
| `IngestionFile` | File metadata attached to an index request: filename, size, timestamp, and MIME type. |
| `IngestionFileList` | The collection of file metadata entries. |
| Ingestion JSON options and converters | The serializer configuration required for the exact queue-message JSON, especially the typed `IngestionProperty.Value` field and lower-case property type tokens. |

That package should exclude anything that is not required to construct or validate the JSON body placed on a provider queue. In particular, it should exclude Studio API DTOs, `ProviderDescriptor`, provider catalogs, `IStudioProvider`, `IIngestionDataProvider`, operation tracking DTOs, rule DTOs, `CanonicalDocument`, dead-letter DTOs, replay DTOs, ingestion journal DTOs, File Share SQL payload loaders, File Share security-token policy, and queue submission clients. Those can live in separate API, tooling, provider, or client packages if a later slice explicitly needs them.

The first version can still include queue-message-focused conveniences as long as they stay dependency-light:

1. static factories such as `IngestionRequest.CreateIndex(...)`, `CreateDelete(...)`, and `CreateAclUpdate(...)`;
2. typed property factories such as `IngestionProperty.String(...)`, `Text(...)`, `DateTime(...)`, and `StringArray(...)`;
3. an `IndexRequestBuilder` for id, source timestamp, security tokens, files, and properties;
4. a non-throwing validator that returns structured contract errors before a producer submits to a queue;
5. a serializer facade so producers do not have to remember custom converter registration;
6. golden JSON examples for `IndexItem`, `DeleteItem`, and `UpdateAcl` messages;
7. an explicit contract version marker so queue-message compatibility can be reasoned about over time.

Queue submission helpers should be treated as a separate optional package, not part of the core contracts assembly. For example, a future `UKHO.Search.Ingestion.AzureQueues` package could wrap Azure Queue Storage submission, but the core contract package should not force every producer to adopt a specific Azure SDK, authentication model, queue naming convention, or deployment topology.

This package is also separate from the ingestion input journal. The remote producer creates the queue message. The ingestion service assigns any journal identity, such as `ShadowId`, after it receives and accepts the message. Producers should not generate `ShadowId` values or know blob/table journal storage details.

## FileShareEmulator review: local-dev-only and out of React plus shadcn/ui scope

FileShareEmulator is a local-development-only project. It is not in scope for migration to the new React plus shadcn/ui application and should be left as-is. The review below is retained only because FileShareEmulator contains logic that is duplicated elsewhere and therefore affects backend/API planning.

### What exists

FileShareEmulator contains Blazor pages for:

1. overall statistics;
2. per-business-unit statistics;
3. indexing pending batches;
4. indexing all pending batches;
5. indexing a batch by id;
6. indexing by business unit;
7. resetting batch indexing status;
8. clearing the ingestion queue and poison queue;
9. deleting Elasticsearch indexes;
10. downloading batch zip files to a configured local path;
11. serving batch zip files through `GET /batch/{batchId}/files`.

Only `GET /batch/{batchId}/files` is already mapped as an API endpoint. The remaining browser operations call server-side services directly.

### Overlap with Studio APIs

Several FileShareEmulator indexing operations overlap conceptually with the retained Studio API:

1. index all pending batches;
2. index a provider-neutral context/business unit;
3. reset indexing status globally;
4. reset indexing status by context/business unit;
5. submit a specific payload/batch.

However, FileShareEmulator itself must not become part of the React plus shadcn/ui application. Its controls, including deleting all Elasticsearch indexes and clearing queues, should remain local-dev-only inside the existing emulator project. Any future API design should only consider whether duplicated backend logic needs a single owner outside the UI.

### Glaring problem

The emulator contains its own batch-to-ingestion-message construction logic in `IndexService`. Similar logic exists in RulesWorkbench and the FileShare Studio provider. This duplication is an architectural problem because it can cause different tools to submit or evaluate subtly different payloads for the same batch.

## RulesWorkbench review

### What exists

RulesWorkbench provides four meaningful workflows:

1. browse and filter App Configuration-backed ingestion rules;
2. edit rules as JSON or through a limited builder;
3. save valid rules back to Azure App Configuration and touch the refresh sentinel;
4. evaluate rules against manually authored or batch-loaded payloads;
5. check a single batch and show candidate-but-unmatched rules;
6. scan batches by business unit and stop at the first non-OK result.

Useful service primitives include:

1. `AppConfigRulesSnapshotStore` for loading, filtering, validating, unwrapping, and locally overriding rule JSON;
2. `IRuleConfigurationWriter` for save-back to App Configuration;
3. `IRuleJsonValidator` for validation;
4. `RuleBuilderMapper` for a small rule-builder model;
5. `EvaluationPayloadMapper` for mapping UI payload DTOs to ingestion requests;
6. `RuleEvaluationService` for applying ingestion rules with report output;
7. `RuleCheckerService` for checker reports;
8. `BatchPayloadLoader`, `BatchScanService`, and `BusinessUnitLookupService` for file-share emulator data access.

### Coupling issues

RulesWorkbench is heavily attached to file-share concerns:

1. `RuleEvaluationService` hardcodes provider name `file-share`.
2. `RuleCheckerService` filters candidates using provider `file-share` and context equal to normalized business-unit name.
3. `BatchPayloadLoader` reads `[Batch]`, `[BatchAttribute]`, `[File]`, and `[BusinessUnit]` directly from the file-share emulator SQL schema.
4. `BatchScanService` scans file-share batches by business unit id.
5. `BusinessUnitLookupService` directly reads the file-share `BusinessUnit` table.
6. Security token generation is file-share-specific via `FileShareEmulator.Common.SecurityTokenPolicy`.

This is the specific problem called out in the request: the data needed for the rule workbench is not currently provided through provider-neutral APIs. It is reconstructed from file-share emulator storage by tool-specific services.

### API gaps

A React plus shadcn/ui rules developer view will need APIs for at least:

1. list providers with rule support;
2. list rules by provider and context;
3. get a rule document;
4. validate a rule document;
5. save a rule document;
6. touch or otherwise trigger refresh;
7. evaluate a supplied payload for a selected provider;
8. fetch an evaluation payload by provider-defined id;
9. list provider contexts, such as business units for file-share;
10. scan candidate payloads by provider context;
11. return candidate-but-unmatched rules and missing required fields;
12. describe provider-specific payload schema or payload examples.
13. load shadowed ingestion inputs as rule-evaluation payloads once an ingestion input journal exists.
14. run current rules against a shadowed input without re-contacting the provider.
15. compare rule outputs for the same shadowed input across rule revisions or draft edits.

The obvious direction is not to make React call file-share SQL-shaped APIs. The backend should expose provider-oriented concepts and keep file-share-specific reconstruction behind the provider boundary.

## Ingestion input journal / shadowing opportunity

The strongest architectural opportunity that emerged from this review is to introduce an optional ingestion input journal, sometimes referred to in discussion as "shadowing". The important product concept is not the storage mechanism; it is the journal of provider-normalized inputs at the ingestion boundary. This should be treated as an ingestion reliability capability with developer-tooling benefits, not merely as a developer convenience.

The intended boundary is: the provider has done its source-specific work, and this is the request the ingestion service is about to ingest. For the current File Share path, that means the message has already been transformed into the `IngestionRequest` shape that `IngestionServiceHost` receives from the provider ingestion queue. It is not the File Share database row, and it is not an arbitrary late-stage canonical document. It is the normalized provider handoff into ingestion.

### Current ingestion entry path

The active ingestion queue path is:

1. File Share tooling or another producer writes a provider-normalized message to the provider ingestion queue. For File Share, this queue is configured by `ingestion:filesharequeuename` and defaults to `file-share-queue`.
2. `IngestionServiceHost` runs `IngestionPipelineHostedService`.
3. `IngestionPipelineHostedService` creates `IngestionSourceNode`.
4. `IngestionSourceNode` polls each registered provider queue through `IQueueClientFactory`.
5. For each received `QueueReceivedMessage`, `IngestionSourceNode` calls `provider.DeserializeIngestionRequestAsync(message.MessageText)`.
6. After deserialization, `IngestionSourceNode` has the raw queue body, typed `IngestionRequest`, provider name, queue name, queue message id, dequeue count, inserted time, and derived request id.
7. `IngestionSourceNode` wraps the typed request in an envelope, starts queue visibility renewal through `QueueMessageAcker`, and calls `provider.ProcessIngestionRequestAsync(...)`.
8. The File Share provider writes the envelope to a bounded ingress channel, then validates, partitions, dispatches, enriches, bulk indexes, and acks.

The best shadow capture point is immediately after successful provider deserialization and before `provider.ProcessIngestionRequestAsync(...)`. Capturing earlier leaves tools with raw provider-specific parsing work. Capturing later loses the clean provider handoff and starts mixing the input with pipeline mutations.

Deserialization failures can also be shadowed, but they are diagnostic records rather than normal rule-debug inputs because there is no valid `IngestionRequest`.

### Failure ownership

The journal clarifies ownership of failures. There is little the ingestion service can do about a provider failing to produce or send a valid input. That remains a provider problem. Once a provider has handed a normalized request to ingestion and the initial ingestion gates have passed, later failures are ingestion-owned. Rule authoring errors, canonical validation failures, enrichment failures that are not caused by missing provider artifacts, indexing failures, ACL update failures, and dead-letter outcomes should not require asking the provider to send the same item again.

This gives a useful ownership model:

1. **Provider handoff failure:** the provider cannot produce or send a usable ingestion request. The provider owns the fix.
2. **Ingress gate failure:** ingestion receives malformed or invalid input at the boundary. The provider normally owns the fix, while ingestion should record diagnostics.
3. **Post-ingress ingestion failure:** the request passed the initial gates and entered the ingestion pipeline, then failed during rules, canonical mapping, enrichment, indexing, ACL update, delete, or downstream persistence. Ingestion owns repair and replay.
4. **Ambiguous provider-dependent enrichment failure:** the request passed ingress, but later enrichment still depends on provider/source artifacts. The current File Share `BatchContentEnricher` may still need ZIP content by batch id. The journal should help classify the failure, but full replay may still need artifact availability unless the source artifact model changes later.

This is why the journal should be part of the ingestion service itself. If ingestion accepted the normalized input, ingestion should retain enough durable state to debug and repair ingestion-owned failures without requiring provider involvement.

### Why this matters

RulesWorkbench currently reconstructs payloads from File Share-specific storage. It reads File Share batch tables, file tables, business units, and security-token policy to recreate what would have entered ingestion. That is fragile because the tool is not using the exact input ingestion saw.

An ingestion input journal changes the source of truth:

1. Current model: File Share SQL tables -> tool-specific reconstruction -> rule evaluation.
2. Proposed model: ingestion input journal -> actual provider-normalized `IngestionRequest` -> rule evaluation.

That is powerful because it creates a provider-neutral tooling substrate. Future providers do not need RulesWorkbench or the React plus shadcn/ui application to understand their source databases. They only need to produce normalized ingestion inputs, and ingestion can journal those inputs at the boundary.

### What to capture

Each shadowed input should have a stable immutable identity, not just queue message id or document id. Queue message ids change on replay, and document ids can appear many times across updates or attempts.

The journal should assign a `ShadowId` and store correlation metadata such as:

1. `ShadowId`.
2. `ProviderName`.
3. `QueueName`.
4. `QueueMessageId`.
5. `DocumentId` or request id.
6. `RequestType`.
7. `ReceivedAtUtc`.
8. `DequeueCount`.
9. Queue `InsertedOnUtc` and `NextVisibleOnUtc` when available.
10. Payload hash.
11. Raw queue message body, or a pointer to it.
12. Normalized `IngestionRequest` JSON, or a pointer to it.
13. Optional provider/tool metadata, such as provider context or business unit when it is already present in the normalized request.
14. `ReplayOfShadowId` when a replayed input is derived from an earlier shadowed input.

The envelope should carry the `ShadowId` through pipeline context after the shadow write succeeds. That lets diagnostics, outcomes, dead letters, and replay chains refer back to the exact input that ingestion accepted.

### Blob-only versus table index plus blob payloads

Future provider payloads may become larger than current File Share messages. A blob-only design is tempting because it is simple and handles large payloads well, but it makes discovery and filtering weak. Blob Storage is excellent when the caller already knows the blob name. It is poor for questions like: show recent inputs for provider X, find document Y, list failed `IndexItem` inputs, or follow replay chains.

The stronger design is a hybrid:

1. Use Azure Table Storage for searchable metadata and pointers.
2. Use Blob Storage for payload bodies such as raw queue JSON and normalized request JSON.

The table row should be the catalog, not the large payload store. Blob should hold bodies that may grow. The table gives tool APIs cheap provider/time/document/status queries. The blob gives safe storage for arbitrarily shaped provider payloads.

A minimal first version can still be modest:

1. one table row per shadowed input;
2. one blob for normalized request JSON;
3. optional second blob for raw queue message JSON;
4. table metadata for provider, document id, request type, received time, queue message id, payload hash, status, and blob pointers.

Blob-only is acceptable for a spike or temporary capture path, but it will become painful once the React plus shadcn/ui developer workspace needs list, filter, pagination, dead-letter linkage, or replay history.

### Dead-letter linkage

Dead-letter linkage should be designed in from the start. Today, blob dead-letter records are written later in the pipeline and include envelope key, message id, error details, node name, breadcrumbs, timestamps, and payload diagnostics. If the envelope carries `ShadowId`, any later dead-letter record can include that `ShadowId`.

This creates a useful chain:

1. shadow input is captured and assigned `ShadowId`;
2. pipeline processes that input;
3. if processing dead-letters, the dead-letter record includes `ShadowId`;
4. tooling can show the original input, processing path, failed node, error, dead-letter payload snapshot, and replay actions together.

This can be modeled as either updates to the shadow index row or separate outcome records. Separate outcome records are cleaner long-term because the original input remains immutable. A simple first version could update a status summary on the input row while also writing richer outcome/dead-letter records separately.

Possible outcome concepts include:

1. `Captured`.
2. `AcceptedByProviderPipeline`.
3. `ValidationFailed`.
4. `DeadLettered`.
5. `Indexed`.
6. `ReplayRequested`.
7. `Replayed`.
8. `Superseded`.

The first implementation does not need a full workflow engine, but it should reserve the identifiers and pointers that make this model possible.

The better long-term model is:

1. **ShadowInput:** immutable provider-normalized input accepted at the ingestion boundary.
2. **IngestionAttempt or IngestionOutcome:** processing result linked to `ShadowId`.
3. **DeadLetterRecord:** failure detail linked to the outcome and back to `ShadowId`.

Dead letters should not become the primary replay source. They are failure outcomes for an accepted input. The journal should be the complete accepted-input history, including successful inputs, failed inputs, and replayed inputs.

### Supersession and replay safety

Replay must be safe by default. A journaled input may no longer be the latest provider input for a document. For example:

1. provider sends version A;
2. ingestion accepts and journals A;
3. A fails because of an ingestion-owned issue, such as an authored rule problem;
4. provider later sends version B;
5. B ingests successfully;
6. a user fixes the rule and tries to replay A.

In that case, replaying A into the live index could overwrite newer successfully ingested data from B. That must be blocked by default.

The journal therefore needs freshness and supersession semantics. It should be possible to tell whether a shadowed input is still the latest known accepted input for a provider/document, whether it has been superseded by a newer input, and whether the current indexed state already reflects a later successful input.

Useful replay-safety metadata includes:

1. `ProviderName`.
2. `DocumentId`.
3. `RequestType`.
4. source timestamp from `IndexRequest.Timestamp` where present.
5. received timestamp.
6. payload hash.
7. provider source version or ETag if a future provider can supply one.
8. `ShadowId` of the last successful ingestion for the provider/document.
9. source timestamp, payload hash, or version currently reflected in the index.
10. `ReplayOfShadowId` for replay lineage.

The safest default live replay rule is: only replay a shadowed input into the live index if it is still the latest known input for that provider/document, or if an explicitly authorized operator chooses a forced replay. Diagnostic replay should remain allowed even when an input is superseded because it does not mutate live state.

This is not upsert-only. Old `DeleteItem` replay could remove a newer upsert. Old `UpdateAcl` replay could overwrite newer access state. Replay guards must treat all request types as ordered document events.

The developer UI should make this visible. A failed shadow input that has been superseded should be shown as safe for diagnostic replay but blocked for normal repair replay. A non-superseded failed input can be offered as a repair candidate.

### Rule-debug loop

The rule-debug loop should be treated as a first-class use case for the journal:

1. select a shadowed ingestion input;
2. load the current or draft rule set;
3. run the rules engine against the stored `IngestionRequest` and provider name;
4. show matched rules, unmatched candidate rules, warnings, missing required fields, and canonical document output;
5. adjust the rule;
6. re-run against the same shadowed input without contacting the provider.

This is not the same as full pipeline replay. Rule-only replay needs only provider name, `IngestionRequest`, and canonical document construction. Full ingestion replay may still invoke provider-dependent enrichers. For File Share, `BatchContentEnricher` may download ZIP content by batch id. If that content is unavailable, full replay may not reproduce later enrichment even though rule evaluation can still run.

The journal therefore supports two related but different products:

1. exact rule/debug evaluation from stored ingestion input;
2. optional full replay or resubmission workflows, with explicit handling for provider-dependent enrichment and binary/source artifacts.

Replay modes should be separated explicitly:

1. **Diagnostic replay:** run rules, canonicalization, indexing mapping, or other selected processing in a non-live path. This does not update the live index and is safe even for superseded inputs.
2. **Repair replay:** reprocess a failed accepted input into the live pipeline only when freshness checks pass and the input has not been superseded.
3. **Forced replay:** explicitly authorized operator action that bypasses normal freshness guards. This should be audited and should not be part of the first casual developer workflow.

### Security token creation and shadowing

The current contract requires security tokens before queueing. `IndexRequest` and `UpdateAclRequest` reject empty token arrays, and File Share tooling currently derives tokens before messages reach ingestion. For File Share, token policy is duplicated in FileShareEmulator.Common and the retained Studio File Share provider. RulesWorkbench also reconstructs tokens when loading batches.

That means a shadow at ingestion entry currently captures the exact tokens ingestion received. This is good for exact replay of current behavior.

It is less good as a pure source-fact record because security tokens are already derived upstream. If the long-term goal is to stop tools and providers from knowing token policy, token derivation should move into ingestion as a provider-aware normalization step. That is a larger contract change because the current `IndexRequest` shape requires tokens.

A future cleaner flow could be:

1. provider submits source facts or a provider-normalized pre-ingestion request;
2. ingestion invokes provider-owned token derivation;
3. ingestion validates the fully normalized `IngestionRequest`;
4. ingestion shadows both the source input and normalized replay source, or shadows the normalized replay source with a derivation-policy marker;
5. pipeline processing continues.

This token move is not required for shadowing to be valuable. A practical phased approach is:

1. first shadow what ingestion receives today, including current security tokens;
2. build rule-debug APIs over those shadowed messages;
3. later decide whether token derivation should move into ingestion/provider runtime to remove duplicated policy from File Share tooling.

Shadowing should not be tied to the existing `IngestionMode` enum. The current ingestion mode means strict versus best-effort binary/content enrichment. Shadowing should use separate configuration, such as `ingestion:shadowing:*` settings.

### Environment and failure semantics

Shadowing should be environment-neutral in code. Whether it is enabled in local, dev, IAT, PRP, live, or any other environment is a human deployment/configuration decision. Different uses of Search may have different answers.

The code should support configuration-driven behavior rather than hardcoded environment restrictions. Important settings include:

1. enabled/disabled;
2. capture mode: raw queue body, normalized request JSON, both, failures only, or all valid inputs;
3. storage table/container names;
4. payload body storage strategy;
5. retention or cleanup expectations;
6. failure behavior.

Failure behavior is a major policy choice. If shadowing is diagnostic tooling, table/blob write failures should probably log and continue. If shadowing is an audit/journal requirement for a deployment, failures may need to block processing or force retry. This should be configurable rather than decided globally in code.

If the journal is treated as an ingestion reliability feature, deployments may reasonably choose stricter behavior than diagnostic mode. A deployment that depends on journal-backed repair may require shadow persistence before processing continues. A deployment that uses the journal only for tooling may prefer best-effort capture. The code should support these policy choices without hardcoding environment names.

### Tooling and API implications

The new React plus shadcn/ui developer workspace should be able to work against journal-backed APIs rather than provider-specific databases. Candidate APIs include:

1. list shadowed ingestion inputs by provider, time range, document id, request type, status, and replay chain;
2. get one shadowed input and its payload pointers;
3. fetch normalized request JSON and raw queue JSON on demand;
4. run current or draft rules against a shadowed input;
5. compare outputs across rule revisions;
6. show ingestion outcomes linked to a shadow input;
7. show dead-letter records linked to a shadow input;
8. request replay or resubmission from a shadow input;
9. show replay lineage using `ReplayOfShadowId`.
10. show whether a failed input has been superseded by a later accepted or successful input;
11. distinguish diagnostic replay from live repair replay;
12. block unsafe live repair replay unless freshness checks pass or an authorized forced replay path is used.

This is likely a better foundation for the developer view than directly reviving the old Workbench or making the React plus shadcn/ui application call File Share-shaped APIs.

### Primary developer journey: dead letter to rule fix to guarded repair

The new developer UI should make the dead-letter-driven repair loop a primary user journey. This should not be a hidden diagnostics page or a collection of disconnected resource screens. A dead letter is one of the clearest signals that developer intervention is needed, and the UI should make the next steps obvious: inspect the failure, inspect the exact accepted input, run the rules/debug path, fix the rule or configuration, validate the fix against the same input, and only then perform a guarded repair replay when live mutation is safe.

Today this journey is split across several places and mental models. A user may need to inspect logs, find a blob dead-letter record, reconstruct or locate the original provider payload, open RulesWorkbench, manually load or rebuild a test payload, reason about whether the input is still current, and then decide whether to re-index. That is too much context switching. The React plus shadcn/ui developer workspace should turn this into a single coherent workflow backed by ingestion APIs.

The ideal journey is:

1. **Open ingestion failures.** The developer opens a failures work queue rather than a generic tool list. The list groups or filters failed ingestion outcomes by provider, time range, request type, error category, error code, failed node, rule/canonical/indexing failure type, document id, and supersession status. The page should distinguish provider handoff or ingress-gate failures from post-ingress ingestion-owned failures because the next action differs. Provider handoff failures are generally not repairable by ingestion; post-ingress failures are candidates for rule debug or guarded repair.
2. **Select a failure.** The failure detail view should show the failed node, error category, error code, message, breadcrumbs, timestamps, provider, document id, request type, queue metadata, `ShadowId`, dead-letter record reference, linked payload pointers, and current replay eligibility. It should be clear whether the failure is likely rule-authored, canonical-validation-related, indexing-related, ACL/delete-related, or provider-artifact-related.
3. **Inspect the associated ingestion input.** From the failure detail, the user should open the associated `ShadowInput`. This view should show the journaled provider-normalized `IngestionRequest` that ingestion accepted, plus raw queue JSON when captured. It should not reconstruct File Share batch data from SQL. The point is to show exactly what ingestion accepted at the provider handoff boundary.
4. **Run rule/debug evaluation.** The UI should let the user run current rules, draft rules, or a selected ruleset version against the associated shadow input. The result should show matched rules, candidate-but-unmatched rules, runtime warnings, validation errors, missing required canonical fields, action summaries, and canonical document output. This should reuse backend rule-evaluation APIs over the journaled input rather than a UI-local mapper.
5. **Open rule authoring in context.** If the failure points at rule authoring or missing canonical fields, the rule editor should open with the failing shadow input pinned as the active test case. The user should not have to copy payload JSON between screens. The rule editor should preserve the link back to the originating failure, `ShadowId`, and dead-letter record.
6. **Fix and re-run against the same input.** After editing a rule, the user should re-run diagnostics against the same shadowed input. This creates a tight loop: change rule, evaluate against the known failing input, inspect matched rules and canonical output, repeat. This is the replacement for the current manual RulesWorkbench flow.
7. **Check supersession and repair eligibility.** Before any live mutation, the UI must show whether the shadow input is still the latest known input for the provider/document. If a later accepted or successful provider update supersedes the failed input, the UI should mark live repair replay as blocked by default. Diagnostic replay remains available because it does not mutate live state.
8. **Perform guarded repair replay when safe.** If the failed input is not superseded and backend freshness checks pass, the user may request guarded live repair replay. The UI should show that this is different from diagnostics. It should create a new attempt or replay lineage entry linked to the original `ShadowId`, then show the new outcome.
9. **Track the repair outcome.** After replay, the UI should show whether the repair succeeded, dead-lettered again, or was blocked. The user should be able to navigate between the original failure, the shadow input, the rule evaluation report, replay attempts, and final outcome.

This journey requires the backend to expose a graph of related concepts rather than isolated blobs or tool-specific DTOs:

1. `ShadowInput`: the immutable accepted provider-normalized input.
2. `IngestionOutcome` or `IngestionAttempt`: the processing result for a shadowed input.
3. `DeadLetterRecord`: failure detail linked to the outcome and back to `ShadowId`.
4. `RuleEvaluationReport`: matched rules, candidate rules, missing fields, warnings, errors, and canonical output for a selected shadow input and ruleset.
5. `ReplayEligibility`: whether diagnostic replay, guarded repair replay, or forced replay is allowed, and why.
6. `ReplayAttempt`: a replay or repair attempt linked to `ReplayOfShadowId` and its own resulting outcome.

The UI should not know blob naming conventions, storage table partition keys, or provider database layouts. It should call APIs that return this graph in task-oriented shapes. The backend can then change storage implementation details without forcing UI changes.

This also argues for a task-oriented developer UI rather than only resource-oriented pages. Separate pages for rules, dead letters, payloads, and replay can still exist, but the primary navigation should include a failures or ingestion repair workspace. That workspace should guide the user through the real operational flow rather than making them assemble it from separate tools.

Diagnostic replay and live repair replay must be visually and operationally distinct. Diagnostic replay is safe and should be easy to run repeatedly while editing rules. Guarded repair replay mutates live state and must require backend eligibility checks. Forced replay, if it exists, should be explicitly authorized, audited, and clearly marked as dangerous.

For superseded inputs, the UI should communicate the situation plainly. The expected behavior is: this input has been superseded by a later accepted or successful ingestion; live repair replay is blocked by default; diagnostic replay remains available for investigation and rule testing. This prevents the repair workflow from becoming a way to accidentally overwrite newer provider updates.

This primary journey justifies the ingestion journal, dead-letter linkage, rule APIs, replay safety model, and React plus shadcn/ui developer workspace as one coherent capability. Without this journey, the React plus shadcn/ui workspace risks becoming only a new arrangement of old tool pages. With this journey, it becomes the operational workspace for ingestion failures.

## Retained Studio API review

`StudioServiceHost` is currently the closest existing API surface for a developer UI. It maps:

1. `GET /providers`;
2. `GET /rules`;
3. `GET /echo`;
4. `GET /ingestion/{provider}/{id}`;
5. `POST /ingestion/{provider}/payload`;
6. `PUT /ingestion/{provider}/all`;
7. `POST /ingestion/{provider}/operations/reset-indexing-status`;
8. `GET /ingestion/{provider}/contexts`;
9. `PUT /ingestion/{provider}/context/{context}`;
10. `POST /ingestion/{provider}/context/{context}/operations/reset-indexing-status`;
11. `GET /operations/active`;
12. `GET /operations/{operationId}`;
13. `GET /operations/{operationId}/events` as server-sent events.

This is a stronger starting point than the Workbench shell for developer-tool APIs. It already has OpenAPI and Scalar integration, CORS for a local frontend origin, provider catalog validation, long-running operation tracking, and SSE events.

But it has significant caveats:

1. It is detached from active solution and Aspire participation by design after cleanup work.
2. It is not currently protected by the same browser auth model as the Blazor hosts. It calls `AddAuthorization()` and `UseAuthorization()`, but the reviewed endpoints do not require an authenticated user unless future configuration adds policies elsewhere.
3. It hardcodes local CORS origin `http://localhost:3000`.
4. It registers only the FileShare Studio provider.
5. Its FileShare provider directly queries emulator SQL tables and writes to the hardcoded `file-share-queue` queue.
6. It exposes ingestion operations, but not the full RulesWorkbench authoring/evaluation/checker feature set.

Planning consequence: Studio API code is worth mining, but reviving it wholesale would reintroduce previously retired Studio/Theia-related surface area unless the new plan explicitly redefines it as the React developer API.

## Workbench review

### What exists

Workbench is a desktop-like Blazor shell with:

1. dynamic module discovery from `modules.json` probe roots;
2. reflection-based module loading;
3. `IWorkbenchModule` registration contracts;
4. `IWorkbenchContributionRegistry` for tools, commands, explorers, menu, toolbar, status bar, and explorer toolbar contributions;
5. `WorkbenchShellManager` for tabbed tool activation and runtime contributions;
6. output panel state and startup notifications;
7. custom grid/splitter components and JavaScript;
8. Radzen shell composition;
9. dummy module tools for Search, PKS, FileShare, and Admin.

The current modules are primarily exemplars:

1. Search module has dummy Search query, Search ingestion, and ingestion rule editor tools.
2. FileShare module has a dummy File Share workspace.
3. PKS module has a dummy operations tool.
4. Admin module has a dummy administration tool.

### Issue

The Workbench is far more complex than the real behavior currently needs. Most of its code is shell infrastructure rather than domain tooling. The module system may be useful as a conceptual input, but it should not be ported mechanically to React.

Recommendation for planning: define the developer UI as product workflows, not as a port of Workbench concepts. If extension points are needed later, define a small metadata-driven navigation/action model after real tool APIs exist.

## Configuration emulator review: out of scope

The configuration emulator combines App Configuration-compatible endpoints with a Blazor explorer under `/_explorer`. It has APIs for `/kv`, `/keys`, `/labels`, and locks, with HMAC/JWT authentication behavior.

This surface is out of scope for the React plus shadcn/ui consolidation. It is expected to move out of this solution and should not be considered a candidate for future refactoring work in this effort. If the React plus shadcn/ui developer view needs rule editing, it should call rule-focused APIs rather than exposing or refactoring the generic App Configuration emulator explorer.

## Cross-cutting API and architecture issues

### 1. Missing public API boundary for the future search product

The end-user search UI has no stable API contract today. The closest behavior is inside `QueryServiceHost`, but that is a Blazor host. Before a React plus shadcn/ui app can become the real search UI, the backend must define request/response contracts and authentication behavior for search.

### 2. Developer tools are fragmented across browser hosts

Today, developer tooling is spread across:

1. QueryServiceHost;
2. IngestionServiceHost;
3. RulesWorkbench;
4. WorkbenchHost;
5. retained StudioServiceHost source.

FileShareEmulator and the configuration emulator are deliberately excluded from the React plus shadcn/ui consolidation target. FileShareEmulator remains a local-dev-only emulator, and the configuration emulator is expected to leave the solution.

The new React plus shadcn/ui app needs one navigation model, but the backend does not yet have one coherent developer API surface.

### 3. Provider boundary is not strong enough for tooling

The repository has provider abstractions for ingestion and provider metadata, and the retained Studio code has provider catalogs. But the actual tooling data needed by RulesWorkbench and other file-share-backed developer workflows is still often file-share SQL-shaped.

An ingestion input journal would strengthen this boundary by making the normalized provider handoff the common tooling substrate. Tools would read provider-neutral shadow input records instead of each tool re-querying provider storage.

Provider-neutral UI planning needs provider-owned APIs for:

1. contexts;
2. payload lookup;
3. payload schema/shape;
4. pending item scans;
5. rule candidate data;
6. indexing/reprocessing operations;
7. statistics relevant to the provider.
8. shadow input discovery and replay metadata.

### 4. Duplicate file-share data access and payload construction

The same basic concepts appear in multiple implementations:

1. business-unit lookup;
2. pending batch lookup;
3. batch attributes lookup;
4. file metadata lookup;
5. active business-unit name lookup;
6. security token calculation;
7. ingestion request construction;
8. queue submission;
9. marking batches indexed;
10. resetting indexing status.

This duplication is a glaring architectural problem. It will make the new UI unreliable unless one backend owner is chosen.

The ingestion input journal does not automatically delete all duplicate code, but it changes the target. Rather than every tool reconstructing File Share payloads, tooling can converge on journal-backed inputs and a smaller number of ingestion-owned/provider-owned normalization services.

### 5. Rules authoring and runtime evaluation are too intertwined with UI host state

RulesWorkbench performs loading, local override caching, editing, validation, saving, evaluation, and checker workflows directly inside one UI host. Some of this belongs behind API endpoints and application services. Some of it is UI state. The boundary is not currently explicit.

The rule-evaluation part should move toward APIs that accept or reference journaled ingestion inputs. This would let rule authoring/debugging use the exact provider-normalized input that entered ingestion, while the UI remains responsible only for editing state, selections, and presentation.

### 6. Authentication model needs redesign for React plus APIs

Current browser hosts use cookie-backed OpenID Connect and per-host cookie isolation. A single React app calling APIs may need one of:

1. a backend-for-frontend with same-site cookies;
2. SPA OIDC with bearer tokens;
3. a hybrid local-development model;
4. separate end-user and developer/admin auth policies.

The current Keycloak client `search-workbench` is named after Workbench and configured for specific localhost redirect URIs. That naming and configuration will not age well as the consolidated UI becomes the real search app.

### 7. API authorization is inconsistent or incomplete

Protected Blazor hosts use fallback authorization policies. The retained Studio API has authorization services configured but no reviewed endpoint-level authorization requirements. The future developer view will need explicit policy decisions for any non-local destructive actions. FileShareEmulator's existing destructive controls are local-dev-only and should remain inside that project rather than being lifted into React.

### 8. Local-only destructive operations must stay local

The current FileShareEmulator UI can clear queues and delete all Elasticsearch indexes. These operations are acceptable as local emulator controls, but they should not be moved into the consolidated React plus shadcn/ui application. If future non-local APIs introduce similar destructive operations, they need separate environment and authorization controls.

### 9. Solution and active source disagree about Studio

The retained Studio API and provider projects exist on disk with tests, but active solution and Aspire composition intentionally exclude them. This is not necessarily wrong, but it is a planning hazard. A future spec must say whether these projects are historical, to be revived, to be renamed, or to be mined and deleted.

### 10. Documentation history contains multiple superseded UI directions

Docs include earlier Studio/Theia/PrimeReact paths and cleanup records. The future React plan should explicitly supersede those directions so the repo does not accumulate another parallel UI track.

### 11. Ingestion shadowing must be named as a journal, not a UI feature

The ingestion shadowing concept should be specified as an ingestion input journal owned by backend services. The React plus shadcn/ui application should consume APIs over that journal. It should not own capture semantics, token derivation, storage format, or dead-letter linkage. Those are backend architecture decisions that need to be settled before UI implementation.

### 12. Ingestion-owned replay must be guarded against stale overwrites

Once a provider-normalized request has passed initial ingestion gates, post-ingress failures are ingestion-owned. The ingestion service should not need the provider to resend the same input just because rules, canonical mapping, indexing, or other ingestion-owned processing failed. However, replaying an older journal entry can be dangerous if a later provider update has already ingested successfully. Any live repair replay must check whether the shadowed input has been superseded. Diagnostic replay can remain available for superseded inputs because it does not mutate live state.

### 13. Remote queue producers need a queue-message-only contract package

Remote .NET producers that submit directly to provider ingestion queues need a stable package for the queue message contract, not a developer API SDK and not provider authoring interfaces. The package should be dependency-light, centered on `IngestionRequest`, and versioned as a wire contract. It must not require references to Studio, RulesWorkbench, provider implementations, queue clients, pipeline runtime, or Search service internals.

The boundary matters because a remote integration may live in a different repository, release cadence, security context, and deployment topology. It should be able to create the same JSON that the ingestion service expects without learning about AppHost, `StudioServiceHost`, `IngestionServiceHost`, journal storage, dead-letter storage, rule evaluation, or React developer tooling.

## Potential API grouping for planning

This is not a proposed final endpoint design. It is a way to group missing or candidate APIs so a spec can be narrowed.

### End-user search APIs

1. Search execution.
2. Result detail/explain.
3. Facet/filter metadata and selections.
4. Sort options.
5. Query suggestions or query understanding diagnostics, if product-visible.
6. Health/version/environment metadata.

### Developer query APIs

1. Generate query plan from raw text.
2. Execute generated or edited query plan.
3. Return Elasticsearch request JSON and execution diagnostics.
4. Return rule matches/query transformations.
5. Return warnings and typed extracted signals.
6. Return structured query interpretation traces that distinguish normalization, typed extraction, query-rule evaluation, residual defaults, and Elasticsearch mapping.
7. Compare current and draft query interpretation output for the same raw query.

### Developer query-rule APIs

1. List effective query rules with rule id, title, enabled state, description, current validation state, and loaded snapshot metadata.
2. Get an authored or effective query-rule document by id.
3. Validate a draft query-rule document using the backend query-rule validator.
4. Evaluate current query rules against a raw query and return matched rules, non-matched rules, predicate traces, action outputs, residual changes, generated plan, request JSON, and optional result data.
5. Evaluate supplied draft rules without saving them to App Configuration.
6. Compare current and draft rule evaluation for one query, including canonical model deltas, filter deltas, boost deltas, sort deltas, residual default deltas, request JSON diffs, result count changes, and top-result ordering changes.
7. Compare current and draft rule evaluation across a named query corpus.
8. Save or promote validated query-rule changes only after the API has enough authorization, audit, and conflict-handling rules.

### Developer ingestion rule APIs

1. List providers that support rules.
2. List rules by provider/context.
3. Get, validate, save, and refresh rule documents.
4. Evaluate supplied payloads.
5. Fetch provider payload by id for evaluation.
6. Check payload or batch against candidate rules.
7. Scan a provider context for problematic payloads.
8. Provide rule schema and builder metadata.
9. Evaluate a shadowed ingestion input by `ShadowId`.
10. Compare current and draft rule output against the same shadowed input.
11. Show matched rules, candidate-but-unmatched rules, missing required fields, and canonical output for a shadowed input.
12. Open rule authoring with a dead-letter-associated `ShadowId` pinned as the active test case.
13. Return rule-debug reports linked back to the originating dead-letter record and ingestion outcome.

### Developer failure workflow APIs

The dead-letter-to-rule-fix workflow should be explicit in the API model rather than assembled manually from unrelated endpoints.

1. List ingestion failures and dead-letter outcomes by provider, time range, request type, error category, failed node, document id, and supersession status.
2. Get a failure detail view that includes failed node, error category/code/message, breadcrumbs, timestamps, provider, document id, request type, queue metadata, `ShadowId`, dead-letter link, payload pointers, and replay eligibility.
3. Load the associated `ShadowInput` from a failure record.
4. Run diagnostic rule evaluation for the associated `ShadowId`.
5. Save or validate a rule edit while preserving the failure and shadow-input context.
6. Re-run diagnostics against the same input after rule changes.
7. Retrieve `ReplayEligibility` for a failed shadow input, including supersession reason and freshness-check result.
8. Start guarded repair replay when eligibility allows it.
9. Start diagnostic replay even when live repair is blocked.
10. Return replay attempt history and lineage for the originating failure.
11. Show whether a repair attempt succeeded, dead-lettered again, or was blocked.

### Developer ingestion/provider APIs

1. List providers and provider metadata.
2. List provider contexts.
3. Fetch provider payload envelope by id.
4. Submit a payload for ingestion.
5. Start provider-wide ingestion.
6. Start context-scoped ingestion.
7. Reset provider/context indexing status.
8. Track long-running operation status and events.
9. Expose queue status, dead-letter status, and indexing status.
10. List ingestion input journal entries by provider, time range, document id, request type, and status.
11. Fetch journal metadata and payload blobs for a `ShadowId`.
12. Show ingestion outcomes and dead-letter records linked to a `ShadowId`.
13. Show supersession status and repair eligibility for a failed shadowed input.
14. Request diagnostic replay from a shadowed input when supported.
15. Request guarded live repair replay from a shadowed input when freshness checks pass.
16. Request forced replay only through an explicitly authorized and audited path, if it is supported at all.
17. Follow replay lineage through `ReplayOfShadowId`.

### Remote ingestion queue contract package

This is not a React API group. It is the .NET package surface needed by remote producers that submit queue messages directly.

1. Queue message envelope: `IngestionRequest` and `IngestionRequestType`.
2. Operation payloads: `IndexRequest`, `DeleteItemRequest`, and `UpdateAclRequest`.
3. Metadata payloads: `IngestionProperty`, `IngestionPropertyType`, and `IngestionPropertyList`.
4. File metadata payloads: `IngestionFile` and `IngestionFileList`.
5. Required System.Text.Json options and converters for queue-message serialization and deserialization.
6. Optional dependency-free builders, factories, validators, serializer facades, and golden JSON examples.
7. Explicit exclusion of Studio API DTOs, provider catalogs, rule DTOs, journal DTOs, dead-letter DTOs, `CanonicalDocument`, provider SPI interfaces, queue clients, and provider-specific token policy.

### Out-of-scope local emulator capabilities

The following capabilities exist in FileShareEmulator and should stay there. They are not candidate React APIs for this effort:

1. File-share statistics.
2. Business-unit statistics.
3. Batch zip stream/download.
4. Clear queues.
5. Delete local Elasticsearch indexes.
6. Reset seeded data or local indexing status.

The planning concern is de-duplication of backend logic, not moving these controls into the new UI.

## Suggested planning questions

1. Is the React and shadcn/ui app one deployed application with two modes, or are end-user search and developer tooling separate deployments sharing a component system?
2. Should there be one backend-for-frontend, or should React call Query, Ingestion, and Studio/Developer APIs directly?
3. What design-token, theming, Tailwind, and copied-component ownership model will govern shadcn/ui usage in the repo?
4. Should `StudioServiceHost` be revived as the developer API host, or should its useful contracts move into a new active host?
5. Which duplicated file-share backend logic should be consolidated while leaving FileShareEmulator's local-dev UI unchanged?
6. What is the provider-neutral model for rule evaluation payloads?
7. Should rule checking operate on provider contexts generally, or is the first version intentionally file-share-only?
8. Which operations are safe in non-local environments?
9. What auth model should govern end-user search versus developer tooling?
10. Should Workbench module/extensibility concepts be deleted entirely, or is a small metadata/navigation contribution model still required?
11. Which existing host-local DTOs should become shared contracts, and which should be replaced?
12. Should the ingestion input journal use table index plus blob payloads from the start, or allow a short-lived blob-only spike?
13. Which fields must be queryable in the journal: provider, document id, request type, received time, status, context, payload hash, dead-letter id, replay lineage?
14. Should shadow persistence be best-effort, required, or deployment-configurable per environment?
15. Should security-token derivation remain upstream for now, or move into ingestion as a provider-owned normalization step in a later slice?
16. What is the initial scope of replay: rule-only evaluation, queue resubmission, full pipeline replay, or all three as separate capabilities?
17. What marks a shadowed input as superseded: later accepted input, later successful input, source timestamp, provider version, payload hash, current indexed `ShadowId`, or a combination?
18. Where should latest-success state live: in the journal/outcome store, in the indexed document metadata, or both?
19. Should repair replay be blocked for superseded `IndexItem`, `DeleteItem`, and `UpdateAcl` requests by default?
20. What permissions and audit trail are required for forced replay, if forced replay is allowed at all?
21. Should the React plus shadcn/ui developer workspace make ingestion failures/dead letters the default entry point for rule debugging and repair?
22. What failure taxonomy should drive the primary work queue: provider handoff, ingress gate, rule/canonical failure, enrichment failure, indexing failure, ACL/delete failure, or dead-letter outcome?
23. What data must the failure detail endpoint return so the UI can avoid calling storage blobs or provider-specific stores directly?
24. Should the rule editor support a pinned `ShadowId` test case as a first-class editing mode?
25. What audit record should be created when a user moves from diagnostic replay to guarded repair replay?
26. What should the standalone queue-message contracts package be named, and what target frameworks should it support for third-party .NET producers?
27. Which helpers belong in the core contracts package versus a separate optional queue-client package?
28. How should queue-message contract versioning, compatibility, and golden JSON fixtures be managed?
29. Should security-token derivation remain entirely upstream for remote producers in the first version, or should a later provider-specific helper package own token policy for selected providers?
30. How should remote producers be told which provider queue to target without making the contracts package depend on provider catalogs or deployment configuration?
31. Should query-rule editing be available in the first developer UI, or should the first version support diagnostics and draft comparison before save-back?
32. What shape should a structured query-rule trace use so the UI can show predicate resolution, match/no-match reasons, action application, residual consumption, and plan deltas without reimplementing rule evaluation?
33. What representative query corpus should be maintained for regression comparisons before query-rule promotion?
34. How should current-versus-draft query-rule comparison report result-order changes, score-shaping changes, and Elasticsearch request diffs without overwhelming the developer?

## Recommended work package arcs

The recommended work package arcs are described in [next-gen-work-package-arcs.md](next-gen-work-package-arcs.md). The ordering is intentional: contract extraction comes first, then API ownership and authentication foundations, then backend capabilities that developer workflows depend on, then the React workspaces that consume those APIs, and finally replacement end-user search and legacy surface retirement.

1. **Remote ingestion queue contracts**: define the standalone .NET assembly for third-party queue producers and reference it back into the solution.
2. **API ownership, host strategy, and security model**: decide where React-facing APIs live, how BFF versus direct API calls work, and how authentication, authorization, CORS, and environment safety are enforced.
3. **React plus shadcn/ui foundation and Keycloak login**: create the app shell, routing, component baseline, design-token model, API-client pattern, and Keycloak login path.
4. **Query APIs and query-rule diagnostics foundation**: formalize search/query APIs and add backend support for structured query-rule traces, draft evaluation, current-versus-draft comparison, and query corpus comparison.
5. **Ingestion input journal and failure model**: implement shadow capture, `ShadowId`, storage, outcomes, dead-letter linkage, supersession, and replay eligibility foundations.
6. **Provider tooling, ingestion rules, and repair APIs**: expose provider-neutral contexts, rule management, rule evaluation against journaled inputs, file-share adapter consolidation, diagnostic replay, guarded repair replay, and token-derivation decisions.
7. **React developer query-rule workbench**: build the developer UI for query-rule inspection, draft editing, trace explanation, current-versus-draft comparison, and query corpus regression.
8. **React developer ingestion repair workspace**: build the failure-driven repair UI over journal, dead-letter, rule diagnostics, replay eligibility, and guarded repair APIs.
9. **React end-user search experience**: build the production search UX over stable end-user search APIs, facets, sorting, result detail, and appropriate auth policy.
10. **Legacy UI retirement and operational hardening**: remove or detach replaced Blazor/Workbench surfaces, keep local-only emulator controls scoped, harden observability and audit paths, and complete documentation updates.

## Architectural concerns to call out plainly

1. The current repository has too many UI hosts for the amount of real UI behavior present. This increases orchestration, authentication, and maintenance cost.
2. The Workbench is over-engineered relative to its current functional value. Porting it would be a mistake unless a concrete extension-product requirement is established.
3. The lack of stable HTTP APIs for current Blazor UI behavior is the main blocker to a React plus shadcn/ui lift.
4. File-share-specific data access has leaked into tools that want to become general developer tooling.
5. The detached Studio API is both useful and risky: it contains many of the right API ideas, but its active status was deliberately removed.
6. Duplicate payload-construction logic should be treated as a root-cause problem, not worked around in the new UI.
7. Destructive local tooling operations must not become ordinary product APIs by accident.
8. The ingestion input journal could become the developer tooling backbone, but only if it is specified as backend architecture with durable identity, storage/query design, dead-letter linkage, and explicit replay semantics.
9. The ingestion input journal is also a reliability boundary: ingestion should own repair of accepted inputs that fail after ingress gates pass, but live repair replay must not overwrite newer provider updates.
10. The React plus shadcn/ui developer workspace should be workflow-led. The primary ingestion workflow should be inspect failure/dead letter, inspect associated shadow input, run rules/debug, fix rules or configuration, verify against the same input, and then use guarded repair replay only when the backend says it is safe.
11. Choosing shadcn/ui usefully narrows the component baseline, but it does not remove the need to define shared tokens, layout conventions, and copied-component governance explicitly.
12. Remote queue-producer integrations need a narrow .NET contracts assembly for ingestion queue messages. Expanding that package to include Studio, provider authoring, runtime pipeline, journal, or UI concepts would recreate the coupling this work is trying to remove.
13. Query-rule diagnostics need to become a first-class developer capability. Search-quality tuning requires structured traces, draft rule evaluation, current-versus-draft comparisons, and representative query corpus regression rather than only final plan JSON and matched rule ids.

## Bottom line

The React plus shadcn/ui work should start with backend/API clarification, not broad component work. The main planning task is to decide the future API ownership model, provider boundary, remote ingestion queue contract package, query-rule diagnostics model, ingestion input journal shape, replay/repair safety model, and primary developer journeys for both query-rule tuning and dead-letter-driven repair. Once those decisions are made, the React app can be built around stable contracts, shadcn/ui primitives, and app-owned workflow components, and the existing Blazor/Workbench surfaces can be retired in stages.

The most useful existing code to mine is the query service layer, the retained Studio API contract/operation model, the ingestion queue request DTOs, the ingestion rules engine, the ingestion dead-letter/diagnostics patterns, and the RulesWorkbench service logic. The least useful code to preserve is the Workbench shell machinery and dummy module contribution system. The most important new backend concepts to specify are the remote ingestion queue contracts package, query-rule diagnostics and comparison APIs, and the ingestion input journal. The contracts package should expose only the queue-message shape needed by remote .NET producers. Query-rule diagnostics should expose the full interpretation trace from normalization through rule evaluation, residual defaults, request mapping, and result comparison. The journal should cover provider-normalized input capture, table-index-plus-blob storage, `ShadowId` correlation, dead-letter linkage, outcome history, supersession handling, and separate diagnostic versus guarded live repair replay APIs over shadowed inputs. The most important developer UI journeys to design are the query-rule tuning loop and the operational repair loop: inspect search interpretation, compare draft rule output, run query corpuses, inspect dead letter, inspect accepted input, run rules/debug, fix, verify, and repair safely. Frontend specification work should treat React plus shadcn/ui as a fixed baseline and define the token, theme, and component-governance model alongside the API contracts.