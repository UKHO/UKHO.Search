# Next-Gen Work Package Arcs

Date: 2026-07-01

## Purpose

This document turns the next-gen planning discussion into an ordered set of work package arcs. It does not define individual work packages, task breakdowns, estimates, acceptance criteria, or implementation plans. Its purpose is to preserve the intended sequencing and scope of each arc so later work package specifications can be created without losing the architectural requirements already identified.

The direction recorded here now assumes a permanent audience split:

- `QueryServiceHost` remains the customer-facing search host.
- a new `WorkbenchHost` becomes the permanently internal developer and admin workbench.
- the legacy Workbench code under `src/Workbench/` is not migration input and should be removed before the new internal host is introduced.

The arcs are still ordered around dependency flow. Stable contracts and ownership decisions come first. Host and security decisions come before UI foundations. Backend capabilities that the internal workbench depends on come before the internal workbench itself. End-user search follows once the public query contracts and public host direction are stable. Remaining legacy retirement comes last, after replacement capabilities exist.

## Recommended Order

1. Remote ingestion queue contracts
2. Browser host ownership, audience split, and security model
3. Blazor Blueprint foundations and Keycloak login
4. Query APIs and query-rule diagnostics foundation
5. Ingestion input journal and failure model
6. Provider tooling, ingestion rules, and repair APIs
7. WorkbenchHost developer query-rule workbench
8. WorkbenchHost developer ingestion repair workspace
9. QueryServiceHost end-user search experience
10. Remaining legacy surface retirement and operational hardening

WP126 has already completed the deletion-first cleanup that removed the legacy Workbench tree under `src/Workbench/`, removed its old host from AppHost, and eliminated overlap between the old Workbench meaning and the future internal `WorkbenchHost` name.

## Arc 1: Remote Ingestion Queue Contracts

This arc extracts the DTOs needed by third-party .NET queue producers into a standalone assembly and updates the current solution to consume that assembly rather than maintaining an internal-only copy of the queue message contract.

The target consumer is a remote .NET project that formats provider-normalized data and submits an ingestion message to a configured provider queue. That consumer is not writing a Search provider, calling a developer API, running the Search service, or participating in either browser host. It only needs to know what a valid ingestion queue message looks like.

The assembly should therefore be narrow and dependency-light. It should contain the `IngestionRequest` envelope, request discriminator, `IndexRequest`, `DeleteItemRequest`, `UpdateAclRequest`, typed ingestion properties, file metadata, and the `System.Text.Json` options and converters required to produce the exact queue-message JSON expected by ingestion. Dependency-free builders, factories, non-throwing validators, serializer facades, golden JSON fixtures, and a visible contract version marker are in scope if they make remote producer code safer.

This arc should happen first because the ingestion runtime, future journal records, replay safety, and remote producers should all agree on the same public queue-message contract.

## Arc 2: Browser Host Ownership, Audience Split, And Security Model

This arc fixes the permanent browser-host topology and the associated security model.

The decision is now:

- keep `QueryServiceHost` as the customer-facing host for end-user search UI and public search-facing contracts,
- introduce a new internal `WorkbenchHost` for developer and admin tooling,
- keep `IngestionServiceHost` as an ingestion/runtime host rather than as a future browser product surface,
- and treat the old Workbench code in `src/Workbench/` as legacy code to be deleted rather than mined into the new internal host by default.

This arc should define:

- route-level ownership between the public search host and the internal workbench,
- authentication and authorization posture for each host,
- internal-only versus public-facing capability boundaries,
- which APIs remain HTTP contracts versus which workflows can use direct server-side composition inside the Blazor hosts,
- and the rule that local-only destructive emulator actions remain outside both product hosts.

This arc is not about building every API. It is the architectural decision point that prevents later work from coupling the wrong audience to the wrong host or reintroducing the old Workbench shell by accident.

## Arc 3: Blazor Blueprint Foundations And Keycloak Login

This arc creates the frontend foundation for the split Interactive Server Blazor direction.

It should establish:

- the new internal `WorkbenchHost` foundation,
- the shared Blazor Blueprint component and theming model,
- routing, shell, and layout conventions for the new internal workbench,
- the QueryServiceHost uplift rules needed for the customer-facing search experience,
- Keycloak login flow and host-specific session behavior,
- editor integration for JSON-heavy rule workflows,
- and testing and quality gates for the browser hosts.

This arc should not port the old Workbench module, explorer, command, or splitter architecture. It should also avoid preserving Radzen- or legacy-shell-specific mechanics unless a concrete requirement survives architectural review.

## Arc 4: Query APIs And Query-Rule Diagnostics Foundation

This arc formalizes the query-side backend contracts and adds the diagnostics needed for serious search-quality tuning.

The current query runtime already normalizes text, runs typed extraction, applies flat query rules, builds residual defaults, maps to Elasticsearch JSON, and returns plan-level diagnostics. However, current diagnostics only expose high-level outcomes such as matched rule ids, applied filters, boosts, sorts, generated plan JSON, and final request JSON. They do not explain why individual rules matched or did not match, which path values were resolved, which actions changed which fields, or how a draft rule changes the final plan and results.

This arc should define and implement support for raw query planning, supplied-plan execution, final request diagnostics, typed extracted signals, and structured query interpretation traces. The trace should distinguish normalization, typed extraction, seed model creation, each rule's predicate evaluation, match and no-match reasons, rule action outputs, consumed tokens and phrases, residual defaults, Elasticsearch mapping, and optional result execution.

It should also support draft query-rule validation, draft evaluation without saving, current-versus-draft comparison for one query, and current-versus-draft comparison across a named representative query corpus.

This arc comes before the internal query-rule workbench because the UI must present backend-owned semantics rather than reimplement query-rule evaluation in the browser.

## Arc 5: Ingestion Input Journal And Failure Model

This arc implements the ingestion input journal, also described as shadowing in the planning discussion. The journal records provider-normalized inputs at the ingestion boundary after successful provider deserialization and before provider pipeline processing mutates the request.

This arc should define the `ShadowId`, capture boundary, capture modes, table/blob storage strategy, metadata schema, payload hash, raw queue message storage, normalized request storage, failure behavior, retention expectations, and environment-neutral configuration. It should also define how the `ShadowId` flows through pipeline context so outcomes, diagnostics, dead letters, and replay attempts can link back to the exact accepted input.

The failure model must distinguish provider handoff failures, ingress gate failures, post-ingress ingestion-owned failures, and ambiguous provider-dependent enrichment failures. The journal should support both successful and failed accepted inputs, not only dead letters.

Supersession and replay safety are central to this arc. The backend must be able to tell whether a shadowed input is still the latest known input for a provider/document, whether a later accepted or successful input supersedes it, and whether live repair replay would risk overwriting newer state.

## Arc 6: Provider Tooling, Ingestion Rules, And Repair APIs

This arc builds the backend APIs and service boundaries needed for provider-neutral developer tooling and ingestion repair workflows.

It should expose provider metadata, provider contexts, payload lookup where appropriate, journal discovery, shadow input retrieval, ingestion outcomes, dead-letter records, replay eligibility, diagnostic replay, guarded live repair replay, and replay lineage. It should also expose rule list/get/validate/save/evaluate/check workflows for ingestion rules and support evaluating current or draft ingestion rules against journaled inputs.

File-share-specific duplication must be addressed here. Current payload construction and data access are duplicated across FileShareEmulator, RulesWorkbench, and retained Studio provider code. This arc should choose a backend owner for batch lookup, payload construction, security token calculation if it remains upstream, queue submission, indexing status updates, and business-unit lookup, while keeping FileShareEmulator's destructive local controls inside the emulator project.

This arc comes after the journal foundation because its most important developer scenarios use journaled provider-normalized inputs as the source of truth.

## Arc 7: WorkbenchHost Developer Query-Rule Workbench

This arc builds the internal `WorkbenchHost` workspace for understanding, editing, and comparing query rules. It consumes the query APIs and diagnostics foundation from Arc 4.

The workbench should make query interpretation explainable. A developer should be able to enter a raw query and see the full pipeline: raw input, normalized text, tokens, typed extracted signals, seed model, per-rule predicate evaluation, matched and non-matched rules, action outputs, consumed tokens and phrases, residual defaults, final query plan, Elasticsearch request JSON, and result data.

The workbench should support draft editing without immediate save-back and should also support query corpus regression.

Save-back can be included only when authorization, audit direction, validation, and conflict behavior are defined.

## Arc 8: WorkbenchHost Developer Ingestion Repair Workspace

This arc builds the failure-driven ingestion repair workspace in the internal `WorkbenchHost`. It consumes the journal, failure model, provider tooling, ingestion-rule, and repair APIs from earlier arcs.

The primary journey starts from failures, not from a generic tool list. A developer opens an ingestion failures work queue, filters by provider, request type, time range, failed node, error category, document id, supersession status, and repair eligibility, then opens a failure detail view.

The detail view should link the failure to its `ShadowId`, dead-letter record, accepted input, queue metadata, processing breadcrumbs, error details, payload pointers, and replay eligibility.

Diagnostic replay, guarded repair replay, and forced replay must be visually and operationally distinct. FileShareEmulator's local destructive operations must not move into WorkbenchHost.

## Arc 9: QueryServiceHost End-User Search Experience

This arc builds the actual end-user search product experience in `QueryServiceHost`. It should consume stable end-user search contracts rather than exposing developer-only diagnostics or the internal workbench's operational concepts.

The end-user experience needs search execution, result display, facets, filter selections, sorting, result details, supported query feature metadata, environment and readiness handling, and appropriate end-user authentication and authorization policy. It should not inherit the current QueryServiceHost developer-workspace layout as the product design.

The query-rule workbench and end-user search experience are related but separate. The internal workbench helps tune search semantics. QueryServiceHost presents the search product to its intended audience.

## Arc 10: Remaining Legacy Surface Retirement And Operational Hardening

This arc removes or deactivates old surfaces once replacement capabilities exist. The legacy Workbench tree under `src/Workbench/` is expected to be deleted earlier as a naming-clarity prerequisite. Arc 10 therefore focuses on the remaining retirement set, including RulesWorkbench, retained Studio/UI/API leftovers, stale docs, and any other overlapping legacy surfaces that survive the earlier deletion-first cleanup.

This arc should also harden observability, audit trails, documentation, and operational safety. It should confirm that destructive local-only operations remain local-only, that forced replay and repair actions are auditable, and that query-rule and ingestion-rule promotion paths are protected.

## Requirements Coverage Map

| Requirement | Covered by arcs |
| --- | --- |
| Remote .NET DTO/contracts assembly for queue producers | Arc 1 |
| Reference extracted DTO assembly back into the solution | Arc 1 |
| Public versus internal host ownership | Arc 2 |
| Delete legacy Workbench before reusing the name | Arc 2 plus pre-Arc 03 WP126 |
| Keycloak authentication for QueryServiceHost and WorkbenchHost | Arc 2, Arc 3 |
| Blazor Blueprint foundations | Arc 3 |
| Search API contract and diagnostics | Arc 4, Arc 9 |
| Query-rule visibility, editing, comparison, and regression | Arc 4, Arc 7 |
| Ingestion input journal/shadowing | Arc 5 |
| Dead-letter linkage, outcomes, supersession, replay eligibility | Arc 5, Arc 6, Arc 8 |
| Provider-neutral tooling and ingestion-rule APIs | Arc 6, Arc 8 |
| File-share backend duplication and adapter consolidation | Arc 6 |
| Developer ingestion repair workspace | Arc 8 |
| End-user search experience | Arc 9 |
| Remaining legacy surface retirement and local-only emulator boundaries | Arc 10 |
| Auth, authorization, environment safety, and audit | Arc 2, Arc 6, Arc 8, Arc 10 |

## Notes For Later Work Package Planning

These arcs are intentionally larger than individual work packages. A later planning pass should split each arc into numbered work-package folders under `dev/work-packages/`, using the repository documentation workflow. That later pass should produce specifications and implementation plans; this document only preserves the ordered architecture and product intent.