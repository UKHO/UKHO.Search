# Specification: WP124 API Contract Governance And Client Strategy

Target output path: `dev/work-packages/124-api-contract-governance-client-strategy/spec-domain-api-contract-governance-client-strategy.md`

Date: 2026-07-01

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md)
- [../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md](../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md)
- [../123-capability-boundaries-local-only-exceptions/spec-domain-capability-boundaries-local-only-exceptions.md](../123-capability-boundaries-local-only-exceptions/spec-domain-capability-boundaries-local-only-exceptions.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the contract-governance baseline for `PublicApiHost` and the initial client-consumption strategy for the new React application.

The current recommendation is to use a hand-written typed fetch layer for all `PublicApiHost` APIs in the first phase, including both `/api/search/*` and `/api/admin/*`. Generated clients should not be the initial default because the query and admin API surfaces are still evolving. Client generation may be adopted later, once the new application is working and the APIs have stabilized.

### 1.2 Scope

In scope for WP124:
- Define the initial frontend client-consumption strategy.
- Define the baseline contract-governance rules for `PublicApiHost` APIs.
- Define the rule that public contracts must be deliberate and must not leak host-local Blazor models or internal storage shapes.
- Define high-level expectations for error shape, request/response explicitness, and later OpenAPI use.

Out of scope for WP124:
- Implementing the APIs.
- Final frontend project structure.
- Detailed authentication model, which is defined in WP122.
- Environment-specific release management.
- Business audit requirements.

### 1.3 Stakeholders

- Frontend authors who need a usable and maintainable client-consumption strategy.
- Backend authors who need a clear rule for what becomes a public API contract.
- Platform owners who need a stable path from early iteration to later contract hardening.
- Later arc work packages that will implement search, admin query, and admin ingestion/provider APIs.

### 1.4 Definitions

- Public API contract: The explicit request/response shape exposed by `PublicApiHost`.
- Hand-written typed fetch: A frontend client layer written manually in TypeScript, with explicit local typing and request/response handling.
- Generated client: A client produced from OpenAPI or an equivalent contract document.
- Contract drift: A mismatch between backend API behavior and the client assumptions consuming it.
- Host-local model: A model currently shaped for a retiring Blazor host or other implementation-local behavior rather than for stable public API use.

## 2. System context

### 2.1 Current state

The repository does not yet have stable `PublicApiHost` APIs or an active generated-client pipeline for the new React application.

Evidence checked:
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md) fixes `PublicApiHost` as the single public API boundary.
- [../../../src/Hosts/QueryServiceHost/Program.cs](../../../src/Hosts/QueryServiceHost/Program.cs) and [../../../src/Hosts/IngestionServiceHost/Program.cs](../../../src/Hosts/IngestionServiceHost/Program.cs) remain current runtime hosts with retiring browser UI, not the final public API composition root.
- OpenAPI is currently visible in local/retained surfaces such as [../../../tools/FileShareEmulator/Program.cs](../../../tools/FileShareEmulator/Program.cs) and [../../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs](../../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs), but not in the active query or ingestion browser/runtime hosts that are being replaced.
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md) requires explicit request/response contracts and says React must not infer semantics from final JSON blobs when structured contracts are required.

The current risk is premature formalization in the wrong place: generating frontend clients too early from unstable or transitional shapes, or leaking host-local models into the public API boundary just because they exist already.

### 2.2 Proposed state

The recommended direction is:
- `PublicApiHost` owns deliberate public API contracts,
- the React application uses a hand-written typed fetch layer for all APIs in the first phase,
- OpenAPI remains important as documentation and later governance material,
- and generated clients remain a later option once the public APIs stabilize.

This keeps the first implementation flexible while still requiring explicit public contracts and preventing accidental leakage of internal model shapes.

### 2.3 Assumptions

- The query APIs are not stable enough yet to justify generated clients as the default.
- The admin APIs are even less stable and should not be locked into generation-first consumption.
- The team wants strong contract discipline, but not at the cost of making early API iteration painful.
- Later migration from hand-written typed fetch to generated clients should remain possible if the APIs stabilize.

### 2.4 Constraints

- `PublicApiHost` remains the only public browser-facing API surface.
- Route families, auth/session boundary, and capability boundaries are already fixed by WP121-WP123.
- Public API contracts must not expose host-local Blazor DTOs, internal storage table keys, provider SQL shapes, blob names, or equivalent backend implementation leakage.

## 3. Component / service design (high level)

### 3.1 Components

WP124 defines four high-level deliverables:

1. Public contract rules
   - Explicit request/response contracts for `PublicApiHost`.

2. Client-consumption strategy
   - Hand-written typed fetch as the initial client pattern for all route families.

3. OpenAPI governance baseline
   - OpenAPI as a contract-governance and documentation tool, not yet as the default client-generation source.

4. Contract leak-prevention rules
   - Prevent host-local or storage-local model shapes from escaping into public APIs.

### 3.2 Data flows

Initial client flow:
1. The React application calls a hand-written typed fetch layer.
2. The typed fetch layer calls `PublicApiHost`.
3. `PublicApiHost` returns explicit public contracts.
4. The React application consumes those typed results without depending on generated client code.

Later hardening path:
1. APIs stabilize.
2. OpenAPI documents become reliable enough for generation-first consumption.
3. The client strategy may be revisited and generation may replace or augment hand-written fetch.

### 3.3 Key decisions

- Recommendation: use a hand-written typed fetch layer for all `PublicApiHost` APIs in the initial phase.
- Recommendation: do not use generated clients as the initial default because the public API surfaces are still evolving.
- Recommendation: keep OpenAPI as part of contract governance and documentation even before it becomes the basis for generated clients.
- Recommendation: require explicit public request/response models rather than letting React infer semantics from ad hoc JSON.
- Recommendation: standardize on RFC 9457-style Problem Details for public API error responses from the start.
- Recommendation: prohibit leakage of host-local Blazor models and backend implementation shapes into public contracts.
- Recommendation: do not lock one pagination convention yet for list-style admin and diagnostic APIs; defer that detail until the list workloads and UX needs are clearer.
- Recommendation: revisit generated-client adoption after the React app is working and the APIs are genuinely stable.

## 4. Functional requirements

FR1. `PublicApiHost` APIs shall expose explicit public request and response contracts.

FR2. The initial React client strategy shall use a hand-written typed fetch layer for all `PublicApiHost` APIs.

FR3. Generated clients shall not be the default client-consumption model in the first implementation phase.

FR4. OpenAPI shall remain part of the contract-governance and documentation baseline even when generated clients are not yet used as the default consumption model.

FR5. Public contracts shall not leak host-local Blazor DTOs, internal domain persistence details, provider SQL shapes, storage table keys, blob names, or similar implementation-local data structures.

FR6. End-user search and admin APIs shall both follow the same explicit-contract rule even if their internal rate of change differs.

FR7. The client strategy shall allow a later shift to generated clients after the APIs stabilize.

FR8. Error responses shall use one deliberate public error shape rather than ad hoc endpoint-specific payloads.

FR8a. The deliberate public error shape shall use RFC 9457-style Problem Details from the start.

FR9. Public API behavior shall not rely on the frontend reverse-engineering meaning from final JSON blobs when structured contracts are required.

FR9a. WP124 shall not fix one mandatory pagination convention for list-style admin and diagnostic APIs yet.

## 5. Non-functional requirements

NFR1. The initial client strategy shall optimize for API evolution speed without abandoning type safety.

NFR2. The contract-governance rules shall reduce accidental contract drift between frontend and backend.

NFR3. The contract-governance rules shall preserve a later path to stronger OpenAPI-driven automation.

NFR4. Public contract design shall remain understandable to frontend authors without exposing backend implementation detail.

NFR5. The approach shall avoid locking the platform into generation-first tooling before the API shapes have proven stable.

NFR6. The contract-governance baseline shall avoid premature standardization of pagination conventions before the real list workloads and UX requirements are clearer.

## 6. Data model

WP124 does not define every concrete payload yet. It defines the categories of public contract shape that later APIs must follow.

Required contract categories:
- end-user search request/response shapes,
- admin query diagnostics request/response shapes,
- admin ingestion/provider/journal/failure/replay request/response shapes,
- system endpoint response shapes,
- and common RFC 9457-style error/problem shapes.

Client-side expectation:
- the React app owns a local typed client layer that maps those categories explicitly,
- without assuming generated code from OpenAPI in the first phase.

## 7. Interfaces & integration

### 7.1 Public contract rules

Every `PublicApiHost` route should be treated as an intentional public contract surface rather than a host-local convenience endpoint.

### 7.2 Client strategy rules

The initial frontend integration rules are:
- hand-written typed fetch for `/api/search/*`,
- hand-written typed fetch for `/api/admin/query/*`,
- hand-written typed fetch for `/api/admin/ingestion/*`,
- and explicit local typing for error and system endpoints.

### 7.3 OpenAPI role

OpenAPI should be used initially for:
- documentation,
- contract inspection,
- and later governance/testing.

It should not yet be treated as the mandatory source for generated frontend clients.

## 8. Observability (logging/metrics/tracing)

WP124 does not define the technical observability baseline, but contract-governance work should support it indirectly by ensuring:
- error shapes are deliberate,
- route shapes are stable enough to identify,
- and client failures are diagnosable from explicit contracts.

Detailed technical observability remains part of WP125.

## 9. Security & compliance

WP124 does not redefine the auth model from WP122.

Its security contribution is contract hygiene:
- do not leak internal identifiers or storage-local detail,
- do not force the client to infer secure behavior from unstable payloads,
- and keep the API surface explicit enough for later authorization and filtering rules to remain understandable.

## 10. Testing strategy

WP124 validation should focus on contract quality and client-strategy suitability.

Validation anchors:
- Confirm the chosen strategy is consistent with WP121 route families and WP122 auth assumptions.
- Confirm the chosen strategy does not require generated clients before the APIs stabilize.
- Confirm later work can add OpenAPI snapshots, Problem Details tests, and typed client tests without redesigning the baseline.

## 11. Rollout / migration

Recommended migration posture:
1. Define explicit public contracts for `PublicApiHost` APIs.
2. Consume them with hand-written typed fetch in the React app.
3. Use OpenAPI for documentation and contract governance while the API surfaces settle.
4. Reassess generated clients only after the application is working and the APIs are genuinely stable.

Wiki review result:
No wiki page update was required for this draft work-package specification. The work records contract-governance and client-strategy decisions rather than a current-state runtime change.

## 12. Open questions

No open questions remain in WP124 at this stage. The initial client strategy, public error shape, and contract-leak prevention rules are fixed here, while generated clients and pagination standardization are intentionally deferred until the APIs and UI workflows are more stable.