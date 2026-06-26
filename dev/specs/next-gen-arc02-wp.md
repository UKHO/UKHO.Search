# Next-Gen Arc 02 Work Packages: API Ownership, Host Strategy, And Security Model

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 02 decides where React-facing APIs live and how they are protected. Later React and workflow packages must not grow against accidental host boundaries, detached historical APIs, or local emulator controls.

## Numbering

Arc 02 work packages use WP120-WP126.

Reserved buffer before Arc 03: WP127-WP139.

## Evidence Checked

- Active Aspire services-mode orchestration is in [../../src/Hosts/AppHost/AppHost.cs](../../src/Hosts/AppHost/AppHost.cs). It starts `IngestionServiceHost`, `QueryServiceHost`, `FileShareEmulator`, `RulesWorkbench`, `WorkbenchHost`, and configuration emulator support; it does not start `StudioServiceHost`.
- Current browser hosts use shared cookie-backed Keycloak authentication through [../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs](../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs) and [../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs](../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs).
- Query and ingestion hosts are Blazor Server composition roots in [../../src/Hosts/QueryServiceHost/Program.cs](../../src/Hosts/QueryServiceHost/Program.cs) and [../../src/Hosts/IngestionServiceHost/Program.cs](../../src/Hosts/IngestionServiceHost/Program.cs).
- Retained Studio API wiring is in [../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs](../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs), with endpoint groups in [../../src/Studio/StudioServiceHost/Api/](../../src/Studio/StudioServiceHost/Api/). It has CORS for `http://localhost:3000`, OpenAPI/Scalar, provider/rules/ingestion/operation endpoints, `AddAuthorization`, and `UseAuthorization`, but no reviewed endpoint-level authenticated policy requirements.
- `StudioServiceHost`, `UKHO.Search.Studio`, and `UKHO.Search.Studio.Providers.FileShare` exist on disk but are not included in [../../Search.slnx](../../Search.slnx) or AppHost.
- FileShareEmulator is local tooling in [../../tools/FileShareEmulator/Program.cs](../../tools/FileShareEmulator/Program.cs). RulesWorkbench is a direct Blazor tool host in [../../tools/RulesWorkbench/Program.cs](../../tools/RulesWorkbench/Program.cs).

## WP120: Confirm Surface Ownership And Active Status

Scope:
- Classify every browser/API-relevant surface as active runtime, detached candidate, local-only emulator, out-of-scope infrastructure, or retirement candidate.

Requirements carried:
- Active services-mode resources are Query, Ingestion, FileShareEmulator, RulesWorkbench, WorkbenchHost, and configuration emulator support.
- `StudioServiceHost` and Studio provider projects are candidate source only until revived, renamed, mined, or deleted by explicit decision.
- FileShareEmulator remains local-development-only and outside React migration.
- The configuration emulator explorer is out of scope and expected to become externalized infrastructure.
- Old Workbench hosts, samples, and Radzen/demo material must not be future UI sources unless a missing behavior is proven.

Validation anchors:
- AppHost and solution participation tests or architecture checks.
- Route inventory checks proving local emulator destructive operations are not React-facing APIs.

## WP121: Choose The React-Facing API Host Strategy

Scope:
- Decide whether React calls existing service hosts directly, a revived/refactored `StudioServiceHost`, a new backend-for-frontend, or separate end-user and developer/tooling API hosts.
- Define route ownership for end-user search, developer query diagnostics, query-rule management, ingestion rules, provider tooling, journal/failure workflows, health/profile, and operational status.

Requirements carried:
- A new React app cannot assume there is already one backend-for-frontend.
- API decisions must come before broad component work.
- Existing Studio endpoints are useful mining material but risky to revive wholesale because the host is detached, locally CORS-bound, file-share-only, and not fully protected.
- Host-local Blazor DTOs must be reviewed before being promoted to API contracts.

Validation anchors:
- Architecture decision record with route map, owning project, auth policy, environment safety classification, OpenAPI/versioning expectations, and rejected alternatives.

## WP122: Define SPA/API Or BFF Authentication And Authorization

Scope:
- Decide the authentication model for the consolidated React app and APIs: BFF with same-site cookies, SPA OIDC with bearer tokens, or a deliberate hybrid.
- Define authorization policies for end-user search, developer diagnostics, rule editing/promotion, ingestion repair, replay, forced replay, and local-only operations.

Requirements carried:
- Current browser hosts use cookie-backed OIDC through the shared `search-workbench` Keycloak client; naming and redirect/origin configuration need redesign for a consolidated app.
- React/API behavior needs deliberate CORS, redirect URI, token/cookie, refresh, logout, and local-development handling.
- Developer/admin operations require explicit endpoint or route-group authorization.
- Destructive, sensitive, replay, repair, forced replay, and rule-promotion operations require authorization and audit decisions before implementation.

Validation anchors:
- Keycloak realm/client tests and endpoint authorization tests for anonymous, authenticated, developer/admin, and forbidden cases.

## WP123: Define Environment Safety And Local-Only Boundaries

Scope:
- Decide which APIs are local-only, which may run in shared/non-local environments, and which require operator roles or audit.

Requirements carried:
- FileShareEmulator controls such as clearing queues, deleting Elasticsearch indexes, resetting local indexing status, and batch zip streaming stay inside the emulator project.
- The configuration emulator explorer is not a React consolidation target.
- Future non-local destructive operations require separate environment and authorization controls.
- Provider handoff failures, ingestion-owned failures, repair replay, and forced replay must be classified for safety.

Validation anchors:
- Environment policy tests where possible and route inventory checks for destructive operations.

## WP124: Define API Contract Governance And Client Strategy

Scope:
- Define API contract standards for explicit request/response models, OpenAPI generation, problem details, versioning, source-generated JSON where appropriate, pagination/filter conventions, and frontend client generation or typed fetch.

Requirements carried:
- React must not infer semantics from final JSON blobs when structured contracts are required.
- API contracts must not leak internal domain entities, provider SQL shapes, storage table keys, blob names, or host-local Blazor state.
- Developer APIs must return task-oriented shapes for failures, shadow inputs, rule evaluations, replay eligibility, and query diagnostics.

Validation anchors:
- Contract tests, OpenAPI snapshot tests, and problem-details tests for validation, auth, not-found, conflict, unsafe replay, and internal errors.

## WP125: Define Observability, Audit, And Operation Tracking Baseline

Scope:
- Establish observability and audit requirements for rule editing, replay, repair, failure workflows, and long-running operations.

Requirements carried:
- Retained Studio operation tracking and SSE are candidate material, but the in-memory store and single-active-operation lock need review.
- Rule saves/promotions, diagnostic replay, guarded repair replay, forced replay, and environment-sensitive operations need traceable audit records.
- Health/readiness and environment metadata are required for UI startup and diagnostics.

Validation anchors:
- Tests for audit record emission and protected health/profile endpoints once implemented.

## WP126: Decide Retained Studio Source Disposition

Scope:
- Decide whether retained Studio code is revived as the developer API host, mined into new active projects, renamed/reframed, or left historical until retirement.

Requirements carried:
- Studio exposes provider discovery, rule discovery, ingestion operations, operation status, and SSE endpoints that are closer to developer APIs than Workbench shell code.
- Reviving it wholesale would reintroduce retired Studio/Theia surface area unless explicitly redefined.
- The retained FileShare Studio provider directly queries emulator SQL and writes to a hardcoded file-share queue, so it is not provider-neutral without refactoring.

Validation anchors:
- Decision record with source movement and test migration plan.

## Arc Requirement Cross-Check

- BFF/direct API and host ownership decision: WP121.
- Revive/refactor Studio versus new API host versus existing service-host APIs: WP121, WP126.
- Active Blazor hosts, detached Studio API, local-only emulator controls, and server-rendered auth accounted for: WP120-WP123.
- Keycloak, CORS, auth, authorization, environment safety, audit: WP122-WP125.
- FileShareEmulator and configuration emulator excluded from React consolidation: WP120, WP123.
- Local destructive operations remain local: WP123.
- API contract governance for later query, ingestion, rules, provider, repair, and frontend clients: WP124.
- Retained Studio source status resolved before React couples to it: WP126.

## Handoff To Arc 03

Arc 03 can scaffold the React application only after this arc identifies the auth model, API host topology, and protected health/profile endpoint that the frontend will prove against.