# Implementation Plan

**Target output path:** `docs/063-provider-metadata-rule-loading/plan-provider-metadata-rule-loading_v0.01.md`

**Based on:** `docs/063-provider-metadata-rule-loading/spec-architecture-provider-metadata-rule-loading_v0.01.md`

**Version:** `v0.01` (`Draft`)

---

## Slice 1 — Provider metadata-backed rule loading in ingestion

- [x] Work Item 1: Validate and canonicalize rule provider identities through `UKHO.Search.ProviderModel` in the ingestion rules-loading path - Completed
  - **Purpose**: Deliver the smallest runnable backend slice that strengthens current ingestion rule loading by resolving rule provider keys through `IProviderCatalog`, rejecting unknown providers, and canonicalizing provider names before rules are exposed to the ingestion runtime.
  - **Acceptance Criteria**:
    - Rules loading resolves provider keys through `IProviderCatalog`.
    - Unknown rule provider keys fail fast with clear validation errors.
    - Rule provider names are canonicalized to `ProviderDescriptor.Name`.
    - Rules for known-but-disabled providers can still be loaded.
    - Existing enabled-provider runtime validation remains intact and separate from rules validation.
    - Automated tests cover unknown providers, case-insensitive resolution, canonical output names, and known-but-disabled provider rules.
  - **Definition of Done**:
    - Provider-aware rule loading implemented in the ingestion rules path
    - Unknown provider rejection and canonicalization added
    - Existing ingestion runtime enablement validation preserved
    - Unit/integration tests added and passing for rule-provider identity behavior
    - Logging and validation errors are diagnosable
    - Documentation updated for the strengthened ingestion rules-loading model
    - Can execute end-to-end via: targeted ingestion rules and provider validation test runs
  - [x] Task 1.1: Integrate `IProviderCatalog` into rule loading - Completed
    - [x] Step 1: Identify the current rule entry ingestion path from rules source through loader and catalog construction. - Completed
    - [x] Step 2: Inject or otherwise compose `IProviderCatalog` into the rule-loading flow at the correct boundary. - Completed
    - [x] Step 3: Ensure provider-name resolution is case-insensitive through the shared Provider Model. - Completed
    - [x] Step 4: Keep dependency direction aligned with onion architecture. - Completed
  - [x] Task 1.2: Canonicalize provider names and fail fast for unknown rule providers - Completed
    - [x] Step 1: Resolve each raw rule provider key against `IProviderCatalog`. - Completed
    - [x] Step 2: Replace raw provider keys with canonical `ProviderDescriptor.Name` values in loaded rules structures. - Completed
    - [x] Step 3: Reject rule entries whose provider key does not resolve to known provider metadata. - Completed
    - [x] Step 4: Ensure logs and exception messages clearly identify the offending provider key and rule source location. - Completed
  - [x] Task 1.3: Preserve separation between rules validity and runtime enablement - Completed
    - [x] Step 1: Ensure rules for known-but-disabled providers remain loadable. - Completed
    - [x] Step 2: Confirm enabled-provider runtime validation still executes independently for ingestion execution. - Completed
    - [x] Step 3: Add regression checks proving the strengthened rule-loading behavior does not replace or weaken startup enablement validation. - Completed
  - [x] Task 1.4: Add full automated coverage for provider-aware ingestion rule loading - Completed
    - [x] Step 1: Add tests for unknown provider rejection. - Completed
    - [x] Step 2: Add tests for case-insensitive provider resolution and canonicalized provider output. - Completed
    - [x] Step 3: Add tests for known-but-disabled providers still loading rules successfully. - Completed
    - [x] Step 4: Add regression tests for existing enabled-provider validation behavior. - Completed
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/*`: provider-aware rules loading, validation, and canonicalization.
    - `src/UKHO.Search.Services.Ingestion/*` and/or `src/Hosts/IngestionServiceHost/*`: startup and integration points if required.
    - `test/UKHO.Search.Infrastructure.Ingestion.Tests/Rules/*`: rules-loading tests.
    - `test/UKHO.Search.Services.Ingestion.Tests/*` and/or `test/IngestionServiceHost.Tests/*`: validation regression tests if needed.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter Rule`
    - `dotnet test .\Search.slnx --filter ProviderValidation`
    - `dotnet test .\Search.slnx --filter IngestionRules`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Updated `IngestionRulesSource` to depend on `IProviderCatalog`, resolve rule provider keys through shared provider metadata, canonicalize provider names to `ProviderDescriptor.Name`, and fail fast for unknown providers.
    - Added a direct `UKHO.Search.ProviderModel` project reference to `UKHO.Search.Infrastructure.Ingestion` and updated `AddIngestionRulesEngine()` to compose File Share provider metadata so provider-aware rule loading works in rules-engine-only composition paths.
    - Added `IngestionRulesSourceProviderIdentityTests` covering unknown provider rejection, canonicalized provider names from case-insensitive rule sources, and successful loading for known-but-disabled providers.
    - Preserved separate enabled-provider runtime validation and verified the existing `IngestionProviderStartupValidator` regression tests still pass unchanged.
    - Verified with `run_build`, `dotnet test test\UKHO.Search.Infrastructure.Ingestion.Tests\UKHO.Search.Infrastructure.Ingestion.Tests.csproj --filter "IngestionRulesSourceProviderIdentity|BootstrapStartupFail|RuleFileLoader"`, and `dotnet test test\UKHO.Search.Services.Ingestion.Tests\UKHO.Search.Services.Ingestion.Tests.csproj --filter IngestionProviderStartupValidator`: 20 passed, 0 failed across the targeted runs.

---

## Slice 2 — Reusable read-oriented rules loading for ingestion and studio

- [x] Work Item 2: Expose a host-neutral read-oriented rules-loading path reusable by both `IngestionServiceHost` and `StudioApiHost` - Completed
  - **Purpose**: Deliver a runnable cross-host slice that extracts or shapes a reusable rules-reading service boundary so both ingestion and studio hosts can load rules for all known providers without requiring ingestion runtime-only services.
  - **Acceptance Criteria**:
    - A reusable read-oriented rules-loading service exists and can be consumed by both ingestion and studio hosts.
    - The reusable service exposes rules by canonical provider name.
    - The reusable service does not require queue, blob, Elasticsearch, or runtime provider services not needed for rule reading.
    - The service preserves diagnostics and validation behavior from provider-aware rule loading.
    - Automated tests cover read-only composition in non-ingestion contexts.
  - **Definition of Done**:
    - Read-oriented rules-loading abstraction introduced or current rules-loading path refactored to be host-neutral
    - Ingestion host uses the shared read-oriented path successfully
    - Studio host can compose the same read-oriented path without ingestion runtime dependencies
    - Tests added and passing for reusable read composition
    - Documentation updated to reflect the new shared rules-loading boundary
    - Can execute end-to-end via: host/service-level rules-loading tests in both ingestion and studio scenarios
  - [x] Task 2.1: Define the reusable read-oriented rules-loading boundary - Completed
    - [x] Step 1: Decide whether to extract a host-neutral facade over the current rules-loading components or refactor the current components directly. - Completed
    - [x] Step 2: Shape the read-oriented contract to return rules by canonical provider name plus any needed diagnostics. - Completed
    - [x] Step 3: Keep the design read-only and avoid collapsing future write concerns into the same abstraction. - Completed
  - [x] Task 2.2: Compose the reusable read-oriented path into ingestion services - Completed
    - [x] Step 1: Replace any direct ingestion-only assumptions with the shared read-oriented rules-loading composition where needed. - Completed
    - [x] Step 2: Preserve existing ingestion behavior and startup ordering. - Completed
    - [x] Step 3: Ensure the shared path remains compatible with current rules sources. - Completed
  - [x] Task 2.3: Add full automated coverage for host-neutral rules reading - Completed
    - [x] Step 1: Add tests proving rules can be read without ingestion runtime dependencies. - Completed
    - [x] Step 2: Add tests proving canonical provider names and diagnostics are preserved by the reusable path. - Completed
    - [x] Step 3: Add regression tests proving ingestion host composition still behaves as before for valid inputs. - Completed
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/*`: reusable rules-loading abstraction or refactor.
    - `src/UKHO.Search.Infrastructure.Ingestion/Injection/*`: read-oriented DI composition.
    - `test/UKHO.Search.Infrastructure.Ingestion.Tests/Rules/*`: host-neutral rules-reading tests.
    - `test/UKHO.Search.Services.Ingestion.Tests/*` and/or `test/IngestionServiceHost.Tests/*`: ingestion integration regressions.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter Rule`
    - `dotnet test .\Search.slnx --filter IngestionRules`
    - `dotnet test .\Search.slnx --filter IngestionServiceHost`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Introduced the host-neutral read-oriented rules abstraction via `IProviderRulesReader`, `ProviderRulesSnapshot`, and `ProviderRuleDefinition` in `src/UKHO.Search.Infrastructure.Ingestion/Rules`.
    - Refactored `IngestionRulesCatalog` to implement both `IIngestionRulesCatalog` and `IProviderRulesReader`, preserving ingestion-specific validated-rule behavior while exposing a reusable canonical read model for other hosts.
    - Registered `IProviderRulesReader` in both `AddIngestionServices()` and `AddIngestionRulesEngine()` so ingestion and rules-engine-only compositions can consume the same read-oriented rules-loading path without runtime provider factories.
    - Added `ProviderRulesReaderCompositionTests` covering rules-engine-only composition without runtime provider factories, canonical provider-name and metadata preservation in snapshots, and case-insensitive provider lookup in the reusable reader.
    - Verified with `run_build`, `dotnet test test\UKHO.Search.Infrastructure.Ingestion.Tests\UKHO.Search.Infrastructure.Ingestion.Tests.csproj --filter ProviderRulesReaderComposition`, and `dotnet test test\UKHO.Search.Services.Ingestion.Tests\UKHO.Search.Services.Ingestion.Tests.csproj --filter IngestionProviderStartupValidator`: 7 passed, 0 failed across the targeted runs.

---

## Slice 3 — Integrate rules loading into `StudioApiHost` and document the strengthened model

- [x] Work Item 3: Integrate provider-aware read-only rules loading into `StudioApiHost`, expose a runnable Studio rule-discovery capability, and complete documentation/regression coverage - Completed
  - **Purpose**: Deliver the Studio-facing vertical slice where `StudioApiHost` loads rules for all known providers through the shared provider-aware rules-loading path and exposes a usable development-time capability without taking on save/update responsibilities yet.
  - **Acceptance Criteria**:
    - `StudioApiHost` composes the shared read-oriented rules-loading path.
    - `StudioApiHost` can read rules for all known providers without requiring ingestion runtime registrations.
    - Rule provider names exposed through Studio-facing rule access are canonicalized through `UKHO.Search.ProviderModel`.
    - The Studio integration remains read-only and does not implement save/update/delete behavior.
    - Automated tests fully cover the Studio-side rules-loading integration.
    - Relevant wiki pages are updated to describe the strengthened rule-loading model and Studio integration.
  - **Definition of Done**:
    - `StudioApiHost` composes and uses shared read-oriented rules loading
    - A runnable Studio-side rule-discovery capability exists
    - API/host tests added and passing for Studio rule reading
    - Full targeted coverage exists for Studio integration and rule-provider strengthening
    - Wiki and work package documentation updated
    - Can execute end-to-end via: `StudioApiHost` tests and optional local host run
  - [x] Task 3.1: Integrate the shared rules-loading path into `StudioApiHost` - Completed
    - [x] Step 1: Add the required rules-loading service composition to `StudioApiHost` without introducing ingestion runtime-only dependencies. - Completed
    - [x] Step 2: Ensure provider metadata and rules-loading composition align on canonical provider identity. - Completed
    - [x] Step 3: Validate that startup succeeds when rules are valid and fails clearly when rule provider identities are invalid. - Completed
  - [x] Task 3.2: Expose a runnable Studio rule-discovery capability - Completed
    - [x] Step 1: Define the Studio-facing read-only response shape for rules by provider using canonical provider names. - Completed
    - [x] Step 2: Add or amend a `StudioApiHost` endpoint to expose rule discovery for all known providers. - Completed
    - [x] Step 3: Keep the endpoint generic and compatible with a future separate write-oriented rule service. - Completed
    - [x] Step 4: Ensure the endpoint does not require ingestion runtime registrations or runtime provider factories. - Completed
  - [x] Task 3.3: Add full automated coverage for Studio rule loading - Completed
    - [x] Step 1: Add API tests covering successful rule discovery across known providers. - Completed
    - [x] Step 2: Add tests covering invalid/unknown provider rule groups failing clearly. - Completed
    - [x] Step 3: Add tests proving Studio rule reading works without ingestion runtime services. - Completed
    - [x] Step 4: Add regression tests ensuring provider metadata and Studio rules composition remain aligned. - Completed
  - [x] Task 3.4: Review and update documentation - Completed
    - [x] Step 1: Update relevant `wiki/` pages to describe Provider Model-backed rule loading and Studio rule discovery. - Completed
    - [x] Step 2: Update any affected work package docs if implementation reveals wording changes are needed. - Completed
    - [x] Step 3: Document that Studio rule operations are read-only in this package and that future save/update remains a separate concern. - Completed
  - **Files**:
    - `src/Studio/StudioApiHost/*`: Studio-side rules-loading composition and read-only endpoint changes.
    - `test/StudioApiHost.Tests/*`: Studio rules-loading API and composition tests.
    - `src/UKHO.Search.Infrastructure.Ingestion/Rules/*` and related DI wiring: shared reusable read-oriented rules-loading support.
    - `wiki/*`: implementation and architecture guidance updates.
    - `docs/063-provider-metadata-rule-loading/*`: work package documentation if wording needs alignment.
  - **Work Item Dependencies**: Work Item 1, Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter StudioApiHost`
    - `dotnet test .\Search.slnx --filter Rule`
    - Optional manual run: `dotnet run --project src/Studio/StudioApiHost/StudioApiHost.csproj`
    - Optional manual verification: use the Studio rule-discovery endpoint and confirm canonical provider names and rules are returned
  - **User Instructions**: None.
  - **Completed Summary**:
    - Added a direct `UKHO.Search.Infrastructure.Ingestion` reference to `StudioApiHost` and composed `AddIngestionRulesEngine()` so the host can reuse the shared provider-aware read-only rules loader without runtime ingestion provider factories.
    - Updated `StudioApiHostApplication` to fail startup early for invalid provider-backed rules and to expose `GET /rules`, returning canonical provider identities plus read-only rule summaries for all known providers.
    - Added `StudioRuleDiscoveryResponse`, `StudioProviderRulesResponse`, and `StudioRuleSummaryResponse` as explicit Studio-facing response contracts.
    - Added `StudioApiHostRulesEndpointTests` covering successful canonical rule discovery, inclusion of known providers without rules, unknown-provider startup failure, and absence of runtime ingestion factory registrations; updated existing Studio host endpoint/composition tests to supply a minimal valid ruleset and assert `/rules` appears in OpenAPI.
    - Updated `wiki/Ingestion-Rules.md` and `wiki/Tools-RulesWorkbench.md` to document Provider Model-backed rule loading, the read-only `StudioApiHost` discovery endpoint, and the separation between Studio discovery and future write operations.
    - Verified with `run_tests` for `StudioApiHost.Tests`: 8 passed, 0 failed; verified with `run_build`: successful.

---

## Summary / key considerations

- Start by strengthening provider identity handling in the ingestion rules-loading path so rule provider keys are catalog-backed and canonicalized.
- Keep rules validity based on known provider metadata, not current runtime enablement, so known-but-disabled providers can still have rules.
- Shape a reusable read-oriented rules-loading boundary before integrating `StudioApiHost` so Studio can load rules without acquiring ingestion runtime dependencies.
- Integrate `StudioApiHost` rule loading now, but keep the package read-only so future save/update operations can be added through a separate write-oriented service without redesigning the read path.
- Full automated coverage is part of every slice, and wiki/work package documentation should be updated as the strengthened rule-loading model becomes the current implementation.
