# Studio and Host Uplift Specification

- Work Package: `072-studio-host-uplift`
- Version: `v0.01`
- Status: `Draft`
- Target Output Path: `docs/072-studio-host-uplift/spec-studio-host-uplift_v0.01.md`

## 1. Overview

### 1.1 Purpose

Define a single work package covering:

1. A planned namespace partition for `UKHO.Search.Studio` into sensible feature-aligned areas centred on `Rules`, `Ingestion`, and `Providers`.
2. A documentation-only pass for `UKHO.Search.Studio` using `./.github/instructions/documentation-pass.instructions.md` as a mandatory constraint.
3. A documentation-only pass for `src/Studio/StudioApiHost`, also using `./.github/instructions/documentation-pass.instructions.md` as a mandatory constraint.

### 1.2 Scope

This specification is intended to describe future implementation work only. It must not change or remove any code as part of the specification activity itself.

The current work item covers the production Studio library and production host only. Test-project documentation work for the related Studio and host test projects must be raised and executed as separate work items so that the user can revert them independently if required.

### 1.3 Stakeholders

- Studio maintainers
- Host application maintainers
- Repository maintainers responsible for documentation quality
- Developers working in the Studio and host codebases

### 1.4 Definitions

- **Documentation pass**: A comment-only uplift governed by `./.github/instructions/documentation-pass.instructions.md`.
- **Namespace partition**: A logical regrouping of types into clearer feature-oriented namespaces without changing intended runtime behaviour.
- **Studio**: `src/Studio/UKHO.Search.Studio`
- **Host project**: `src/Studio/StudioApiHost`

## 2. System context

### 2.1 Current state

`UKHO.Search.Studio` exists as a Studio project in the repository and appears to have accumulated concerns that now need clearer namespace boundaries. The host project in scope for this work package is confirmed as `src/Studio/StudioApiHost`.

### 2.2 Proposed state

A future implementation should:

- define a clear namespace partitioning approach for Studio code around `Rules`, `Ingestion`, and `Providers`
- align physical folders strictly with the target namespace partition for the Studio project
- apply the repository documentation-pass standard to the agreed Studio project scope
- apply the same documentation-pass standard to the agreed host project scope
- preserve existing behaviour during any documentation-only implementation work

### 2.3 Assumptions

- The deliverable for this work package is a single markdown specification document.
- The documentation-pass instruction file is mandatory and non-negotiable for the documentation portions of the work.
- The namespace partitioning requirement is a planning/design outcome in this specification and does not authorize code changes during specification authoring.

### 2.4 Constraints

- Do not change, remove, or otherwise alter code behaviour as part of documentation-pass implementation.
- Do not widen scope beyond the explicitly named projects without clarification.
- Keep this work package as a single specification document.
- Treat documentation uplift for `test/UKHO.Search.Studio.Tests` and `test/StudioApiHost.Tests` as mandatory follow-on work, but keep them out of this work item and track them as separate work items.

## 3. Component / service design (high level)

### 3.1 Components

- `UKHO.Search.Studio`
- `StudioApiHost`
- Repository documentation standards and source-code commenting rules

### 3.2 Data flows

At a high level, the Studio project interacts with ingestion, rules, and provider concerns, while the host project provides application hosting behaviour around the Studio surface. Detailed flows will be confirmed after clarification.

In practical terms, the Studio library holds reusable contracts and behaviours that the host exposes through API and runtime orchestration layers. The main design concern is therefore keeping the Studio library internally organised while leaving the host focused on hosting, endpoints, and operation lifecycle management.

### 3.3 Key decisions

The Studio library currently exposes a mix of provider registration, ingestion DTO-style contracts, rule discovery contracts, and dependency injection support. The host currently contains API endpoints, operation coordination, operation storage, application startup composition, and top-level bootstrap code.

## 4. Functional requirements

### 4.1 Namespace partition for `UKHO.Search.Studio`

The namespace adjustment applies only to `src/Studio/UKHO.Search.Studio`.

The Studio partition is expected to use strict folder alignment so that physical source layout mirrors the target namespace boundaries wherever files remain in scope.

Cross-cutting types that are genuinely common across Studio concerns may remain in the root `UKHO.Search.Studio` namespace. Dependency-injection wiring should remain where it is rather than being moved into a new shared namespace as part of this work item.

The partition should be principle-led rather than exhaustively prescriptive. `Rules`, `Ingestion`, and `Providers` are guidance anchors for organising the code, and implementers should make sensible placement decisions that reduce root namespace clutter without forcing artificial reshuffles.

At a minimum, the future namespace adjustment should:

1. move rule-discovery and rule-response types into a coherent `Rules` area
2. move ingestion payload, result, status, operation, and context types into a coherent `Ingestion` area
3. move provider contracts, provider catalog concerns, and provider registration validation concerns into a coherent `Providers` area
4. leave genuinely shared or cross-cutting Studio abstractions in the root namespace only where that is the clearest outcome
5. preserve existing behaviour while updating namespaces and matching folders

### 4.2 Documentation pass for `UKHO.Search.Studio` and `StudioApiHost`

The namespace partition and the documentation pass must be delivered as separate follow-on work items. The partition must happen first so that any later documentation work is applied to the settled structure rather than being invalidated by subsequent moves, but this should be treated as a simple namespace adjustment rather than an over-engineered restructuring exercise.

The two production documentation passes should be grouped into a single follow-on work item covering both `src/Studio/UKHO.Search.Studio` and `src/Studio/StudioApiHost`.

That documentation work must:

1. follow `./.github/instructions/documentation-pass.instructions.md` as a mandatory and non-negotiable standard
2. remain comment-only, with no code logic, signature, formatting-only, or behavioural changes
3. inspect every hand-maintained `.cs` file in `src/Studio/UKHO.Search.Studio` and `src/Studio/StudioApiHost`
4. exclude generated and machine-maintained files such as `obj`, designer output, and source-generator output
5. add or improve XML documentation for all eligible public API surface in scope
6. add developer-level explanatory comments throughout methods and executable logic, including top-level host bootstrap code
7. rewrite weak or inconsistent existing comments where necessary so the code reads in one consistent documentation style

### 4.3 Scope exclusions

The namespace adjustment does not require namespace tidy-up work inside `StudioApiHost`.

Documentation uplift for `test/UKHO.Search.Studio.Tests` and `test/StudioApiHost.Tests` is intentionally out of scope for this work item and must be handled separately so those changes can be reverted independently if required.

## 5. Non-functional requirements

1. Namespace changes must preserve existing runtime behaviour.
2. The Studio namespace adjustment should improve maintainability and discoverability without creating unnecessary architectural churn.
3. Documentation work must preserve all non-comment formatting except for the minimum formatting directly required to insert comments cleanly.
4. Documentation added during the later pass should be high-depth, internally consistent, and aligned with current code behaviour as the primary source of truth.
5. The resulting Studio namespace layout should reduce root-level clutter and make feature ownership clearer for future maintenance.

## 6. Data model

No persistent data-model change is required by this specification.

The Studio namespace adjustment may relocate DTO-style and contract types associated with ingestion, provider metadata, and rule discovery, but it must not alter their structure or behaviour as part of the namespace work alone.

## 7. Interfaces & integration

`UKHO.Search.Studio` provides contracts and abstractions consumed by `StudioApiHost` and related provider implementations.

The namespace adjustment must preserve those integration points, including public types used by host APIs, provider registration, ingestion operations, and rule discovery flows.

The later documentation pass must document those public interfaces explicitly rather than relying on inherited documentation alone.

## 8. Observability (logging/metrics/tracing)

This work item does not introduce new observability requirements.

For the documentation pass, existing logging and operational flow in `StudioApiHost` should be documented where helpful to explain externally visible behaviour and operation lifecycle handling, but observability behaviour must not be changed.

## 9. Security & compliance

This work item does not change security posture or compliance controls.

The later documentation pass should document security-relevant behaviour only where it can be reliably inferred from the code and current repository guidance, without introducing speculative claims.

## 10. Testing strategy

For the namespace adjustment work, validation should confirm that the solution still builds and that relevant tests continue to pass after namespace and folder changes.

For the later documentation-only pass, validation must include a full solution build and a full test suite run, in line with `./.github/instructions/documentation-pass.instructions.md`.

Test-project documentation uplift is not part of this work item and should be validated in its own separate work items.

## 11. Rollout / migration

Recommended sequencing:

1. complete the simple namespace and folder adjustment in `UKHO.Search.Studio`
2. confirm build and test stability on the adjusted structure
3. perform the combined production documentation pass for `UKHO.Search.Studio` and `StudioApiHost`
4. handle documentation uplift for `test/UKHO.Search.Studio.Tests` and `test/StudioApiHost.Tests` separately if still required

## 12. Open questions

None at present.

## 13. Clarified decisions

- The host project in scope for this work package is `src/Studio/StudioApiHost`.
- The `UKHO.Search.Studio` namespace partition must include strict folder alignment rather than namespace-only changes.
- Documentation uplift for `test/UKHO.Search.Studio.Tests` and `test/StudioApiHost.Tests` is required, but it must be delivered as separate work items rather than inside this work package.
- Genuinely common Studio types may remain in the root `UKHO.Search.Studio` namespace.
- Existing DI wiring should remain in place rather than being moved as part of the namespace partition.
- Namespace partitioning and documentation uplift must be separate follow-on work items, with partitioning completed before documentation begins.
- The production documentation passes for `UKHO.Search.Studio` and `StudioApiHost` should be combined into a single follow-on work item.
- The namespace design in this specification should stay principle-led rather than exhaustively mapping every current type, and implementers should use sensible judgment to avoid cluttering the root namespace.
- The specification should describe sequencing and separation rules only; it should not explicitly enumerate or over-design follow-on work items for what is intended to be a simple namespace adjustment.
- The namespace adjustment applies only to `UKHO.Search.Studio`; `StudioApiHost` remains in scope for documentation only.
