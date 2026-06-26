# Implementation Plan

**Target output path:** `dev/work-packages/mvp/062-studio-provider/plan-studio-provider_v0.01.md`

**Based on:** `dev/work-packages/mvp/062-studio-provider/spec-architecture-studio-provider_v0.01.md`

**Version:** `v0.01` (`Draft`)

---

## Slice 1 — Extract the shared Provider Model and prove ingestion still composes through it

- [x] Work Item 1: Introduce `UKHO.Search.ProviderModel`, migrate generic provider metadata/registration into it, and keep ingestion provider composition working end to end - Completed
  - **Purpose**: Deliver the smallest runnable backend slice that establishes `UKHO.Search.ProviderModel` as the shared home for provider identity, metadata, catalogs, and generic registration concerns, while preserving the existing ingestion-side provider model through the refactor.
  - **Acceptance Criteria**:
    - A new project `src/UKHO.Search.ProviderModel` exists and is added to the solution `.slnx`.
    - A new test project `test/UKHO.Search.ProviderModel.Tests` exists and is added to the solution `.slnx`.
    - Existing generic provider metadata and registration code is moved from ingestion-specific shared code into `UKHO.Search.ProviderModel`.
    - Existing generic provider metadata/registration tests are reviewed and moved to `test/UKHO.Search.ProviderModel.Tests` where appropriate.
    - Ingestion-side consumers continue to compile and use the new Provider Model without behavior regressions.
    - Full automated coverage exists for Provider Model contracts, normalization, duplicate detection, catalog behavior, and registration behavior.
  - **Definition of Done**:
    - `UKHO.Search.ProviderModel` project created and added to `.slnx`
    - `UKHO.Search.ProviderModel.Tests` created and added to `.slnx`
    - Generic provider model code refactored into Provider Model
    - Existing relevant tests migrated and new Provider Model tests added
    - Ingestion composition builds successfully against the new project
    - Documentation and plan remain aligned with the implemented shared model
    - Can execute end-to-end via: Provider Model and ingestion provider regression tests
  - [x] Task 1.1: Create the Provider Model project and matching test project - Completed
    - [x] Step 1: Add `src/UKHO.Search.ProviderModel/UKHO.Search.ProviderModel.csproj`. - Completed
    - [x] Step 2: Add `test/UKHO.Search.ProviderModel.Tests/UKHO.Search.ProviderModel.Tests.csproj`. - Completed
    - [x] Step 3: Add both projects to the repository solution `.slnx`. - Completed
    - [x] Step 4: Ensure project references preserve onion architecture direction. - Completed
  - [x] Task 1.2: Move generic provider metadata and registration concerns into `UKHO.Search.ProviderModel` - Completed
    - [x] Step 1: Move generic provider identity and metadata types such as descriptors and catalogs into the new Provider Model project. - Completed
    - [x] Step 2: Move generic DI registration helpers and validation/normalization logic into the Provider Model project. - Completed
    - [x] Step 3: Update namespaces and project references in ingestion-side code to consume Provider Model instead of the old location. - Completed
    - [x] Step 4: Keep ingestion-specific provider runtime abstractions in ingestion-specific projects. - Completed
  - [x] Task 1.3: Migrate and expand Provider Model test coverage - Completed
    - [x] Step 1: Review existing provider metadata/registration tests and move the generic ones into `test/UKHO.Search.ProviderModel.Tests`. - Completed
    - [x] Step 2: Add any missing unit tests for provider descriptor validation, case-insensitive lookup, deterministic ordering, duplicate detection, and registration idempotency. - Completed
    - [x] Step 3: Ensure migrated tests still reflect the canonical `file-share` provider identity and shared registration model. - Completed
    - [x] Step 4: Remove or simplify duplicate legacy tests left behind in other test projects where appropriate. - Completed
  - [x] Task 1.4: Prove ingestion still composes through the refactored Provider Model - Completed
    - [x] Step 1: Update ingestion provider packages and host/service registration code to reference `UKHO.Search.ProviderModel`. - Completed
    - [x] Step 2: Run targeted ingestion/provider tests to confirm the refactor preserves existing behavior. - Completed
    - [x] Step 3: Fix only regressions caused by the Provider Model extraction. - Completed
  - **Files**:
    - `src/UKHO.Search.ProviderModel/*`: new shared provider model code.
    - `test/UKHO.Search.ProviderModel.Tests/*`: canonical shared provider model test coverage.
    - `src/UKHO.Search.Ingestion/*`: remove/migrate generic provider metadata concerns and update references.
    - `src/Providers/UKHO.Search.Ingestion.Providers.FileShare/*`: update references to the shared Provider Model.
    - `test/UKHO.Search.Ingestion.Tests/*`: retain only ingestion-specific provider tests after migration.
    - solution `.slnx`: add the new production and test projects.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter ProviderModel`
    - `dotnet test .\Search.slnx --filter ProviderCatalog`
    - `dotnet test .\Search.slnx --filter ProviderDescriptor`
    - `dotnet test .\Search.slnx --filter FileShare`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Added `src/UKHO.Search.ProviderModel` and `test/UKHO.Search.ProviderModel.Tests`, and added both projects to `Search.slnx`.
    - Moved shared provider metadata types and DI registration helpers into `UKHO.Search.ProviderModel` via `ProviderDescriptor`, `IProviderCatalog`, `ProviderCatalog`, and `ProviderServiceCollectionExtensions`.
    - Updated ingestion/services/provider/host consumers to reference the shared Provider Model while keeping ingestion runtime abstractions in `UKHO.Search.Ingestion`.
    - Moved the generic provider descriptor/catalog tests out of `test/UKHO.Search.Ingestion.Tests` into `test/UKHO.Search.ProviderModel.Tests` and removed the old duplicated test files.
    - Verified with `run_build`, `dotnet build .\Search.slnx -v minimal`, and `dotnet test .\Search.slnx --filter "ProviderModel|ProviderCatalog|ProviderDescriptor|FileShare|IngestionProviderStartupValidator" --no-build`: 58 passed, 0 failed.

---

## Slice 2 — Add shared Studio contracts and a File Share tandem Studio provider with registration only

- [x] Work Item 2: Introduce `UKHO.Search.Studio` and `UKHO.Search.Studio.Providers.FileShare` with registration, validation, and matching tests - Completed
  - **Purpose**: Deliver a runnable Studio-side backend slice where generic Studio provider contracts exist, a File Share tandem Studio provider can be registered without host pollution, and the registration model is validated against canonical provider identity.
  - **Acceptance Criteria**:
    - A new project `src/Studio/UKHO.Search.Studio` exists and is added to `.slnx`.
    - A new provider-specific tandem project `src/Providers/UKHO.Search.Studio.Providers.FileShare` exists and is added to `.slnx`.
    - New matching test projects `test/UKHO.Search.Studio.Tests` and `test/UKHO.Search.Studio.Providers.FileShare.Tests` exist and are added to `.slnx`.
    - `UKHO.Search.Studio` defines generic Studio provider abstractions such as `IStudioProvider` and supporting generic descriptors/registration contracts.
    - The File Share tandem Studio provider registers against the shared Provider Model using canonical provider identity only.
    - Startup/registration validation exists for duplicate Studio provider identities and for Studio providers without matching provider metadata.
    - No provider-specific Studio behavior such as `IndexAll` or `IndexByContext` is implemented yet.
  - **Definition of Done**:
    - `UKHO.Search.Studio` and its test project created and added to `.slnx`
    - `UKHO.Search.Studio.Providers.FileShare` and its test project created and added to `.slnx`
    - Generic Studio provider contracts implemented
    - File Share Studio tandem provider registration implemented
    - Studio registration validation implemented and tested
    - No functional provider commands implemented beyond registration/contract scaffolding
    - Can execute end-to-end via: Studio contract and File Share Studio provider tests
  - [x] Task 2.1: Create the shared Studio contracts project and matching test project - Completed
    - [x] Step 1: Add `src/Studio/UKHO.Search.Studio/UKHO.Search.Studio.csproj`. - Completed
    - [x] Step 2: Add `test/UKHO.Search.Studio.Tests/UKHO.Search.Studio.Tests.csproj`. - Completed
    - [x] Step 3: Add both projects to the solution `.slnx`. - Completed
    - [x] Step 4: Add project references so Studio contracts depend on the shared Provider Model, not ingestion-specific shared code. - Completed
  - [x] Task 2.2: Define generic Studio provider abstractions - Completed
    - [x] Step 1: Add `IStudioProvider` and any generic Studio descriptor or registration models needed by Studio hosts. - Completed
    - [x] Step 2: Ensure the contract uses canonical provider identity from `UKHO.Search.ProviderModel`. - Completed
    - [x] Step 3: Keep the contract generic enough to support later operations without encoding provider-specific terms now. - Completed
    - [x] Step 4: Add unit tests for the shared Studio contract and registration model. - Completed
  - [x] Task 2.3: Create the File Share tandem Studio provider project and matching test project - Completed
    - [x] Step 1: Add `src/Providers/UKHO.Search.Studio.Providers.FileShare/UKHO.Search.Studio.Providers.FileShare.csproj`. - Completed
    - [x] Step 2: Add `test/UKHO.Search.Studio.Providers.FileShare.Tests/UKHO.Search.Studio.Providers.FileShare.Tests.csproj`. - Completed
    - [x] Step 3: Add both projects to the solution `.slnx`. - Completed
    - [x] Step 4: Keep the project limited to registration and provider pairing only; do not add functional provider commands yet. - Completed
  - [x] Task 2.4: Implement Studio provider registration and validation - Completed
    - [x] Step 1: Add shared registration helpers for Studio providers. - Completed
    - [x] Step 2: Add validation for duplicate Studio provider names and mismatched/missing Provider Model metadata. - Completed
    - [x] Step 3: Add File Share Studio provider registration using canonical `file-share` identity. - Completed
    - [x] Step 4: Add tests for happy path registration, duplicate detection, and metadata alignment failure behavior. - Completed
  - **Files**:
    - `src/Studio/UKHO.Search.Studio/*`: generic Studio provider contracts and registration support.
    - `test/UKHO.Search.Studio.Tests/*`: shared Studio contract tests.
    - `src/Providers/UKHO.Search.Studio.Providers.FileShare/*`: File Share tandem Studio provider registration-only implementation.
    - `test/UKHO.Search.Studio.Providers.FileShare.Tests/*`: tandem provider registration tests.
    - solution `.slnx`: add all new Studio production and test projects.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter StudioProvider`
    - `dotnet test .\Search.slnx --filter ProviderModel`
    - `dotnet test .\Search.slnx --filter FileShare`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Added `src/Studio/UKHO.Search.Studio` plus `test/UKHO.Search.Studio.Tests` and created the generic Studio registration layer via `IStudioProvider`, `IStudioProviderCatalog`, `StudioProviderCatalog`, `IStudioProviderRegistrationValidator`, `StudioProviderRegistrationValidator`, and `StudioProviderServiceCollectionExtensions`.
    - Added `src/Providers/UKHO.Search.Studio.Providers.FileShare` plus `test/UKHO.Search.Studio.Providers.FileShare.Tests` and implemented registration-only File Share Studio wiring via `FileShareStudioProvider` and `AddFileShareStudioProvider()` using canonical `file-share` identity only.
    - Added automated tests covering deterministic Studio provider catalog behavior, case-insensitive lookup, duplicate Studio provider rejection, metadata alignment validation, File Share Studio registration happy path, and idempotent File Share Studio registration.
    - Added all new Studio production and test projects to `Search.slnx`.
    - Verified with `run_build`, `dotnet test test\UKHO.Search.ProviderModel.Tests\UKHO.Search.ProviderModel.Tests.csproj --no-build`, `dotnet test test\UKHO.Search.Studio.Tests\UKHO.Search.Studio.Tests.csproj`, `dotnet test test\UKHO.Search.Studio.Providers.FileShare.Tests\UKHO.Search.Studio.Providers.FileShare.Tests.csproj`, and `dotnet test .\Search.slnx --filter "StudioProvider|FileShareProviderRegistration|ProviderModel" --no-build`: 29 passed, 0 failed in the targeted solution run.

---

## Slice 3 — Amend `StudioApiHost` to return full provider metadata and finalize documentation/regression coverage

- [x] Work Item 3: Amend `StudioApiHost` `/providers` to return full Provider Model metadata and complete wiki/regression updates - Completed
  - **Purpose**: Deliver the host-facing Studio slice where `StudioApiHost` composes the shared Provider Model and Studio providers, exposes full provider metadata through the existing `/providers` endpoint, and documents the new shared architecture.
  - **Acceptance Criteria**:
    - `StudioApiHost` composes the shared Provider Model and Studio provider registrations without runtime ingestion dependencies.
    - The existing `/providers` endpoint returns the full provider metadata shape defined by the shared Provider Model for each provider.
    - The endpoint remains generic and contains no provider-specific logic.
    - Relevant wiki pages are reviewed and updated to reflect the new shared Provider Model, Studio projects, solution changes, and `/providers` contract.
    - Automated tests cover the amended `/providers` response, generic host composition, and regression of existing provider discovery behavior.
  - **Definition of Done**:
    - `StudioApiHost` uses Provider Model and Studio provider composition
    - `/providers` returns full provider metadata
    - API tests and composition tests are added and passing
    - wiki pages are reviewed and updated
    - full regression runs pass for Provider Model, Studio, ingestion, and StudioApiHost affected areas
    - Can execute end-to-end via: `StudioApiHost` tests and optional local API run
  - [x] Task 3.1: Update `StudioApiHost` composition to use Provider Model and Studio provider registration - Completed
    - [x] Step 1: Update `StudioApiHost` dependencies and startup composition to consume `UKHO.Search.ProviderModel` and `UKHO.Search.Studio`. - Completed
    - [x] Step 2: Register the File Share Studio tandem provider via the generic Studio registration model. - Completed
    - [x] Step 3: Ensure `StudioApiHost` still starts without ingestion runtime services. - Completed
  - [x] Task 3.2: Amend the existing `/providers` endpoint to return full provider metadata - Completed
    - [x] Step 1: Define the full provider metadata response shape from the shared Provider Model. - Completed
    - [x] Step 2: Update the existing `/providers` endpoint to return that full metadata for each provider. - Completed
    - [x] Step 3: Keep the endpoint generic and free of provider-specific logic. - Completed
    - [x] Step 4: Add API tests for the full metadata shape and any required ordering/identity guarantees. - Completed
  - [x] Task 3.3: Review and update wiki/documentation alignment - Completed
    - [x] Step 1: Update relevant wiki pages to describe `UKHO.Search.ProviderModel` as the shared provider metadata/registration layer for ingestion and studio. - Completed
    - [x] Step 2: Update wiki guidance for `UKHO.Search.Studio`, tandem provider projects, matching test projects, and `.slnx` inclusion. - Completed
    - [x] Step 3: Update wiki guidance for the amended `/providers` contract returning full provider metadata. - Completed
    - [x] Step 4: Update the work package documents only if implementation reveals wording changes are needed. - Completed
  - [x] Task 3.4: Run regression validation across affected areas - Completed
    - [x] Step 1: Run targeted tests for `UKHO.Search.ProviderModel.Tests`, `UKHO.Search.Studio.Tests`, `UKHO.Search.Studio.Providers.FileShare.Tests`, and `StudioApiHost.Tests`. - Completed
    - [x] Step 2: Run affected ingestion/provider regression tests to confirm the shared Provider Model refactor did not break existing behavior. - Completed
    - [x] Step 3: Fix only regressions caused by this package and re-run until green. - Completed
  - **Files**:
    - `src/Studio/StudioApiHost/*`: host composition and `/providers` endpoint changes.
    - `test/StudioApiHost.Tests/*`: API and composition tests for full provider metadata.
    - `wiki/*`: pages covering provider metadata, ingestion provider mechanism, Search Studio, and source map alignment.
    - `dev/work-packages/mvp/062-studio-provider/*`: work package documentation if wording needs final adjustment.
  - **Work Item Dependencies**: Work Item 1, Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter StudioApiHost`
    - `dotnet test .\Search.slnx --filter ProviderModel`
    - Optional manual run: `dotnet run --project src/Studio/StudioApiHost/StudioApiHost.csproj`
    - Optional manual verification URL: `/providers`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Updated `StudioApiHost` to reference `UKHO.Search.Studio` and `UKHO.Search.Studio.Providers.FileShare`, compose the File Share Studio provider via `AddFileShareStudioProvider()`, and validate Studio provider registration at startup without introducing ingestion runtime dependencies.
    - Amended the existing `/providers` endpoint to return the full shared `ProviderDescriptor` metadata shape from `UKHO.Search.ProviderModel` instead of projecting a reduced anonymous payload.
    - Expanded `StudioApiHost.Tests` to verify Studio provider composition, runtime-free startup, full provider metadata deserialization, and deterministic provider ordering from `/providers`.
    - Updated `wiki/Provider-Metadata-and-Split-Registration.md`, `wiki/Ingestion-Service-Provider-Mechanism.md`, `wiki/Home.md`, and `wiki/Documentation-Source-Map.md` to document the shared Provider Model, tandem Studio providers, and the amended `/providers` contract.
    - Verified with `dotnet build .\Search.slnx -v minimal`, `run_build`, `dotnet test test\StudioApiHost.Tests\StudioApiHost.Tests.csproj`, and `dotnet test .\Search.slnx --filter "StudioApiHost|StudioProvider|ProviderModel|FileShare|IngestionProviderStartupValidator" --no-build`: 71 passed, 0 failed in the targeted solution run.

---

## Summary / key considerations

- Make `UKHO.Search.ProviderModel` the single shared home for generic provider metadata and registration concerns before adding new Studio contracts.
- Migrate generic provider metadata tests into `test/UKHO.Search.ProviderModel.Tests` so the refactored model has one canonical test home.
- Add new production and test projects to the solution `.slnx` as part of the implementation, not as cleanup.
- Keep `UKHO.Search.Studio` generic and reserve provider-specific behavior for tandem provider projects under `src/Providers/`.
- Amend the existing `StudioApiHost` `/providers` endpoint to return full Provider Model metadata while staying generic and development-time only.
- Leave functional provider commands such as `IndexAll` and provider-specific context execution for later work packages.
