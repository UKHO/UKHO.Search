# Next-Gen Work Package Arcs

Date: 2026-06-26

## Purpose

This document turns the planning discussion in [next-gen-consolidation-discussion.md](next-gen-consolidation-discussion.md) into an ordered set of work package arcs. It does not define individual work packages, task breakdowns, estimates, acceptance criteria, or implementation plans. Its purpose is to preserve the intended sequencing and the scope of each arc so later work package specifications can be created without losing the architectural requirements already identified.

The arcs are ordered around dependency flow. Stable contracts and ownership decisions come first. Backend capabilities that the developer workflows depend on come before React workflow implementation. End-user search follows once the React foundation and search APIs are stable. Legacy surface retirement comes last, after replacement capabilities exist.

## Recommended Order

1. Remote ingestion queue contracts
2. API ownership, host strategy, and security model
3. React plus shadcn/ui foundation and Keycloak login
4. Query APIs and query-rule diagnostics foundation
5. Ingestion input journal and failure model
6. Provider tooling, ingestion rules, and repair APIs
7. React developer query-rule workbench
8. React developer ingestion repair workspace
9. React end-user search experience
10. Legacy UI retirement and operational hardening

This order is intentionally not the same as simply building the React app first. The React application needs stable backend contracts, authentication shape, and workflow-supporting APIs before the most important screens can be built without churn.

## Arc 1: Remote Ingestion Queue Contracts

This arc extracts the DTOs needed by third-party .NET queue producers into a standalone assembly and updates the current solution to consume that assembly rather than maintaining an internal-only copy of the queue message contract.

The target consumer is a remote .NET project that formats provider-normalized data and submits an ingestion message to a configured provider queue. That consumer is not writing a Search provider, calling a developer API, running the Search service, or participating in the React UI. It only needs to know what a valid ingestion queue message looks like.

The assembly should therefore be narrow and dependency-light. It should contain the `IngestionRequest` envelope, request discriminator, `IndexRequest`, `DeleteItemRequest`, `UpdateAclRequest`, typed ingestion properties, file metadata, and the System.Text.Json options and converters required to produce the exact queue-message JSON expected by ingestion. Dependency-free builders, factories, non-throwing validators, serializer facades, golden JSON fixtures, and a visible contract version marker are in scope if they make remote producer code safer.

The arc explicitly excludes Studio DTOs, provider catalogs, provider-authoring interfaces, pipeline runtime types, `CanonicalDocument`, rule models, journal models, dead-letter models, queue clients, Azure SDK dependencies, and provider-specific token policy. Queue submission helpers can be considered later as a separate package that depends on the contracts package, not as part of the core contract assembly.

This arc should happen first because the ingestion runtime, future journal records, replay safety, and remote producers should all agree on the same public queue-message contract.

## Arc 2: API Ownership, Host Strategy, And Security Model

This arc decides where the React-facing APIs live and how the future application authenticates and authorizes users. It should settle whether the repository revives and refactors `StudioServiceHost`, creates a new backend-for-frontend, adds APIs to existing service hosts, or splits end-user search APIs from developer/tooling APIs.

The decision matters because the current source tree contains active Blazor hosts, a detached but useful Studio API, local-only emulator controls, and server-rendered authentication patterns. The React application should not grow against accidental or temporary backend boundaries.

This arc should define the route-level API ownership model, SPA/API or BFF authentication approach, Keycloak client naming/configuration direction, local-development CORS behavior, authorization policy split between end-user and developer/admin operations, and environment safety rules for destructive or sensitive operations. It should also define which APIs are local-only, which APIs may run outside local development, and which actions require audit records.

This arc is not about building all APIs. It is the architectural decision point that prevents later work from coupling the React app to the wrong host or exposing local emulator operations as product capabilities.

## Arc 3: React Plus shadcn/ui Foundation And Keycloak Login

This arc creates the frontend foundation for the consolidated browser application. It should establish the React app structure, shadcn/ui adoption model, Tailwind configuration, TypeScript setup, routing, application shell, design-token ownership, copied-component governance, API-client pattern, linting, formatting, test approach, and Keycloak login flow against the identity management included in the Aspire solution.

The app should be a real authenticated shell, not a broad implementation of product workflows yet. It should prove that the React application can start in local orchestration, sign in through Keycloak, hold or refresh identity state according to the chosen auth model, call a protected health or profile-style endpoint, and render the shared navigation/layout conventions that later workspaces will use.

This arc should also decide how Monaco or an equivalent editor component will be integrated, because both ingestion-rule and query-rule developer workflows need JSON editing. It should not port Workbench module mechanics, Radzen concepts, or Blazor layout assumptions into React.

This arc comes before developer UI implementation so later screens share the same authentication, layout, component, and API-client patterns.

## Arc 4: Query APIs And Query-Rule Diagnostics Foundation

This arc formalizes the query-side backend contracts and adds the diagnostics needed for serious search-quality tuning. It covers both general search/query APIs and query-rule-specific diagnostic APIs.

The current query runtime already normalizes text, runs typed extraction, applies flat query rules, builds residual defaults, maps to Elasticsearch JSON, and returns plan-level diagnostics. However, current diagnostics only expose high-level outcomes such as matched rule ids, applied filters, boosts, sorts, generated plan JSON, and final request JSON. They do not explain why individual rules matched or did not match, which path values were resolved, which actions changed which fields, or how a draft rule changes the final plan and results.

This arc should define and implement API support for raw query planning, supplied-plan execution, final request diagnostics, typed extracted signals, and structured query interpretation traces. The trace should distinguish normalization, typed extraction, seed model creation, each rule's predicate evaluation, match and no-match reasons, rule action outputs, consumed tokens and phrases, residual defaults, Elasticsearch mapping, and optional result execution.

It should also support draft query-rule validation, draft evaluation without saving, current-versus-draft comparison for one query, and current-versus-draft comparison across a named representative query corpus. The comparison should show model deltas, filters, boosts, sorts, residual defaults, request JSON differences, result count changes, top-result order changes, matched-field changes, warnings, and timing where available.

This arc comes before the React developer query-rule workbench because the UI must present backend-owned semantics rather than reimplement query-rule evaluation in the browser.

## Arc 5: Ingestion Input Journal And Failure Model

This arc implements the ingestion input journal, also described as shadowing in the planning discussion. The journal records provider-normalized inputs at the ingestion boundary after successful provider deserialization and before provider pipeline processing mutates the request.

This arc should define the `ShadowId`, capture boundary, capture modes, table/blob storage strategy, metadata schema, payload hash, raw queue message storage, normalized request storage, failure behavior, retention expectations, and environment-neutral configuration. It should also define how the `ShadowId` flows through pipeline context so outcomes, diagnostics, dead letters, and replay attempts can link back to the exact accepted input.

The failure model must distinguish provider handoff failures, ingress gate failures, post-ingress ingestion-owned failures, and ambiguous provider-dependent enrichment failures. The journal should support both successful and failed accepted inputs, not only dead letters.

Supersession and replay safety are central to this arc. The backend must be able to tell whether a shadowed input is still the latest known input for a provider/document, whether a later accepted or successful input supersedes it, and whether live repair replay would risk overwriting newer state. Diagnostic replay can remain available for superseded inputs because it does not mutate live state.

This arc comes before the ingestion repair workspace because the workspace depends on journal-backed APIs, dead-letter linkage, outcome records, and replay eligibility.

## Arc 6: Provider Tooling, Ingestion Rules, And Repair APIs

This arc builds the backend APIs and service boundaries needed for provider-neutral developer tooling and ingestion repair workflows.

It should expose provider metadata, provider contexts, payload lookup where appropriate, journal discovery, shadow input retrieval, ingestion outcomes, dead-letter records, replay eligibility, diagnostic replay, guarded live repair replay, and replay lineage. It should also expose rule list/get/validate/save/evaluate/check workflows for ingestion rules and support evaluating current or draft ingestion rules against journaled inputs.

File-share-specific duplication must be addressed here. Current payload construction and data access are duplicated across FileShareEmulator, RulesWorkbench, and retained Studio provider code. This arc should choose a backend owner for batch lookup, payload construction, security token calculation if it remains upstream, queue submission, indexing status updates, and business-unit lookup, while keeping FileShareEmulator's destructive local controls inside the emulator project.

This arc should also define the security-token derivation direction. In the first version, security tokens may remain upstream in queue messages. If token derivation later moves into ingestion or provider-owned normalization, that should be treated as a deliberate contract change rather than a side effect of the UI consolidation.

This arc comes after the journal foundation because its most important developer scenarios use journaled provider-normalized inputs as the source of truth.

## Arc 7: React Developer Query-Rule Workbench

This arc builds the React developer workspace for understanding, editing, and comparing query rules. It consumes the query APIs and diagnostics foundation from Arc 4.

The workbench should make query interpretation explainable. A developer should be able to enter a raw query and see the full pipeline: raw input, normalized text, tokens, typed extracted signals, seed model, per-rule predicate evaluation, matched and non-matched rules, action outputs, consumed tokens and phrases, residual defaults, final query plan, Elasticsearch request JSON, and result data.

The workbench should support draft editing without immediate save-back. A developer should be able to open a rule, edit JSON, validate it using backend validation, evaluate the draft against the same query, and compare current versus draft output. The comparison should focus on practical search-quality questions: did the rule change canonical intent, filters, boosts, sorts, residual defaults, request JSON, hit count, top-result order, matched fields, warnings, or timing?

The workbench should also support query corpus regression. Query rules are global, so a fix for one phrase can break another. A named representative query suite gives developers a way to evaluate draft rule changes before promotion.

Save-back can be included only when API authorization, audit, validation, conflict handling, and App Configuration update behavior are defined. A useful first version may support diagnostics and draft comparison before enabling rule promotion.

## Arc 8: React Developer Ingestion Repair Workspace

This arc builds the failure-driven ingestion repair workspace in the React app. It consumes the journal, failure model, provider tooling, ingestion-rule, and repair APIs from earlier arcs.

The primary journey starts from failures, not from a generic tool list. A developer opens an ingestion failures work queue, filters by provider, request type, time range, failed node, error category, document id, supersession status, and repair eligibility, then opens a failure detail view.

The detail view should link the failure to its `ShadowId`, dead-letter record, accepted input, queue metadata, processing breadcrumbs, error details, payload pointers, and replay eligibility. From there the developer should inspect the exact journaled `IngestionRequest`, run current or draft ingestion rules against that input, open rule authoring with the input pinned as the active test case, re-run diagnostics, check supersession, and request guarded repair replay only when the backend permits it.

Diagnostic replay, guarded repair replay, and forced replay must be visually and operationally distinct. Diagnostic replay is safe and should be easy to repeat. Guarded repair replay mutates live state and must use backend freshness checks. Forced replay, if allowed, needs explicit authorization and audit and should not be a casual workflow.

This arc should not move FileShareEmulator's local destructive operations into React. The repair workspace is for operational ingestion-owned failures over accepted inputs, not for emulator administration.

## Arc 9: React End-User Search Experience

This arc builds the actual end-user search product experience in the React app. It should consume stable end-user search APIs rather than host-local Blazor state or developer-only query diagnostics.

The end-user experience needs search execution, result display, facets, filter selections, sorting, result details, supported query feature metadata, environment/readiness handling, and appropriate end-user authentication and authorization policy. It should not inherit the current QueryServiceHost developer-workspace layout as the product design.

The query-rule workbench and end-user search experience are related but separate. The query-rule workbench helps tune search semantics. The end-user search experience presents the tuned search product to its intended audience. Developer-only plan JSON, raw Elasticsearch request bodies, and draft rule controls should not bleed into the end-user interface.

This arc comes after the frontend foundation and query API contracts are stable. It can run in parallel with some developer-workspace work once the shared search APIs and design language are clear, but it should not force premature coupling to incomplete developer tooling APIs.

## Arc 10: Legacy UI Retirement And Operational Hardening

This arc removes or deactivates old surfaces once replacement capabilities exist. It should retire Blazor/Razor developer surfaces and the Workbench shell in stages, leaving FileShareEmulator and the configuration emulator within their declared scope.

The Workbench shell, dummy modules, module discovery, contribution registries, custom splitters, and tab-management machinery should not be ported unless a concrete product requirement is established. Retained Studio source should either be revived under the new API strategy, mined and deleted, or explicitly left as historical source for a defined reason.

This arc should also harden observability, audit trails, documentation, and operational safety. It should confirm that destructive local-only operations remain local-only, that forced replay and repair actions are auditable, that query-rule and ingestion-rule promotion paths are protected, and that documentation clearly supersedes older Studio/Theia/PrimeReact/Workbench directions.

This arc comes last because retiring surfaces before replacements exist would increase risk and reduce local development capability.

## Requirements Coverage Map

| Requirement | Covered by arcs |
| --- | --- |
| Remote .NET DTO/contracts assembly for queue producers | Arc 1 |
| Reference extracted DTO assembly back into the solution | Arc 1 |
| API host ownership and BFF/direct API decision | Arc 2 |
| Keycloak authentication for React app in Aspire | Arc 2, Arc 3 |
| React plus shadcn/ui frontend foundation | Arc 3 |
| Search API contract and diagnostics | Arc 4, Arc 9 |
| Query-rule visibility, editing, comparison, and regression | Arc 4, Arc 7 |
| Ingestion input journal/shadowing | Arc 5 |
| Dead-letter linkage, outcomes, supersession, replay eligibility | Arc 5, Arc 6, Arc 8 |
| Provider-neutral tooling and ingestion-rule APIs | Arc 6, Arc 8 |
| File-share backend duplication and adapter consolidation | Arc 6 |
| Developer ingestion repair workspace | Arc 8 |
| End-user search experience | Arc 9 |
| Workbench/Blazor retirement and local-only emulator boundaries | Arc 10 |
| Auth, authorization, environment safety, audit | Arc 2, Arc 6, Arc 8, Arc 10 |

## Notes For Later Work Package Planning

These arcs are intentionally larger than individual work packages. A later planning pass should split each arc into numbered work-package folders under `dev/work-packages/`, using the repository documentation workflow. That later pass should produce specifications and implementation plans; this document only preserves the ordered architecture and product intent.