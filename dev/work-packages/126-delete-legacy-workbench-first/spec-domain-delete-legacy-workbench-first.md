# Specification: WP126 Delete The Legacy Workbench First

Target output path: `dev/work-packages/126-delete-legacy-workbench-first/spec-domain-delete-legacy-workbench-first.md`

Date: 2026-07-01

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md](../120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md)
- [../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md](../121-react-facing-api-host-strategy/spec-domain-react-facing-api-host-strategy.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines the deletion-first work package that removes the legacy Workbench tree under `src/Workbench/` before the new internal `WorkbenchHost` is introduced.

The purpose is clarity. The old Workbench implementation and the future internal `WorkbenchHost` must not coexist under the same name or compete for the same conceptual role. If useful behavior is needed later, it can be reintroduced deliberately from history or by re-implementation, not by leaving the old shell in place.

### 1.2 Scope

In scope for WP126:
- remove the legacy Workbench host from AppHost,
- remove legacy Workbench projects from the maintained solution,
- delete the legacy `src/Workbench/` tree,
- update planning and runtime-composition documentation so the old Workbench is no longer described as active,
- and leave later work free to introduce a brand-new internal `WorkbenchHost` under `src/Hosts/` without ambiguity.

Out of scope for WP126:
- creating the new internal `WorkbenchHost`,
- migrating any specific feature into the new host,
- deleting `tools/RulesWorkbench`,
- or changing `FileShareEmulator`.

### 1.3 Stakeholders

- Arc 02 owners who need the legacy Workbench removed before the new host is created.
- AppHost and solution maintainers who own active composition.
- Future internal-host authors who need a clean namespace and project identity.

### 1.4 Definitions

- Legacy Workbench tree: Everything under `src/Workbench/`, including server hosts, shared Workbench libraries, modules, samples, and vendored Radzen/demo material.
- Delete-first: Remove now rather than keeping the code in-tree as migration reference.
- Future internal `WorkbenchHost`: The new internal host that will be introduced later under `src/Hosts/`.

## 2. System context

### 2.1 Pre-implementation baseline

Evidence checked before execution:
- [../../../src/Hosts/AppHost/AppHost.cs](../../../src/Hosts/AppHost/AppHost.cs) started the legacy `WorkbenchHost`.
- [../../../Search.slnx](../../../Search.slnx) included Workbench-related projects from `src/Workbench/`.
- [../../../src/Workbench](../../../src/Workbench) contained:
  - `server/`
  - `modules/`
  - `samples/`
  - `radzen-blazor/`

### 2.2 Proposed state

After WP126:
- AppHost no longer references the legacy `WorkbenchHost`.
- the maintained solution no longer references legacy Workbench projects.
- the entire `src/Workbench/` tree is deleted.
- planning documents no longer treat the old Workbench as an active or future-source surface.
- any later internal `WorkbenchHost` work starts from a clean slate.

### 2.3 Constraints

- Do not keep the legacy Workbench tree around as a convenience reference.
- Do not rename the old Workbench into some other legacy project to avoid deletion.
- Do not delete `tools/RulesWorkbench` as part of this work package.
- Do not touch `FileShareEmulator` beyond documentation references if needed.

## 3. Key decisions

- Delete the old Workbench tree outright rather than quarantining it in-place.
- Remove its AppHost and solution participation before introducing a new internal `WorkbenchHost`.
- Rely on version history for later source mining rather than keeping the old shell in-tree.
- Treat the deletion as an enabling prerequisite, not as optional cleanup.

## 4. Functional requirements

FR1. WP126 shall remove the legacy `WorkbenchHost` from AppHost services-mode orchestration.

FR2. WP126 shall remove legacy Workbench projects from the maintained solution.

FR3. WP126 shall delete the entire `src/Workbench/` tree.

FR4. WP126 shall update planning and runtime-composition documentation so the old Workbench is no longer described as an active future-facing surface.

FR5. WP126 shall preserve the ability to mine legacy behavior later through repository history rather than through live source retention.

FR6. WP126 shall not create the new internal `WorkbenchHost`.

FR7. WP126 shall not delete `tools/RulesWorkbench`.

FR8. WP126 shall not modify `FileShareEmulator` behavior.

## 5. Non-functional requirements

NFR1. The result shall eliminate naming ambiguity around `WorkbenchHost`.

NFR2. The result shall reduce accidental coupling to the legacy Workbench shell architecture.

NFR3. The result shall leave the repository in a state where future internal-host work starts cleanly under `src/Hosts/`.

## 6. Data model

Removal target inventory:

| Path group | Removal expectation |
| --- | --- |
| `src/Workbench/server/` | Delete |
| `src/Workbench/modules/` | Delete |
| `src/Workbench/samples/` | Delete |
| `src/Workbench/radzen-blazor/` | Delete |
| AppHost reference to legacy `WorkbenchHost` | Remove |
| Solution references to legacy Workbench projects | Remove |

## 7. Interfaces and integration

WP126 removes legacy runtime and solution integration. It does not introduce a replacement host in the same work package.

## 8. Observability

WP126 does not define new observability behavior. It only removes the legacy Workbench surface from active runtime composition.

## 9. Security and compliance

WP126 improves boundary clarity by removing an obsolete internal browser host before a new internal host is introduced.

## 10. Testing strategy

Validation anchors:
- confirm AppHost no longer references the legacy `WorkbenchHost`,
- confirm the maintained solution no longer references `src/Workbench/` projects,
- confirm the `src/Workbench/` tree is absent,
- confirm planning docs no longer treat the old Workbench as active future-source material.

## 11. Rollout and migration

Recommended migration posture:
1. remove orchestration and solution references,
2. delete the legacy source tree,
3. update documentation,
4. only then begin new internal `WorkbenchHost` work.

Wiki review result:
No wiki page update is required until implementation occurs. This work package records a planned deletion and planning-direction update rather than a completed runtime change.

## 12. Open questions

None at this stage. WP126 fixes the deletion-first prerequisite unambiguously.