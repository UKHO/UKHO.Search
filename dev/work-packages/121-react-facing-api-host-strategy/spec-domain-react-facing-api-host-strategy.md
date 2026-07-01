# Specification: WP121 React-Facing API Host Strategy

Target output path: `dev/work-packages/121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md`

Date: 2026-07-01

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the recommended public backend topology for the consolidated React application.

The current recommendation is that the React application should talk to one public backend host that acts as the sole browser-facing API boundary. That public backend host should be introduced as a brand-new project under `src/Hosts/` named `PublicApiHost` rather than by repurposing an existing runtime host. Transitional internal HTTP between the new public host and the existing runtime hosts is acceptable where it reduces delivery risk, but the preferred end state is direct shared-service composition for query-side behavior inside the new public host. `IngestionServiceHost` remains a distinct long-term runtime host because its ingestion role stays operationally separate.

The recommended public backend host may initially delegate to the existing service-side runtime, including pass-through or proxy-style behavior where that is the cheapest transition path. However, the long-term public ownership of API contracts, auth boundaries, route groups, health/profile endpoints, and browser-facing policy should sit in that single public host rather than being split across multiple public origins.

### 1.2 Scope

In scope for WP121:
- Choose the recommended React-facing API topology.
- Define which runtime surfaces remain behind the public API boundary.
- Define high-level route ownership for end-user search, developer query diagnostics, ingestion and provider tooling, health/profile, and operational status.
- Record the transition stance on proxy/delegation versus long-term public contract ownership.
- State which repository surfaces are not valid candidates for the public backend role.

Out of scope for WP121:
- Final authentication model details such as BFF cookies versus SPA bearer tokens.
- Detailed endpoint authorization policies.
- Detailed request/response contract models.
- Retirement execution for old UI surfaces.
- FileShareEmulator changes.
- React project placement and frontend project structure.

### 1.3 Stakeholders

- Arc 02 owners deciding the browser-facing backend boundary.
- Frontend authors who need a stable single API origin for the React application.
- Query and ingestion service owners whose runtime capabilities will be exposed through the chosen public boundary.
- Security and platform owners who need one place to enforce browser-facing auth, CORS, and diagnostics.
- Later work packages that implement query APIs, ingestion tooling APIs, and legacy UI retirement.

### 1.4 Definitions

- Public backend host: The single browser-facing API boundary that the React application calls.
- Service-side runtime host: A runtime host that continues to own domain or processing behavior behind the public boundary.
- Delegation: The public backend host calling into existing runtime services rather than reimplementing behavior immediately.
- Pass-through proxy: A transitional form of delegation where the public host mainly forwards requests while still owning the public route, auth, and policy boundary.
- Retirement-bound UI surface: A current UI surface that remains available for source inspection but is not an approved future platform direction.

## 2. System context

### 2.1 Current state

The repository currently has no single React-facing backend host.

Evidence checked:
- [../../../src/Hosts/AppHost/AppHost.cs](../../../src/Hosts/AppHost/AppHost.cs) starts `IngestionServiceHost`, `QueryServiceHost`, `FileShareEmulator`, `RulesWorkbench`, and `WorkbenchHost` in the local services-mode stack.
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md) fixes the retained service-side runtime as `IngestionServiceHost`, `QueryServiceHost`, and the provider mechanism.
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md) also fixes non-emulator UI surfaces as retirement-bound and available for source inspection only.
- Current browser hosts use shared cookie-backed Keycloak authentication through [../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs](../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs) and [../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs](../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs).
- Query and ingestion browser/runtime entry points are currently mixed in [../../../src/Hosts/QueryServiceHost/Program.cs](../../../src/Hosts/QueryServiceHost/Program.cs) and [../../../src/Hosts/IngestionServiceHost/Program.cs](../../../src/Hosts/IngestionServiceHost/Program.cs).

The main architectural problem is that current browser-facing behavior is spread across hosts that are also current runtime composition roots. That is workable for Blazor-hosted UI, but it is not the cleanest boundary for a consolidated React application.

### 2.2 Proposed state

The recommended direction is:
- one brand-new public backend host under `src/Hosts/PublicApiHost` as the sole browser-facing API origin for React,
- `IngestionServiceHost` retained as a distinct service-side runtime host,
- query-side behavior moved toward shared-service composition behind the new public host rather than a permanently separate `QueryServiceHost` host boundary,
- the provider mechanism retained behind those runtime boundaries,
- and `FileShareEmulator` left untouched as local-only tooling.

The public backend host should own:
- browser-facing route structure,
- the external API contract boundary,
- browser-facing auth and CORS policy,
- public health/profile endpoints,
- and the separation between end-user and developer/admin surfaces.

That separation should be visible from the start through clear route families on the same host rather than through a flat undifferentiated API surface. The current recommendation is to keep one host while using distinct route families such as `/api/search/*` for end-user search and `/api/admin/*` for developer/admin tooling, with the admin family split by domain into clearer sub-families such as `/api/admin/query/*` and `/api/admin/ingestion/*`.

System endpoints should remain conventional top-level endpoints on that same host rather than being moved under a separate `/api/system/*` route family.

End-user search routes should stay unversioned in the URL at this stage, using a simple pattern such as `/api/search/*` rather than reserving `/api/v1/*` from day one.

Developer/admin routes should also stay unversioned in the URL at this stage, using patterns such as `/api/admin/query/*` and `/api/admin/ingestion/*` rather than reserving `/api/v1/*` from day one.

The public backend host should use one shared auth/session boundary for both `/api/search/*` and `/api/admin/*`, with authorization and route policy distinguishing end-user and developer/admin access.

The public backend host may initially delegate to the existing service-side runtime over the cheapest safe path. That can include pass-through forwarding in the short term. The recommendation does not require an immediate deep extraction of all query and ingestion behavior into a brand-new service layer. It does, however, require that the React application sees one public backend boundary rather than multiple direct service origins.

WP121 does not fix a single production deployment model for the React application. The public backend host must support either a same-origin deployment where it also serves the React app or a separate frontend deployment where React is hosted independently but still talks to the same public API boundary.

- `PublicApiHost` is the single browser-facing API composition root.
- It may also serve the React application or its built static assets when deployment convenience warrants it.
- That allowance does not change the primary responsibility of the host, which remains ownership of the public API boundary, auth/session handling, route structure, and browser-facing policy.

### 2.3 Assumptions

- The React application benefits from a single browser-facing origin for configuration, auth, and generated clients.
- Ingestion runtime behavior should remain operationally distinct even after UI consolidation.
- Query capabilities should survive as services and contracts even if `QueryServiceHost` does not survive as a distinct long-term host.
- Introducing a brand-new public backend host is preferable to repurposing one existing runtime host because it keeps public API ownership separate from existing runtime composition roots.
- Proxy/delegation may be an acceptable transition tactic if it buys faster delivery without freezing the wrong public ownership model.
- The public API topology can be fixed without fixing a single production frontend-hosting model yet.
- Retirement-bound UI surfaces may be inspected for behavior reference, but they are not backend candidates by default.

### 2.4 Constraints

- `FileShareEmulator` remains outside the React-facing backend design.
- Retirement-bound UI surfaces must not be revived as the public backend direction.
- Host-local Blazor DTOs must not leak directly into the public API contract surface.
- Final auth model selection is deferred to WP122, but WP121 must choose a topology that supports a single browser-facing boundary cleanly.
- Detailed anonymous-versus-authenticated entry-route policy is deferred to WP122.
- React project placement relative to `src/Hosts/PublicApiHost` is deferred to Arc 03.
- The chosen topology must work whether the React app is served by the public backend host or deployed separately.

## 3. Component / service design (high level)

### 3.1 Components

WP121 defines five high-level elements:

1. React application
   - Calls one public backend host only.

2. Public backend host
   - Owns browser-facing routes, contract shaping, auth boundary, CORS boundary, and public system endpoints.

3. QueryServiceHost
   - May remain a transitional runtime host during migration, but is not assumed to survive as a distinct long-term host if query behavior is absorbed into shared services behind the new public host.

4. IngestionServiceHost
   - Remains the ingestion-side runtime owner behind the public boundary and is expected to stay distinct long term.

5. Provider mechanism
   - Remains runtime extension and provider behavior infrastructure behind the service-side runtime.

### 3.2 Data flows

Recommended external flow:
1. The React application sends all API traffic to the public backend host.
2. The public backend host authenticates, authorizes, shapes, and routes the request.
3. The public backend host delegates to query-side or ingestion-side runtime behavior as required during transition.
4. The public backend host returns the browser-facing response contract.

Example end-user flow:
1. React calls the public host under the end-user route family, such as `/api/search/*`.
2. The public host routes the request to query-side runtime behavior.
3. The public host returns the stable end-user search contract.

Example developer flow:
1. React calls the public host under the developer/admin route family, such as `/api/admin/query/*` or `/api/admin/ingestion/*`, for query diagnostics, rule tooling, or ingestion repair workflows.
2. The public host routes to the appropriate query-side or ingestion-side runtime behavior.
3. The public host applies the correct developer/admin policy boundary and returns the stable developer contract.

### 3.3 Key decisions

- Recommendation: use one public backend host as the sole browser-facing API boundary.
- Recommendation: implement that public backend boundary as a brand-new host project named `PublicApiHost` under `src/Hosts/` rather than by converting `QueryServiceHost` or `IngestionServiceHost` into the browser-facing edge.
- Recommendation: keep `IngestionServiceHost` and the provider mechanism as distinct runtime concerns behind that public boundary rather than exposing them directly to React.
- Recommendation: allow short-term pass-through or proxy-style delegation where it accelerates migration, but treat direct shared-service composition as the preferred end state for query-side behavior.
- Recommendation: do not assume `QueryServiceHost` survives as a distinct long-term host if its useful query runtime behavior can be absorbed cleanly into shared services behind the new public host.
- Recommendation: put end-user and developer/admin APIs on the same public host, with separation achieved from the start through clear route families and authorization rather than multiple public origins by default.
- Recommendation: split the admin route family by domain so query-focused admin APIs and ingestion-focused admin APIs are visibly separated from the start.
- Recommendation: keep provider-related admin and diagnostic endpoints inside the ingestion admin family rather than creating a separate provider-only top-level admin route family.
- Recommendation: keep health, readiness, version, profile, and environment metadata as conventional top-level endpoints on the same host rather than placing them under `/api/system/*`.
- Recommendation: keep end-user search routes unversioned in the URL for now, using `/api/search/*` rather than `/api/v1/search/*`.
- Recommendation: keep developer/admin routes unversioned in the URL for now, using `/api/admin/query/*` and `/api/admin/ingestion/*` rather than `/api/v1/*` forms.
- Recommendation: use one shared auth/session boundary across `/api/search/*` and `/api/admin/*`, with authorization separating the route families rather than separate session mechanisms by default.
- Recommendation: publish one combined API discovery/OpenAPI surface on the public host rather than separate public and admin discovery documents.
- Recommendation: keep the API topology neutral on whether the public backend host also serves the React app in production or whether the frontend is deployed separately.
- Recommendation: leave detailed anonymous-versus-authenticated entry-route rules to WP122 rather than overloading WP121 with auth-policy design.
- Recommendation: leave React project placement and frontend project structure to Arc 03 rather than fixing them in WP121.
- Recommendation: treat `PublicApiHost` as API-first, but allow it to serve the React application or built static assets when deployment convenience warrants it.
- Recommendation: do not use retirement-bound UI surfaces as backend-host candidates, even if they contain source that can inform the new API design.

## 4. Functional requirements

FR1. The consolidated React application shall call one public backend host as its sole browser-facing API origin.

FR2. The public backend host shall be the only approved browser-facing owner of external API routes for the React application.

FR2a. The public backend host shall be introduced as a brand-new project under `src/Hosts/` named `PublicApiHost` rather than by repurposing `QueryServiceHost`, `IngestionServiceHost`, or any retirement-bound UI surface into the browser-facing edge.

FR3. `IngestionServiceHost` shall remain a distinct service-side runtime host behind the public backend boundary.

FR3a. `QueryServiceHost` may remain as a transitional runtime host during migration, but the preferred end state shall be shared-service composition for query-side behavior inside the new public backend host rather than a permanently distinct `QueryServiceHost` host boundary.

FR4. The provider mechanism shall remain behind the service-side runtime boundary rather than being exposed as a direct browser-facing host concern.

FR5. The public backend host shall own browser-facing route structure for end-user search, developer query diagnostics, ingestion tooling, provider tooling, and system endpoints.

FR6. The public backend host shall expose health/profile or equivalent startup metadata endpoints required by the React application.

FR7. The public backend host may initially delegate to the service-side runtime through pass-through or proxy-style behavior where that is the cheapest safe migration path.

FR8. Even when initial delegation is proxy-style, the public backend host shall remain the long-term owner of the browser-facing contract boundary, not just a permanent transparent relay.

FR8a. The preferred end state for query-side integration shall be direct shared-service composition inside the public backend host rather than permanent internal HTTP calls to a distinct `QueryServiceHost`.

FR9. React shall not call `QueryServiceHost` and `IngestionServiceHost` as separate public origins by default.

FR10. Retirement-bound UI surfaces shall not be treated as public backend host candidates by default.

FR11. Host-local Blazor DTOs shall not be promoted directly into the public API surface without deliberate contract review.

FR12. The public backend host shall support both end-user and developer/admin routes, with policy separation handled inside that single public origin unless a later work package proves a split is necessary.

FR12a. The public backend host shall expose clear route families from the start so end-user and developer/admin APIs are visibly separated even though they share one host.

FR12b. The recommended initial route-family pattern is an end-user search family such as `/api/search/*` and a developer/admin family such as `/api/admin/*`.

FR12c. The developer/admin family shall be split by domain from the start, with route families such as `/api/admin/query/*` and `/api/admin/ingestion/*`.

FR12d. Provider-related admin and diagnostic endpoints shall live inside the ingestion admin family, such as `/api/admin/ingestion/*`, rather than under a separate provider-only admin route family.

FR13. `FileShareEmulator` shall remain outside the public backend topology and outside React consolidation.

FR13a. Health, readiness, version, profile, and environment metadata endpoints shall remain as conventional top-level endpoints on the same public host rather than moving under `/api/system/*`.

FR13aa. End-user search routes shall remain unversioned in the URL at this stage, using `/api/search/*` rather than `/api/v1/search/*`.

FR13ab. Developer/admin routes shall remain unversioned in the URL at this stage, using `/api/admin/query/*` and `/api/admin/ingestion/*` rather than `/api/v1/*` forms.

FR13ac. The public backend host shall define one shared auth/session boundary for both `/api/search/*` and `/api/admin/*`, with authorization and route policy distinguishing end-user and developer/admin access.

FR13ad. The public backend host shall publish one combined API discovery/OpenAPI surface rather than separate public and admin discovery documents.

FR13ae. Detailed anonymous-versus-authenticated entry-route policy shall be defined in WP122 rather than in WP121.

FR13af. React project placement and frontend project structure shall be defined in Arc 03 rather than in WP121.

FR13b. The public backend host topology shall support both a same-host deployment model, where the host also serves the React application, and a separate deployment model, where React is hosted independently but calls the same public API boundary.

FR13c. `PublicApiHost` shall remain the single browser-facing API composition root even when it also serves the React application or its built static assets.

FR14. Later work packages shall implement query, ingestion, rules, provider, and repair APIs against the public backend host selected by this specification.

## 5. Non-functional requirements

NFR1. The recommended topology shall minimize browser-facing origin sprawl.

NFR2. The recommended topology shall reduce CORS, redirect URI, and frontend environment configuration complexity compared with multiple direct public service origins.

NFR3. The topology shall keep public contract ownership clear even if internal delegation remains transitional.

NFR4. The topology shall preserve the ability to keep ingestion-side runtime behavior distinct while refactoring query-side behavior toward shared-service composition behind the public boundary.

NFR5. The topology shall not require retirement-bound UI surfaces to become active implementation targets.

NFR6. The topology shall avoid coupling the public API boundary to one mandatory production frontend-hosting model before that deployment decision is explicitly made.

NFR7. Allowing `PublicApiHost` to serve the React application or built static assets shall not weaken its primary responsibility as the public API boundary and browser-facing policy host.

## 6. Data model

WP121 does not define detailed request/response payloads yet. It defines public route ownership groups.

Recommended public route ownership groups:
- End-user search routes, expected under an unversioned family such as `/api/search/*`.
- Developer query diagnostics and query-rule routes, expected under a family such as `/api/admin/query/*`.
- Developer ingestion, provider, journal, failure, and replay routes, expected under a family such as `/api/admin/ingestion/*`.
- System routes such as health, readiness, version, profile, and environment metadata, expected as conventional top-level endpoints on the same host.

Internal ownership expectation:
- Query-side behavior remains owned by query runtime services and should be factored so the new public host can compose it directly in the preferred end state.
- Ingestion-side behavior remains owned by ingestion runtime services and stays behind a distinct `IngestionServiceHost` runtime boundary.
- Provider-specific behavior remains behind the service/runtime boundary.

## 7. Interfaces & integration

### 7.1 Public boundary responsibilities

The public backend host is expected to own:
- public route naming,
- request/response contract shaping,
- authentication entry behavior,
- shared auth/session handling,
- authorization boundary enforcement,
- combined API discovery/OpenAPI publication,
- problem-details behavior,
- and browser-facing endpoint discoverability.

Within the repository structure, `PublicApiHost` is the single browser-facing API composition root for the consolidated React application.

### 7.2 Internal delegation model

WP121 recommends fixing the public boundary before fully fixing the internal delegation mechanism.

Acceptable transitional internal approaches may include:
- delegating over internal HTTP between hosts,
- delegating through extracted shared services,
- or a staged mix while the old browser UI is retired.

The current recommendation does choose a preferred end state among those internal mechanics:
- internal HTTP is acceptable as a transition tactic,
- but direct shared-service composition is preferred for query-side behavior,
- while ingestion remains distinct behind `IngestionServiceHost`.

### 7.3 Disallowed direction

The following directions are not the recommended baseline for this repository:
- exposing React directly to multiple public service origins,
- repurposing `QueryServiceHost` or `IngestionServiceHost` into the long-term browser-facing public edge,
- treating a permanently distinct `QueryServiceHost` host boundary as mandatory when shared query services behind the new public host would suffice,
- reviving retirement-bound UI surfaces as the new browser-facing backend host,
- or treating a transparent proxy as the final architectural explanation rather than a transition tactic.

## 8. Observability (logging/metrics/tracing)

The public backend host should become the main browser-facing observability boundary.

That means later work should be able to attach:
- request correlation,
- route identity,
- user/role boundary checks,
- and stable public endpoint telemetry

at one place rather than across multiple public browser-facing hosts.

Minimal technical observability is deferred to WP125. Business audit is deferred until later hardening work.

## 9. Security & compliance

WP121 recommends a topology that makes later security work simpler:
- one browser-facing origin,
- one shared CORS/auth/session boundary,
- and one place to separate end-user from developer/admin functionality.

Within that shared boundary, authorization rather than separate session mechanisms should distinguish search access from admin access.

The detailed auth mechanism remains an explicit WP122 decision.

## 10. Testing strategy

WP121 validation should focus on design and downstream implementability rather than code execution.

Validation anchors:
- Confirm that the chosen topology is consistent with [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md).
- Confirm that later query, ingestion, provider, and frontend work packages can point to one public backend host as their browser-facing dependency.
- Confirm that the chosen topology does not require retirement-bound UI surfaces to be revived.

## 11. Rollout / migration

The recommended migration posture is staged:
1. Fix one public browser-facing backend host as the target boundary.
2. Allow short-term delegation to existing runtime hosts if that reduces delivery risk.
3. Move query-side behavior toward shared-service composition inside the public host while preserving a distinct ingestion runtime host.
4. Move browser-facing APIs behind the public host.
5. Retire the old non-emulator UI surfaces once replacement React and API workflows exist.

Wiki review result:
No wiki page update was required for this draft work-package specification. Reviewed the current architecture and local-runtime documentation previously used by WP120. WP121 records a proposed backend topology rather than a current-state implementation change.

## 12. Open questions

No open questions remain in WP121 at this stage. The host strategy direction, route families, host naming, auth/session boundary stance, discovery shape, and deployment-model neutrality are now fixed here, while detailed auth policy passes to WP122 and frontend project placement passes to Arc 03.