# Next-Gen Arc 02 Work Packages: API Ownership, Host Strategy, And Security Model

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 02 decides where the consolidated React application gets its backend APIs and how those APIs are protected. The retained service-side runtime is `IngestionServiceHost`, `QueryServiceHost`, and the provider mechanism. `FileShareEmulator` stays local-only and untouched. Other UI surfaces are retirement-bound and must not be treated as future platform direction, although they remain available for source inspection and should only receive minimal edits when broader build/solution changes force them.

## Numbering

Arc 02 work packages use WP120-WP125.

Reserved buffer before Arc 03: WP126-WP139.

## Evidence Checked

- Active Aspire services-mode orchestration is in [../../src/Hosts/AppHost/AppHost.cs](../../src/Hosts/AppHost/AppHost.cs). It starts `IngestionServiceHost`, `QueryServiceHost`, `FileShareEmulator`, `RulesWorkbench`, `WorkbenchHost`, and configuration emulator support; it does not start `StudioServiceHost`.
- Current browser hosts use shared cookie-backed Keycloak authentication through [../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs](../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs) and [../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs](../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs).
- Query and ingestion hosts are Blazor Server composition roots in [../../src/Hosts/QueryServiceHost/Program.cs](../../src/Hosts/QueryServiceHost/Program.cs) and [../../src/Hosts/IngestionServiceHost/Program.cs](../../src/Hosts/IngestionServiceHost/Program.cs).
- Retained Studio API wiring is in [../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs](../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs), with endpoint groups in [../../src/Studio/StudioServiceHost/Api/](../../src/Studio/StudioServiceHost/Api/). It has CORS for `http://localhost:3000`, OpenAPI/Scalar, provider/rules/ingestion/operation endpoints, `AddAuthorization`, and `UseAuthorization`, but no reviewed endpoint-level authenticated policy requirements.
- `StudioServiceHost`, `UKHO.Search.Studio`, and `UKHO.Search.Studio.Providers.FileShare` exist on disk but are not included in [../../Search.slnx](../../Search.slnx) or AppHost.
- FileShareEmulator is local tooling in [../../tools/FileShareEmulator/Program.cs](../../tools/FileShareEmulator/Program.cs). RulesWorkbench is a direct Blazor tool host in [../../tools/RulesWorkbench/Program.cs](../../tools/RulesWorkbench/Program.cs).

## WP120: Confirm Surface Ownership And Active Status

Scope:
- Classify every browser/API-relevant surface as active service-side runtime, local-only emulator/tooling, out-of-scope infrastructure, or retirement candidate.

Requirements carried:
- Active services-mode resources are Query, Ingestion, FileShareEmulator, RulesWorkbench, WorkbenchHost, and configuration emulator support.
- The retained service-side baseline is Query, Ingestion, and the provider mechanism.
- FileShareEmulator remains local-development-only and outside React migration.
- The configuration emulator explorer is out of scope and expected to become externalized infrastructure.
- Old Workbench hosts, RulesWorkbench, retained Studio surfaces, samples, and Radzen/demo material are retirement-bound rather than future UI sources. They remain inspectable reference material but should not be modified except for explicit retirement work or minimal build-compatibility changes.

Validation anchors:
- AppHost and solution participation tests or architecture checks.
- Route inventory checks proving local emulator destructive operations are not React-facing APIs.

## WP121: Choose The React-Facing API Host Strategy

Scope:
- Decide whether React calls existing service hosts directly, a new backend-for-frontend, or separate end-user and developer/tooling API hosts.
- Define route ownership for end-user search, developer query diagnostics, query-rule management, ingestion rules, provider tooling, journal/failure workflows, health/profile, and operational status.

Requirements carried:
- A new React app cannot assume there is already one backend-for-frontend.
- API decisions must come before broad component work.
- Retirement-bound UI surfaces are not backend candidates by default, even if they currently contain related browser or API behavior.
- Host-local Blazor DTOs must be reviewed before being promoted to API contracts.

Validation anchors:
- Architecture decision record with route map, owning project, auth policy, capability boundary assumptions, OpenAPI/versioning expectations, and rejected alternatives.

## WP122: Define SPA/API Or BFF Authentication And Authorization

Scope:
- Decide the authentication model for the consolidated React app and APIs: BFF with same-site cookies, SPA OIDC with bearer tokens, or a deliberate hybrid.
- Define authorization policies for end-user search, developer diagnostics, rule editing/promotion, ingestion repair, replay, forced replay, and local-only operations.

Requirements carried:
- Current browser hosts use cookie-backed OIDC through the shared `search-workbench` Keycloak client; naming and redirect/origin configuration need redesign for a consolidated app.
- React/API behavior needs deliberate CORS, redirect URI, token/cookie, refresh, logout, and local-development handling.
- Developer/admin operations require explicit endpoint or route-group authorization.
- Destructive, sensitive, replay, repair, forced replay, and rule-promotion operations require authorization decisions before implementation; business audit is deferred until later requirements are clearer.

Validation anchors:
- Keycloak realm/client tests and endpoint authorization tests for anonymous, authenticated, developer/admin, and forbidden cases.

## WP123: Define Capability Boundaries And Local-Only Exceptions

Scope:
- Decide which capabilities belong in `PublicApiHost`, which remain local-only exceptions, and which stay out of scope without using environment as the main design axis.

Requirements carried:
- FileShareEmulator controls such as clearing queues, deleting Elasticsearch indexes, resetting local indexing status, and batch zip streaming stay inside the emulator project.
- The configuration emulator explorer is not a React consolidation target.
- PublicApiHost capabilities are assumed available in all environments for now.
- Provider handoff failures, ingestion-owned failures, repair replay, and forced replay must be classified as platform capabilities, not environment-specific exceptions, in the current phase.

Validation anchors:
- Route inventory and host-boundary checks proving `FileShareEmulator`-only controls stay outside the public platform surface.

## WP124: Define API Contract Governance And Client Strategy

Scope:
- Define API contract standards for explicit request/response models, OpenAPI generation, problem details, versioning, source-generated JSON where appropriate, pagination/filter conventions, and frontend client generation or typed fetch.

Requirements carried:
- React must not infer semantics from final JSON blobs when structured contracts are required.
- API contracts must not leak internal domain entities, provider SQL shapes, storage table keys, blob names, or host-local Blazor state.
- Developer APIs must return task-oriented shapes for failures, shadow inputs, rule evaluations, replay eligibility, and query diagnostics.

Validation anchors:
- Contract tests, OpenAPI snapshot tests, and problem-details tests for validation, auth, not-found, conflict, unsafe replay, and internal errors.

## WP125: Define Minimal Technical Observability Baseline

Scope:
- Establish the minimum technical observability required for `PublicApiHost` startup, diagnostics, and debugging without defining business audit requirements yet.

Requirements carried:
- Health/readiness and minimal technical system metadata are required for UI startup and diagnostics.
- Request correlation, authorization-failure visibility, and basic route-level diagnostics must be possible at `PublicApiHost`.
- Business audit and operation-tracking requirements are explicitly deferred until the new React platform is working and business requirements are clearer.

Validation anchors:
- Tests or smoke checks for health/readiness/profile-version style endpoints and basic technical diagnostics once implemented.

## Arc Requirement Cross-Check

- BFF/direct API and host ownership decision: WP121.
- Active service-side hosts, retirement-bound UI surfaces, local-only emulator controls, and server-rendered auth accounted for: WP120-WP123.
- Keycloak, CORS, auth, authorization, capability boundaries, and minimal technical observability: WP122-WP125.
- FileShareEmulator and configuration emulator excluded from React consolidation: WP120, WP123.
- Local destructive operations remain local: WP123.
- API contract governance for later query, ingestion, rules, provider, repair, and frontend clients: WP124.
- Retained Studio and other non-emulator UI surfaces are fixed as retirement-bound by WP120 and must not be treated as future platform direction.

## Handoff To Arc 03

Arc 03 can scaffold the React application only after this arc identifies the auth model, API host topology, and protected health/profile endpoint that the frontend will prove against.