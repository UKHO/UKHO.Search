# Specification: QueryServiceHost Keycloak authentication alignment

- **Work package**: `093-query-auth`
- **Document**: `docs/093-query-auth/spec-host-query-auth_v0.01.md`
- **Version**: `v0.01`
- **Date**: `2026-04-15`

## 1. Overview

`QueryServiceHost` is currently an interactive Blazor host, but it does not yet consume the repository's shared Keycloak browser-host authentication model. The repository already has an established pattern in `IngestionServiceHost` and `WorkbenchHost`, implemented through the shared authentication composition in `src/Hosts/UKHO.Search.ServiceDefaults`.

This work package aligns `QueryServiceHost` with that same shared Keycloak approach so the query UI behaves consistently with the other browser-facing hosts in the local Aspire environment.

The work package also covers two related source-of-truth concerns:

1. the checked-in Keycloak realm export must include the exact local callback/origin URLs required by `QueryServiceHost`; and
2. the repository must explicitly allow the `launchSettings.json` files for both `QueryServiceHost` and `IngestionServiceHost` to remain committed in source control.

For the avoidance of doubt, this specification treats `IngestionServiceHost` as the existing reference implementation for the requested host-authentication behavior. That is the host in the current repository that already demonstrates the shared Keycloak integration pattern to be mirrored by `QueryServiceHost`.

### 1.1 Business intent

The query UI should not be the odd browser host out. A developer or tester starting the local environment should encounter one consistent authentication story across the browser-facing services:

- unauthenticated access redirects through Keycloak;
- authenticated access restores the local browser session through the ASP.NET Core cookie scheme; and
- role and claims handling remains consistent because every host uses the same shared authentication composition.

### 1.2 Components and files expected to be impacted

At minimum, the implementation is expected to touch or verify the following areas:

- `src/Hosts/QueryServiceHost/Program.cs`
- `src/Hosts/QueryServiceHost/Components/Routes.razor`
- `src/Hosts/QueryServiceHost/Components/Authentication/*` (new host-local redirect component, if required)
- `src/Hosts/AppHost/Realms/ukho-search-realm.json`
- `.gitignore`
- `src/Hosts/QueryServiceHost/Properties/launchSettings.json`
- `src/Hosts/IngestionServiceHost/Properties/launchSettings.json`
- `test/QueryServiceHost.Tests/*`
- `test/AppHost.Tests/*`

## 2. Scope

### 2.1 In scope

1. Update `QueryServiceHost` to use the shared Keycloak browser-host authentication path already used by `IngestionServiceHost`.
2. Ensure `QueryServiceHost` exposes the same shared authentication lifecycle endpoints under `/authentication` via the shared service-defaults extension.
3. Ensure the `QueryServiceHost` Blazor routing surface is authorization-aware and redirects unauthenticated users into the shared login flow.
4. Update the checked-in Keycloak realm export so the shared browser client allows the `QueryServiceHost` local HTTPS callback, logout callback, and origin URLs.
5. Ensure repository ignore rules do not exclude the checked-in `launchSettings.json` files for:
   - `src/Hosts/QueryServiceHost/Properties/launchSettings.json`
   - `src/Hosts/IngestionServiceHost/Properties/launchSettings.json`
6. Add or update automated tests that pin the Query host authentication composition and the realm JSON endpoint contract.

### 2.2 Out of scope

- Creating a new Keycloak realm.
- Introducing a separate Keycloak client for `QueryServiceHost`.
- Redesigning the shared authentication abstractions in `UKHO.Search.ServiceDefaults` unless a minimal extension is genuinely required.
- Changing the repository's role model, group model, or claims-transformation semantics.
- Production identity-provider hardening beyond the existing local-developer setup.
- Broad `.gitignore` cleanup unrelated to the two named `launchSettings.json` files.
- Changing the current local ports unless that becomes explicitly required by a separate work item.

## 3. Requirements

### 3.1 Functional requirements

- **FR1**: An unauthenticated request for the normal `QueryServiceHost` UI MUST be challenged through Keycloak rather than rendering protected content anonymously.
- **FR2**: After a successful Keycloak sign-in, the browser MUST return to the Query shell root (`/`) and continue with an authenticated session.
- **FR3**: `QueryServiceHost` MUST use the same shared browser-host authentication model as the other browser-facing hosts rather than introducing host-local Keycloak wiring.
- **FR4**: The only deliberately anonymous browser authentication lifecycle routes exposed for `QueryServiceHost` MUST be the shared `/authentication/login` and `/authentication/logout` endpoints supplied by the shared service-defaults path.
- **FR5**: The checked-in realm export MUST contain the exact local `QueryServiceHost` URLs required for sign-in, sign-out callback, and browser origin validation.
- **FR6**: The repository MUST allow the two specified `launchSettings.json` files to remain versioned in Git so that their local endpoint contracts are visible and reviewable in source control.

### 3.2 Technical requirements

- **TR1**: `QueryServiceHost` MUST call `AddKeycloakBrowserHostAuthentication("search-workbench")` during startup, matching the established shared-client pattern used by the existing browser hosts.
- **TR2**: `QueryServiceHost` MUST map the shared authentication lifecycle endpoints via `MapKeycloakBrowserHostAuthenticationEndpoints()`.
- **TR3**: `QueryServiceHost` MUST restore the authenticated principal and enforce authorization through `UseAuthentication()` followed by `UseAuthorization()` before the interactive Razor component endpoints are mapped.
- **TR4**: `QueryServiceHost` routing MUST move from a plain `RouteView` to an authorization-aware route view so unauthorized interactive navigation is redirected into the shared login flow.
- **TR5**: If `QueryServiceHost` requires a redirect component analogous to `IngestionServiceHost.Components.Authentication.RedirectToLogin`, that component MUST remain host-local and minimal, with no duplicate authentication protocol logic.
- **TR6**: The Keycloak realm export in `src/Hosts/AppHost/Realms/ukho-search-realm.json` MUST include the `QueryServiceHost` HTTPS values currently defined by `src/Hosts/QueryServiceHost/Properties/launchSettings.json`, namely:
  - `https://localhost:7161/signin-oidc`
  - `https://localhost:7161/signout-callback-oidc`
  - `https://localhost:7161`
- **TR7**: The existing `IngestionServiceHost` and `WorkbenchHost` redirect, logout, and origin values in the realm export MUST be preserved.
- **TR8**: The implementation MUST continue to use the existing shared Keycloak client identifier `search-workbench` unless a new requirement explicitly supersedes this specification.
- **TR9**: Automated tests MUST pin the source-level composition of `QueryServiceHost` in the same style already used for `IngestionServiceHost` and `WorkbenchHost`, so authentication drift is caught without needing a full interactive runtime boot.
- **TR10**: Automated tests for the checked-in realm export MUST be expanded so the shared browser-host client configuration is verified for `QueryServiceHost` as well as the already-covered hosts.
- **TR11**: Repository ignore rules MUST explicitly allow the two named `launchSettings.json` files if any broader ignore pattern could otherwise exclude them. If those files are already not ignored, the implementation MUST still leave the repository in a state where they are definitively trackable and reviewable.

### 3.3 Non-functional requirements

- **NFR1**: The change MUST keep browser-host authentication behavior consistent across `QueryServiceHost`, `IngestionServiceHost`, and `WorkbenchHost`.
- **NFR2**: The work MUST reuse existing shared infrastructure and avoid creating a second authentication composition path.
- **NFR3**: The implementation MUST keep local developer setup understandable by making the checked-in realm JSON and checked-in launch settings the authoritative contract for host callback URLs.
- **NFR4**: The work MUST minimize the risk of future redirect-URI regressions by pinning exact local URLs in automated tests.

## 4. High-level design

### 4.1 Host authentication alignment

`QueryServiceHost` should mirror the existing browser-host pattern already established elsewhere in the repository:

1. register shared Keycloak browser-host authentication services;
2. expose the shared login/logout lifecycle endpoints;
3. enable authentication and authorization middleware in the correct order; and
4. use authorization-aware Blazor routing so unauthorized route access becomes a full login redirect rather than a partially rendered anonymous shell.

This keeps the authentication composition rooted in `UKHO.Search.ServiceDefaults`, which is already the repository's shared home for browser-host Keycloak behavior.

### 4.2 Query host routing behavior

The current `QueryServiceHost` route surface uses a plain `RouteView`. That is insufficient for host behavior that should mirror `IngestionServiceHost`.

The target behavior is:

- server-side fallback authorization protects the initial host request;
- Blazor routing uses an authorization-aware route view; and
- unauthorized route rendering triggers a redirect component that performs a full-page navigation to `/authentication/login`.

### 4.3 Keycloak realm export alignment

The current realm export already supports multiple local browser hosts through the shared `search-workbench` client. `QueryServiceHost` must be added to that same client contract rather than creating a new client.

Because Keycloak evaluates redirect URIs exactly, the local HTTPS port from `QueryServiceHost` launch settings is part of the contract. The realm export must therefore be kept in sync with the checked-in launch settings.

### 4.4 Repository source-control contract for launch settings

This work package treats the checked-in `launchSettings.json` files for `QueryServiceHost` and `IngestionServiceHost` as deliberate repository assets, not accidental local files. They define the local callback URLs that the Keycloak realm export must honor.

The repository must therefore not rely on a fragile assumption that generic ignore patterns will never change. The intended end state is that these two files are clearly allowed by `.gitignore` behavior and can remain committed without ambiguity.

## 5. Detailed design expectations

### 5.1 `QueryServiceHost` startup

The startup path in `src/Hosts/QueryServiceHost/Program.cs` should be aligned with the ingestion host pattern for browser authentication.

Expected characteristics:

- shared browser-host authentication registration is added;
- shared authentication lifecycle endpoint mapping is added;
- authentication middleware is present;
- authorization middleware is present;
- middleware ordering remains compatible with the existing interactive Blazor host pipeline.

The implementation should not copy protocol configuration directly into `QueryServiceHost`; that would reintroduce composition drift the shared service-defaults layer was created to avoid.

### 5.2 `QueryServiceHost` route protection

The route component in `src/Hosts/QueryServiceHost/Components/Routes.razor` should move to the same authorization-aware pattern used by `IngestionServiceHost`.

Expected characteristics:

- `AuthorizeRouteView` replaces the plain route view;
- unauthorized route rendering uses a redirect component;
- the redirect component navigates to the shared login endpoint with `forceLoad: true`.

If a host-local redirect component is introduced, it should follow the same minimal responsibility as the ingestion equivalent and should not introduce host-local authentication constants or alternate route prefixes.

### 5.3 Keycloak realm JSON

The `search-workbench` client definition in `src/Hosts/AppHost/Realms/ukho-search-realm.json` must be extended to include the Query host values derived from the checked-in launch settings.

Minimum required additions:

- `https://localhost:7161/signin-oidc` in `redirectUris`
- `https://localhost:7161` in `webOrigins`
- `https://localhost:7161/signout-callback-oidc` in post-logout redirect values
- `https://localhost:7161` in post-logout redirect values where the client currently allows root return URLs

This update must not remove the existing entries for the other browser hosts.

### 5.4 Launch settings and `.gitignore`

The implementation must ensure these files are not excluded by repository ignore rules:

- `src/Hosts/QueryServiceHost/Properties/launchSettings.json`
- `src/Hosts/IngestionServiceHost/Properties/launchSettings.json`

The preferred result is explicit and durable, so future ignore-rule changes do not silently make these files non-trackable.

### 5.5 Automated tests

At minimum, the work should add or update tests covering:

1. **Query host composition tests**
   - startup source contains shared authentication registration;
   - startup source maps shared lifecycle endpoints;
   - startup source contains `UseAuthentication()` and `UseAuthorization()` in the correct order relative to component mapping;
   - routes source uses `AuthorizeRouteView` and redirect-to-login behavior.

2. **AppHost realm/client configuration tests**
   - the shared `search-workbench` client contains the Query host redirect URI;
   - the shared client contains the Query host web origin;
   - the shared client contains the Query host post-logout redirect values.

3. **AppHost orchestration tests**
   - if not already pinned sufficiently, add or update source-based tests to verify the `QueryServiceHost` AppHost slice keeps its Keycloak reference and startup dependency.

4. **Repository ignore verification**
   - add a light-weight source test if useful, or otherwise ensure the implementation includes a deliberate and reviewable `.gitignore` change that keeps the two launch settings files trackable.

## 6. Validation and acceptance criteria

### 6.1 Acceptance criteria

This work package is complete when all of the following are true:

1. `QueryServiceHost` uses the shared Keycloak browser-host authentication composition path.
2. An unauthenticated user is redirected into the shared login flow rather than browsing the Query UI anonymously.
3. The checked-in realm export includes the exact Query host callback/origin URLs required by launch settings.
4. Existing browser-host realm export values for the other hosts remain intact.
5. The repository clearly allows the named `launchSettings.json` files to remain committed.
6. Automated tests pin the Query host composition and realm configuration so future regressions are caught early.

### 6.2 Manual verification expectations

Minimum manual verification should confirm:

1. starting the local environment through `AppHost` still launches successfully;
2. browsing to `QueryServiceHost` while signed out triggers the Keycloak login flow;
3. successful sign-in returns to the Query UI root;
4. logout returns cleanly through the shared sign-out flow; and
5. after a fresh Keycloak realm import, the Query host does not fail with `invalid_request` for `redirect_uri`.

### 6.3 Important operational note

Keycloak realm JSON changes are only applied automatically on a fresh import against an empty Keycloak data store. If the realm export is updated during implementation or testing, local verification may require deleting the persisted Keycloak Docker volume before restarting the AppHost environment.

## 7. Risks, assumptions, and decisions

### 7.1 Assumptions

- The repository's current reference implementation for this work is `IngestionServiceHost`, even though the request text refers to "IntegrationServiceHost".
- `QueryServiceHost` will continue to use the current HTTPS port from checked-in launch settings unless another work item changes that contract.
- The shared Keycloak client remains `search-workbench` for all local browser hosts in scope.

### 7.2 Risks

- **Risk 1: redirect-URI drift**  
  If the Query host launch port changes without a matching realm JSON update, Keycloak will reject login with `invalid_request` for `redirect_uri`.

- **Risk 2: composition drift**  
  If Query host startup reimplements Keycloak wiring locally instead of using `UKHO.Search.ServiceDefaults`, future hosts may diverge in subtle ways.

- **Risk 3: source-control ambiguity for launch settings**  
  If `.gitignore` behavior is left implicit, future ignore-rule edits may silently stop tracking the launch settings files that define the realm callback contract.

### 7.3 Decisions

- **Decision 1**: Reuse the existing shared browser-host authentication composition in `UKHO.Search.ServiceDefaults`.
- **Decision 2**: Reuse the existing shared Keycloak client `search-workbench` rather than introducing a Query-specific client.
- **Decision 3**: Treat the checked-in launch settings as part of the local authentication contract and keep them trackable in the repository.
