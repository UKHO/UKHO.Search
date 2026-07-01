# Specification: WP103 In-Repo Consumer Contract Convergence

Target output path: `dev/work-packages/103-in-repo-consumer-contract-convergence/spec-domain-in-repo-consumer-contract-convergence.md`

Date: 2026-06-30

Source material:
- [../../specs/next-gen-arc01-wp.md](../../specs/next-gen-arc01-wp.md)
- [../100-remote-ingestion-queue-contracts/spec-domain-remote-ingestion-queue-contracts.md](../100-remote-ingestion-queue-contracts/spec-domain-remote-ingestion-queue-contracts.md)
- [../101-queue-message-json-contract/spec-domain-queue-message-json-contract.md](../101-queue-message-json-contract/spec-domain-queue-message-json-contract.md)
- [../102-producer-safe-helpers-validation/spec-domain-producer-safe-helpers-validation.md](../102-producer-safe-helpers-validation/spec-domain-producer-safe-helpers-validation.md)
- [../../docs/discussion/next-gen-consolidation-discussion.md](../../docs/discussion/next-gen-consolidation-discussion.md)
- [../../docs/discussion/next-gen-work-package-arcs.md](../../docs/discussion/next-gen-work-package-arcs.md)

## 1. Overview

### 1.1 Purpose

This specification defines WP103, which converges the remaining in-repository queue-message consumers on `UKHO.Search.Ingestion.Contracts`.

WP101 extracted the queue-message DTOs and JSON contract into the contracts package and rewired the active ingestion runtime path. WP102 added producer-safe helper APIs, serializer facade entry points, and a non-throwing validator. WP103 completes the in-repo migration by removing the remaining routine reliance on `UKHO.Search.Ingestion.Requests` across tools, tests, and retained consumer-facing code that still reference the old runtime-local contract namespace.

### 1.2 Scope

In scope for WP103:
- Refactor remaining in-repo queue-message consumers from `UKHO.Search.Ingestion.Requests` to `UKHO.Search.Ingestion.Contracts`.
- Update the affected tests so they validate the extracted contract package rather than the deprecated runtime-local copy.
- Keep active behavior unchanged while reducing duplicate queue-message contract ownership inside the repository.
- Preserve the architectural boundary that queue-message authoring belongs in the contracts package while runtime-only processing remains outside it.
- Cover the full remaining in-repo consumer estate for Arc 01: FileShareEmulator, RulesWorkbench, infrastructure-ingestion tests, provider tests, integration tests, service-adjacent tests, and retained Studio provider consumers that still point at `UKHO.Search.Ingestion.Requests`.

Out of scope for WP103:
- New transport helpers or queue-submission SDK work.
- Browser-host or public developer-API decisions that belong to later arcs.
- Journal, replay, dead-letter, or `ShadowId` generation concerns.
- File Share security-token derivation redesign.
- Removal of the runtime-local request-model files unless the migration proves that removal is safe and naturally part of the convergence slice.

### 1.3 Stakeholders

- Search platform maintainers who need one canonical queue-message contract inside the repository.
- Ingestion runtime and infrastructure test owners whose slices still reference the old runtime-local request namespace.
- Tooling owners for FileShareEmulator and RulesWorkbench.
- Studio provider owners whose retained ingestion payload path is now part of the convergence work.

### 1.4 Definitions

- Consumer convergence: the act of moving remaining repository consumers onto the extracted contracts package so one canonical queue-message contract is used across runtime, tooling, and tests.
- Runtime-local copy: the older `UKHO.Search.Ingestion.Requests` namespace and files that historically owned the queue-message DTOs before WP101.
- Retained consumer: a tool, test, or provider-facing code path that still depends on the old runtime-local request types even though the extracted contracts package now exists.
- Transitional compatibility surface: the remaining runtime-local request-model files that may temporarily stay in the repository after consumer convergence if removal would be a separate compatibility decision.

## 2. System context

### 2.1 Current state

The repository now has a canonical contracts package, but a substantial set of in-repo consumers still imports `UKHO.Search.Ingestion.Requests`.

Evidence checked:
- The active ingestion runtime path already moved to `UKHO.Search.Ingestion.Contracts` during WP101.
- Remaining imports of `UKHO.Search.Ingestion.Requests` still exist across multiple test and tooling surfaces, including:
  - `tools/FileShareEmulator.Common/FileShareIngestionMessageFactory.cs`
  - `tools/FileShareEmulator/Services/IndexService.cs`
  - `tools/RulesWorkbench/Services/RuleEvaluationService.cs`
  - `tools/RulesWorkbench/Services/EvaluationPayloadMapper.cs`
   - `src/Providers/UKHO.Search.Studio.Providers.FileShare/*`
  - `test/FileShareEmulator.Common.Tests/*`
  - `test/RulesWorkbench.Tests/*`
  - `test/UKHO.Search.Infrastructure.Ingestion.Tests/*`
  - `test/UKHO.Search.Ingestion.Providers.FileShare.Tests/*`
  - `test/UKHO.Search.IntegrationTests/*`
  - `test/UKHO.Search.Services.Ingestion.Tests/*`
   - `test/UKHO.Search.Studio.Providers.FileShare.Tests/*`
- The runtime-local request namespace and serializer files still exist under `src/UKHO.Search.Ingestion/Requests/`.

This means the repository has already established one canonical queue-message contract package, but the wider in-repo consumer estate has not yet fully converged on it.

### 2.2 Proposed state

After WP103:
- Repository tooling, focused tests, and retained Studio-facing consumer code that are still in scope for Arc 01 reference `UKHO.Search.Ingestion.Contracts` instead of `UKHO.Search.Ingestion.Requests`.
- Queue-message construction, serialization, and validation inside those consumers use the extracted contracts package and, where appropriate, the producer-safe helper APIs introduced by WP102.
- The repository is closer to one obvious queue-message contract story: raw DTOs and helper APIs live in `UKHO.Search.Ingestion.Contracts`, while runtime-only processing remains elsewhere.
- If the runtime-local request-model files remain after convergence, they are an explicitly recorded transitional compatibility surface rather than an actively consumed parallel contract.

The desired end state is convergence, not a behavioral redesign. The migration succeeds when consumers point at the extracted contract package and their tests continue to prove the same queue-message behavior.

### 2.3 Assumptions

- The remaining `UKHO.Search.Ingestion.Requests` usages represent migration debt rather than intentional long-term dual ownership.
- The retained Studio provider slice can converge without changing the public Studio-facing workflow or payload semantics.
- Most consumer changes should be namespace, reference, and helper-surface migrations rather than behavioral rewrites.
- The helper and validator surfaces introduced in WP102 are now stable enough for selective in-repo adoption where they simplify authoring code.

### 2.4 Constraints

- The active ingestion runtime must continue to deserialize queue messages through `src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs` without behavioral regression.
- Producers must still not generate `ShadowId` or learn journal implementation details.
- File Share–specific security-token derivation remains upstream and must not be silently absorbed into the contracts package during convergence.
- The migration should stay focused on queue-message contract convergence rather than broad redesign of tooling workflows.

## 3. Component / service design (high level)

### 3.1 Components

WP103 affects four high-level areas:

1. Tooling consumers
   - FileShareEmulator.Common
   - FileShareEmulator
   - RulesWorkbench

2. Test consumers
   - infrastructure ingestion tests
   - File Share provider tests
   - integration tests
   - tooling-related tests
   - services ingestion tests

3. Studio-facing consumers
   - `src/Providers/UKHO.Search.Studio.Providers.FileShare/*`
   - `test/UKHO.Search.Studio.Providers.FileShare.Tests/*`

4. Runtime-local legacy request surface
   - `src/UKHO.Search.Ingestion/Requests/*`
   - only to the extent needed to remove accidental continued ownership or leave an explicitly documented temporary bridge

### 3.2 Data flows

Current state:
1. Some in-repo consumers still construct or deserialize queue-message payloads from `UKHO.Search.Ingestion.Requests`.
2. The active runtime consumes `UKHO.Search.Ingestion.Contracts`.
3. The repository therefore still carries a split internal consumer story even though the canonical package already exists.

Target state after WP103:
1. In-repo consumers construct and inspect queue-message payloads through `UKHO.Search.Ingestion.Contracts`.
2. Helper, serializer, and validator APIs from WP102 may be used where they improve clarity without changing behavior.
3. The runtime and in-repo tooling/tests share the same package-owned contract surface.

### 3.3 Key decisions

- Ownership decision: `UKHO.Search.Ingestion.Contracts` remains the one canonical queue-message contract surface for runtime, tooling, and tests.
- Convergence decision: WP103 should prefer direct migration of consumer imports and references over preserving dual contract ownership indefinitely.
- Behavior-preservation decision: consumer refactors should preserve current queue-message behavior and test intent rather than opportunistically redesign authoring flows.
- Helper-adoption decision: WP102 helper APIs may be adopted selectively where they simplify authoring code, but raw DTO usage remains acceptable when it keeps a migrated consumer clearer.
- Legacy-surface decision (Unverified): whether the runtime-local `src/UKHO.Search.Ingestion/Requests/*` files should be removed, shimmed, or left temporarily in place after the migration still depends on the final consumer scope and compatibility needs.
- Studio-scope decision: retained Studio provider consumers are part of WP103 and should converge on the extracted contracts package without changing Studio-facing payload behavior.

## 4. Functional requirements

FR1. Remaining in-repo queue-message consumers that are in scope for Arc 01 shall reference `UKHO.Search.Ingestion.Contracts` rather than `UKHO.Search.Ingestion.Requests`.

FR2. The active ingestion runtime shall continue to deserialize queue messages in `src/UKHO.Search.Infrastructure.Ingestion/Queue/IngestionSourceNode.cs` without behavioral regression.

FR3. FileShareEmulator-related code that still constructs queue-message payloads shall converge on the extracted contracts package.

FR4. RulesWorkbench-related code that still consumes queue-message payloads shall converge on the extracted contracts package.

FR5. Infrastructure-ingestion and provider-focused tests that still reference the runtime-local request namespace shall converge on the extracted contracts package.

FR6. Integration tests that prove queue-message behavior shall continue to pass after being pointed at the extracted contracts package.

FR7. Retained Studio provider code and its tests shall converge on the extracted contracts package while preserving current Studio-facing payload behavior.

FR8. Consumer migration shall not introduce queue-submission, journal, replay, dead-letter, or `ShadowId` concerns into the contracts package.

FR9. Any retained runtime-local request surface left after WP103 shall be explicitly justified and documented rather than silently persisting as accidental dual ownership.

## 5. Non-functional requirements

NFR1. WP103 shall reduce internal queue-message contract duplication without widening the dependency surface of `UKHO.Search.Ingestion.Contracts`.

NFR2. The migration shall preserve current queue-message JSON compatibility and validation behavior.

NFR3. The affected test estate shall make migration regressions obvious without requiring unrelated full-solution cleanup.

NFR4. The resulting contributor story shall make the canonical queue-message contract location obvious to future maintainers.

NFR5. The work package shall preserve Studio-facing behavior while converging its internal queue-message contract dependency onto the extracted package.

## 6. Data model

WP103 does not introduce a new queue-message model. It converges repository consumers onto the existing extracted contract and helper surfaces already delivered by WP101 and WP102.

Important data-model implications:
- DTOs, serializer options, helper APIs, and validator APIs belong in `UKHO.Search.Ingestion.Contracts`
- provider/runtime-specific models such as `CanonicalDocument` remain outside the contracts package
- producer and tool consumers should not need a second queue-message model once convergence is complete

## 7. Interfaces & integration

### 7.1 Internal integration points

Likely migration targets include:
- `tools/FileShareEmulator.Common/*`
- `tools/FileShareEmulator/*`
- `tools/RulesWorkbench/*`
- `src/Providers/UKHO.Search.Studio.Providers.FileShare/*`
- `test/FileShareEmulator.Common.Tests/*`
- `test/RulesWorkbench.Tests/*`
- `test/UKHO.Search.Infrastructure.Ingestion.Tests/*`
- `test/UKHO.Search.Ingestion.Providers.FileShare.Tests/*`
- `test/UKHO.Search.IntegrationTests/*`
- `test/UKHO.Search.Services.Ingestion.Tests/*`
- `test/UKHO.Search.Studio.Providers.FileShare.Tests/*`

### 7.2 Integration boundary

WP103 is about consumer convergence, not about broadening the package’s responsibilities. Consumers may adopt:
- raw DTOs from `UKHO.Search.Ingestion.Contracts`
- serializer facade entry points from WP102
- helper or builder APIs from WP102 where they clarify authoring code

They must not use WP103 as a reason to move queue transport, provider policy, or journal concerns into the contracts package.

## 8. Observability (logging/metrics/tracing)

WP103 does not require new package-level observability primitives.

Consumer refactors should preserve existing logging and diagnostic intent where those consumers already emit logs or user-visible diagnostics.

## 9. Security & compliance

WP103 shall preserve the existing separation between queue-message authoring and security-token derivation policy.

Consumer migration must not imply that the contracts package now owns queue authentication, provider authorization, or journal identity generation.

## 10. Testing strategy

Validation for WP103 is expected to include targeted runs of the remaining consumer-aligned test projects, including the slices called out in the Arc 01 roadmap:
- `UKHO.Search.Ingestion.Tests` if additional migration fallout reaches that baseline suite
- `UKHO.Search.Infrastructure.Ingestion.Tests`
- `FileShareEmulator.Common.Tests`
- `FileShareEmulator.Tests`
- `RulesWorkbench.Tests`
- `UKHO.Search.Studio.Providers.FileShare.Tests`
- other focused provider or integration tests directly touched by the migration

## 11. Rollout / migration

WP103 is a convergence work package, so rollout is mostly internal:
- migrate consumer references in manageable slices
- keep the repository runnable and testable after each slice
- remove or explicitly justify remaining runtime-local request ownership by the end of the work package

## 12. Open questions

No open questions remain at the specification level for WP103.

The following implementation decisions are now captured by the spec and should be treated as the working defaults unless later evidence forces a change:
- retained Studio provider consumers are included in WP103 and converge on `UKHO.Search.Ingestion.Contracts`
- if the runtime-local request-model files remain after consumer convergence, they are treated as an explicitly recorded transitional compatibility surface rather than an actively consumed parallel contract
