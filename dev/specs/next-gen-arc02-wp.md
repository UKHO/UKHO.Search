# Next-Gen Arc 02 Work Packages: Browser Host Ownership, Audience Split, And Security Model

Date: 2026-07-01

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 02 fixes the permanent browser-host topology and the associated security model.

The direction is now:

- `QueryServiceHost` remains the customer-facing search host.
- a new `WorkbenchHost` becomes the permanently internal developer and admin search workbench.
- `IngestionServiceHost` remains an ingestion/runtime host rather than a future browser product host.
- `FileShareEmulator` stays local-only and untouched.
- the old Workbench tree under `src/Workbench/` is legacy code, not migration input, and should be removed before the new internal `WorkbenchHost` is introduced.

## Numbering

Arc 02 work packages use WP120-WP126.

Reserved buffer before Arc 03: WP127-WP139.

## Evidence Checked

- Active Aspire services-mode orchestration is in [../../src/Hosts/AppHost/AppHost.cs](../../src/Hosts/AppHost/AppHost.cs). It starts `IngestionServiceHost`, `QueryServiceHost`, `FileShareEmulator`, `RulesWorkbench`, and configuration emulator support; it does not start `StudioServiceHost`.
- Current browser hosts use shared cookie-backed Keycloak authentication through [../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs](../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs) and [../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs](../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs).
- Query and ingestion hosts are Blazor Server composition roots in [../../src/Hosts/QueryServiceHost/Program.cs](../../src/Hosts/QueryServiceHost/Program.cs) and [../../src/Hosts/IngestionServiceHost/Program.cs](../../src/Hosts/IngestionServiceHost/Program.cs).
- The legacy Workbench implementation that previously lived under `src/Workbench/` has been removed by WP126 and is no longer part of the maintained runtime or solution.
- Retained Studio API wiring is in [../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs](../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs), with endpoint groups in [../../src/Studio/StudioServiceHost/Api/](../../src/Studio/StudioServiceHost/Api/). It has CORS for `http://localhost:3000`, OpenAPI/Scalar, provider/rules/ingestion/operation endpoints, `AddAuthorization`, and `UseAuthorization`, but no reviewed endpoint-level authenticated policy requirements.
- `StudioServiceHost`, `UKHO.Search.Studio`, and `UKHO.Search.Studio.Providers.FileShare` exist on disk but are not included in [../../Search.slnx](../../Search.slnx) or AppHost.
- FileShareEmulator is local tooling in [../../tools/FileShareEmulator/Program.cs](../../tools/FileShareEmulator/Program.cs). RulesWorkbench is a direct Blazor tool host in [../../tools/RulesWorkbench/Program.cs](../../tools/RulesWorkbench/Program.cs).

## WP120: Confirm Surface Ownership, Active Status, And Legacy Workbench Disposition

Scope:
- Classify every browser/API-relevant surface as active runtime, public host, internal host target, local-only tool, out-of-scope infrastructure, or retirement/delete candidate.

Requirements carried:
- `QueryServiceHost` remains the public host target.
- `IngestionServiceHost` remains the ingestion/runtime host.
- `FileShareEmulator` remains local-development-only.
- `tools/RulesWorkbench` remains a temporary legacy tool to be replaced, not a future platform direction.
- the old Workbench tree under `src/Workbench/` is a delete-first candidate and must not be treated as future-source material.

Validation anchors:
- AppHost and solution participation checks.
- Route inventory checks proving local emulator destructive operations are not promoted.

## WP121: Choose The Split Browser Host Strategy

Scope:
- Fix the permanent audience split and define which host owns which workflows.
- Define route and API ownership between `QueryServiceHost`, the new `WorkbenchHost`, and the retained runtime hosts.

Requirements carried:
- `QueryServiceHost` is the customer-facing search host.
- `WorkbenchHost` is the permanently internal developer/admin workbench.
- `IngestionServiceHost` remains a runtime host rather than a product UI host.
- the new `WorkbenchHost` must not inherit the old Workbench shell by default.

Validation anchors:
- Architecture decision record with route map, owning project, auth policy, capability boundary assumptions, OpenAPI/versioning expectations, and rejected alternatives.

## WP122: Define Browser Host Authentication And Authorization

Scope:
- Decide the auth/session posture for the public and internal browser hosts.
- Define authorization boundaries for end-user search, internal diagnostics, rule editing, replay, repair, and other operational actions.

Requirements carried:
- Current browser hosts already use cookie-backed OIDC through shared service defaults.
- Public and internal hosts may use the same identity realm, but they must not be treated as one undifferentiated audience.
- Server-side claims-based filtering remains mandatory for search.

Validation anchors:
- Keycloak client and route-authorization tests for public, internal, forbidden, and logout flows.

## WP123: Define Capability Boundaries And Local-Only Exceptions

Scope:
- Decide which capabilities belong in `QueryServiceHost`, which belong in `WorkbenchHost`, which stay inside `IngestionServiceHost`, and which remain local-only exceptions.

Requirements carried:
- FileShareEmulator controls such as clearing queues, deleting Elasticsearch indexes, resetting local indexing status, and batch zip streaming stay inside the emulator project.
- The configuration emulator explorer is not a product-host target.
- Internal tooling capabilities belong in `WorkbenchHost`, not in the public host.

Validation anchors:
- Capability inventory and host-boundary checks proving local-only controls stay out of the product hosts.

## WP124: Define API Contract Governance And Host Integration Strategy

Scope:
- Define contract rules for public and internal host endpoints.
- Define when a browser host should expose deliberate HTTP contracts versus when server-side composition is acceptable.

Requirements carried:
- Public search contracts must be explicit and stable.
- Internal workbench endpoints must still use deliberate contracts when they cross host or browser boundaries.
- Host-local Blazor DTOs, provider SQL shapes, storage keys, and legacy shell state must not leak into deliberate contracts.

Validation anchors:
- Contract tests, Problem Details tests, and host-integration design review.

## WP125: Define Minimal Technical Observability Baseline

Scope:
- Establish the minimum technical observability required for `QueryServiceHost` and the new `WorkbenchHost` startup, diagnostics, and debugging without defining business audit requirements yet.

Requirements carried:
- Health/readiness and minimal technical system metadata are required for UI startup and diagnostics.
- Request correlation, authorization-failure visibility, and basic route-level diagnostics must be possible at both hosts.
- Business audit remains deferred.

Validation anchors:
- Smoke checks for health/readiness/metadata and telemetry-path participation once implemented.

## WP126: Delete The Legacy Workbench First

Scope:
- Remove the old Workbench host, libraries, modules, samples, and vendored support code under `src/Workbench/` from the active solution and runtime before the new internal `WorkbenchHost` is introduced.

Requirements carried:
- remove the legacy Workbench host from AppHost,
- remove old Workbench projects from the maintained solution,
- delete the old Workbench tree rather than leaving a parallel meaning of `WorkbenchHost` in place,
- and update planning/docs so the future internal host name is unambiguous.

Validation anchors:
- Solution and AppHost reference checks proving the legacy Workbench tree is no longer active.

## Arc Requirement Cross-Check

- Public versus internal browser-host ownership: WP121.
- Active runtime hosts, legacy Workbench deletion, local-only emulator controls, and detached Studio code: WP120 and WP126.
- Keycloak, auth, authorization, audience separation, and server-side claims filtering: WP122.
- Capability boundaries and local-only exceptions: WP123.
- Contract governance and host integration rules: WP124.
- Minimal technical observability: WP125.

## Handoff To Arc 03

Arc 03 can establish the new Blazor Blueprint foundations only after this arc fixes the permanent host topology, audience split, auth posture, capability boundaries, and the deletion-first removal of the legacy Workbench tree.