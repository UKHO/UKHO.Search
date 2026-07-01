# Specification: WP123 Capability Boundaries And Local-Only Exceptions

Target output path: `dev/work-packages/123-capability-boundaries-local-only-exceptions/spec-domain-capability-boundaries-local-only-exceptions.md`

Date: 2026-07-01

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md)
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md)
- [../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md](../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the capability boundary for the consolidated platform after the host and auth decisions made in WP120-WP122.

WP123 deliberately ignores environment as a design axis for now. The current assumption is that capabilities exposed through `PublicApiHost` are available in all environments for now, with later environment-specific restriction deferred until the React application and the core admin workflows are working.

WP123 therefore focuses on a simpler question: which capabilities belong in `PublicApiHost`, which remain outside it, and which are the local-only exceptions that must not be folded into the public React/API platform.

### 1.2 Scope

In scope for WP123:
- Define the capability groups that belong in `PublicApiHost`.
- Define the local-only exception boundary for `FileShareEmulator`.
- Define the out-of-scope configuration tooling boundary.
- Record the current assumption that `PublicApiHost` capabilities are available in all environments for now.
- Distinguish public search, admin query, admin ingestion/provider, system endpoints, and emulator-only controls.

Out of scope for WP123:
- Environment-based gating rules for non-local or live deployments.
- Business audit requirements.
- Detailed auth policy names and claim mappings.
- Detailed request/response contract design.
- Retirement execution for old UI surfaces.

### 1.3 Stakeholders

- Arc 02 owners who need a simpler capability model than environment-based gating.
- Backend owners implementing `PublicApiHost` capabilities.
- Frontend authors who need to know which capability groups exist in the public platform.
- Emulator/tooling owners who need `FileShareEmulator` to stay outside the public platform surface.
- Later hardening work that may add environment-specific restriction or audit after the new platform is working.

### 1.4 Definitions

- Public capability: A capability intentionally exposed through `PublicApiHost` for the consolidated React-plus-API platform.
- Admin capability: A non-end-user capability exposed through `PublicApiHost` under the authenticated admin route families.
- Local-only exception: A capability intentionally kept outside `PublicApiHost` and outside the React platform, even if it exists in the local Aspire stack.
- Mutating or high-risk capability: A capability that changes state or triggers operational actions, such as replay, repair, rule promotion, or queue/index mutation.
- Out-of-scope tooling: Supporting repository or local-stack tooling that is not a React consolidation target.

## 2. System context

### 2.1 Current state

WP120 fixed the surface boundary, WP121 fixed the public host strategy, and WP122 fixed the browser auth model.

Evidence checked:
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md) fixes `IngestionServiceHost`, `QueryServiceHost`, and the provider mechanism as the retained service-side runtime and keeps `FileShareEmulator` outside React consolidation.
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md) fixes `PublicApiHost` as the single browser-facing API composition root.
- [../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md](../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md) fixes a BFF/session-cookie model with authenticated search and authenticated admin routes.
- [../../../tools/FileShareEmulator/Services/IngestionQueueService.cs](../../../tools/FileShareEmulator/Services/IngestionQueueService.cs) contains queue-clearing behavior through `ClearAllAsync`.
- [../../../tools/FileShareEmulator/Services/IndexService.cs](../../../tools/FileShareEmulator/Services/IndexService.cs) contains batch index reset behavior through `ResetAllToPendingAsync`.
- [../../../tools/FileShareEmulator/Api/BatchFilesApi.cs](../../../tools/FileShareEmulator/Api/BatchFilesApi.cs) exposes the local batch-file retrieval route under the emulator surface.

The current simplification is deliberate:
- do not design the public capability surface around environment labels,
- do not design audit-driven restrictions yet,
- and keep the one clear exception, `FileShareEmulator`, out of the public platform surface.

### 2.2 Proposed state

The recommended capability boundary is:
- `PublicApiHost` owns end-user search, admin query, admin ingestion/provider, and system endpoints.
- `FileShareEmulator` remains a local-only exception and is not represented as a `PublicApiHost` capability set.
- Configuration emulator tooling remains out of scope for React consolidation.
- Capability availability is assumed across environments for now; future live restrictions are deferred.

### 2.3 Assumptions

- Environment-specific capability suppression is premature for the current phase of the work.
- Business audit requirements are premature for the current phase of the work.
- It is more important now to get the public capability shape right than to invent future live restrictions.
- The local-only exception boundary for `FileShareEmulator` is a surface-ownership decision, not an environment-policy design axis.

### 2.4 Constraints

- `PublicApiHost` remains the only public browser-facing API surface.
- `FileShareEmulator` stays outside that surface.
- Configuration emulator tooling stays out of scope.
- Future environment restriction or audit decisions may be added later, but they must not distort the current capability model.

## 3. Component / service design (high level)

### 3.1 Components

WP123 defines four high-level deliverables:

1. Public capability inventory
   - Defines what `PublicApiHost` is allowed to expose.

2. Local-only exception list
   - Defines what remains outside the public platform surface.

3. Out-of-scope tooling list
   - Defines what is intentionally not part of the React/API platform.

4. Deferred-hardening note
   - Explicitly postpones environment gating and business audit design.

### 3.2 Data flows

Public platform flow:
1. The React application calls `PublicApiHost`.
2. `PublicApiHost` routes to public search or admin capability groups.
3. Backend query or ingestion/provider logic executes behind that boundary.

Local-only exception flow:
1. A developer/operator uses `FileShareEmulator` in the local Aspire stack.
2. Emulator-specific controls such as queue clearing, batch reset, or local file access stay inside the emulator surface.
3. Those controls are not re-expressed as `PublicApiHost` capabilities in the current phase.

### 3.3 Key decisions

- Recommendation: ignore environment as a capability-design axis for now.
- Recommendation: assume `PublicApiHost` capabilities are available in all environments for now.
- Recommendation: keep `FileShareEmulator` as the main local-only exception outside the public platform surface.
- Recommendation: classify capabilities by public search, admin query, admin ingestion/provider, system endpoints, and local-only exceptions.
- Recommendation: defer business audit and later live-restriction design until the new React platform is working and the business requirements are clearer.

## 4. Functional requirements

FR1. WP123 shall define and maintain a canonical capability inventory for the public platform.

FR2. The capability inventory shall classify capabilities as one of the following: end-user search capability, admin query capability, admin ingestion/provider capability, system endpoint capability, local-only exception, or out-of-scope tooling.

FR3. `PublicApiHost` capabilities shall be assumed available in all environments for now.

FR4. `FileShareEmulator` shall remain the primary local-only exception outside `PublicApiHost`.

FR5. Emulator controls such as queue clearing, batch index reset, and local batch file handling shall remain outside the public platform surface.

FR6. Configuration emulator tooling shall remain out of scope for React consolidation.

FR7. Mutating or high-risk capabilities may exist inside the public admin surface, but environment-specific availability restrictions are deferred.

FR8. WP123 shall not require environment-based suppression logic as a prerequisite for exposing public or admin capabilities through `PublicApiHost`.

FR9. WP123 shall explicitly defer business audit and live-environment hardening concerns until later work.

FR10. Later work packages shall not treat the absence of environment gating in this phase as an accidental omission.

## 5. Non-functional requirements

NFR1. The capability boundary shall remain simple enough to implement without speculative environment-policy branches.

NFR2. The capability boundary shall preserve the clear local-only exception for `FileShareEmulator`.

NFR3. The specification shall be explicit about what is deferred so later hardening work can add restrictions without architectural rework.

NFR4. The specification shall avoid mixing capability ownership decisions with undeclared business-audit requirements.

## 6. Data model

WP123 introduces a capability register rather than a runtime DTO model.

Each inventory record should answer these fields:
- Capability group.
- Owning surface.
- Public/admin/local-only/out-of-scope classification.
- Mutating or read-oriented posture.
- Notes on deferred hardening.

Initial capability register:

| Capability group | Owning surface | Classification | Mutating posture | Notes |
| --- | --- | --- | --- | --- |
| End-user search | `PublicApiHost` | Public capability | Read-oriented | Authenticated search only, per WP122 |
| Admin query diagnostics and rule workflows | `PublicApiHost` | Admin capability | Mixed read/mutate | Available in all environments for now |
| Admin ingestion/provider/journal/failure/replay workflows | `PublicApiHost` | Admin capability | Mixed read/mutate | Available in all environments for now |
| Health/readiness/profile/version endpoints | `PublicApiHost` | System endpoint capability | Read-oriented | Supports startup and diagnostics |
| Queue clearing and poison-queue clearing | `FileShareEmulator` | Local-only exception | Mutating | Stays outside the public platform |
| Batch index reset and local indexing control | `FileShareEmulator` | Local-only exception | Mutating | Stays outside the public platform |
| Local batch ZIP/file access via emulator surface | `FileShareEmulator` | Local-only exception | Read-oriented | Stays outside the public platform |
| Configuration explorer/tooling | Configuration emulator surface | Out-of-scope tooling | Mixed | Not a React consolidation target |

## 7. Interfaces & integration

### 7.1 Public platform boundary

`PublicApiHost` owns the public platform capability groups. Later work packages should model API contracts, UI flows, and auth around those groups rather than around environment labels.

### 7.2 Local-only exception boundary

`FileShareEmulator` remains outside the public platform capability set. Its operations can be referenced for developer tooling context, but they are not requirements for `PublicApiHost` parity in this phase.

### 7.3 Deferred hardening boundary

Environment-specific restriction and business audit are deliberately deferred. Later hardening work may refine access, but that is not part of WP123's capability model.

## 8. Observability (logging/metrics/tracing)

WP123 does not define business audit.

Its observability concern is only structural:
- public capability groups should be observable through `PublicApiHost`,
- local-only exception behavior remains within the emulator surface,
- and later technical observability work can build on this simpler capability map.

## 9. Security & compliance

WP123 does not design the auth model; that remains in WP122.

Its security contribution is boundary clarity:
- know which capabilities belong in the public platform,
- know which remain local-only,
- and avoid inventing environment restrictions before the platform itself is stable.

## 10. Testing strategy

WP123 validation should focus on boundary correctness rather than runtime execution.

Validation anchors:
- Confirm the capability boundary aligns with WP120-WP122.
- Confirm `FileShareEmulator`-only controls remain outside `PublicApiHost` requirements.
- Confirm the spec does not require environment-based gating in the current phase.
- Confirm later work packages can consume this capability map without needing live-environment policy decisions first.

## 11. Rollout / migration

WP123 has no runtime rollout because it records a capability boundary rather than an implementation change.

Its procedural effect is:
1. Stop using environment labels as the main capability-planning axis.
2. Keep `FileShareEmulator` as the explicit local-only exception.
3. Let the new public platform be built first.
4. Add environment and audit hardening later if the business actually requires it.

Wiki review result:
No wiki page update was required for this draft work-package specification. The work simplifies planning rules and capability boundaries rather than changing current runtime behavior.

## 12. Open questions

No open questions remain in WP123 at this stage. The capability boundary is fixed as `PublicApiHost` capabilities available in all environments for now, `FileShareEmulator` as the local-only exception, and environment/audit hardening deferred until later work.