# Specification: WP122 Public API Authentication And Authorization

Target output path: `dev/work-packages/122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md`

Date: 2026-07-01

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md)
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the recommended authentication and authorization model for `PublicApiHost` and the consolidated React application.

The current recommendation is a BFF/session-cookie model through `PublicApiHost`. The browser should authenticate through `PublicApiHost`, hold a secure server-managed session cookie, and call the public API without directly carrying bearer tokens in normal browser operation.

This model must support server-side access to user claims and roles for security filtering in query execution. The query-side backend must be able to read the authenticated user's claims principal and apply security filtering in backend code rather than trusting the browser to enforce data visibility.

### 1.2 Scope

In scope for WP122:
- Choose the recommended browser authentication model.
- Define where session and sign-in/out responsibility lives.
- Define the high-level authorization model for end-user, developer/admin, and local-only operations.
- Define the requirement that query-side security filtering reads claims and roles server-side.
- Define local-development and logout/refresh expectations at a high level.

Out of scope for WP122:
- Detailed public API route ownership, which is defined in WP121.
- Detailed request/response contracts, which belong to WP124.
- React project placement, which belongs to Arc 03.
- Concrete implementation of every authorization policy and every endpoint attribute.

### 1.3 Stakeholders

- Security and platform owners responsible for browser-facing auth and session behavior.
- Query owners who need claims and roles available for backend security filtering.
- Ingestion and admin-tooling owners who need stronger policy boundaries for replay, repair, and operational actions.
- Frontend authors who need a secure and operable authentication model for the React application.
- Later work packages that implement API endpoints and UI login flows.

### 1.4 Definitions

- BFF/session-cookie: A backend-for-frontend model where the browser authenticates through the backend and the browser session is represented by a secure cookie rather than a frontend-managed bearer token.
- Shared auth/session boundary: One authentication entry point and session model for both `/api/search/*` and `/api/admin/*`, with authorization separating access.
- Claims principal: The authenticated user identity restored server-side and used by backend code for authorization and security filtering.
- Security filtering: Backend logic that restricts search visibility or result shape based on user claims, roles, or equivalent security context.
- Local-only operation: An operation intentionally kept within local tooling boundaries, such as `FileShareEmulator` controls.

## 2. System context

### 2.1 Current state

The current browser hosts already use a shared cookie-backed OpenID Connect model.

Evidence checked:
- [../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs](../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs) configures cookie-backed authentication with OpenID Connect challenge flow, shared Keycloak wiring, host-isolated cookies, and a fallback authenticated-user policy.
- [../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs](../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs) maps shared login/logout lifecycle endpoints under `/authentication`.
- [../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationDefaults.cs](../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationDefaults.cs) shows the current realm and authentication route defaults.
- [../../../src/Hosts/QueryServiceHost/Program.cs](../../../src/Hosts/QueryServiceHost/Program.cs) wires `AddKeycloakBrowserHostAuthentication("search-workbench", "query")` and protects the host with the shared authentication flow.
- [../../../src/Hosts/IngestionServiceHost/Program.cs](../../../src/Hosts/IngestionServiceHost/Program.cs) wires `AddKeycloakBrowserHostAuthentication("search-workbench", "ingestion")` and protects the host with the same shared authentication flow.
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md) fixes `PublicApiHost` as the single browser-facing API composition root with one shared auth/session boundary across `/api/search/*` and `/api/admin/*`.

The main issue is not that the current repository lacks a secure auth model. The issue is that the secure auth model is currently attached to retiring browser hosts rather than to the future browser-facing API boundary.

### 2.2 Proposed state

The recommended direction is:
- `PublicApiHost` owns browser-facing sign-in, logout, session, and auth challenge flow,
- the browser holds a secure session cookie rather than frontend-managed bearer tokens,
- `/api/search/*` and `/api/admin/*` share one auth/session boundary,
- `/api/search/*` requires authentication for all search requests,
- authorization distinguishes public search access from admin access,
- and query-side security filtering reads claims and roles in backend code.

Where `PublicApiHost` temporarily delegates to other runtime hosts during migration, the browser-facing auth model must still terminate at `PublicApiHost`. The backend should then use shared-service composition where possible or trusted server-side context propagation where transitional internal HTTP remains.

### 2.3 Assumptions

- The repository benefits from one browser-facing auth/session model rather than separate browser token handling and per-host auth logic.
- Query execution must be able to read user claims and roles in backend code to enforce security filtering correctly.
- Security filtering is a server-side responsibility, not a frontend responsibility.
- A BFF/session-cookie model is a better default than SPA-managed bearer tokens for this React-plus-public-host shape unless a later hard requirement proves otherwise.

### 2.4 Constraints

- `PublicApiHost` remains the single browser-facing API composition root defined by WP121.
- `FileShareEmulator` remains outside this browser-facing auth model and outside React consolidation.
- Retirement-bound UI surfaces are not the place to define future auth behavior.
- Detailed anonymous-versus-authenticated search access remains a product decision and must stay explicit rather than accidental.

For the current WP122 decision set, that explicit policy is now fixed as authenticated search only.

## 3. Component / service design (high level)

### 3.1 Components

WP122 defines five high-level elements:

1. React application
   - Uses browser session state established through `PublicApiHost`.

2. `PublicApiHost`
   - Owns browser-facing login/logout/session behavior and route-level authorization boundaries.

3. Query-side backend execution
   - Reads claims and roles server-side for security filtering.

4. Admin and operational APIs
   - Require stronger authorization than general end-user search routes.

5. Local-only tooling boundary
   - Keeps emulator-only destructive actions outside the public host model.

### 3.2 Data flows

Recommended authentication flow:
1. The browser reaches `PublicApiHost`.
2. `PublicApiHost` challenges through the configured identity provider when sign-in is required.
3. `PublicApiHost` restores the authenticated principal from the secure session cookie.
4. `PublicApiHost` authorizes the requested route.
5. Query or admin backend code reads the restored claims principal server-side.
6. Query execution applies security filtering using claims/roles before returning results.

Recommended logout flow:
1. The browser triggers logout through `PublicApiHost`.
2. `PublicApiHost` clears the local session and upstream OpenID Connect session.
3. The browser returns to the chosen public entry route.

### 3.3 Key decisions

- Recommendation: use a BFF/session-cookie model through `PublicApiHost`.
- Recommendation: do not expose frontend-managed bearer tokens as the default browser auth mechanism.
- Recommendation: keep one shared auth/session boundary across `/api/search/*` and `/api/admin/*`.
- Recommendation: require authentication for all `/api/search/*` requests in the baseline model rather than allowing anonymous search by default.
- Recommendation: distinguish end-user and admin access with authorization and policy rather than separate browser auth systems.
- Recommendation: start `/api/admin/*` with one general admin role rather than splitting query-admin and ingestion-admin roles in the initial authorization model.
- Recommendation: keep destructive or high-risk admin actions under the same general admin role in the baseline model rather than introducing an elevated operator role at this stage.
- Recommendation: require query-side security filtering to read claims and roles server-side.
- Recommendation: leave exact route-level authorization policy names and concrete claim-to-policy mappings to later implementation-focused work once this auth model is fixed.
- Recommendation: if transitional internal HTTP exists, do not treat it as permission for the browser to bypass `PublicApiHost`.

## 4. Functional requirements

FR1. `PublicApiHost` shall own the browser-facing authentication entry point for the consolidated React application.

FR2. The browser authentication model shall be BFF/session-cookie based rather than frontend-managed bearer-token based by default.

FR3. The browser shall call `PublicApiHost` using the established browser session rather than supplying bearer tokens directly in the normal React-to-API flow.

FR4. `PublicApiHost` shall expose login and logout behavior suitable for the chosen BFF/session-cookie model.

FR5. `/api/search/*` and `/api/admin/*` shall share one auth/session boundary, with authorization and route policy distinguishing end-user and developer/admin access.

FR5a. `/api/search/*` shall require authentication for all search requests in the baseline model defined by WP122.

FR6. Query-side backend logic shall be able to read the authenticated user's claims and roles server-side.

FR7. Query-side security filtering shall be enforced in backend code using server-side claims/role context.

FR8. The frontend shall not be treated as the authority for search-result security filtering.

FR8a. `/api/admin/*` shall start with one general admin role in the baseline authorization model rather than separate query-admin and ingestion-admin roles.

FR8b. Destructive or high-risk admin actions such as replay, forced replay, rule promotion, or repair shall remain under the same general admin role in the baseline model rather than requiring an additional elevated operator role.

FR9. Admin operations such as diagnostics, rule editing, replay, repair, and other operational actions shall require explicit authorization beyond any baseline authenticated-user session.

FR10. Local-only emulator operations shall remain outside the `PublicApiHost` browser auth model.

FR11. The chosen auth model shall support both same-host and separate-frontend deployment models already allowed by WP121.

FR12. If `PublicApiHost` delegates to other runtime code during migration, the authenticated server-side user context required for authorization and security filtering shall remain available to backend execution.

FR13. Anonymous-versus-authenticated search-route policy shall remain explicit and shall not be inferred accidentally from the transport model.

FR13a. For the current WP122 decision set, `/api/search/*` shall be treated as authenticated-only rather than anonymously accessible.

FR13b. Exact route-level authorization policy names and concrete claim-to-policy mappings shall be defined in later implementation-focused work rather than fixed in WP122.

## 5. Non-functional requirements

NFR1. The auth model shall minimize browser-side token exposure in the normal React-to-API flow.

NFR2. The auth model shall preserve secure server-side access to claims and roles for query filtering.

NFR3. The auth model shall fit the single-public-host topology already defined in WP121.

NFR4. The auth model shall avoid unnecessary CORS, token-refresh, and browser-token-lifecycle complexity when the same security outcome can be achieved with server-managed sessions.

NFR5. The model shall remain secure whether the React app is served by `PublicApiHost` or deployed separately.

NFR6. The auth model shall be specific enough to guide later implementation while avoiding premature commitment to policy naming and claim-mapping details that belong in implementation-focused work.

## 6. Data model

WP122 does not define concrete token or session payloads. It defines the required security-context shape.

Required backend security-context capabilities:
- authenticated-user identity,
- role claims,
- other claims needed for search security filtering,
- and enough route/policy context to distinguish end-user from admin access.

The spec requires those values to be available server-side to query execution and authorization logic. It does not require the browser to manage them as bearer tokens.

## 7. Interfaces & integration

### 7.1 Browser-facing auth surface

`PublicApiHost` is expected to own:
- login entry behavior,
- logout behavior,
- session restoration,
- route challenge behavior,
- and the shared auth/session boundary for public and admin API routes.

### 7.2 Claims propagation expectation

Preferred model:
- shared-service composition where query execution can read the server-side principal directly.

Acceptable transitional model:
- carefully controlled server-to-server context propagation when temporary internal HTTP remains.

Disallowed baseline:
- relying on the browser to enforce result security,
- or assuming frontend-managed tokens are required just because roles must be visible to backend query logic.

## 8. Observability (logging/metrics/tracing)

Authentication and authorization behavior should be observable at `PublicApiHost` as the single browser-facing boundary.

That means later work should be able to track:
- login and logout lifecycle behavior,
- authorization failures,
- route-family access patterns,
- and security-filtering-relevant request identity context

without spreading those concerns across multiple public hosts.

Minimal technical observability remains part of WP125. Detailed business audit and operation tracking are deferred until later hardening work.

## 9. Security & compliance

WP122 recommends a security-first model for this repository shape:
- server-managed browser sessions,
- server-side claim evaluation,
- backend-enforced search filtering,
- and stronger policy boundaries for admin and operational actions.

The baseline search posture in this model is authenticated access rather than anonymous access.

The baseline admin posture in this model is one general admin role rather than multiple split admin roles.

The baseline high-risk action posture in this model is to keep replay, forced replay, rule promotion, and repair under that same general admin role.

This aligns with the requirement that query APIs must securely read user claims and roles to apply security filtering.

## 10. Testing strategy

WP122 validation should focus on auth-model correctness and downstream implementability.

Validation anchors:
- Confirm the model is consistent with the public-host topology in [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md).
- Confirm the model allows server-side claims and role access for query filtering.
- Confirm the model keeps browser-facing auth/session handling at `PublicApiHost`.
- Confirm later work can add endpoint authorization tests for anonymous, authenticated, developer/admin, and forbidden flows.

## 11. Rollout / migration

Recommended migration posture:
1. Fix `PublicApiHost` as the browser-facing auth/session owner.
2. Reuse the repository's existing shared cookie-backed OIDC pattern as the starting point.
3. Move browser-facing login/logout/session behavior to `PublicApiHost`.
4. Ensure query-side execution reads claims and roles server-side for security filtering.
5. Add detailed policy decisions and tests in later WP122 follow-on implementation work.

Wiki review result:
No wiki page update was required for this draft work-package specification. The work records a recommended auth model rather than a current-state implementation change.

## 12. Open questions

No open questions remain in WP122 at this stage. The browser auth model, session boundary, authenticated-search posture, general admin role model, and server-side claims-based filtering posture are now fixed here. Detailed policy names, claim mappings, and endpoint-by-endpoint enforcement remain for later implementation-focused work.