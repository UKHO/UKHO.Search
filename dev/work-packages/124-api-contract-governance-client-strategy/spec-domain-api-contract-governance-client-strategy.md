# Specification: WP124 API Contract Governance And Host Integration Strategy

Target output path: `dev/work-packages/124-api-contract-governance-client-strategy/spec-domain-api-contract-governance-client-strategy.md`

Date: 2026-07-01

Repository note:
The folder name is retained for numbering continuity. The canonical decision in this file supersedes the earlier React client-strategy assumption.

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md)
- [../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md](../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md)
- [../123-capability-boundaries-local-only-exceptions/spec-domain-capability-boundaries-local-only-exceptions.md](../123-capability-boundaries-local-only-exceptions/spec-domain-capability-boundaries-local-only-exceptions.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the contract-governance baseline for the split browser-host direction and the rule for when a host should expose deliberate HTTP contracts versus when server-side composition is acceptable.

The abandoned React direction made a browser-side typed fetch layer central. That is no longer the right baseline. The new direction is:
- keep deliberate HTTP contracts for public search and any host interactions that genuinely cross browser or host boundaries,
- allow server-side composition inside Interactive Server Blazor hosts where that is the simplest correct implementation,
- and require both public and internal contracts to avoid leaking host-local DTOs or storage-local detail.

### 1.2 Scope

In scope for WP124:
- define public and internal contract-governance rules,
- define the baseline error-shape rule,
- define when server-side host composition is acceptable,
- and define leak-prevention rules for host-local and storage-local shapes.

Out of scope for WP124:
- implementing the APIs,
- detailed auth policy,
- pagination standardization,
- or business audit requirements.

### 1.3 Stakeholders

- Public-host authors who need stable customer-facing contracts.
- Internal-host authors who need deliberate internal workflow contracts when endpoints are exposed.
- Backend authors who need clear leak-prevention rules.
- Later work packages that implement search, diagnostics, and repair workflows.

### 1.4 Definitions

- Deliberate contract: An explicit request and response model exposed intentionally by a host endpoint.
- Host-local model: A model shaped for a specific Blazor host's internal state or page logic.
- Storage-local model: A model shaped around SQL rows, storage keys, blob names, or other persistence details.
- Server-side composition: A Blazor host calling application services directly without forcing a browser-facing HTTP layer for every interaction.

## 2. System context

### 2.1 Current state

Evidence checked:
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md) fixes the split between `QueryServiceHost` and `WorkbenchHost`.
- [../../../src/Hosts/QueryServiceHost/Program.cs](../../../src/Hosts/QueryServiceHost/Program.cs) and [../../../src/Hosts/IngestionServiceHost/Program.cs](../../../src/Hosts/IngestionServiceHost/Program.cs) remain current runtime hosts with host-local UI state and models.
- [../../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs](../../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs) shows that deliberate HTTP contracts exist elsewhere in the repository, even though that surface is detached.
- [../../../tools/RulesWorkbench/Program.cs](../../../tools/RulesWorkbench/Program.cs) shows legacy internal tooling that currently mixes UI behavior and direct service/data access.

### 2.2 Proposed state

The recommended direction is:
- `QueryServiceHost` owns deliberate public search-facing contracts.
- `WorkbenchHost` owns deliberate internal workflow contracts when the browser or another host needs them.
- Both hosts may use direct server-side composition for internal page logic where no cross-browser or cross-host contract is needed.
- RFC 9457-style Problem Details remains the baseline public and internal error shape for deliberate endpoints.
- Host-local Blazor DTOs, provider SQL shapes, storage keys, blob names, and legacy shell state must not leak into deliberate contracts.

### 2.3 Assumptions

- Interactive Server Blazor reduces the need for a browser-managed API client layer, but it does not remove the need for contract discipline.
- Public search endpoints need stronger contract stability than internal workbench endpoints.
- Internal workbench endpoints still need deliberate contracts when exposed, even if they change faster than public contracts.

### 2.4 Constraints

- Public search contracts must not depend on host-local QueryServiceHost page models.
- Internal workbench contracts must not depend on deleted Workbench shell state or temporary legacy tooling models.
- Server-side composition is acceptable only when it does not hide a real cross-boundary contract need.

## 3. Key decisions

- Use deliberate HTTP contracts for customer-facing search endpoints.
- Use deliberate HTTP contracts for internal workbench endpoints that are called from browser interactions or cross-host integration.
- Allow direct server-side composition inside Interactive Server Blazor hosts for purely internal page and component workflows.
- Standardize on RFC 9457-style Problem Details for deliberate endpoint errors.
- Prohibit leakage of host-local Blazor state, provider SQL shapes, storage keys, blob names, and deleted Workbench shell artifacts into deliberate contracts.
- Defer pagination and generated-client tooling decisions until real endpoint shapes and workloads exist.

## 4. Functional requirements

FR1. Public search-facing endpoints owned by `QueryServiceHost` shall expose explicit deliberate request and response contracts.

FR2. Internal workbench endpoints owned by `WorkbenchHost` shall expose explicit deliberate request and response contracts when they cross browser or host boundaries.

FR3. Direct server-side composition shall be allowed inside Interactive Server Blazor hosts when no deliberate externalized contract is required.

FR4. Host-local Blazor page models shall not be promoted directly into deliberate contracts.

FR5. Storage-local shapes such as provider SQL rows, storage keys, or blob names shall not be promoted directly into deliberate contracts unless a later work package explicitly justifies them.

FR6. Deliberate error responses shall use RFC 9457-style Problem Details.

FR7. Later work packages shall document whether a given workflow uses deliberate HTTP contracts, direct host composition, or a mix of both.

FR8. Contract governance shall not depend on a browser-side typed fetch layer being the primary integration mechanism.

## 5. Non-functional requirements

NFR1. Contract rules shall preserve strong boundary discipline without forcing unnecessary HTTP indirection.

NFR2. The baseline shall remain flexible enough to support fast internal workbench evolution.

NFR3. The baseline shall preserve stronger stability for customer-facing search contracts.

NFR4. The baseline shall reduce accidental contract drift between host internals and deliberate endpoint models.

## 6. Data model

Required contract categories:
- public search request and response shapes,
- internal query diagnostics and rule-workflow shapes,
- internal ingestion-repair and provider-tooling shapes,
- system metadata and health shapes,
- and common RFC 9457-style error shapes.

## 7. Interfaces and integration

### 7.1 Public host rule

`QueryServiceHost` should treat customer-facing search endpoints as deliberate contracts.

### 7.2 Internal host rule

`WorkbenchHost` should use deliberate contracts for browser-exposed workflow endpoints, but it may use direct server-side composition for internal page orchestration that does not justify an HTTP boundary.

### 7.3 Runtime-host rule

`IngestionServiceHost` remains a runtime host. Its retained runtime role does not imply that it must become a broad browser-facing contract surface.

## 8. Observability

WP124 does not define the observability baseline, but deliberate contracts and deliberate error shapes should make later diagnostics more understandable.

## 9. Security and compliance

WP124 does not redefine auth. Its security contribution is contract hygiene:
- do not leak internal identifiers or storage-local detail,
- do not let host-local page state become an implicit API surface,
- and keep public versus internal contracts understandable for later authorization review.

## 10. Testing strategy

Validation anchors:
- confirm the contract strategy is consistent with WP121-WP123,
- confirm the rules permit direct host composition where appropriate,
- confirm deliberate endpoints use Problem Details,
- and confirm later work can add contract tests without redesigning the baseline.

## 11. Rollout and migration

Recommended migration posture:
1. decide which workflows need deliberate endpoints,
2. define explicit contracts for those workflows,
3. allow direct host composition where it simplifies purely internal page behavior,
4. keep leak-prevention rules in force throughout the migration.

Wiki review result:
No wiki page update was required for this planning work package. The work records contract and integration rules rather than a current-state runtime change.

## 12. Open questions

None at this stage. WP124 now fixes the baseline rule set for deliberate contracts and host-internal composition in the split Blazor host direction.