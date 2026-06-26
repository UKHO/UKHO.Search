# Next-Gen Arc 03 Work Packages: React Plus shadcn/ui Foundation And Keycloak Login

Date: 2026-06-26

Source discussion: [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)  
Source arc summary: [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## Arc Intent

Arc 03 creates the authenticated React plus shadcn/ui foundation that later developer and end-user workspaces consume. It must create the app shell, routing, auth proof, component baseline, editor foundation, API-client pattern, linting, formatting, and frontend test strategy. It must not port Workbench, Radzen, Bootstrap, PrimeReact, or Blazor layout assumptions into React.

## Numbering

Arc 03 work packages use WP140-WP145.

Reserved buffer before Arc 04: WP146-WP159.

## Evidence Checked

- No `package.json` exists in the workspace, so there is no active React/Node app, Tailwind setup, or shadcn/ui baseline.
- Active browser hosts are Blazor/Radzen or Blazor/Bootstrap: [../../src/Hosts/QueryServiceHost/Program.cs](../../src/Hosts/QueryServiceHost/Program.cs), [../../src/Hosts/IngestionServiceHost/Program.cs](../../src/Hosts/IngestionServiceHost/Program.cs), [../../tools/RulesWorkbench/Program.cs](../../tools/RulesWorkbench/Program.cs), and [../../src/Workbench/server/WorkbenchHost/Program.cs](../../src/Workbench/server/WorkbenchHost/Program.cs).
- Shared Keycloak browser-host authentication exists for server-rendered hosts in [../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs](../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs).
- AppHost starts browser-relevant services and Keycloak in services mode: [../../src/Hosts/AppHost/AppHost.cs](../../src/Hosts/AppHost/AppHost.cs).
- Workbench module and shell concepts that should not be mechanically ported are represented by [../../src/Workbench/server/UKHO.Workbench/Modules/WorkbenchContributionRegistry.cs](../../src/Workbench/server/UKHO.Workbench/Modules/WorkbenchContributionRegistry.cs) and [../../src/Workbench/modules/UKHO.Workbench.Modules.Search/SearchWorkbenchModule.cs](../../src/Workbench/modules/UKHO.Workbench.Modules.Search/SearchWorkbenchModule.cs).

## WP140: Scaffold The React Application And Toolchain

Scope:
- Create the React application project and package management baseline in the location chosen by Arc 02.
- Establish TypeScript, routing, build, dev server, tests, linting, formatting, environment configuration, and local Aspire integration expectations.

Requirements carried:
- The repo currently has no package-managed React frontend.
- The app should be a real authenticated application shell, not a landing page or placeholder component demo.
- The app must run locally and expose a usable URL.

Validation anchors:
- Install, build, lint, typecheck, unit test, and local dev-server smoke commands.

## WP141: Establish shadcn/ui, Tailwind, Tokens, And Component Governance

Scope:
- Initialize Tailwind and shadcn/ui, define application-owned component governance, design tokens, theme variables, copied-component update rules, naming conventions, and accessibility baseline.

Requirements carried:
- React plus shadcn/ui is the fixed frontend direction.
- shadcn/ui is the primary component baseline; app-owned workflow components compose from primitives.
- Do not port Radzen, Bootstrap, PrimeReact, Workbench shell widgets, or Blazor layout assumptions.

Validation anchors:
- Component render tests and desktop/mobile visual smoke once Playwright is available.

## WP142: Implement Authenticated Shell And Keycloak Login Proof

Scope:
- Implement the initial authenticated shell according to Arc 02's SPA/API or BFF model.
- Prove login, logout, identity state, protected endpoint access, and error handling.

Requirements carried:
- The app must sign in through Keycloak, hold or refresh identity state according to the chosen auth model, call a protected health/profile endpoint, and render shared navigation/layout conventions.
- Local development must work with Aspire-managed identity services.

Validation anchors:
- Keycloak login smoke where practical and API call test against protected profile/health endpoint.

## WP143: Define Navigation, Layout, And Workspace Conventions

Scope:
- Build shared shell structure for query-rule tuning, ingestion repair, and end-user search workspaces.

Requirements carried:
- Developer UI should be workflow-led, not a port of Workbench module loading or contribution registries.
- Workbench assembly loading, command/menu/status/toolbar contributions, custom splitters, and tab management are non-goals unless a concrete product need appears.

Validation anchors:
- Shell route tests, keyboard navigation checks, and responsive shell smoke.

## WP144: Integrate Editor, JSON, And API Client Foundations

Scope:
- Select and prove Monaco or a comparable editor for JSON-heavy rule workflows.
- Establish generated client, typed fetch, or API-wrapper conventions.

Requirements carried:
- Query-rule and ingestion-rule workflows both need JSON editing.
- Browser must not implement backend rule semantics; it should call validate/evaluate/trace/compare APIs.
- Error payloads and validation details must be displayable without leaking storage/provider internals.

Validation anchors:
- Editor state tests and API-client tests for success, validation, authorization, conflict, and server-error responses.

## WP145: Create Frontend Testing And Quality Gates

Scope:
- Define unit, component, accessibility, Playwright/E2E, lint, formatting, typecheck, build, and local smoke gates.

Requirements carried:
- Later developer workspaces must verify desktop/mobile rendering, keyboard navigation, text fit, non-overlap, endpoint-backed states, and auth/error flows.

Validation anchors:
- Initial frontend build/lint/typecheck/test and Playwright shell smoke when the dev server is available.

## Arc Requirement Cross-Check

- React app structure, Node package setup, TypeScript, routing, shell, local orchestration, scripts, linting, formatting, and tests: WP140, WP145.
- shadcn/ui, Tailwind, tokens, theming, component governance: WP141.
- Avoid Radzen, Bootstrap, PrimeReact, Workbench module mechanics, and Blazor layout assumptions: WP141, WP143.
- Keycloak login and protected endpoint proof: WP142.
- Navigation/layout conventions for later workspaces: WP143.
- Monaco/comparable editor and API-client pattern: WP144.
- Future UI uses backend semantics and stable APIs rather than local rule evaluation or storage details: WP144-WP145.

## Handoff To Arc 04

Arc 04 provides the query and query-rule diagnostics APIs consumed by the React developer query-rule workbench and end-user search surfaces.