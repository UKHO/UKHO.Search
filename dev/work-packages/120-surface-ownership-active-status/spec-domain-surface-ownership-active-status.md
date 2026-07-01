# Specification: WP120 Surface Ownership, Active Status, And Legacy Workbench Disposition

Target output path: `dev/work-packages/120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md`

Date: 2026-07-01

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification records the current browser-facing and API-relevant surfaces in the repository and classifies each one as one of the following:

- retained runtime host,
- public host target,
- internal host target,
- local-only tool,
- out-of-scope infrastructure,
- temporary legacy surface,
- or delete-first legacy surface.

WP120 exists to stop Arc 02 decisions from living only in discussion. The immediate clarifying decision is that the old Workbench tree under `src/Workbench/` is not future-source material. It is legacy code to be removed before a new internal `WorkbenchHost` is introduced.

### 1.2 Scope

In scope for WP120:
- classify each browser-facing or API-relevant surface that matters to Arc 02,
- separate the retained runtime from public and internal browser-host targets,
- distinguish temporary legacy tools from delete-first legacy code,
- record the evidence that supports each classification,
- and define the handoff expectations for later Arc 02 and implementation work packages.

Out of scope for WP120:
- implementing the host split,
- implementing authentication or authorization,
- deleting the old Workbench tree,
- or designing request and response contracts.

### 1.3 Stakeholders

- Arc 02 owners who must freeze surface ownership before UI and API implementation.
- Authors of WP121-WP126.
- Platform maintainers responsible for preventing accidental coupling to legacy surfaces.
- Security and operational reviewers who need explicit public, internal, local-only, and delete-first boundaries.

### 1.4 Definitions

- Public host target: The long-term customer-facing browser host.
- Internal host target: The long-term internal developer and admin browser host.
- Retained runtime host: A host that remains in the runtime path but is not the future browser product surface for a given audience.
- Temporary legacy surface: A surface that may still run today and may still be inspected for behavior reference, but is planned for replacement.
- Delete-first legacy surface: Legacy code that should be removed early to avoid naming, ownership, or architectural confusion.
- Local-only tool: A surface intentionally kept for local development or operator-only use.

## 2. System context

### 2.1 Current state

Evidence checked:
- [../../../src/Hosts/AppHost/AppHost.cs](../../../src/Hosts/AppHost/AppHost.cs) starts `IngestionServiceHost`, `QueryServiceHost`, `FileShareEmulator`, `RulesWorkbench`, Keycloak, and configuration emulator support in services mode.
- [../../../Search.slnx](../../../Search.slnx) includes the current maintained host and tool projects and no longer includes the deleted legacy `src/Workbench/` tree.
- [../../../src/Hosts/QueryServiceHost/Program.cs](../../../src/Hosts/QueryServiceHost/Program.cs) shows an active Interactive Server Blazor host with shared browser-host auth.
- [../../../src/Hosts/IngestionServiceHost/Program.cs](../../../src/Hosts/IngestionServiceHost/Program.cs) shows an active runtime host with browser UI still attached.
- [../../../tools/RulesWorkbench/Program.cs](../../../tools/RulesWorkbench/Program.cs) shows a direct internal tool host that remains useful for behavior reference but is not the long-term internal platform direction.
- WP126 has removed the legacy `src/Workbench/` tree from the repository, AppHost, and the maintained solution.
- [../../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs](../../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs) shows a retained but detached Studio API host.

Two current-state distinctions matter most:

1. AppHost participation is the strongest signal for what runs today.
2. AppHost participation does not decide what survives the next-gen direction.

### 2.2 Proposed state

After WP120:
- `QueryServiceHost` is the public host target.
- a new `WorkbenchHost` is the internal host target.
- `IngestionServiceHost` remains a retained runtime host.
- the provider mechanism remains retained runtime support.
- `FileShareEmulator` remains a local-only tool.
- `tools/RulesWorkbench` becomes a temporary legacy surface to be replaced later.
- the entire `src/Workbench/` tree becomes a delete-first legacy surface.
- retained Studio surfaces remain historical reference material unless a later work package explicitly revives narrow pieces of behavior.

### 2.3 Constraints

- WP120 must describe the repository as it exists now while still fixing the target ownership model.
- WP120 must not treat currently running legacy UI as future-source material by default.
- FileShareEmulator must remain local-only.
- The old Workbench tree must be treated as an early removal target, not as the seed for the future internal host.

## 3. Key decisions

- `QueryServiceHost` remains the customer-facing search host target.
- `IngestionServiceHost` remains the ingestion/runtime host.
- the provider mechanism remains runtime support and is not a retirement target.
- `FileShareEmulator` remains local-only and out of product-host scope.
- `tools/RulesWorkbench` is temporary legacy tooling that may still inform later migration slices but is not the long-term host direction.
- the legacy Workbench tree under `src/Workbench/` is a delete-first legacy surface and should not survive into the new naming model.
- retained Studio surfaces remain detached historical source unless later work explicitly mines or revives them.

## 4. Functional requirements

FR1. WP120 shall define and maintain a canonical inventory of browser-facing and API-relevant surfaces for Arc 02.

FR2. The inventory shall classify each relevant surface as one of the following: public host target, internal host target, retained runtime host, retained runtime support, local-only tool, temporary legacy surface, delete-first legacy surface, or out-of-scope infrastructure.

FR3. Each classification shall cite concrete evidence from current repository structure, startup wiring, or solution composition.

FR4. `QueryServiceHost` shall be classified as the public host target.

FR5. `IngestionServiceHost` shall be classified as a retained runtime host.

FR6. The provider mechanism shall be classified as retained runtime support.

FR7. `FileShareEmulator` shall be classified as a local-only tool.

FR8. `tools/RulesWorkbench` shall be classified as a temporary legacy surface rather than as the target internal host.

FR9. The entire `src/Workbench/` tree shall be classified as a delete-first legacy surface.

FR10. Retained Studio surfaces shall be classified as historical reference material unless a later work package explicitly changes that disposition.

FR11. Later Arc 02 and implementation work packages shall reference this specification when they depend on surface ownership or retirement assumptions.

FR12. The specification shall make clear that the future internal `WorkbenchHost` name does not imply reuse of the old Workbench tree.

## 5. Non-functional requirements

NFR1. The inventory shall be current-state and evidence-backed.

NFR2. The classification record shall remain stable at a canonical path so later work packages can reference it without ambiguity.

NFR3. The specification shall avoid overstating future value for legacy surfaces that are only being retained temporarily or deleted early.

NFR4. The specification shall be explicit enough to prevent accidental reuse of old Workbench shell code under the same name.

## 6. Data model

Initial inventory register:

| Surface | Primary path | Current state | Classification | Carry-forward |
| --- | --- | --- | --- | --- |
| QueryServiceHost | [../../../src/Hosts/QueryServiceHost](../../../src/Hosts/QueryServiceHost) | Active AppHost browser host | Public host target | WP121-WP125, Arc 09 |
| IngestionServiceHost | [../../../src/Hosts/IngestionServiceHost](../../../src/Hosts/IngestionServiceHost) | Active AppHost runtime/browser host | Retained runtime host | WP121-WP125, Arc 05-08 |
| Provider mechanism | [../../../src/UKHO.Search.ProviderModel](../../../src/UKHO.Search.ProviderModel) and [../../../src/UKHO.Search.Ingestion.Providers.FileShare](../../../src/UKHO.Search.Ingestion.Providers.FileShare) | Active through runtime hosts | Retained runtime support | Arc 01, Arc 06 |
| FileShareEmulator | [../../../tools/FileShareEmulator](../../../tools/FileShareEmulator) | Active local tool | Local-only tool | WP123 only for boundary confirmation |
| RulesWorkbench | [../../../tools/RulesWorkbench](../../../tools/RulesWorkbench) | Active internal legacy tool | Temporary legacy surface | Arc 06-08 replacement planning |
| Legacy Workbench tree | Deleted by WP126 | Removed from AppHost, the maintained solution, and the repository tree | Delete-first legacy surface | Completed prerequisite for later internal-host work |
| Retained Studio surfaces | [../../../src/Studio/StudioServiceHost](../../../src/Studio/StudioServiceHost), [../../../src/Studio/UKHO.Search.Studio](../../../src/Studio/UKHO.Search.Studio), and [../../../src/Providers/UKHO.Search.Studio.Providers.FileShare](../../../src/Providers/UKHO.Search.Studio.Providers.FileShare) | Detached historical source | Temporary historical reference | Later explicit review only |
| Configuration emulator support | [../../../configuration](../../../configuration) | Active supporting infrastructure | Out-of-scope infrastructure | WP123 |

## 7. Interfaces and integration

WP120 is evidence-driven rather than code-generated. The approved evidence sources are:
- AppHost wiring,
- solution membership,
- host bootstrap code,
- direct tool routing where local-only boundaries matter,
- and current wiki coverage of active runtime composition.

## 8. Observability

WP120 introduces no runtime observability changes. Its observability role is documentary:
- make the retained runtime visible,
- make the local-only boundary visible,
- and make the delete-first Workbench decision visible before implementation begins.

## 9. Security and compliance

WP120 does not implement auth changes. Its security contribution is boundary clarity:
- local-only emulator surfaces are not product-host candidates,
- legacy Workbench code is not future platform direction,
- and temporary legacy tooling is not equivalent to approved long-term architecture.

## 10. Testing strategy

WP120 validation is evidence review rather than runtime implementation testing.

Validation anchors:
- confirm AppHost services-mode participants in [../../../src/Hosts/AppHost/AppHost.cs](../../../src/Hosts/AppHost/AppHost.cs),
- confirm solution participation in [../../../Search.slnx](../../../Search.slnx),
- confirm the legacy Workbench tree shape under [../../../src/Workbench](../../../src/Workbench),
- and confirm later work packages cite this specification when they depend on ownership or retirement assumptions.

## 11. Rollout and migration

WP120 has no runtime rollout. Its migration effect is procedural:
1. surface classification stops living only in discussion,
2. the delete-first status of the old Workbench tree becomes explicit,
3. later Arc 02 work packages refine and implement decisions against this stored baseline.

Wiki review result:
No wiki page update was required for this planning work package. Reviewed [../../../wiki/Architecture-Walkthrough.md](../../../wiki/Architecture-Walkthrough.md), [../../../wiki/Ingestion-Walkthrough.md](../../../wiki/Ingestion-Walkthrough.md), and [../../../wiki/Tools-RulesWorkbench.md](../../../wiki/Tools-RulesWorkbench.md). Those pages describe the current runtime and tooling state; WP120 stores future ownership decisions rather than changing current runtime behavior.

## 12. Open questions

None at this stage. WP120 now fixes the high-level surface boundary as follows: keep `QueryServiceHost`, `IngestionServiceHost`, and the provider mechanism; leave `FileShareEmulator` local-only; replace `RulesWorkbench` later; and delete the old `src/Workbench/` tree before introducing the new internal `WorkbenchHost`.