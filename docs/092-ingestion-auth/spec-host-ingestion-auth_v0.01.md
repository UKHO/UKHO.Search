# Work Package: `092-ingestion-auth` — `IngestionServiceHost` authentication parity with `WorkbenchHost`

**Target output path:** `docs/092-ingestion-auth/spec-host-ingestion-auth_v0.01.md`

**Version:** `v0.01` (`Draft`)

## Change Log

- `v0.01` — Initial draft created for aligning `IngestionServiceHost` with the existing `WorkbenchHost` authentication model.
- `v0.01` — Captures that access to the `IngestionServiceHost` Blazor UI must require Keycloak authentication before any interactive page is available.
- `v0.01` — Captures that the host must adopt the same cookie-backed OpenID Connect pattern currently used by `WorkbenchHost`.
- `v0.01` — Captures that authorization should default to authenticated access for the UI surface, with only explicitly anonymous endpoints exempted.
- `v0.01` — Captures the requirement to mirror the current login and logout endpoint experience used by `WorkbenchHost`.
- `v0.01` — Captures the expectation that the implementation should minimise divergence from `WorkbenchHost`, ideally through shared auth wiring where practical.
- `v0.01` — Confirms that `IngestionServiceHost` should reuse the existing Keycloak client configuration currently used by `WorkbenchHost`.
- `v0.01` — Confirms that the authenticated baseline should apply to every browser-accessible endpoint except the login and logout lifecycle routes.

## 1. Overview

### 1.1 Purpose

This specification defines the authentication requirements for `IngestionServiceHost` so that its Blazor UI is protected in the same way as `WorkbenchHost`.

The purpose of this work is to ensure that any user attempting to access the `IngestionServiceHost` user interface is authenticated by Keycloak before the UI is rendered or used. The intended outcome is a consistent repository-wide host authentication model for browser-facing Blazor Server hosts.

### 1.2 Scope

This specification includes:

- introducing the same authentication and authorization setup pattern used by `WorkbenchHost` into `IngestionServiceHost`
- requiring authenticated access by default across the `IngestionServiceHost` Blazor UI surface
- redirecting unauthenticated users through Keycloak before they can use the UI
- providing explicit login and logout endpoints equivalent to those exposed by `WorkbenchHost`
- ensuring the request pipeline orders authentication and authorization correctly for the Blazor host
- aligning any supporting claims transformation, configuration, and host wiring needed for parity
- identifying opportunities to avoid the two hosts drifting apart in future authentication changes

This specification excludes:

- redesigning the `IngestionServiceHost` UI itself
- changing ingestion pipeline behaviour or background-processing semantics unrelated to browser authentication
- introducing fine-grained feature authorization or new role models beyond what is required for host-level parity
- changing non-Blazor ingestion endpoints unless they are part of the same browser-access surface and therefore must respect the same authentication posture
- defining a complete enterprise identity strategy beyond the immediate need for parity with the current `WorkbenchHost` setup

### 1.3 Stakeholders

- developers maintaining `IngestionServiceHost`
- developers maintaining `WorkbenchHost`
- platform and hosting maintainers responsible for Keycloak integration
- operational users who access the ingestion UI
- security stakeholders expecting browser-facing internal tooling to require authenticated access

### 1.4 Definitions

- `IngestionServiceHost`: the Blazor Server host under `src/Hosts/IngestionServiceHost`
- `WorkbenchHost`: the existing Blazor Server host under `src/workbench/server/WorkbenchHost`
- `Keycloak`: the OpenID Connect identity provider used by the repository's current Workbench authentication flow
- `authentication parity`: equivalent authentication behaviour, flow, and protection model between the two hosts, not merely a visually similar login screen
- `fallback authorization policy`: the default authorization rule applied when an endpoint does not declare a more specific policy
- `anonymous endpoint`: an endpoint deliberately marked as accessible without a signed-in user, such as the login initiation route

## 2. System context

### 2.1 Current state

`WorkbenchHost` already implements a complete cookie-backed OpenID Connect integration against Keycloak. It configures cookie authentication as the default local session mechanism, uses OpenID Connect as the challenge scheme, saves tokens, and enforces authenticated access through a fallback authorization policy. It also exposes explicit login and logout endpoints and applies a Keycloak realm-role claims transformation before authorization.

By contrast, `IngestionServiceHost` currently runs as a Blazor Server host without equivalent browser authentication wiring. Its Blazor routes and pages are therefore available without the same Keycloak-first access pattern currently expected from `WorkbenchHost`.

This creates an inconsistency across browser-facing hosts in the repository. Users can reach the ingestion UI without first being challenged by Keycloak, while Workbench already requires authentication for normal use.

### 2.2 Proposed state

After this work package:

- `IngestionServiceHost` will use the same cookie and OpenID Connect authentication pattern as `WorkbenchHost`
- `IngestionServiceHost` will reuse the same Keycloak client configuration currently used by `WorkbenchHost`
- an unauthenticated request for the `IngestionServiceHost` Blazor UI will result in an authentication challenge through Keycloak before the user can use the interface
- authenticated state will flow through the Blazor component tree in the same way as it does in `WorkbenchHost`
- default authorization for the host will require an authenticated user unless an endpoint is explicitly declared anonymous
- every browser-accessible endpoint will require authenticated access except the login and logout lifecycle routes
- login and logout endpoints equivalent to the Workbench model will exist for the ingestion host experience
- supporting claims transformation and authentication pipeline ordering will be aligned with the current Workbench approach where applicable
- the two hosts will be positioned to share auth wiring or a shared abstraction where that reduces future drift and duplicated maintenance

### 2.3 Assumptions

- the phrase "exactly the same auth setup as `WorkbenchHost`" means the end-user authentication flow, host protection model, and Keycloak integration pattern should match the existing Workbench implementation as closely as practical
- `IngestionServiceHost` is a browser-facing Blazor Server host and therefore should follow the same security baseline as `WorkbenchHost`
- the existing Keycloak realm used by Workbench remains the canonical identity source for this work
- `IngestionServiceHost` should reuse the same Keycloak client configuration currently used by `WorkbenchHost` rather than introducing a dedicated client as part of this work package
- if `IngestionServiceHost` currently lacks role-based features, authenticated-user enforcement is still required as the baseline minimum
- where Workbench-specific auth code can be shared safely, reuse is preferable to copy-and-paste duplication because parity must be sustainable over time
- any exceptions to authenticated access should be explicit, minimal, and limited to the login and logout lifecycle routes

### 2.4 Constraints

- the work must remain within the existing repository architecture and host structure
- the solution must protect the Blazor UI before normal user interaction occurs
- every browser-accessible endpoint in `IngestionServiceHost` must require authenticated access except the login and logout lifecycle routes
- the authentication provider is Keycloak, matching the current Workbench integration model
- the user experience must be equivalent to `WorkbenchHost` from a browser authentication perspective
- the implementation must not weaken the current `WorkbenchHost` posture or create divergent host-by-host security behaviour without a documented reason
- the specification should prefer a maintainable shared approach where practical, but parity is mandatory even if initial implementation uses host-local wiring
- the work package is documentation-only at this stage and does not itself implement code changes

## 3. Component / service design (high level)

### 3.1 Components

1. `IngestionServiceHost`
   - the browser-facing Blazor Server host whose UI must now require Keycloak authentication
   - owns the request pipeline, route mapping, and host-level authentication enforcement

2. `Keycloak OpenID Connect integration`
   - the external identity flow used to authenticate browser users
   - provides the challenge and sign-out behaviour used by the host

3. `Cookie authentication session`
   - stores the authenticated local session after successful OpenID Connect sign-in
   - allows the authenticated principal to flow across later requests and the Blazor circuit

4. `Claims transformation`
   - normalises Keycloak claims into shapes that ASP.NET authorization can use consistently
   - should remain aligned with the existing Workbench role-claim transformation behaviour if the ingestion UI requires the same role awareness now or later

5. `Login and logout endpoints`
   - explicit endpoints used to initiate authentication and sign-out flows
   - should mirror the Workbench user journey so both hosts behave consistently

6. `Shared authentication wiring`
   - an optional but preferred shared registration or extension layer that keeps both hosts on the same auth baseline
   - reduces the chance that one host evolves away from the approved configuration

### 3.2 Data flows

#### Unauthenticated browser access flow

1. A user navigates to the `IngestionServiceHost` Blazor UI.
2. The host evaluates the request against its default authorization posture.
3. Because the user is not yet authenticated, the host issues an OpenID Connect challenge.
4. The browser is redirected to Keycloak.
5. After successful authentication, Keycloak returns the user to the host.
6. The host creates the local cookie-backed session.
7. The authenticated principal flows into the Blazor UI and the requested page becomes available.

#### Authenticated navigation flow

1. A signed-in user navigates between pages within `IngestionServiceHost`.
2. The existing authentication cookie is used to restore the user principal.
3. Authorization evaluates the request under the default authenticated-user policy.
4. The Blazor UI renders with the authenticated user context available.

#### Logout flow

1. The user invokes logout.
2. The host clears the local cookie session and signs out via the OpenID Connect handler.
3. The browser returns to the configured post-logout location.
4. A later attempt to access the UI results in a fresh Keycloak authentication challenge.

### 3.3 Key decisions

- `IngestionServiceHost` should no longer be treated as a special-case internal UI that can remain anonymously reachable; it must align with the authenticated-host baseline already established by `WorkbenchHost`
- parity should be measured in effective behaviour and security posture, not merely in copying a subset of configuration settings
- login, logout, default authorization, and claims-normalisation behaviour should remain consistent between the two hosts to reduce operator confusion and maintenance drift
- `IngestionServiceHost` should reuse the same Keycloak client configuration currently used by `WorkbenchHost` as part of maintaining exact authentication parity for this work item
- where feasible, the repository should converge on shared host-auth configuration rather than maintaining parallel but supposedly identical setups in separate `Program.cs` files
- the browser UI must be the protected surface; background ingestion processing should continue to behave according to host/service design and should not be accidentally coupled to browser sign-in state
- for this work item, the only anonymous browser-accessible endpoints should be the login and logout lifecycle routes already required to support the authentication flow

## 4. Functional requirements

1. The system shall configure `IngestionServiceHost` to use the same cookie-backed OpenID Connect authentication model currently used by `WorkbenchHost`.
2. The system shall use Keycloak as the authentication provider for `IngestionServiceHost`.
3. The system shall reuse the same Keycloak client configuration currently used by `WorkbenchHost`.
4. The system shall challenge unauthenticated users through Keycloak before allowing normal access to the `IngestionServiceHost` Blazor UI.
5. The system shall use cookie authentication as the persisted sign-in mechanism after successful Keycloak authentication.
6. The system shall flow authentication state through the Blazor component tree for `IngestionServiceHost`.
7. The system shall apply a fallback authorization policy in `IngestionServiceHost` that requires an authenticated user by default.
8. The system shall require any route, page, or endpoint serving the normal `IngestionServiceHost` UI experience to be protected by the authenticated-user baseline unless it is one of the login or logout lifecycle routes.
9. The system shall expose login and logout endpoints for `IngestionServiceHost` that are functionally equivalent to those used by `WorkbenchHost`.
10. The system shall allow the login endpoint itself to remain anonymously reachable so an unauthenticated user can start the sign-in flow.
11. The system shall execute authentication before authorization in the `IngestionServiceHost` request pipeline.
12. The system shall preserve correct Blazor Server interactive behaviour while enforcing authenticated access.
13. The system shall align any Keycloak claims transformation needed by `IngestionServiceHost` with the existing Workbench approach.
14. The system shall avoid introducing host-specific authentication behaviour that diverges from `WorkbenchHost` unless a documented requirement justifies it.
15. The system shall ensure that direct navigation to any protected ingestion UI URL by an unauthenticated user results in an authentication challenge rather than anonymous page rendering.
16. The system shall ensure that after logout, a subsequent attempt to access the UI requires re-authentication through Keycloak.
17. The system should factor common authentication setup into shared host-level registration or extensions if doing so reduces duplication without changing the required behaviour.
18. The system shall treat the login and logout lifecycle routes as the only intentionally anonymous browser-accessible ingestion-host endpoints.
19. The system shall keep the authentication experience sufficiently aligned with `WorkbenchHost` that users do not need to learn a different sign-in pattern for the ingestion UI.

## 5. Non-functional requirements

1. The authentication change shall maintain a consistent user experience across the repository's browser-facing Blazor hosts.
2. The solution shall minimise configuration drift between `WorkbenchHost` and `IngestionServiceHost`.
3. The authentication setup shall be maintainable by developers without requiring separate identity strategies for the two hosts.
4. The protected host shall continue to support normal Blazor Server interactivity after the user is authenticated.
5. The design shall favour explicit, reviewable security configuration over implicit or accidental protection.
6. The host shall fail safely such that absence of authentication should not result in anonymous access to the protected UI.

## 6. Data model

No new business data model is required for this work item.

The only relevant identity data is the authenticated user principal and any Keycloak-issued claims required to establish or evaluate authenticated access within `IngestionServiceHost`.

If role claims are normalised in the same way as `WorkbenchHost`, those claims should be treated as part of the authentication and authorization context rather than as new application-domain data.

## 7. Interfaces & integration

- `IngestionServiceHost` must integrate with the same Keycloak realm, Keycloak client configuration, and OpenID Connect flow currently used by `WorkbenchHost`.
- `IngestionServiceHost` must integrate with ASP.NET Core cookie authentication for local session persistence.
- the host must integrate authentication state into the Blazor Server rendering pipeline through cascading authentication state.
- the request pipeline must integrate login and logout endpoints in a way that is compatible with the host authorization posture while keeping those routes as the only anonymous browser-accessible exceptions.
- if claims transformation is required, it should integrate before authorization evaluation so role or identity decisions see the expected claim shape.
- if a shared auth extension or library is introduced, both `WorkbenchHost` and `IngestionServiceHost` should consume it as the canonical registration path.

## 8. Observability (logging/metrics/tracing)

- authentication configuration and startup should emit enough host-level logging to support operational diagnosis of sign-in configuration problems without exposing secrets
- failed or unexpected authentication behaviour should be diagnosable through normal ASP.NET and host logging
- if shared auth wiring is introduced, its startup path should make it clear which scheme names and provider settings are in effect
- no new domain metrics are required purely for this work item, but the change must not remove existing host observability

## 9. Security & compliance

This work item is fundamentally a security alignment change.

The primary security requirement is that the `IngestionServiceHost` Blazor UI must not be available anonymously when `WorkbenchHost` already requires Keycloak authentication. The repository should present a consistent baseline that browser-accessible operational tooling is authenticated first.

The design should continue to rely on standard ASP.NET Core authentication and authorization middleware ordering, cookie-backed session handling, and OpenID Connect challenge flows rather than introducing custom security mechanisms.

Only the login and logout lifecycle routes should bypass the fallback authenticated-user requirement for browser-accessible endpoints in this host.

If role claims from Keycloak are required for present or future authorization decisions, those claims should be normalised using the same trusted transformation model already established in `WorkbenchHost`.

## 10. Testing strategy

- verify that an unauthenticated request to the `IngestionServiceHost` UI results in an authentication challenge rather than anonymous rendering
- verify that the login route starts the Keycloak challenge flow
- verify that successful authentication results in an authenticated session that can access the Blazor UI
- verify that the fallback authorization policy protects normal ingestion-host UI routes by default
- verify that logout clears the session and requires a fresh authentication flow for later access
- verify that any claims transformation used by the host behaves consistently with `WorkbenchHost`
- verify that the host still renders interactive Blazor pages correctly after authentication is established
- verify that the only anonymous browser-accessible endpoints are the documented login and logout lifecycle routes

## 11. Rollout / migration

1. Compare the current `WorkbenchHost` authentication setup against `IngestionServiceHost` and identify the exact parity gap.
2. Add equivalent authentication, authorization, claims-transformation, and login/logout wiring to `IngestionServiceHost`.
3. Protect the Blazor UI with the authenticated-user default policy.
4. Validate unauthenticated, authenticated, and logout user journeys.
5. Where practical, refactor shared host authentication wiring to reduce future divergence.
6. Update any relevant operational documentation if the ingestion host gains explicit login and logout routes or other identity-related behaviour that users need to know.

## 12. Open questions

None currently.
