# Specification: WP120 Surface Ownership And Active Status

Target output path: `dev/work-packages/120-surface-ownership-active-status/spec-domain-surface-ownership-active-status.md`

Date: 2026-07-01

Source material:
- [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md)
- [../../../docs/discussion/next-gen-consolidation-discussion.md](../../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../../docs/discussion/next-gen-work-package-arcs.md](../../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification records the current browser-facing and API-relevant surfaces in the repository and classifies each one as active service-side runtime, local-only emulator/tooling, out-of-scope infrastructure, or retirement candidate.

WP120 exists to stop Arc 02 decisions from living only in the arc outline. Before this file, the ownership and active-status decisions were described in [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md), but they were not yet stored as a canonical work-package specification under `dev/work-packages/`.

WP120 is intentionally a decision-storage and boundary-classification work package. It does not require code changes unless the validation work uncovers a mismatch between the documented state and the actual running or reachable surfaces.

### 1.2 Scope

In scope for WP120:
- Classify each browser-facing or API-relevant surface that matters to Arc 02.
- Separate the service-side runtime that remains in play from the UI surfaces that are to be retired.
- Record the service-side baseline as `IngestionServiceHost`, `QueryServiceHost`, and the provider mechanism.
- Record `FileShareEmulator` as untouched local-only tooling outside the React consolidation path.
- Record the evidence that supports each classification.
- Define the canonical storage location for these decisions.
- Define the handoff expectations for later Arc 02 and implementation work packages.
- State explicitly whether WP120 itself is expected to change code.

Out of scope for WP120:
- Choosing the long-term React-facing API host topology in detail.
- Implementing new APIs, auth flows, route protection, or contract models.
- Reviving, renaming, or deleting retained Studio code.
- Replacing Blazor surfaces with React.
- Hardening local-only boundaries in code unless validation proves the current documentation is wrong.
- Reopening whether Workbench, RulesWorkbench, or retained Studio UI should survive as future UI sources.

### 1.3 Stakeholders

- Arc 02 owners who must freeze host and boundary decisions before frontend and workflow implementation.
- Authors of later Arc 02 work packages WP121-WP125.
- Authors of later implementation work packages that create React apps, HTTP APIs, and developer workflows.
- Search platform maintainers responsible for preventing accidental coupling to historical or local-only surfaces.
- Security and operational reviewers who need explicit local-only, detached, and active-surface boundaries.

### 1.4 Definitions

- Surface: A host, tool, project, endpoint set, or retained code area that could plausibly be treated as a browser-facing or API-owning integration point.
- Active service-side runtime: A service or extension mechanism that remains part of the backend/runtime path during and after UI consolidation.
- Local-only emulator/tool: A surface intended for local development or operator tooling rather than a consolidated product UI or shared API boundary.
- Out-of-scope infrastructure: Supporting infrastructure or configuration tooling that may exist in the repo or local stack but is not a React consolidation target.
- Retirement candidate: A UI or historical surface that is to be replaced rather than evolved as part of the consolidated React-plus-backend direction. Retirement candidates remain available for source inspection and behavior reference, but they should not be modified unless a separate work item requires retirement cleanup or an unrelated build/solution change forces a minimal compatibility edit.

## 2. System context

### 2.1 Current state

Arc 02 currently has an outline specification, but its decisions were not yet persisted as work-package-level source-of-truth documents under `dev/work-packages/`.

Evidence checked:
- [../../../src/Hosts/AppHost/AppHost.cs](../../../src/Hosts/AppHost/AppHost.cs) starts `IngestionServiceHost`, `QueryServiceHost`, `FileShareEmulator`, `RulesWorkbench`, `WorkbenchHost`, Keycloak, and configuration emulator support in services mode.
- [../../../Search.slnx](../../../Search.slnx) includes the current domain, service, infrastructure, Workbench, host, tool, configuration, and test projects, but does not include `StudioServiceHost`, `UKHO.Search.Studio`, or `UKHO.Search.Studio.Providers.FileShare`.
- [../../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs](../../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs) shows a retained Studio API host with local CORS for `http://localhost:3000`, OpenAPI/Scalar endpoints, provider/rules/diagnostics/ingestion/operations mappings, and authorization middleware, but no reviewed endpoint-level policy boundary.
- [../../../tools/FileShareEmulator/Program.cs](../../../tools/FileShareEmulator/Program.cs) shows FileShareEmulator as a direct tool host in the active local stack.
- [../../../tools/FileShareEmulator/Api/BatchFilesApi.cs](../../../tools/FileShareEmulator/Api/BatchFilesApi.cs) shows an explicit GET route at `/batch/{batchId}/files`; the route scan performed for WP120 did not identify a separate shared React-facing API host inside FileShareEmulator.
- [../../../tools/RulesWorkbench/Program.cs](../../../tools/RulesWorkbench/Program.cs) shows RulesWorkbench as a direct Blazor tool host that loads shared configuration and the ingestion rules engine rather than exposing a separately governed product API surface.
- [../../../wiki/Architecture-Walkthrough.md](../../../wiki/Architecture-Walkthrough.md), [../../../wiki/Ingestion-Walkthrough.md](../../../wiki/Ingestion-Walkthrough.md), and [../../../wiki/Tools-RulesWorkbench.md](../../../wiki/Tools-RulesWorkbench.md) already describe the current AppHost-started runtime/tooling state.

Two current-state distinctions matter most:

1. AppHost participation is the strongest signal for what runs today.
2. AppHost participation does not decide what survives the consolidation: WP120 keeps `IngestionServiceHost`, `QueryServiceHost`, and the provider mechanism, leaves `FileShareEmulator` untouched as local-only tooling, and treats the other UI surfaces as retirement candidates.

### 2.2 Proposed state

This specification becomes the canonical WP120 storage location for surface-ownership and active-status decisions.

After WP120:
- Later Arc 02 work packages shall reference this specification when choosing API hosts, auth models, environment safety boundaries, contract governance, and retirement handling.
- Later implementation work packages shall treat this specification as the baseline inventory of what remains service-side, what stays local-only and untouched, and what retires.
- The arc outline in [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md) remains a planning map, but this work-package spec becomes the durable requirement record.

The intended steady-state direction is now explicit:
- `IngestionServiceHost` and `QueryServiceHost` remain the two service-side runtime hosts.
- The provider mechanism remains part of the runtime path.
- `FileShareEmulator` remains untouched local-only tooling.
- Any other UI surface in the repository is retirement-bound in favor of one React application and a suitable backend.
- Retirement-bound projects remain available for source inspection, but they are not active implementation targets and should not be modified unless required for build compatibility or explicit retirement work.

WP120 does not itself introduce runtime behavior changes. Its primary deliverable is a maintained inventory and classification register.

### 2.3 Assumptions

- `AppHost` remains the authoritative composition root for normal local developer startup.
- `Search.slnx` remains the authoritative view of projects that are intentionally part of the maintained solution, even when some of those projects are not runtime entry points.
- Later Arc 02 work packages will define the target API host, auth model, local-only policy, and contract strategy rather than leaving those decisions implicit.
- A retained or currently running UI surface must not be treated as approved future implementation source just because it still exists or still starts under AppHost.

### 2.4 Constraints

- WP120 must describe the current repository state as it exists today, not a speculative future runtime.
- WP120 must not infer stable product APIs from Blazor host-local state or local tool routes.
- FileShareEmulator must remain classified as local-only unless a later work package makes an explicit non-local design decision.
- Non-emulator UI retirement is fixed for WP120 and must not be reopened implicitly through nuanced source-mining assumptions.
- Where evidence is incomplete, the specification must keep that uncertainty explicit rather than pretending a stronger decision exists.

## 3. Component / service design (high level)

### 3.1 Components

WP120 defines three high-level deliverables:

1. Surface inventory register
   - A canonical list of the relevant hosts, tools, retained sources, and supporting surfaces.

2. Classification rules
   - A repeatable way to distinguish what remains as service-side runtime, what stays local-only, and what is retirement-bound.

3. Downstream handoff map
   - Explicit links from the inventory to the later Arc 02 and implementation work packages that must consume the keep/retire boundary.

### 3.2 Data flows

Current decision flow before WP120:
1. Arc 02 intent is described in [../../specs/next-gen-arc02-wp.md](../../specs/next-gen-arc02-wp.md).
2. Later arc documents refer back to Arc 02 for host, auth, and safety decisions.
3. The decisions risk being missed because they are not yet anchored to a canonical per-work-package spec file.

Target decision flow after WP120:
1. Current source code, AppHost wiring, solution participation, and route inventory provide the evidence base.
2. WP120 records the classification and rationale in this canonical spec.
3. WP121-WP125 refine host, auth, safety, contract, and audit decisions against this stored baseline.
4. Implementation work packages such as [../../specs/next-gen-arc03-wp.md](../../specs/next-gen-arc03-wp.md), [../../specs/next-gen-arc04-wp.md](../../specs/next-gen-arc04-wp.md), [../../specs/next-gen-arc06-wp.md](../../specs/next-gen-arc06-wp.md), [../../specs/next-gen-arc09-wp.md](../../specs/next-gen-arc09-wp.md), and [../../specs/next-gen-arc10-wp.md](../../specs/next-gen-arc10-wp.md) implement changes against the recorded boundary.

### 3.3 Key decisions

- WP120 is a documentation-first decision package. Code changes are not a required output of this work package.
- `IngestionServiceHost` and `QueryServiceHost` remain the active service-side runtime hosts.
- The provider mechanism remains active runtime support for the service side.
- `FileShareEmulator` is not to be touched by the consolidation work and remains local-only tooling.
- Any other UI surface, including Workbench, RulesWorkbench, and retained Studio UI/API hosts, is retirement-bound rather than future-source material.
- Retirement-bound projects remain inspectable for source reference, but they should not be modified except for explicit retirement work or minimal edits required by broader build/solution changes.
- AppHost participation is evidence of current runtime presence, not a reason to keep a UI surface.
- The durable storage location for WP120 decisions is this file under `dev/work-packages/120-surface-ownership-active-status/`.
- Later work packages must cite this specification when they rely on a surface classification or source-disposition assumption.

## 4. Functional requirements

FR1. WP120 shall define and maintain a canonical inventory of browser-facing and API-relevant surfaces for Arc 02.

FR2. The inventory shall classify each relevant surface as one of the following: active service-side runtime, local-only emulator/tool, out-of-scope infrastructure, or retirement candidate.

FR3. Each classification shall cite concrete evidence from the current repository, startup wiring, solution composition, or route inventory.

FR4. The specification shall state explicitly that WP120 requires no code changes unless the documented current state proves false.

FR5. The specification shall record that WP120 is primarily a decision-storage work package and that later work packages consume its output.

FR6. `IngestionServiceHost` and `QueryServiceHost` shall be treated as the two active service-side runtime hosts that remain in play through UI consolidation.

FR7. The provider mechanism, including the shared provider model and active provider project set, shall be treated as active service-side runtime support rather than a retirement target.

FR8. `FileShareEmulator` shall remain local-only, outside React consolidation, and untouched by the keep/retire decisions for other UI surfaces.

FR9. The current non-emulator UI surfaces, including Workbench, RulesWorkbench, and any browser UI currently embedded in the service hosts, shall be treated as retirement candidates rather than future-source material.

FR10. Retained Studio surfaces and Workbench sample/demo material shall be treated as retirement candidates rather than future-source material.

FR11. Configuration emulator support shall be classified as out-of-scope infrastructure for React consolidation.

FR12. AppHost participation shall be treated as evidence of current runtime presence only; it shall not override an explicit retirement decision.

FR13. Later Arc 02 and implementation work packages shall plan toward one React application and a suitable backend rather than continued investment in the retirement-bound UI surfaces.

FR14. Later Arc 02 and implementation work packages shall reference this specification when they depend on the active service-side baseline, local-only boundaries, or retirement decisions.

FR15. Retirement-bound projects shall remain available for source inspection and behavior reference, but they shall not be modified unless the work item is explicit retirement cleanup or a broader build/solution change makes a minimal compatibility edit unavoidable.

FR16. If a later work package proposes mining behavior from a retirement candidate, it shall justify the specific behavior gap rather than treating the retired surface as a general source.

## 5. Non-functional requirements

NFR1. The inventory shall be current-state and evidence-backed.

NFR2. The classification record shall remain stable at a canonical path so later work packages can reference it without ambiguity.

NFR3. The specification shall keep uncertain or incomplete decisions explicit rather than hiding them.

NFR4. The specification shall avoid overstating runtime stability or future ownership for retired UI surfaces, host-local Blazor state, or retained historical APIs.

NFR5. The work package shall not change application behavior unless follow-on validation proves the current documentation is wrong and an implementation correction is then separately scheduled.

NFR6. The specification shall make downstream dependency on Arc 02 decisions visible enough that future arc authors cannot plausibly miss them.

## 6. Data model

WP120 introduces a decision register rather than a runtime DTO model.

Each inventory record should answer these fields:
- Surface name.
- Primary repository path.
- AppHost services-mode participation.
- Solution participation.
- Explicit HTTP/API presence.
- Current classification.
- Environment-safety or keep/retire posture.
- Carry-forward work packages.

Initial inventory register:

| Surface | Primary path | AppHost services mode | In `Search.slnx` | Explicit HTTP/API shape | Current classification | Carry-forward |
| --- | --- | --- | --- | --- | --- | --- |
| IngestionServiceHost | [../../../src/Hosts/IngestionServiceHost](../../../src/Hosts/IngestionServiceHost) | Yes | Yes | Service host with current browser UI and runtime endpoints | Active service-side runtime; current UI retirement-bound | WP121, WP122, WP124 |
| QueryServiceHost | [../../../src/Hosts/QueryServiceHost](../../../src/Hosts/QueryServiceHost) | Yes | Yes | Service host with current browser UI and runtime endpoints | Active service-side runtime; current UI retirement-bound | WP121, WP122, WP124 |
| Provider mechanism | [../../../src/UKHO.Search.ProviderModel](../../../src/UKHO.Search.ProviderModel) and [../../../src/Providers/UKHO.Search.Ingestion.Providers.FileShare](../../../src/Providers/UKHO.Search.Ingestion.Providers.FileShare) | Indirect via service hosts | Yes | No direct browser UI; provider registration and runtime extension path | Active service-side runtime support | WP121, WP124, WP200-WP202 |
| FileShareEmulator | [../../../tools/FileShareEmulator](../../../tools/FileShareEmulator) | Yes | Yes | Direct Blazor tool host plus GET `/batch/{batchId}/files` route | Local-only emulator/tool; untouched by React consolidation | WP123 only for boundary confirmation |
| WorkbenchHost and RulesWorkbench | [../../../src/workbench/server/WorkbenchHost](../../../src/workbench/server/WorkbenchHost) and [../../../tools/RulesWorkbench](../../../tools/RulesWorkbench) | Yes | Yes | Current Blazor UI surfaces in the local stack | Retirement candidate | WP121 for retirement-aware host strategy, Arc 10 WP283-WP284 for cleanup |
| Retained Studio surfaces | [../../../src/Studio/StudioServiceHost](../../../src/Studio/StudioServiceHost), [../../../src/Studio/UKHO.Search.Studio](../../../src/Studio/UKHO.Search.Studio), and [../../../src/Providers/UKHO.Search.Studio.Providers.FileShare](../../../src/Providers/UKHO.Search.Studio.Providers.FileShare) | No | No | Detached historical API/UI surfaces | Retirement candidate | Arc 10 WP284 |
| Workbench sample and Radzen/demo material | [../../../src/Workbench/samples](../../../src/Workbench/samples) and [../../../src/Workbench/radzen-blazor](../../../src/Workbench/radzen-blazor) | No | Yes | Sample/demo/support material rather than active runtime entry points | Retirement candidate | Arc 10 WP284 |
| Configuration emulator support | [../../../configuration](../../../configuration) | Yes, via AppHost wiring | Yes | Configuration infrastructure, not a React-facing API surface | Out-of-scope infrastructure | WP123 |

Interpretation notes:
- `IngestionServiceHost` and `QueryServiceHost` remain as the two active service-side runtime hosts.
- Their current browser UI does not survive the consolidation; React replaces the non-emulator UI surface.
- The provider mechanism remains part of the backend/runtime path and is not a retirement target.
- `FileShareEmulator` is intentionally classified by environment safety rather than by mere runtime presence and is not to be touched by this consolidation direction.
- AppHost currently launches Workbench and RulesWorkbench, but that is a current-state fact, not a future-source endorsement.
- Retained Studio surfaces and sample/demo material are recorded as retirement candidates so later work does not accidentally treat them as migration inputs.
- Retirement status does not hide the code. Those projects remain readable reference material, but they are not intended to receive feature work unless a separate build or retirement concern forces a narrow edit.

## 7. Interfaces & integration

### 7.1 Evidence sources

The WP120 classification interface is evidence-driven rather than code-generated. The current approved evidence sources are:
- AppHost startup wiring.
- Solution membership.
- Direct startup/bootstrap code.
- Explicit route inventory for local tools where safety classification matters.
- Existing wiki pages that describe current local runtime composition.

### 7.2 Downstream consumers

- WP121 consumes the keep/retire inventory when choosing the backend shape behind the single React application.
- WP122 consumes the inventory when separating end-user, developer, admin, and local-only auth boundaries.
- WP123 consumes the inventory when freezing local-only and non-local safety rules, especially for `FileShareEmulator` and configuration support.
- WP124 consumes the inventory when deciding which service-side contracts and APIs are promoted behind the retained backend runtime.
- WP125 consumes the inventory when deciding which operational actions in the retained backend require audit and operation tracking.
- Arc 10 retirement cleanup work consumes the retirement-candidate rows when planning in-place deactivation, removal, or any explicitly justified narrow extraction.
- Later implementation work packages consume the same inventory so they do not accidentally couple new code to retirement-bound UI surfaces.

### 7.3 Change control

If a later work package changes a classification, the work-package spec for that decision shall cite WP120 and record the changed rationale explicitly. The classification must not drift by implication through implementation alone.

## 8. Observability (logging/metrics/tracing)

WP120 introduces no new runtime logging, metrics, or tracing behavior.

Its observability role is documentary:
- make the retained service-side runtime visible,
- make retirement-bound UI surfaces visible,
- and make local-only boundaries visible before new APIs are implemented.

Any runtime observability changes required by replay, rule editing, repair, or protected operations belong to later work packages, especially WP125 and implementation follow-ons.

## 9. Security & compliance

WP120 does not implement authentication or authorization changes.

Its security contribution is boundary clarity:
- local-only emulator/tool surfaces are not assumed to be safe React-facing APIs,
- retirement-bound UI/API hosts are not assumed to be approved future surfaces just because they currently run or still exist,
- and future non-local operations must pass through the later Arc 02 auth, authorization, and audit decisions.

The immediate compliance outcome is that future authors have an explicit place to look before exposing developer or destructive operations beyond local tooling.

## 10. Testing strategy

WP120 validation is evidence review rather than runtime implementation testing.

Validation anchors for this specification:
- Confirm AppHost services-mode participants in [../../../src/Hosts/AppHost/AppHost.cs](../../../src/Hosts/AppHost/AppHost.cs).
- Confirm solution participation, including the provider mechanism and the absence of retained Studio projects from [../../../Search.slnx](../../../Search.slnx).
- Confirm retained Studio API wiring in [../../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs](../../../src/Studio/StudioServiceHost/StudioServiceHostApplication.cs).
- Confirm FileShareEmulator explicit route inventory in [../../../tools/FileShareEmulator/Api/BatchFilesApi.cs](../../../tools/FileShareEmulator/Api/BatchFilesApi.cs).
- Confirm later work packages cite this specification when they depend on the active service-side baseline, local-only boundaries, or retirement decisions.

No build, runtime, or endpoint test execution is required to complete WP120 itself because the work package does not change code behavior. If later implementation work reveals a mismatch, that follow-on work package must add the appropriate executable validation.

## 11. Rollout / migration

WP120 has no runtime rollout because it does not deploy code.

Its migration effect is procedural:
1. Surface classification stops living only in the arc outline.
2. This canonical spec becomes the WP120 storage location.
3. Later Arc 02 work packages refine and implement decisions against this stored keep/retire baseline.

Wiki review result:
No wiki page update was required for this draft work-package specification. Reviewed [../../../wiki/Architecture-Walkthrough.md](../../../wiki/Architecture-Walkthrough.md), [../../../wiki/Ingestion-Walkthrough.md](../../../wiki/Ingestion-Walkthrough.md), and [../../../wiki/Tools-RulesWorkbench.md](../../../wiki/Tools-RulesWorkbench.md). Those pages already describe the current AppHost-started runtime and tooling surfaces. WP120 adds a planning and governance storage artifact rather than a current-state runtime change.

## 12. Open questions

None at this stage. WP120 now fixes the high-level keep/retire boundary as follows: keep `IngestionServiceHost`, `QueryServiceHost`, and the provider mechanism; leave `FileShareEmulator` untouched as local-only tooling; retire the remaining non-emulator UI surfaces in favor of one React application and a suitable backend.