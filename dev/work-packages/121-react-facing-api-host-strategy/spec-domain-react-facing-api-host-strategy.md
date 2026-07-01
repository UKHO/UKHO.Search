# Specification: WP121 Browser Host Strategy And Audience Split

Target output path: `dev/work-packages/121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md`

Date: 2026-07-01

Repository note:
The folder name is retained for numbering continuity. The canonical decision in this file supersedes the earlier React/PublicApiHost assumption.

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the recommended split browser-host topology for the next-gen direction.

The decision is:
- keep `QueryServiceHost` as the customer-facing host for end-user search UI and public search-facing contracts,
- create a brand-new internal `WorkbenchHost` under `src/Hosts/` for developer and admin tooling,
- keep `IngestionServiceHost` as an ingestion/runtime host rather than a future browser product host,
- and delete the legacy Workbench tree under `src/Workbench/` before the new internal host is introduced.

### 1.2 Scope

In scope for WP121:
- choose the permanent audience split,
- define which host owns which user-facing and internal workflows,
- define route and API ownership at a high level,
- define the transition stance on reuse versus fresh host creation,
- and state which repository surfaces are not valid host candidates.

Out of scope for WP121:
- final auth policy details,
- detailed request and response models,
- implementation of every endpoint,
- or the physical deletion work itself.

### 1.3 Stakeholders

- Arc 02 owners deciding the long-term host boundary.
- Public search owners who need a stable customer-facing host.
- Developer and admin tooling owners who need a permanent internal host.
- Security and platform owners who need the audience split fixed before auth hardening.

### 1.4 Definitions

- Public host: The long-term customer-facing browser host.
- Internal host: The long-term internal developer and admin browser host.
- Retained runtime host: A host that remains in the runtime path but is not itself the future browser platform for a given audience.
- Transition delegation: Temporary calling patterns used while behavior is moved behind the chosen host boundaries.

## 2. System context

### 2.1 Current state

Evidence checked:
- [../../../src/Hosts/AppHost/AppHost.cs](../../../src/Hosts/AppHost/AppHost.cs) starts `IngestionServiceHost`, `QueryServiceHost`, `FileShareEmulator`, and `RulesWorkbench`.
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md) fixes `QueryServiceHost` as the public host target and the old Workbench tree as delete-first legacy code.
- [../../../src/Hosts/QueryServiceHost/Program.cs](../../../src/Hosts/QueryServiceHost/Program.cs) shows an active Interactive Server Blazor host with shared browser-host auth.
- [../../../src/Hosts/IngestionServiceHost/Program.cs](../../../src/Hosts/IngestionServiceHost/Program.cs) shows an active runtime host with browser UI still attached.
- [../../../tools/RulesWorkbench/Program.cs](../../../tools/RulesWorkbench/Program.cs) shows a direct internal tool host that remains legacy tooling rather than the long-term host target.

### 2.2 Proposed state

The recommended topology is:
- `QueryServiceHost` is the public host.
- a new `WorkbenchHost` under `src/Hosts/WorkbenchHost` is the internal host.
- `IngestionServiceHost` remains a runtime host used behind those browser hosts.
- the provider mechanism remains runtime support.
- the old Workbench tree under `src/Workbench/` is removed before the new internal host is introduced.

High-level ownership:
- `QueryServiceHost` owns customer-facing search routes, customer-facing search UI, and public search-facing contracts.
- `WorkbenchHost` owns internal query-rule, ingestion-repair, provider-tooling, and operational workflows.
- `IngestionServiceHost` owns ingestion runtime behavior and background processing rather than a long-term browser UI.

### 2.3 Assumptions

- The customer-facing and internal audiences are permanently separate.
- That permanent audience split is more important than sharing one browser host.
- The future internal host should be created fresh rather than inheriting the deleted legacy Workbench shell.
- Shared services and deliberate HTTP contracts can coexist, but host ownership must be fixed before those implementation details are chosen.

### 2.4 Constraints

- `FileShareEmulator` remains outside the product-host topology.
- The legacy Workbench tree is not a host candidate.
- Temporary legacy tooling such as RulesWorkbench does not define the long-term host topology.
- Host-local DTOs must not become public or internal contracts without deliberate review.

## 3. Key decisions

- `QueryServiceHost` remains the public host.
- `WorkbenchHost` is the permanent internal host name, but it will be a new host under `src/Hosts/`, not the old `src/Workbench/` implementation.
- `IngestionServiceHost` remains distinct as an ingestion/runtime host.
- The customer-facing and internal host split is deliberate and permanent.
- The old Workbench tree is deleted before the new internal host is introduced.
- `RulesWorkbench` is not the long-term internal host, even if it remains useful for short-term behavior reference.

## 4. Functional requirements

FR1. The next-gen direction shall use two browser hosts rather than one undifferentiated browser host.

FR2. `QueryServiceHost` shall be the customer-facing search host.

FR3. A new `WorkbenchHost` under `src/Hosts/` shall be the permanent internal developer and admin host.

FR4. `IngestionServiceHost` shall remain a retained runtime host rather than the future customer-facing or internal browser platform.

FR5. The old Workbench tree under `src/Workbench/` shall not be reused as the implementation basis of the new internal host.

FR6. Customer-facing search workflows shall belong to `QueryServiceHost`.

FR7. Internal query-rule, ingestion-repair, provider-tooling, and operational workflows shall belong to `WorkbenchHost`.

FR8. Public search-facing contracts shall be owned by `QueryServiceHost`.

FR9. Internal tooling contracts and internal workflow endpoints shall be owned by `WorkbenchHost` unless a later work package explicitly assigns them elsewhere.

FR10. Host ownership decisions shall remain valid whether a given interaction uses direct server-side composition or explicit HTTP endpoints.

FR11. Later work packages shall not treat `RulesWorkbench` or the deleted Workbench tree as the architectural seed of the long-term internal host.

## 5. Non-functional requirements

NFR1. The host topology shall reflect the permanent audience split rather than temporary implementation convenience.

NFR2. The topology shall reduce the risk of internal tooling concepts leaking into the customer-facing host.

NFR3. The topology shall reduce the risk of customer-facing requirements constraining the internal workbench unnecessarily.

NFR4. The topology shall keep the new internal host free from legacy Workbench shell coupling.

## 6. Data model

High-level host ownership register:

| Host or surface | Long-term role | Notes |
| --- | --- | --- |
| `QueryServiceHost` | Public host | Customer-facing search UI and public search-facing contracts |
| New `WorkbenchHost` | Internal host | Developer/admin workbench under `src/Hosts/` |
| `IngestionServiceHost` | Retained runtime host | Ingestion runtime and background processing |
| `FileShareEmulator` | Local-only tool | Not part of product-host topology |
| `tools/RulesWorkbench` | Temporary legacy tool | Replacement target, not long-term host |
| `src/Workbench/` | Delete-first legacy tree | Removed before new internal host introduction |

## 7. Interfaces and integration

The public and internal hosts may each use a mix of:
- shared service composition,
- deliberate HTTP endpoints for browser interactions,
- and transitional internal delegation while replacement work is in flight.

WP121 fixes ownership, not every integration mechanism.

## 8. Observability

WP121 does not define the technical observability baseline. It only fixes which host will ultimately own public search traffic and which host will own internal operational workflows.

## 9. Security and compliance

WP121 fixes the audience split that later security work depends on:
- customer-facing search is not hosted beside permanent internal workflows in one browser host,
- internal operational tooling is not treated as a feature area of the public host,
- and the deleted legacy Workbench tree is not allowed to drive future security posture by accident.

## 10. Testing strategy

WP121 validation should focus on architectural correctness and downstream implementability.

Validation anchors:
- confirm the host strategy is consistent with WP120,
- confirm the split leaves a clean public/internal boundary,
- confirm the future internal host is a fresh host rather than a renamed old Workbench shell,
- and confirm later work packages can assign capabilities and auth policy against this topology.

## 11. Rollout and migration

Recommended migration posture:
1. fix the ownership model,
2. delete the old Workbench tree,
3. create the new internal `WorkbenchHost` under `src/Hosts/`,
4. evolve `QueryServiceHost` toward the public search product role,
5. replace temporary legacy internal tools over time.

Wiki review result:
No wiki page update was required for this planning work package. The work records the target host topology rather than a current-state runtime change.

## 12. Open questions

None at this stage. WP121 now fixes the host strategy as a permanent split between `QueryServiceHost` for customer-facing search and a new internal `WorkbenchHost`, while rejecting reuse of the old `src/Workbench/` implementation.