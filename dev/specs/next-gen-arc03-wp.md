# Next-Gen Arc 03 Work Packages: Blazor Blueprint Foundations And Keycloak Login

Date: 2026-07-01

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 03 creates the split Interactive Server Blazor foundation that later internal and public workspaces consume. It must establish the Blazor Blueprint component baseline, host shells, routing, auth proof, editor foundation, and testing approach for the public `QueryServiceHost` direction and the new internal `WorkbenchHost` direction.

It must not port the deleted legacy Workbench shell architecture, and it should not preserve Radzen- or legacy-shell-specific mechanics unless a concrete requirement survives later review.

## Numbering

Arc 03 work packages use WP140-WP145.

Reserved buffer before Arc 04: WP146-WP159.

## Evidence Checked

- Active browser hosts are currently Blazor-based: [../../src/Hosts/QueryServiceHost/Program.cs](../../src/Hosts/QueryServiceHost/Program.cs), [../../src/Hosts/IngestionServiceHost/Program.cs](../../src/Hosts/IngestionServiceHost/Program.cs), and [../../tools/RulesWorkbench/Program.cs](../../tools/RulesWorkbench/Program.cs).
- Shared Keycloak browser-host authentication already exists for server-rendered hosts in [../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs](../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs).
- Blazor Blueprint supports Interactive Server render mode, theme.css-based theming, and optional Tailwind utility compilation for custom host code.
- The old Workbench shell mechanics under `src/Workbench/` are legacy and should not be reused mechanically.

## WP140: Establish Shared Browser Host Foundations

Scope:
- Define the shared host bootstrap conventions for QueryServiceHost uplift and the new internal WorkbenchHost.
- Establish common layout, static asset, theme, provider, and service-registration expectations.

Requirements carried:
- The direction is Interactive Server Blazor, not React.
- Both browser hosts must support real authenticated shells, not placeholder pages.

Validation anchors:
- Host startup smoke, static asset checks, and shared layout verification.

## WP141: Establish Blazor Blueprint, Theme Tokens, And Component Governance

Scope:
- Add Blazor Blueprint as the component baseline.
- Define theme.css ownership, shared token conventions, optional Tailwind build guidance, and component-governance rules.

Requirements carried:
- Do not mechanically port Radzen, Bootstrap, legacy Workbench widgets, or old shell mechanics.
- Shared look-and-feel should come from Blazor Blueprint plus host-owned workflow components.

Validation anchors:
- Component render tests and desktop/mobile visual smoke once Playwright is available.

## WP142: Prove QueryServiceHost Public Shell And Login Flow

Scope:
- Establish the customer-facing QueryServiceHost shell conventions.
- Prove login, logout, session behavior, protected endpoint access, and public-search shell behavior.

Requirements carried:
- The public host must support the long-term end-user search experience.
- Local development must work with Aspire-managed identity services.

Validation anchors:
- Keycloak login smoke and protected public-host endpoint checks.

## WP143: Prove The New Internal WorkbenchHost Shell And Login Flow

Scope:
- Create the new internal `WorkbenchHost` as a clean host under `src/Hosts/`.
- Prove internal login/logout/session behavior, navigation, and shell conventions without importing the deleted legacy Workbench architecture.

Requirements carried:
- The internal host is permanently internal.
- It should feel like a focused operations and developer tool host, not a recreation of the deleted module shell.

Validation anchors:
- Keycloak login smoke, route/navigation checks, and protected internal-host endpoint checks.

## WP144: Integrate Editor, JSON, And Host Interaction Foundations

Scope:
- Select and prove Monaco or a comparable editor for JSON-heavy rule workflows.
- Define how the hosts call backend services or deliberate HTTP endpoints without reintroducing browser-side rule semantics.

Requirements carried:
- Query-rule and ingestion-rule workflows both need JSON editing.
- Browser hosts must present backend-owned semantics rather than recreating them locally.

Validation anchors:
- Editor state tests and success/validation/error interaction checks.

## WP145: Create Browser Host Testing And Quality Gates

Scope:
- Define unit, accessibility, Playwright/E2E, lint/build, and host smoke gates for the public and internal browser hosts.

Requirements carried:
- Later developer workspaces must verify desktop/mobile rendering, keyboard navigation, text fit, non-overlap, endpoint-backed states, and auth/error flows.

Validation anchors:
- Initial build/test/smoke gates for the browser hosts.

## Arc Requirement Cross-Check

- Shared Blazor host foundations: WP140.
- Blazor Blueprint, theming, and component governance: WP141.
- QueryServiceHost public shell and Keycloak proof: WP142.
- New WorkbenchHost internal shell and Keycloak proof: WP143.
- Monaco/comparable editor and host interaction foundation: WP144.
- Browser host testing and quality gates: WP145.

## Handoff To Arc 04

Arc 04 provides the query and query-rule diagnostics capabilities consumed by the new internal `WorkbenchHost` workbench and by the public `QueryServiceHost` search experience.