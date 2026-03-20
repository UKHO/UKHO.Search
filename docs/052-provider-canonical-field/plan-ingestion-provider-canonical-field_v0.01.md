# Implementation Plan

**Target output path:** `docs/052-provider-canonical-field/plan-ingestion-provider-canonical-field_v0.01.md`

**Based on:** `docs/052-provider-canonical-field/spec-domain-provider-canonical-field_v0.01.md`

**Version:** `v0.01` (`Draft`)

---

## Slice 1 — Provider provenance reaches `CanonicalDocument`

- [x] Work Item 1: Propagate provider context from queue ingress to `CanonicalDocumentBuilder` and stamp immutable `Provider` - Completed
  - **Purpose**: Deliver the smallest end-to-end backend slice where a queued ingestion request produces an upsert operation whose `CanonicalDocument` always contains the originating provider identifier.
  - **Acceptance Criteria**:
    - `CanonicalDocument` exposes a required immutable `Provider` value.
    - `CanonicalDocumentBuilder` requires provider context at construction time and cannot create a document without it.
    - A mandatory `ProviderParameters` carrier exists and transports provider-scoped context through generic pipeline stages.
    - Provider context is sourced from the queue-read/request-dispatch path and reaches canonical document creation without making generic nodes provider-specific.
    - Missing provider context fails fast at the earliest possible opportunity.
    - Updated unit/pipeline tests prove that newly created canonical documents always have `Provider` set.
  - **Definition of Done**:
    - Code implemented in domain/provider pipeline layers
    - Unit and pipeline tests added/updated and passing
    - Fail-fast behavior is covered by tests
    - Logging/error messages remain diagnosable for missing provider context
    - Can execute end-to-end via: `dotnet test .\Search.slnx --filter Provider|CanonicalDocument|IngestionRequestDispatchNode`
  - [x] Task 1.1: Extend the canonical model with immutable provider provenance - Completed
    - [x] Step 1: Update `CanonicalDocument` to add a required `Provider` property. - Completed
    - [x] Step 2: Ensure `Provider` is assigned only at construction time and cannot be mutated later. - Completed
    - [x] Step 3: Update any serialization helpers or constructors so `Provider` participates in the canonical model consistently. - Completed
  - [x] Task 1.2: Introduce provider-scoped pipeline parameter transport - Completed
    - [x] Step 1: Create `ProviderParameters` as the mandatory generic carrier for `Provider`. - Completed
    - [x] Step 2: Thread `ProviderParameters` through the queue-read/request-dispatch flow. - Completed
    - [x] Step 3: Preserve generic node contracts by passing provider context as data rather than specializing node types. - Completed
    - [x] Step 4: Fail fast immediately if the provider identity is absent when constructing or forwarding provider-scoped parameters. - Completed
  - [x] Task 1.3: Update canonical document construction - Completed
    - [x] Step 1: Update `CanonicalDocumentBuilder` to require `ProviderParameters` or equivalent provider context input. - Completed
    - [x] Step 2: Stamp `Provider` using the existing provider identifier, for example `file-share`. - Completed
    - [x] Step 3: Ensure all creation paths used by current providers comply with the new required input. - Completed
  - [x] Task 1.4: Add targeted test coverage for provider propagation and immutability - Completed
    - [x] Step 1: Update existing `CanonicalDocument` tests impacted by the new required field. - Completed
    - [x] Step 2: Update existing `CanonicalDocumentBuilder` tests to supply provider context and assert `Provider` is set. - Completed
    - [x] Step 3: Add pipeline-level tests covering queue-read/request-dispatch propagation through to `CanonicalDocumentBuilder`. - Completed
    - [x] Step 4: Add fail-fast tests proving missing provider context is rejected at the earliest opportunity. - Completed
    - [x] Step 5: Add immutability coverage proving `Provider` cannot be changed after document creation. - Completed
  - **Files**:
    - `src/UKHO.Search.Ingestion/Pipeline/Documents/CanonicalDocument.cs`: add immutable `Provider` to the canonical model.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/Pipeline/Documents/CanonicalDocumentBuilder.cs`: require provider context and stamp `Provider`.
    - `src/UKHO.Search.Ingestion/*`: update queue-read/dispatch path contracts to carry `ProviderParameters`.
    - `src/UKHO.Search.Ingestion.Providers.FileShare/*`: update provider-specific wiring that supplies provider identity.
    - `test/UKHO.Search.Ingestion.Tests/Documents/*`: update canonical document tests.
    - `test/UKHO.Search.Ingestion.Tests/Pipeline/*`: add/update propagation and fail-fast tests.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter CanonicalDocument`
    - `dotnet test .\Search.slnx --filter CanonicalDocumentBuilder`
    - `dotnet test .\Search.slnx --filter IngestionRequestDispatchNode`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Added immutable required `CanonicalDocument.Provider` support and updated minimal construction/serialization paths.
    - Introduced `ProviderParameters` plus envelope context wiring so File Share ingress and dispatch can propagate provider identity without specializing generic nodes, with fail-fast behavior when provider context is absent.
    - Updated `CanonicalDocumentBuilder`, file-share graph defaults, synthetic pipeline wiring, RulesWorkbench document creation, and affected ingestion tests/helpers.
    - Verified with `run_build` and the full `UKHO.Search.Ingestion.Tests` project run: 278 passed, 0 failed.

---

## Slice 2 — Provider reaches the canonical index contract and developer guidance

- [x] Work Item 2: Expose `Provider` in the canonical index definition and update documentation/wiki guidance - Completed
  - **Purpose**: Complete the end-to-end feature so provider provenance is not only present on the canonical model but also represented in the index contract and explained clearly to developers.
  - **Acceptance Criteria**:
    - The canonical index definition maps `Provider` as a `keyword`.
    - Any canonical-to-index projection includes `Provider` consistently.
    - Documentation updates explain what `Provider` is, where it comes from, and why users cannot set it directly.
    - Relevant wiki pages describe `Provider` as a system-managed canonical provenance field.
    - Existing or new tests covering mapping/projection impacted by the change pass.
  - **Definition of Done**:
    - Infrastructure mapping/projection changes implemented
    - Documentation and wiki pages updated in the same work package/repository
    - Relevant unit/integration tests passing
    - Can execute end-to-end via: `dotnet test .\Search.slnx --filter Elastic|CanonicalIndex`; manual review of updated markdown links/pages
  - [x] Task 2.1: Update canonical index projection and mapping - Completed
    - [x] Step 1: Update the canonical index document/projection to carry `Provider`. - Completed
    - [x] Step 2: Update the canonical index definition to map `provider` as a `keyword`. - Completed
    - [x] Step 3: Update any mapping validation or index bootstrapping assertions impacted by the new field. - Completed
  - [x] Task 2.2: Refresh developer documentation - Completed
    - [x] Step 1: Update canonical model documentation under `docs/` where `CanonicalDocument` shape is described. - Completed
    - [x] Step 2: Update `wiki/CanonicalDocument-and-Discovery-Taxonomy.md` to explain `Provider` as provenance metadata. - Completed
    - [x] Step 3: Update `wiki/Ingestion-Pipeline.md` to explain that provider identity is known at queue ingress and passed to document construction. - Completed
    - [x] Step 4: Review any provider-mechanism wiki content and amend wording so readers understand why `Provider` cannot be user-supplied. - Completed
  - [x] Task 2.3: Add mapping and documentation regression coverage - Completed
    - [x] Step 1: Update any existing mapping/projection tests that now require `Provider`. - Completed
    - [x] Step 2: Add tests asserting the canonical index definition contains a `keyword` mapping for `Provider` if such coverage exists in the repository pattern. - Completed
    - [x] Step 3: Manually verify wiki/doc links and terminology remain consistent. - Completed
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDefinition.cs`: add `provider` keyword mapping.
    - `src/UKHO.Search.Infrastructure.Ingestion/Elastic/CanonicalIndexDocument.cs`: include `Provider` in projection if required.
    - `docs/011-canonical-document/*` or other canonical docs: update canonical model guidance as needed.
    - `wiki/CanonicalDocument-and-Discovery-Taxonomy.md`: explain `Provider` semantics.
    - `wiki/Ingestion-Pipeline.md`: explain provider propagation from queue ingress.
    - `wiki/Ingestion-Service-Provider-Mechanism.md`: clarify provider ownership and non-user-settable behavior if needed.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter CanonicalIndex`
    - `dotnet test .\Search.slnx --filter Elastic`
    - Review updated markdown pages in `docs/` and `wiki/`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Added `Provider` to the canonical Elasticsearch projection and mapped `provider` explicitly as a `keyword`, including mapping validation updates.
    - Added elastic test coverage for projection and payload serialization, and updated mapping validation tests to expect the new field.
    - Refreshed canonical/provider pipeline documentation in `wiki/` and added new canonical model spec versions under `docs/011-canonical-document/` describing the provider provenance field.
    - Verified with `run_build`, targeted elastic tests, and the full `UKHO.Search.Ingestion.Tests` project run: 279 passed, 0 failed.

---

## Slice 3 — Full regression sweep and repository-wide validation

- [x] Work Item 3: Update all impacted tests and execute the full test suite - Completed
  - **Purpose**: Harden the change so the repository demonstrates that `Provider` is mandatory everywhere it should be, with no lingering tests or construction paths still omitting provider context.
  - **Acceptance Criteria**:
    - All previously failing tests caused by the new required provider context are updated.
    - New tests demonstrate that every relevant canonical document creation path sets `Provider`.
    - The full test suite passes after implementation.
    - No documentation or test artifact contradicts the new invariant that `Provider` is always set.
  - **Definition of Done**:
    - Full suite executed successfully
    - Any incidental failing tests impacted by this change are fixed
    - Documentation reflects the final implemented behavior
    - Can execute end-to-end via: `dotnet test .\Search.slnx`
  - [x] Task 3.1: Sweep for affected construction and test helpers - Completed
    - [x] Step 1: Find all test helpers and fixtures that construct `CanonicalDocument` or related pipeline requests. - Completed
    - [x] Step 2: Update those helpers so they require or default valid provider context where appropriate. - Completed
    - [x] Step 3: Remove any outdated assumptions that canonical documents can be created without provider identity. - Completed
  - [x] Task 3.2: Execute full repository validation - Completed
    - [x] Step 1: Run the full test suite. - Completed
    - [x] Step 2: Triage any failures introduced by the new invariant. - Completed
    - [x] Step 3: Update tests or wiring only where failures are genuinely caused by this feature. - Completed
    - [x] Step 4: Re-run the full test suite until green. - Completed
  - [x] Task 3.3: Final documentation consistency check - Completed
    - [x] Step 1: Confirm spec, plan, and wiki wording align on immutability, fail-fast behavior, and `ProviderParameters`. - Completed
    - [x] Step 2: Confirm no documentation suggests users can set `Provider` directly. - Completed
  - **Files**:
    - `test/**/*`: update impacted tests, fixtures, and helpers.
    - `docs/052-provider-canonical-field/spec-domain-provider-canonical-field_v0.01.md`: update only if implementation reveals wording needs correction.
    - `docs/052-provider-canonical-field/plan-ingestion-provider-canonical-field_v0.01.md`: track completed outcomes if the work package is later refreshed.
  - **Work Item Dependencies**: Work Item 1, Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Swept remaining canonical construction helpers/usages and confirmed provider context is now supplied consistently across source, test, and tooling code.
    - Ran the full solution test suite with `dotnet test .\Search.slnx`, triaged provider-related validation fallout, and confirmed the suite is green: 434 passed, 0 failed.
    - Re-checked the updated plan/spec/wiki wording to ensure `Provider` is described as immutable, fail-fast, pipeline-owned provenance rather than user-supplied metadata.

---

## Summary / key considerations

- Start with the smallest backend vertical slice: queue/provider context must successfully produce a provider-stamped `CanonicalDocument`.
- Preserve onion boundaries: canonical model changes stay in domain/provider layers, while index mapping stays in infrastructure.
- Use `ProviderParameters` as the mandatory generic transport to avoid compromising existing pipeline-node generality.
- Treat missing provider context as a contract violation and fail fast early.
- Finish with repository-wide regression coverage and a full `dotnet test .\Search.slnx` run because this change makes provider context mandatory across existing tests and helpers.
