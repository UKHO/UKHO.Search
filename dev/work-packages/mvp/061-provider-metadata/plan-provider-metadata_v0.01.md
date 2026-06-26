# Implementation Plan

**Target output path:** `dev/work-packages/mvp/061-provider-metadata/plan-provider-metadata_v0.01.md`

**Based on:** `dev/work-packages/mvp/061-provider-metadata/spec-architecture-provider-metadata_v0.01.md`

**Version:** `v0.01` (`Draft`)

---

## Slice 1 — Shared provider metadata contract and File Share split registration

- [x] Work Item 1: Introduce shared provider metadata and prove File Share can register metadata and runtime separately - Completed
  - **Purpose**: Deliver the smallest runnable backend slice that formalizes provider identity in a shared inward model and proves a real provider package can participate through split registration without changing deployment topology.
  - **Acceptance Criteria**:
    - A shared provider metadata model exists in an inward-facing project, including `ProviderDescriptor` and `IProviderCatalog`.
    - Provider descriptors are validated and exposed through a deterministic, case-insensitive catalog.
    - The File Share provider package exposes explicit metadata-only registration and runtime registration entry points.
    - Runtime registration either implies metadata registration idempotently or the required ordering is made explicit and enforced.
    - Automated tests cover descriptor validation, catalog lookup, duplicate-name rejection, File Share metadata registration, File Share runtime registration, and canonical `file-share` naming.
  - **Definition of Done**:
    - Shared contract and catalog implemented in the domain layer
    - File Share provider updated to use split registration
    - Unit tests added and passing for all new contract and registration behavior
    - Logging/error messages are diagnosable for duplicate or invalid provider metadata
    - Documentation in this work package remains aligned with the implemented contract
    - Can execute end-to-end via: targeted provider metadata and File Share provider test runs
  - [x] Task 1.1: Introduce the shared provider metadata contract - Completed
    - [x] Step 1: Add `ProviderDescriptor` to the shared inward project used by ingestion contracts. - Completed
    - [x] Step 2: Add `IProviderCatalog` plus a concrete catalog implementation that performs case-insensitive lookup and deterministic enumeration. - Completed
    - [x] Step 3: Define validation rules for canonical provider names and duplicate detection. - Completed
    - [x] Step 4: Ensure the metadata contract does not depend on host or infrastructure concerns. - Completed
  - [x] Task 1.2: Add DI primitives for provider metadata composition - Completed
    - [x] Step 1: Add service registration helpers for registering provider descriptors into DI. - Completed
    - [x] Step 2: Ensure the catalog can be built from registered descriptors without requiring runtime provider factories. - Completed
    - [x] Step 3: Decide and implement idempotent behavior for repeated metadata registration. - Completed
  - [x] Task 1.3: Update the File Share provider package to use split registration - Completed
    - [x] Step 1: Define the canonical File Share provider descriptor using the existing `file-share` name. - Completed
    - [x] Step 2: Add metadata-only registration in the File Share provider package. - Completed
    - [x] Step 3: Refactor runtime registration so the File Share provider factory and runtime services are registered separately from metadata-only registration. - Completed
    - [x] Step 4: Keep existing runtime behavior intact while removing any need for development-time hosts to reference runtime-only services. - Completed
  - [x] Task 1.4: Add full automated test coverage for the contract and File Share registration slice - Completed
    - [x] Step 1: Add unit tests for `ProviderDescriptor` construction and validation rules. - Completed
    - [x] Step 2: Add unit tests for `IProviderCatalog` behavior, including case-insensitive lookup, deterministic ordering, and duplicate-name failure. - Completed
    - [x] Step 3: Add provider-package tests proving File Share metadata registration works without runtime dependencies. - Completed
    - [x] Step 4: Add provider-package tests proving File Share runtime registration composes correctly and preserves the canonical `file-share` identity. - Completed
    - [x] Step 5: Add idempotency or registration-order tests based on the chosen runtime/metadata composition rule. - Completed
  - **Files**:
    - `src/UKHO.Search.Ingestion/*`: shared provider metadata contract and catalog.
    - `src/Providers/UKHO.Search.Ingestion.Providers.FileShare/*`: File Share provider descriptor and split registration wiring.
    - `test/UKHO.Search.Ingestion.Tests/*`: provider metadata contract and catalog tests.
    - `test/UKHO.Search.Ingestion.Providers.FileShare.Tests/*`: File Share split-registration tests.
  - **Work Item Dependencies**: None.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter ProviderDescriptor`
    - `dotnet test .\Search.slnx --filter ProviderCatalog`
    - `dotnet test .\Search.slnx --filter FileShare`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Added shared provider metadata support in `UKHO.Search.Ingestion` via `ProviderDescriptor`, `IProviderCatalog`, `ProviderCatalog`, and DI registration helpers for idempotent descriptor registration.
    - Added `FileShareProviderMetadata` plus split-registration entry points in the File Share provider package for metadata-only and runtime composition, while preserving the existing `AddFileShareProvider()` behavior for current callers.
    - Added automated tests covering descriptor validation, catalog ordering and duplicate detection, metadata-only registration, runtime registration, and idempotent File Share registration behavior.
    - Verified with `run_build` and targeted test runs for `UKHO.Search.Ingestion.Tests` and `UKHO.Search.Ingestion.Providers.FileShare.Tests`: 169 passed, 0 failed.

---

## Slice 2 — Ingestion runtime validates enabled providers before bootstrap

- [x] Work Item 2: Compose metadata and runtime registrations in `IngestionServiceHost` and fail fast on invalid provider enablement - Completed
  - **Purpose**: Deliver a runnable ingestion-host slice where provider enablement is validated against shared metadata and runtime registrations before queue creation, polling, or other bootstrap activity can begin.
  - **Acceptance Criteria**:
    - `IngestionServiceHost` composes provider metadata and runtime registrations through the new split-registration pattern.
    - Configuration-backed enabled providers are validated against both the provider catalog and runtime registrations.
    - Startup fails deterministically when a configured provider is unknown, duplicated, or missing runtime registration.
    - Validation occurs before queue/bootstrap side effects are triggered.
    - Automated tests cover happy path, invalid provider names, duplicate registrations, metadata-without-runtime cases, and no-side-effect fail-fast behavior.
  - **Definition of Done**:
    - `IngestionServiceHost` composition updated to use split registration
    - Startup validation implemented and logged clearly
    - Host-level and integration-style tests added and passing
    - No queue/bootstrap work occurs on invalid configuration
    - Can execute end-to-end via: host-focused automated tests and targeted startup validation runs
  - [x] Task 2.1: Define the enabled-provider configuration model - Completed
    - [x] Step 1: Add or formalize the configuration binding model for enabled providers using canonical provider names. - Completed
    - [x] Step 2: Normalize configuration handling so provider names are compared case-insensitively while preserving canonical output. - Completed
    - [x] Step 3: Keep configuration as enablement-only and avoid shifting provider identity ownership into config. - Completed
  - [x] Task 2.2: Add ingestion runtime validation services - Completed
    - [x] Step 1: Introduce a validator that checks enabled providers against `IProviderCatalog` and registered runtime factories. - Completed
    - [x] Step 2: Ensure duplicate provider names fail deterministically during startup. - Completed
    - [x] Step 3: Ensure validation runs before queue creation, queue polling, or other ingestion bootstrap work. - Completed
    - [x] Step 4: Add clear logging for each validation failure mode. - Completed
  - [x] Task 2.3: Update `IngestionServiceHost` composition - Completed
    - [x] Step 1: Replace direct provider runtime wiring with the File Share split-registration entry points. - Completed
    - [x] Step 2: Register metadata and runtime services in the correct composition root. - Completed
    - [x] Step 3: Insert the provider validation step into startup in a way that preserves existing runnable behavior for valid configurations. - Completed
  - [x] Task 2.4: Add full automated test coverage for ingestion-host validation - Completed
    - [x] Step 1: Add host-level tests for valid startup with the File Share provider enabled. - Completed
    - [x] Step 2: Add host-level tests for unknown configured provider names. - Completed
    - [x] Step 3: Add host-level tests for duplicate provider registrations. - Completed
    - [x] Step 4: Add host-level tests for metadata present but runtime registration missing. - Completed
    - [x] Step 5: Add tests proving no queue/bootstrap side effects occur when validation fails. - Completed
    - [x] Step 6: Add regression tests proving existing File Share ingestion composition still works on the happy path. - Completed
  - **Files**:
    - `src/UKHO.Search.Infrastructure.Ingestion/*` or `src/UKHO.Search.Services.Ingestion/*`: provider enablement validation logic.
    - `src/Hosts/IngestionServiceHost/Program.cs`: updated split-registration composition and startup validation placement.
    - `test/IngestionServiceHost.Tests/*`: host startup validation tests.
    - `test/UKHO.Search.Infrastructure.Ingestion.Tests/*` and/or `test/UKHO.Search.Services.Ingestion.Tests/*`: supporting validation tests.
  - **Work Item Dependencies**: Work Item 1.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter IngestionServiceHost`
    - `dotnet test .\Search.slnx --filter ProviderValidation`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Added shared enabled-provider configuration support via `IngestionProviderOptions`, updated `IngestionProviderService` to filter runtime factories by configured provider names, and added `IngestionProviderStartupValidator` for metadata/runtime validation with clear fail-fast errors.
    - Updated `AddIngestionServices` to bind provider enablement from the `ingestion` section, switched ingestion runtime composition to `AddFileShareProviderRuntime(...)`, and passed host configuration from `IngestionServiceHost` into ingestion registration.
    - Moved provider validation to the start of `IngestionPipelineHostedService.StartAsync()` so invalid provider configuration fails before bootstrap or queue polling side effects occur.
    - Added host and service tests covering enabled-provider filtering, unknown providers, duplicate runtime registrations, metadata-without-runtime cases, and bootstrap suppression on validation failure; removed placeholder smoke tests from the matching host/service test projects.
    - Verified with `run_build` and targeted test runs for `UKHO.Search.Services.Ingestion.Tests`, `IngestionServiceHost.Tests`, `UKHO.Search.Ingestion.Tests`, `UKHO.Search.Ingestion.Providers.FileShare.Tests`, and `UKHO.Search.Infrastructure.Ingestion.Tests`: 308 passed, 0 failed.

---

## Slice 3 — `StudioApiHost` provider discovery endpoint and development-time documentation

- [x] Work Item 3: Expose provider metadata through `StudioApiHost` and complete development-time documentation and regression coverage - Completed
  - **Purpose**: Deliver the development-time vertical slice where `StudioApiHost` can surface provider metadata to Theia through a minimal API using metadata-only composition, while keeping studio components optional in live deployments.
  - **Acceptance Criteria**:
    - `StudioApiHost` composes provider metadata registrations only and does not require ingestion runtime services.
    - A development-time provider discovery endpoint returns provider metadata from the host-local catalog.
    - The response exposes canonical names and display metadata consistently.
    - Documentation and wiki pages explain split registration, provider metadata ownership, and the development-time-only role of `StudioApiHost` and Theia.
    - Automated tests cover API behavior, metadata-only composition, and live-deployment assumptions where studio components are absent.
  - **Definition of Done**:
    - `StudioApiHost` exposes a working provider discovery endpoint
    - API and host tests added and passing
    - Wiki and work package documentation updated to reflect final implemented behavior
    - Full regression suite for the feature passes
    - Can execute end-to-end via: `StudioApiHost` tests and optional local API execution
  - [x] Task 3.1: Update `StudioApiHost` composition to use metadata-only registration - Completed
    - [x] Step 1: Register provider metadata in `StudioApiHost` using the new split-registration entry point(s). - Completed
    - [x] Step 2: Ensure no ingestion runtime dependencies are required for host startup. - Completed
    - [x] Step 3: Optionally bind enabled-provider configuration annotation if that behavior is chosen for the API contract. - Completed
  - [x] Task 3.2: Implement the development-time provider discovery endpoint - Completed
    - [x] Step 1: Add a minimal API endpoint such as `/providers` backed by `IProviderCatalog`. - Completed
    - [x] Step 2: Shape the response for stable consumption by Theia, including `name`, `displayName`, and `description`. - Completed
    - [x] Step 3: Keep the endpoint metadata-only and avoid leaking runtime-specific internals or secrets. - Completed
  - [x] Task 3.3: Add full automated test coverage for `StudioApiHost` and topology assumptions - Completed
    - [x] Step 1: Add API tests proving metadata-only composition returns provider descriptors successfully. - Completed
    - [x] Step 2: Add tests proving `StudioApiHost` does not require ingestion runtime registrations. - Completed
    - [x] Step 3: Add tests for enabled-state annotation if implemented. - Completed
    - [x] Step 4: Add topology/regression tests proving live ingestion runtime assumptions remain valid when `StudioApiHost` and Theia are absent. - Completed
  - [x] Task 3.4: Finalize documentation and full feature validation - Completed
    - [x] Step 1: Update the work package spec and plan only if implementation reveals required wording adjustments. - Completed
    - [x] Step 2: Update `wiki/Provider-Metadata-and-Split-Registration.md` with final implementation guidance and onboarding steps. - Completed
    - [x] Step 3: Update `wiki/Ingestion-Service-Provider-Mechanism.md`, `wiki/Home.md`, and `wiki/Documentation-Source-Map.md` if final implementation details require alignment. - Completed
    - [x] Step 4: Run the full relevant test suite for the feature and fix only failures caused by this work. - Completed
  - **Files**:
    - `src/Studio/StudioApiHost/Program.cs`: metadata-only provider composition and `/providers` endpoint.
    - `test/StudioApiHost.Tests/*`: provider discovery API tests.
    - `wiki/Provider-Metadata-and-Split-Registration.md`: final implementation guidance.
    - `wiki/Ingestion-Service-Provider-Mechanism.md`: provider mechanism alignment.
    - `dev/work-packages/mvp/061-provider-metadata/*`: work package documentation updates if needed.
  - **Work Item Dependencies**: Work Item 1, Work Item 2.
  - **Run / Verification Instructions**:
    - `dotnet test .\Search.slnx --filter StudioApiHost`
    - `dotnet test .\Search.slnx --filter Provider`
    - Optional manual run: `dotnet run --project src/Studio/StudioApiHost/StudioApiHost.csproj`
    - Optional manual verification URL: `/providers`
  - **User Instructions**: None.
  - **Completed Summary**:
    - Refactored `StudioApiHost` startup into `StudioApiHostApplication.BuildApp(...)`, added metadata-only File Share provider composition, and exposed a `/providers` minimal API that returns `name`, `displayName`, and `description` from `IProviderCatalog`.
    - Added `StudioApiHost.Tests` coverage for the `/providers` endpoint and for metadata-only host composition proving that no `IIngestionDataProviderFactory` runtime registrations are required; removed the placeholder smoke test from the matching test project.
    - Updated `wiki/Provider-Metadata-and-Split-Registration.md` and `wiki/Ingestion-Service-Provider-Mechanism.md` to document the implemented `AddFileShareProviderMetadata()` and `AddFileShareProviderRuntime(...)` split-registration pattern and the development-time `/providers` API contract.
    - Verified with `run_build` and targeted regression runs for `StudioApiHost.Tests`, `UKHO.Search.Services.Ingestion.Tests`, `IngestionServiceHost.Tests`, `UKHO.Search.Ingestion.Tests`, `UKHO.Search.Ingestion.Providers.FileShare.Tests`, and `UKHO.Search.Infrastructure.Ingestion.Tests`: 310 passed, 0 failed.

---

## Summary / key considerations

- Start with the shared inward metadata model and prove it with the existing File Share provider before changing host composition.
- Keep provider identity code-owned in provider packages; configuration only enables a subset of already-known providers.
- Preserve onion boundaries by keeping the metadata contract inward, runtime wiring in provider/infrastructure layers, and startup composition in hosts.
- Treat invalid provider enablement as a startup contract violation and fail fast before queue/bootstrap side effects occur.
- Keep `StudioApiHost` and Theia explicitly development-time only by using metadata-only composition rather than host-to-host discovery.
- Full automated test coverage is part of each slice, not a final optional cleanup step; finish with targeted host/API runs and a broader regression sweep for all affected provider paths.
