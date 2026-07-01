# Specification: WP122 Browser Host Authentication And Authorization

Target output path: `dev/work-packages/122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md`

Date: 2026-07-01

Repository note:
The folder name is retained for numbering continuity. The canonical decision in this file supersedes the earlier React/PublicApiHost assumption.

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md)
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the recommended authentication and authorization model for the split browser-host direction.

The current recommendation is:
- keep the repository's existing cookie-backed OpenID Connect model as the baseline,
- give `QueryServiceHost` and the new `WorkbenchHost` distinct browser-host auth/session boundaries,
- keep search-side security filtering server-side,
- and enforce stronger policy on the internal host than on the public host.

### 1.2 Scope

In scope for WP122:
- choose the host-level auth/session posture,
- define where login and logout responsibility lives,
- define the high-level authorization split between public search and internal operations,
- define the server-side claims requirement for search filtering,
- and define the expected relationship between Keycloak clients, cookies, and host boundaries.

Out of scope for WP122:
- detailed endpoint attributes for every route,
- detailed request and response contracts,
- business audit requirements,
- or exact claim-to-policy mapping tables.

### 1.3 Stakeholders

- Security and platform owners responsible for browser-facing auth behavior.
- Query owners who need claims and roles available for backend filtering.
- Internal tooling owners who need stronger boundaries for replay, repair, and rule-authoring workflows.
- Later work packages that implement public and internal host routes.

### 1.4 Definitions

- Browser-host auth boundary: The combination of login entry point, session cookie, logout behavior, and authorization defaults owned by a given browser host.
- Public host session: The session used by customer-facing search in `QueryServiceHost`.
- Internal host session: The session used by the internal `WorkbenchHost`.
- Server-side security filtering: Backend logic that restricts search visibility based on the authenticated principal.

## 2. System context

### 2.1 Current state

Evidence checked:
- [../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs](../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationServiceCollectionExtensions.cs) configures cookie-backed authentication with OpenID Connect challenge flow, host-isolated cookies, and a fallback authenticated-user policy.
- [../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs](../../../src/Hosts/UKHO.Search.ServiceDefaults/BrowserHostAuthenticationEndpointRouteBuilderExtensions.cs) maps shared login/logout lifecycle endpoints.
- [../../../src/Hosts/QueryServiceHost/Program.cs](../../../src/Hosts/QueryServiceHost/Program.cs) already uses the shared browser-host auth composition.
- [../../../src/Hosts/IngestionServiceHost/Program.cs](../../../src/Hosts/IngestionServiceHost/Program.cs) also uses the shared browser-host auth composition today.
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md) fixes the future split between `QueryServiceHost` and a new internal `WorkbenchHost`.

### 2.2 Proposed state

The recommended direction is:
- `QueryServiceHost` owns the public browser-host auth boundary.
- the new `WorkbenchHost` owns the internal browser-host auth boundary.
- each host uses host-specific cookies and login/logout routes based on the shared repository auth composition.
- both hosts may use the same Keycloak realm, but they should not be treated as one undifferentiated browser session by design.
- search-side security filtering reads claims and roles server-side inside backend code.

### 2.3 Assumptions

- The repository's existing cookie-backed OIDC model is a better starting point than inventing SPA token handling.
- The permanent audience split justifies separate host auth boundaries even if the same identity realm is reused.
- Public search still requires secure server-side filtering.
- Internal operations need a stronger authorization posture than customer-facing search.

### 2.4 Constraints

- `FileShareEmulator` remains outside this product-host auth model.
- The deleted legacy Workbench tree is not the place to define future auth behavior.
- Detailed endpoint policy names remain a later implementation concern.

## 3. Key decisions

- Keep cookie-backed OIDC as the baseline browser-host auth model.
- Give `QueryServiceHost` and `WorkbenchHost` separate browser-host session boundaries.
- Prefer separate Keycloak clients for public and internal hosts, even if they share one realm.
- Require authenticated access to public search unless a later product decision explicitly opens anonymous routes.
- Require internal-role-based access to `WorkbenchHost` workflows.
- Keep search-side filtering server-side.

## 4. Functional requirements

FR1. `QueryServiceHost` shall own the public browser-host login, logout, and session boundary.

FR2. The new `WorkbenchHost` shall own the internal browser-host login, logout, and session boundary.

FR3. The baseline browser auth model for both hosts shall remain cookie-backed OpenID Connect rather than browser-managed bearer tokens.

FR4. The public and internal hosts shall use distinct host-specific cookies and host keys.

FR5. The preferred direction is separate Keycloak clients for the public and internal hosts, even if both use the same realm.

FR6. `QueryServiceHost` search requests shall run with server-side access to the authenticated principal when authorization or security filtering requires it.

FR7. Search-side security filtering shall be enforced in backend code rather than in the browser.

FR8. `WorkbenchHost` shall require internal authorization stronger than a generic authenticated-user session.

FR9. Replay, forced replay, repair, rule promotion, and equivalent operational actions shall require explicit internal authorization.

FR10. `FileShareEmulator` local-only operations shall remain outside the product-host auth model.

FR11. Later work packages shall define endpoint-by-endpoint policy names and claim mappings without changing the host-boundary decision made here.

## 5. Non-functional requirements

NFR1. The auth model shall reuse proven repository patterns where practical.

NFR2. The auth model shall preserve strict separation between customer-facing and internal audiences.

NFR3. The model shall avoid forcing browser-side token lifecycle complexity when server-managed sessions achieve the same goal.

NFR4. The model shall preserve secure server-side access to claims and roles for search filtering.

## 6. Data model

Required host-level security context capabilities:
- authenticated user identity,
- role claims,
- any additional claims needed for search filtering,
- and enough route context to distinguish public-search access from internal operational access.

The browser does not need to manage bearer tokens directly in the baseline model.

## 7. Interfaces and integration

### 7.1 Host auth ownership

`QueryServiceHost` owns:
- public login and logout behavior,
- public session restoration,
- and public route challenge behavior.

`WorkbenchHost` owns:
- internal login and logout behavior,
- internal session restoration,
- and internal route challenge behavior.

### 7.2 Claims propagation

Preferred model:
- direct server-side composition where search execution can read the principal directly.

Acceptable transitional model:
- carefully controlled server-to-server context propagation when temporary internal HTTP exists.

## 8. Observability

WP122 does not define the technical observability baseline, but it requires the host split to be visible enough that later work can diagnose:
- login and logout behavior,
- authorization failures,
- and search-filtering-relevant identity context.

## 9. Security and compliance

WP122 recommends:
- server-managed browser sessions,
- server-side claim evaluation,
- backend-enforced search filtering,
- and stronger internal authorization for the internal workbench.

Business audit remains deferred.

## 10. Testing strategy

Validation anchors:
- confirm the model is consistent with WP121's split-host direction,
- confirm the model allows server-side claims and role access for search filtering,
- confirm the public and internal hosts do not collapse into one undifferentiated session model by accident,
- and confirm later work can add endpoint authorization tests without redesigning the host boundary.

## 11. Rollout and migration

Recommended migration posture:
1. keep the shared browser-host auth composition,
2. assign distinct public and internal host auth boundaries,
3. introduce separate Keycloak clients or equivalent separated configuration,
4. keep search filtering server-side,
5. add detailed internal policies in later implementation slices.

Wiki review result:
No wiki page update was required for this planning work package. The work records the target auth model rather than a current-state runtime change.

## 12. Open questions

None at this stage. WP122 now fixes the baseline auth direction as separate public and internal browser-host session boundaries built on the repository's existing cookie-backed OIDC approach.