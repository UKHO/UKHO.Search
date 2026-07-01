# Specification: WP123 Capability Boundaries And Local-Only Exceptions

Target output path: `dev/work-packages/123-capability-boundaries-local-only-exceptions/spec-domain-capability-boundaries-local-only-exceptions.md`

Date: 2026-07-01

Repository note:
The folder name is retained for numbering continuity. The canonical decision in this file supersedes the earlier one-host public-platform assumption.

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md)
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md)
- [../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md](../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines which capabilities belong in the public host, which belong in the internal host, which remain runtime-only, and which stay local-only.

The previous simplification of one public platform plus local exceptions is no longer the right planning shape. The permanent audience split means capability ownership is now primarily:
- `QueryServiceHost` public capability,
- `WorkbenchHost` internal capability,
- `IngestionServiceHost` runtime-only capability,
- or local-only exception.

### 1.2 Scope

In scope for WP123:
- define capability ownership between `QueryServiceHost`, `WorkbenchHost`, `IngestionServiceHost`, and local-only tools,
- define the local-only exception boundary for `FileShareEmulator`,
- define the out-of-scope configuration tooling boundary,
- and record which legacy surfaces are replacement targets rather than capability owners.

Out of scope for WP123:
- detailed endpoint design,
- business audit requirements,
- detailed auth policy names,
- or the physical implementation of the capabilities.

### 1.3 Stakeholders

- Arc 02 owners who need host-by-host capability ownership.
- Public search owners.
- Internal workbench owners.
- Ingestion/runtime owners.
- Emulator/tooling owners who need local-only boundaries kept explicit.

### 1.4 Definitions

- Public capability: A capability owned by `QueryServiceHost` for customer-facing search.
- Internal capability: A capability owned by `WorkbenchHost` for developer and admin workflows.
- Runtime-only capability: A capability that stays behind runtime hosts rather than being modeled as a browser-host feature.
- Local-only exception: A capability intentionally kept outside the product hosts.

## 2. System context

### 2.1 Current state

Evidence checked:
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md) fixes the host ownership and legacy disposition baseline.
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md) fixes the split between `QueryServiceHost` and `WorkbenchHost`.
- [../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md](../122-public-api-auth-authorization/spec-domain-public-api-auth-authorization.md) fixes separate public and internal browser-host auth boundaries.
- [../../../tools/FileShareEmulator/Services/IngestionQueueService.cs](../../../tools/FileShareEmulator/Services/IngestionQueueService.cs) contains queue-clearing behavior.
- [../../../tools/FileShareEmulator/Services/IndexService.cs](../../../tools/FileShareEmulator/Services/IndexService.cs) contains local indexing control and reset behavior.
- [../../../tools/FileShareEmulator/Api/BatchFilesApi.cs](../../../tools/FileShareEmulator/Api/BatchFilesApi.cs) exposes local batch-file retrieval.

### 2.2 Proposed state

The recommended capability boundary is:
- `QueryServiceHost` owns customer-facing search capabilities.
- `WorkbenchHost` owns internal query-rule, ingestion-repair, provider-tooling, and operational capabilities.
- `IngestionServiceHost` owns runtime behavior that does not need to become a browser-host feature.
- `FileShareEmulator` remains the main local-only exception.
- configuration emulator tooling remains out of scope.

### 2.3 Assumptions

- The host split is the primary capability-planning axis.
- Public search must stay clean of internal operational concepts.
- Internal tooling must not be distorted by customer-facing UX expectations.
- Some behavior may remain runtime-only even when a related workflow appears in the internal host.

### 2.4 Constraints

- `QueryServiceHost` is not the place for internal rule-authoring, replay, or repair workflows.
- `WorkbenchHost` is not the place for customer-facing product search.
- `FileShareEmulator` remains outside both product hosts.
- configuration emulator tooling remains out of scope.

## 3. Key decisions

- Customer-facing search execution, result display, facets, filtering, and result detail belong to `QueryServiceHost`.
- Query-rule diagnostics, draft editing, rule comparison, and query-corpus workflows belong to `WorkbenchHost`.
- Ingestion-failure queues, journal browsing, replay eligibility, diagnostic replay, guarded replay, and related operator workflows belong to `WorkbenchHost`.
- Background ingestion runtime remains with `IngestionServiceHost`.
- Queue clearing, index deletion, local reset, and local batch-file handling remain with `FileShareEmulator`.

## 4. Functional requirements

FR1. WP123 shall define and maintain a canonical capability inventory for the split-host direction.

FR2. The capability inventory shall classify capabilities as public, internal, runtime-only, local-only, or out-of-scope.

FR3. Customer-facing search capabilities shall belong to `QueryServiceHost`.

FR4. Internal query-rule capabilities shall belong to `WorkbenchHost`.

FR5. Internal ingestion-repair and provider-tooling capabilities shall belong to `WorkbenchHost`.

FR6. Ingestion runtime behavior that does not require a browser-host feature shall remain owned by `IngestionServiceHost`.

FR7. Emulator controls such as queue clearing, index deletion, reset, and local batch-file handling shall remain outside the product hosts.

FR8. Configuration emulator tooling shall remain out of scope for the product hosts.

FR9. Legacy surfaces shall not be treated as capability owners merely because they still exist or still run today.

## 5. Non-functional requirements

NFR1. The capability boundary shall stay simple enough to support implementation planning without speculative host overlap.

NFR2. The capability boundary shall preserve a clean public-versus-internal separation.

NFR3. The capability boundary shall make local-only exceptions explicit enough that they are not reintroduced accidentally into product hosts.

## 6. Data model

Initial capability register:

| Capability group | Owning surface | Classification | Notes |
| --- | --- | --- | --- |
| End-user search execution, facets, result detail | `QueryServiceHost` | Public capability | Customer-facing search product |
| Query diagnostics, draft rule editing, comparison, corpus workflows | `WorkbenchHost` | Internal capability | Developer/admin workbench |
| Ingestion failures, journal browsing, replay, repair, provider tooling | `WorkbenchHost` | Internal capability | Internal operational workflows |
| Background ingestion processing | `IngestionServiceHost` | Runtime-only capability | Not a long-term browser-host feature |
| Queue clearing, index deletion, reset, local batch files | `FileShareEmulator` | Local-only exception | Remains outside product hosts |
| Configuration explorer/tooling | Configuration emulator surface | Out-of-scope | Not a product-host target |

## 7. Interfaces and integration

Later work packages may implement these capabilities using:
- direct server-side composition in the owning host,
- deliberate HTTP endpoints where browser-host interaction benefits from them,
- or a mix of both.

WP123 fixes ownership, not every transport detail.

## 8. Observability

WP123 does not define the observability baseline. It only fixes where capability ownership sits so later logging, traces, metrics, and audit work can be assigned to the correct host.

## 9. Security and compliance

WP123 does not redefine auth. Its security contribution is boundary clarity:
- customer-facing capabilities stay with the public host,
- internal operational capabilities stay with the internal host,
- local-only emulator controls remain outside both.

## 10. Testing strategy

Validation anchors:
- confirm the capability boundary aligns with WP120-WP122,
- confirm emulator-only controls remain outside product-host requirements,
- and confirm later work packages can assign routes and policies without re-opening host ownership.

## 11. Rollout and migration

Recommended migration posture:
1. fix capability ownership by host,
2. build replacement internal workflows in `WorkbenchHost`,
3. keep runtime-only behavior with runtime hosts where browser hosting adds no value,
4. leave local-only behavior in FileShareEmulator.

Wiki review result:
No wiki page update was required for this planning work package. The work records target capability ownership rather than a current-state runtime change.

## 12. Open questions

None at this stage. WP123 now fixes capability ownership by host rather than by a one-host public-platform model.